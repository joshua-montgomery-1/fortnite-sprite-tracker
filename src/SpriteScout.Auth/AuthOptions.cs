namespace SpriteScout.Auth;

public sealed class AuthOptions
{
    public string Issuer { get; set; } = "https://localhost:7082/identity/";
    public string Resource { get; set; } = "https://localhost:7082/mcp";
    public List<McpClientOptions> Clients { get; set; } = [];
    public AuthCertificateOptions Certificates { get; set; } = new();
}

public sealed class AuthCertificateOptions
{
    public string SigningBase64 { get; set; } = "";
    public string SigningPath { get; set; } = "";
    public string? SigningPassword { get; set; }
    public string EncryptionBase64 { get; set; } = "";
    public string EncryptionPath { get; set; } = "";
    public string? EncryptionPassword { get; set; }
}
