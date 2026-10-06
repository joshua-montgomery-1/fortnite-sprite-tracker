namespace SpriteScout.Auth;

public sealed class McpClientOptions
{
    public string ClientId { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string ApplicationType { get; set; } = "native";
    public List<string> RedirectUris { get; set; } = [];
}
