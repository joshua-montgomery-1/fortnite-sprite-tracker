namespace SpriteScout.Auth;

public sealed class AuthOptions
{
    public bool Enabled { get; set; }
    public string Issuer { get; set; } = "https://localhost:7082/identity/";
    public string Resource { get; set; } = "https://localhost:7082/mcp";
    public List<McpClientOptions> Clients { get; set; } = [];
}
