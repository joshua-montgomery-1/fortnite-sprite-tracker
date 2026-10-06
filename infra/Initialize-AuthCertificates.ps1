#Requires -Version 7.0
[CmdletBinding()]
param(
    [ValidatePattern('^[\w.-]+/[\w.-]+$')]
    [string] $Repository = 'joshua-montgomery-1/fortnite-sprite-tracker'
)

$ErrorActionPreference = 'Stop'
if (-not $IsWindows) { throw 'Run in PowerShell 7 on Windows; local password backups use Windows DPAPI.' }
if (-not (Get-Command gh -ErrorAction SilentlyContinue)) { throw 'Install GitHub CLI and run gh auth login first.' }

function Set-GitHubSecret {
    param([string] $Name, [string] $Value)
    $Value | & gh secret set $Name --repo $Repository --env production
    if ($LASTEXITCODE -ne 0) { throw "Could not save GitHub production secret $Name." }
}

$secretNames = (& gh secret list --repo $Repository --env production --json name | ConvertFrom-Json).name
if ($LASTEXITCODE -ne 0) { throw 'Could not read production secret names. Check GitHub login and repository access.' }
$variables = & gh variable list --repo $Repository --env production --json name,value | ConvertFrom-Json
if ($LASTEXITCODE -ne 0) { throw 'Could not read production variables.' }
$directory = Join-Path $env:LOCALAPPDATA "SpriteScout/AuthCertificates/$Repository"
$null = New-Item -ItemType Directory -Path $directory -Force
$currentIdentity = [System.Security.Principal.WindowsIdentity]::GetCurrent().User.Value
& icacls $directory /inheritance:r /grant:r "*${currentIdentity}:(OI)(CI)F" '*S-1-5-18:(OI)(CI)F' | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'Could not restrict access to the local certificate backup directory.' }

# Refuse to generate replacement keys if GitHub already has credentials without a local backup.
foreach ($role in 'signing', 'encryption') {
    $hasPfx = Test-Path -LiteralPath (Join-Path $directory "$role.pfx")
    $hasPassword = Test-Path -LiteralPath (Join-Path $directory "$role.password.xml")
    $prefix = "AUTH_$($role.ToUpperInvariant())"
    if ($hasPfx -ne $hasPassword -or
        (-not $hasPfx -and ($secretNames -contains "${prefix}_PFX" -or $secretNames -contains "${prefix}_PASSWORD"))) {
        throw "Existing $role credentials require their original local PFX/password backup. Refusing to replace them."
    }
}

foreach ($role in 'signing', 'encryption') {
    $pfxPath = Join-Path $directory "$role.pfx"
    $passwordPath = Join-Path $directory "$role.password.xml"
    $prefix = "AUTH_$($role.ToUpperInvariant())"
    if (-not (Test-Path -LiteralPath $pfxPath)) {
        $password = [Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(48))
        $securePassword = ConvertTo-SecureString $password -AsPlainText -Force
        $rsa = [System.Security.Cryptography.RSA]::Create(3072)
        try {
            $request = [System.Security.Cryptography.X509Certificates.CertificateRequest]::new(
                "CN=Sprite Scout OAuth $role", $rsa, [System.Security.Cryptography.HashAlgorithmName]::SHA256,
                [System.Security.Cryptography.RSASignaturePadding]::Pkcs1)
            $usage = if ($role -eq 'signing') {
                [System.Security.Cryptography.X509Certificates.X509KeyUsageFlags]::DigitalSignature
            } else { [System.Security.Cryptography.X509Certificates.X509KeyUsageFlags]::KeyEncipherment }
            $request.CertificateExtensions.Add([System.Security.Cryptography.X509Certificates.X509KeyUsageExtension]::new($usage, $true))
            $certificate = $request.CreateSelfSigned([DateTimeOffset]::UtcNow.AddMinutes(-5), [DateTimeOffset]::UtcNow.AddYears(2))
            try {
                $securePassword | Export-Clixml -LiteralPath $passwordPath
                [IO.File]::WriteAllBytes($pfxPath, $certificate.Export(
                    [System.Security.Cryptography.X509Certificates.X509ContentType]::Pfx, $password))
            } finally { $certificate.Dispose() }
        } finally { $rsa.Dispose() }
    } else {
        $securePassword = Import-Clixml -LiteralPath $passwordPath
        $password = [System.Net.NetworkCredential]::new('', $securePassword).Password
    }

    $certificate = [System.Security.Cryptography.X509Certificates.X509Certificate2]::new(
        $pfxPath, $password, [System.Security.Cryptography.X509Certificates.X509KeyStorageFlags]::EphemeralKeySet)
    try {
        if (-not $certificate.HasPrivateKey -or $certificate.NotAfter.ToUniversalTime() -le [DateTime]::UtcNow) {
            throw "The $role certificate is expired or missing its private key. Renew it before deployment."
        }
        $remoteThumbprint = ($variables | Where-Object name -EQ "${prefix}_THUMBPRINT").value
        if ($remoteThumbprint -and $remoteThumbprint -ne $certificate.Thumbprint) {
            throw "The local $role certificate differs from GitHub's recorded thumbprint. Refusing to rotate it automatically."
        }
        $base64 = [Convert]::ToBase64String([IO.File]::ReadAllBytes($pfxPath))
        if ($base64.Length -gt 48 * 1024) { throw 'PFX exceeds the GitHub secret size limit.' }
        Set-GitHubSecret -Name "${prefix}_PFX" -Value $base64
        Set-GitHubSecret -Name "${prefix}_PASSWORD" -Value $password
        & gh variable set "${prefix}_THUMBPRINT" --body $certificate.Thumbprint --repo $Repository --env production
        if ($LASTEXITCODE -ne 0) { throw "Could not record the $role certificate thumbprint." }
        Write-Host "$role certificate saved; expires $($certificate.NotAfter.ToString('yyyy-MM-dd'))."
    } finally {
        $certificate.Dispose()
        $password = $null
        $base64 = $null
    }
}

Write-Host "Certificate backups: $directory (password backups can only be decrypted by this Windows user)."
Write-Host 'Register https://spritescout.com/identity/signin-google in Google; retain the website /signin-google callback.'
Write-Host 'Configure AUTH_CLIENTS in the GitHub production environment, then deploy the reviewed release changes.'
