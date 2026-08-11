using System.Net;
using System.Net.Http.Json;
using FortniteSpriteTracker.Shared.Profiles;

namespace FortniteSpriteTracker.Services;

public sealed class AccountClient(HttpClient httpClient)
{
    public async Task<UserProfileDto?> GetProfileAsync(CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync("api/me", cancellationToken);
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

    private sealed record AntiforgeryTokenResponse(string Token);
}
