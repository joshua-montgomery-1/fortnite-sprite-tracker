using System.Net.Http.Json;
using FortniteSpriteTracker.Shared.Collections;

namespace FortniteSpriteTracker.Services;

public sealed class CollectionClient(HttpClient httpClient)
{
    public async Task<IReadOnlyList<SpriteProgressDto>> GetAsync(
        CancellationToken cancellationToken = default)
    {
        return await httpClient.GetFromJsonAsync<SpriteProgressDto[]>(
            "api/me/collection/",
            cancellationToken) ?? [];
    }

    public async Task<SpriteProgressDto> UpdateAsync(
        UpdateSpriteProgressRequest request,
        CancellationToken cancellationToken = default)
    {
        var token = await httpClient.GetFromJsonAsync<AntiforgeryTokenResponse>(
            "api/antiforgery/token",
            cancellationToken) ?? throw new InvalidOperationException("The antiforgery token response was empty.");

        using var message = new HttpRequestMessage(HttpMethod.Put, "api/me/collection/")
        {
            Content = JsonContent.Create(request)
        };
        message.Headers.Add("X-XSRF-TOKEN", token.Token);

        using var response = await httpClient.SendAsync(message, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<SpriteProgressDto>(cancellationToken)
            ?? throw new InvalidOperationException("The collection response was empty.");
    }

    private sealed record AntiforgeryTokenResponse(string Token);
}
