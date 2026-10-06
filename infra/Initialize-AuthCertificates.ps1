#Requires -Version 7.0
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string] $SubscriptionId,
    [Parameter(Mandatory)][ValidatePattern('^[a-z0-9]{3,24}$')][string] $StorageAccountName,
    [string] $ResourceGroupName = 'rg-sprite-scout-prod',
    [string] $Repository = 'joshua-montgomery-1/fortnite-sprite-tracker'
)

$ErrorActionPreference = 'Stop'
if (-not $IsWindows) { throw 'Run this script in PowerShell 7 on Windows; local password backups use Windows DPAPI.' }
foreach ($command in 'az', 'gh') {
    if (-not (Get-Command $command -ErrorAction SilentlyContinue)) { throw "Install $command and sign in before running this script." }
}

function Invoke-Azure {
    param([string[]] $Arguments)
    $result = & az @Arguments --subscription $SubscriptionId --only-show-errors
    if ($LASTEXITCODE -ne 0) { throw "Azure command failed: $($Arguments[0..1] -join ' ')" }
    return $result
}

function Set-GitHubSecret {
    param([string] $Name, [string] $Value)
    $Value | & gh secret set $Name --repo $Repository --env production
    if ($LASTEXITCODE -ne 0) { throw "Could not save GitHub production secret $Name." }
}

# Check both logins before provisioning resources. Never print provider credentials.
$null = Invoke-Azure -Arguments @('account', 'show', '-o', 'none')
& gh auth status 2>$null | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'Run gh auth login first.' }
$region = Invoke-Azure -Arguments @('group', 'show', '--name', $ResourceGroupName, '--query', 'location', '-o', 'tsv')

$directory = Join-Path $env:LOCALAPPDATA "SpriteScout/AuthCertificates/$SubscriptionId/$StorageAccountName"
$null = New-Item -ItemType Directory -Path $directory -Force
$currentIdentity = [System.Security.Principal.WindowsIdentity]::GetCurrent().User.Value
& icacls $directory /inheritance:r /grant:r "*${currentIdentity}:(OI)(CI)F" '*S-1-5-18:(OI)(CI)F' | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'Could not restrict access to the local certificate backup directory.' }

$accounts = (Invoke-Azure -Arguments @('storage', 'account', 'list', '--resource-group', $ResourceGroupName, '-o', 'json')) | ConvertFrom-Json
if (-not ($accounts | Where-Object name -EQ $StorageAccountName)) {
    $null = Invoke-Azure -Arguments @('storage', 'account', 'create', '--name', $StorageAccountName,
        '--resource-group', $ResourceGroupName, '--location', $region, '--sku', 'Standard_LRS',
        '--kind', 'StorageV2', '--https-only', 'true', '--min-tls-version', 'TLS1_2',
        '--allow-blob-public-access', 'false', '-o', 'none')
}
$storageKey = Invoke-Azure -Arguments @('storage', 'account', 'keys', 'list', '--account-name', $StorageAccountName,
    '--resource-group', $ResourceGroupName, '--query', '[0].value', '-o', 'tsv')
$previousStorageKey = $env:AZURE_STORAGE_KEY
$env:AZURE_STORAGE_KEY = $storageKey
try {
    $null = Invoke-Azure -Arguments @('storage', 'share', 'create', '--account-name', $StorageAccountName,
        '--name', 'auth-certificates', '--quota', '1', '-o', 'none')

    foreach ($role in 'signing', 'encryption') {
        $pfxPath = Join-Path $directory "$role.pfx"
        $passwordPath = Join-Path $directory "$role.password.xml"
        $remoteExists = (Invoke-Azure -Arguments @('storage', 'file', 'exists', '--account-name', $StorageAccountName,
            '--share-name', 'auth-certificates', '--path', "$role.pfx", '--query', 'exists', '-o', 'tsv')) -eq 'true'
        $hasPfx = Test-Path -LiteralPath $pfxPath
        $hasPassword = Test-Path -LiteralPath $passwordPath
        if ($hasPfx -ne $hasPassword -or ($remoteExists -and -not $hasPfx)) {
            throw "Existing $role credentials require their original local PFX/password backup. Refusing to replace them."
        }
        if (-not $hasPfx) {
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
            if ($remoteExists) {
                $remoteThumbprint = Invoke-Azure -Arguments @('storage', 'file', 'metadata', 'show', '--account-name', $StorageAccountName,
                    '--share-name', 'auth-certificates', '--path', "$role.pfx", '--query', 'thumbprint', '-o', 'tsv')
                if ($remoteThumbprint -ne $certificate.Thumbprint) { throw "Remote $role certificate differs from the local backup. Refusing to rotate it automatically." }
            } else {
                $null = Invoke-Azure -Arguments @('storage', 'file', 'upload', '--account-name', $StorageAccountName,
                    '--share-name', 'auth-certificates', '--source', $pfxPath, '--path', "$role.pfx",
                    '--metadata', "thumbprint=$($certificate.Thumbprint)", '-o', 'none')
            }
            Write-Host "$role certificate retained; expires $($certificate.NotAfter.ToString('yyyy-MM-dd'))."
        } finally { $certificate.Dispose() }
        Set-GitHubSecret -Name "AUTH_$($role.ToUpperInvariant())_PASSWORD" -Value $password
        $password = $null
    }
    Set-GitHubSecret -Name 'AUTH_STORAGE_KEY' -Value $storageKey
    & gh variable set AUTH_STORAGE_ACCOUNT --body $StorageAccountName --repo $Repository --env production
    if ($LASTEXITCODE -ne 0) { throw 'Could not save the GitHub production storage account variable.' }
} finally {
    $env:AZURE_STORAGE_KEY = $previousStorageKey
    $storageKey = $null
}

Write-Host "Certificate backups: $directory (password backups can only be decrypted by this Windows user)."
Write-Host 'Register https://spritescout.com/identity/signin-google in the Google OAuth client; retain /signin-google.'
Write-Host 'Configure AUTH_CLIENTS in the GitHub production environment, then deploy the reviewed release changes.'
