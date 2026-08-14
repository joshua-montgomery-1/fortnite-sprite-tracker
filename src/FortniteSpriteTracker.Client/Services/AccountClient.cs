using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FortniteSpriteTracker.Shared.Profiles;

namespace FortniteSpriteTracker.Services;

public sealed class AccountClient(HttpClient httpClient)
{
    public async Task<UserProfileDto?> GetProfileAsync(CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/me/");
        request.Headers.CacheControl = new CacheControlHeaderValue
        {
            NoCache = true,
            NoStore = true
        };

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<UserProfileDto>(cancellationToken);
    }

    public async Task<UserProfileDto> UpdateProfileAsync(
        UpdateUserProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        var token = await httpClient.GetFromJsonAsync<AntiforgeryTokenResponse>(
            "api/antiforgery/token",
            cancellationToken) ?? throw new InvalidOperationException("The antiforgery token response was empty.");

        using var message = new HttpRequestMessage(HttpMethod.Put, "api/me/profile")
        {
            Content = JsonContent.Create(request)
        };
        message.Headers.Add("X-XSRF-TOKEN", token.Token);

        using var response = await httpClient.SendAsync(message, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<UserProfileDto>(cancellationToken)
            ?? throw new InvalidOperationException("The profile response was empty.");
    }

    public async Task LogoutAsync(CancellationToken cancellationToken = default)
    {
        var token = await GetAntiforgeryTokenAsync(cancellationToken);
        using var message = new HttpRequestMessage(HttpMethod.Post, "auth/logout");
        message.Headers.Add("X-XSRF-TOKEN", token);
        using var response = await httpClient.SendAsync(message, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteAccountAsync(CancellationToken cancellationToken = default)
    {
        var token = await GetAntiforgeryTokenAsync(cancellationToken);
        using var message = new HttpRequestMessage(HttpMethod.Delete, "api/me/")
        {
            Content = JsonContent.Create(new DeleteAccountRequest("DELETE"))
        };
        message.Headers.Add("X-XSRF-TOKEN", token);
        using var response = await httpClient.SendAsync(message, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private async Task<string> GetAntiforgeryTokenAsync(CancellationToken cancellationToken)
    {
        var token = await httpClient.GetFromJsonAsync<AntiforgeryTokenResponse>(
            "api/antiforgery/token",
            cancellationToken) ?? throw new InvalidOperationException("The antiforgery token response was empty.");
        return token.Token;
    }

    private sealed record AntiforgeryTokenResponse(string Token);
}
