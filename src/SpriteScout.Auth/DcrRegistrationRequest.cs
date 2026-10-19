using System.Text.Json;
using System.Text.Json.Serialization;

namespace SpriteScout.Auth;

public sealed class DcrRegistrationRequest
{
    [JsonPropertyName("client_name")]
    public string? ClientName { get; init; }

    [JsonPropertyName("redirect_uris")]
    public string[]? RedirectUris { get; init; }

    [JsonPropertyName("application_type")]
    public string? ApplicationType { get; init; }

    [JsonPropertyName("grant_types")]
    public string[]? GrantTypes { get; init; }

    [JsonPropertyName("response_types")]
    public string[]? ResponseTypes { get; init; }

    [JsonPropertyName("token_endpoint_auth_method")]
    public string? TokenEndpointAuthMethod { get; init; }

    [JsonPropertyName("scope")]
    public string? Scope { get; init; }

    [JsonPropertyName("client_uri")]
    public string? ClientUri { get; init; }

    [JsonPropertyName("logo_uri")]
    public string? LogoUri { get; init; }

    [JsonPropertyName("contacts")]
    public string[]? Contacts { get; init; }

    [JsonPropertyName("tos_uri")]
    public string? TermsOfServiceUri { get; init; }

    [JsonPropertyName("policy_uri")]
    public string? PolicyUri { get; init; }

    [JsonPropertyName("software_id")]
    public string? SoftwareId { get; init; }

    [JsonPropertyName("software_version")]
    public string? SoftwareVersion { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? UnsupportedMetadata { get; init; }
}

public sealed record DcrRegistrationOutcome(
    string? Error,
    string? ErrorDescription,
    IReadOnlyDictionary<string, object?>? Metadata)
{
    public bool Succeeded => Metadata is not null;

    public static DcrRegistrationOutcome Failure(string error, string description) =>
        new(error, description, null);
}
