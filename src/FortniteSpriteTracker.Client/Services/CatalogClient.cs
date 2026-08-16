using System.Net.Http.Json;
using FortniteSpriteTracker.Shared.Catalog;

namespace FortniteSpriteTracker.Services;

public sealed class CatalogClient(HttpClient httpClient)
{
    public async Task<IReadOnlyList<SeasonDto>> GetSeasonsAsync(CancellationToken cancellationToken = default) =>
        await httpClient.GetFromJsonAsync<SeasonDto[]>("api/seasons", cancellationToken) ?? [];

    public async Task<SpriteCatalogDto> GetAsync(int seasonId, CancellationToken cancellationToken = default) =>
        await httpClient.GetFromJsonAsync<SpriteCatalogDto>($"api/catalog/{seasonId}", cancellationToken)
        ?? throw new InvalidOperationException("The catalog response was empty.");
}
