using System.Net;
using System.Net.Http.Json;
using FortniteSpriteTracker.Shared.Players;

namespace FortniteSpriteTracker.Services;

public sealed class PlayerClient(HttpClient httpClient)
{
    private sealed record AntiforgeryTokenResponse(string Token);
    public async Task<PlayerSummaryDto?> FindByEpicUsernameAsync(string epicUsername, CancellationToken cancellationToken = default)
    {
        var url = $"api/players/search?epicUsername={Uri.EscapeDataString(epicUsername.Trim())}";
        using var response = await httpClient.GetAsync(url, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PlayerSummaryDto>(cancellationToken);
    }

    public async Task<PlayerCollectionDto?> GetAsync(Guid publicId, int? seasonId = null, CancellationToken cancellationToken = default)
    {
        var url = seasonId is null
            ? $"api/players/{publicId:D}"
            : $"api/players/{publicId:D}?seasonId={seasonId.Value}";
        using var response = await httpClient.GetAsync(url, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PlayerCollectionDto>(cancellationToken);
    }

    public async Task<IReadOnlyList<TrackedPlayerDto>?> GetTrackedAsync(int? seasonId = null, CancellationToken cancellationToken = default)
    {
        var url = seasonId is null
            ? "api/me/tracked-players/"
            : $"api/me/tracked-players/?seasonId={seasonId.Value}";
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Options.Set(SessionExpiredHandler.SuppressRedirect, true);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.Unauthorized) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TrackedPlayerDto[]>(cancellationToken) ?? [];
    }

    public Task TrackAsync(Guid publicId, CancellationToken cancellationToken = default) =>
        ChangeTrackingAsync(publicId, HttpMethod.Post, cancellationToken);

    public Task UntrackAsync(Guid publicId, CancellationToken cancellationToken = default) =>
        ChangeTrackingAsync(publicId, HttpMethod.Delete, cancellationToken);

    private async Task ChangeTrackingAsync(Guid publicId, HttpMethod method, CancellationToken cancellationToken)
    {
        var token = await httpClient.GetFromJsonAsync<AntiforgeryTokenResponse>("api/antiforgery/token", cancellationToken)
            ?? throw new InvalidOperationException("The antiforgery token response was empty.");
        using var request = new HttpRequestMessage(method, $"api/me/tracked-players/{publicId:D}");
        request.Headers.Add("X-XSRF-TOKEN", token.Token);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
