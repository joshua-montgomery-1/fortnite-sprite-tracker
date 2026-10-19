using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace SpriteScout.Auth;

public sealed class DcrClientRegistrationService(
    IOpenIddictApplicationManager applications,
    AuthOptions settings)
{
    private const int MaximumRedirectUris = 10;
    private const int MaximumRedirectUriLength = 2048;
    private const int MaximumClientNameLength = 100;

    public async Task<DcrRegistrationOutcome> RegisterAsync(
        DcrRegistrationRequest request, CancellationToken cancellationToken)
    {
        if (request.UnsupportedMetadata is { Count: > 0 })
            return Failure("invalid_client_metadata", "The request contains unsupported client metadata.");

        var redirectUris = request.RedirectUris;
        if (redirectUris is null || redirectUris.Length is < 1 or > MaximumRedirectUris ||
            redirectUris.Any(string.IsNullOrWhiteSpace) ||
            redirectUris.Distinct(StringComparer.Ordinal).Count() != redirectUris.Length)
            return Failure("invalid_redirect_uri", "Provide between one and ten unique redirect URIs.");

        if (request.ClientName?.Length > MaximumClientNameLength || request.ApplicationType?.Length > 20)
            return Failure("invalid_client_metadata", "Client metadata contains an oversized string value.");

        var applicationType = request.ApplicationType ?? "web";
        if (applicationType is not ("web" or "native"))
            return Failure("invalid_client_metadata", "application_type must be web or native.");

        foreach (var value in redirectUris)
        {
            if (value.Length > MaximumRedirectUriLength || value.Contains('*') ||
                !Uri.TryCreate(value, UriKind.Absolute, out var redirect) ||
                redirect.Fragment.Length != 0 || redirect.UserInfo.Length != 0 ||
                (redirect.Scheme != Uri.UriSchemeHttps &&
                 !(applicationType == "native" && redirect.Scheme == Uri.UriSchemeHttp && redirect.IsLoopback)))
                return Failure("invalid_redirect_uri", "Redirect URIs must be HTTPS, or native loopback HTTP, without fragments or user information.");
        }

        if (request.GrantTypes is { Length: > 0 } requestedGrants &&
            (requestedGrants.Any(string.IsNullOrWhiteSpace) || requestedGrants.Distinct(StringComparer.Ordinal).Count() != requestedGrants.Length))
            return Failure("invalid_client_metadata", "grant_types must contain unique, non-empty values.");
        if (request.ResponseTypes is { } responseTypes &&
            (responseTypes.Length != 1 || responseTypes[0] != "code"))
            return Failure("invalid_client_metadata", "Only the code response type is supported.");
        if (request.TokenEndpointAuthMethod is not (null or "none"))
            return Failure("invalid_client_metadata", "Only public clients using token_endpoint_auth_method=none are supported.");

        if (request.Scope?.Length > 500)
            return Failure("invalid_scope", "The requested scope value is too long.");
        var scopes = request.Scope?.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            ?? [AuthDefaults.AccountReadScope];
        var allowedScopes = new HashSet<string>([AuthDefaults.AccountReadScope, AuthDefaults.AccountWriteScope,
            AuthDefaults.CollectionReadScope, AuthDefaults.CollectionWriteScope, Scopes.OfflineAccess], StringComparer.Ordinal);
        if (scopes.Length == 0 || scopes.Length > allowedScopes.Count || scopes.Distinct(StringComparer.Ordinal).Count() != scopes.Length ||
            scopes.Any(scope => !allowedScopes.Contains(scope)) || !scopes.Contains(AuthDefaults.AccountReadScope, StringComparer.Ordinal))
            return Failure("invalid_scope", "Request only supported scopes and include account:read.");

        var grants = request.GrantTypes is not { Length: > 0 }
            ? ["authorization_code"]
            : request.GrantTypes;
        var refreshRequested = scopes.Contains(Scopes.OfflineAccess, StringComparer.Ordinal);
        if (grants.Any(grant => grant is not ("authorization_code" or "refresh_token")) ||
            !grants.Contains("authorization_code", StringComparer.Ordinal) ||
            grants.Contains("refresh_token", StringComparer.Ordinal) != refreshRequested)
            return Failure("invalid_client_metadata", "Use authorization_code; refresh_token is supported only with offline_access.");

        if (!IsSafeOptionalUri(request.ClientUri) || !IsSafeOptionalUri(request.LogoUri) ||
            !IsSafeOptionalUri(request.TermsOfServiceUri) || !IsSafeOptionalUri(request.PolicyUri))
            return Failure("invalid_client_metadata", "client_uri, logo_uri, tos_uri, and policy_uri must be HTTPS URLs.");
        if (request.ClientUri?.Length > 2048 || request.LogoUri?.Length > 2048 ||
            request.TermsOfServiceUri?.Length > 2048 || request.PolicyUri?.Length > 2048)
            return Failure("invalid_client_metadata", "Client metadata URLs are too long.");
        if (request.SoftwareId?.Length > 100 || request.SoftwareVersion?.Length > 100)
            return Failure("invalid_client_metadata", "software_id and software_version must be at most 100 characters.");
        if (request.Contacts is { Length: > 5 } || request.Contacts?.Any(contact => contact is null || contact.Length > 254) == true)
            return Failure("invalid_client_metadata", "contacts must contain at most five strings of 254 characters each.");

        var displayName = string.IsNullOrWhiteSpace(request.ClientName) ? "MCP client" : request.ClientName;
        var slug = Regex.Replace(displayName.Normalize(NormalizationForm.FormKD).ToLowerInvariant(), @"[^a-z0-9]+", "-").Trim('-');
        slug = slug.Length > 40 ? slug[..40].TrimEnd('-') : slug;
        if (string.IsNullOrEmpty(slug)) slug = "mcp-client";

        string clientId;
        object? existing;
        do
        {
            clientId = $"{slug}-{Convert.ToHexString(RandomNumberGenerator.GetBytes(8)).ToLowerInvariant()}";
            existing = await applications.FindByClientIdAsync(clientId, cancellationToken);
        } while (existing is not null);

        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = clientId,
            DisplayName = displayName,
            ApplicationType = applicationType,
            ClientType = ClientTypes.Public,
            ConsentType = ConsentTypes.Explicit,
            Permissions =
            {
                Permissions.Endpoints.Authorization, Permissions.Endpoints.Token,
                Permissions.GrantTypes.AuthorizationCode,
                Permissions.ResponseTypes.Code, Permissions.Prefixes.Resource + settings.Resource
            },
            Requirements = { Requirements.Features.ProofKeyForCodeExchange }
        };
        if (refreshRequested) descriptor.Permissions.Add(Permissions.GrantTypes.RefreshToken);
        foreach (var redirectUri in redirectUris) descriptor.RedirectUris.Add(new Uri(redirectUri, UriKind.Absolute));
        foreach (var scope in scopes) descriptor.Permissions.Add(Permissions.Prefixes.Scope + scope);
        await applications.CreateAsync(descriptor, cancellationToken);

        var metadata = new Dictionary<string, object?>
        {
            ["client_id"] = clientId,
            ["client_id_issued_at"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            ["client_name"] = displayName,
            ["redirect_uris"] = redirectUris,
            ["application_type"] = applicationType,
            ["grant_types"] = grants,
            ["response_types"] = new[] { "code" },
            ["token_endpoint_auth_method"] = "none",
            ["scope"] = string.Join(' ', scopes)
        };
        AddIfPresent(metadata, "client_uri", request.ClientUri);
        AddIfPresent(metadata, "logo_uri", request.LogoUri);
        AddIfPresent(metadata, "contacts", request.Contacts);
        AddIfPresent(metadata, "tos_uri", request.TermsOfServiceUri);
        AddIfPresent(metadata, "policy_uri", request.PolicyUri);
        AddIfPresent(metadata, "software_id", request.SoftwareId);
        AddIfPresent(metadata, "software_version", request.SoftwareVersion);

        return new DcrRegistrationOutcome(null, null, metadata);
    }

    private static bool IsSafeOptionalUri(string? value) =>
        value is null || Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
        uri.Scheme == Uri.UriSchemeHttps && uri.UserInfo.Length == 0;

    private static void AddIfPresent(Dictionary<string, object?> values, string key, object? value)
    {
        if (value is not null) values[key] = value;
    }

    private static DcrRegistrationOutcome Failure(string error, string description) =>
        DcrRegistrationOutcome.Failure(error, description);
}
