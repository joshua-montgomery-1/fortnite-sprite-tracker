using System.Net.Http.Headers;
using System.Net.Http.Json;
using FortniteSpriteTracker.Shared.CheatCodes;

namespace FortniteSpriteTracker.Services;

public sealed class CheatCodeClient(HttpClient httpClient)
{
    public Task<CheatCodeCatalogDto?> GetCatalogAsync(CancellationToken cancellationToken = default) =>
        httpClient.GetFromJsonAsync<CheatCodeCatalogDto>("api/cheat-codes", cancellationToken);

    public async Task<IReadOnlyList<CheatCodeProgressDto>> GetProgressAsync(CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/me/cheat-codes/");
        request.Headers.CacheControl = new CacheControlHeaderValue { NoCache = true, NoStore = true };
        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CheatCodeProgressDto[]>(cancellationToken) ?? [];
    }

    public async Task UpdateAsync(UpdateCheatCodeProgressRequest request, CancellationToken cancellationToken = default)
    {
        var token = await httpClient.GetFromJsonAsync<AntiforgeryTokenResponse>("api/antiforgery/token", cancellationToken)
            ?? throw new InvalidOperationException("The antiforgery token response was empty.");
        using var message = new HttpRequestMessage(HttpMethod.Put, "api/me/cheat-codes/")
        {
            Content = JsonContent.Create(request)
        };
        message.Headers.Add("X-XSRF-TOKEN", token.Token);
        using var response = await httpClient.SendAsync(message, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private sealed record AntiforgeryTokenResponse(string Token);
}
