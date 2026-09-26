using FortniteSpriteTracker.Services;
using FortniteSpriteTracker.Shared.Catalog;
using Microsoft.AspNetCore.Components;

namespace FortniteSpriteTracker.Pages;

public partial class SpriteDetail
{
    [Parameter] public string? FamilySlug { get; set; }
    [Parameter] public string? VariantSlug { get; set; }
    [Inject] private CatalogClient Catalog { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;

    private IReadOnlyList<SpriteSeasonAppearance> appearances = [];
    private SpriteSeasonAppearance? selectedAppearance;
    private SpriteCatalogDto? catalog => selectedAppearance?.Catalog;
    private SpriteVariantDetail? Detail => selectedAppearance?.Detail;
    private bool loading = true;
    private bool notFound;
    private string CurrentPath => "/" + Navigation.ToBaseRelativePath(Navigation.Uri);

    protected override async Task OnParametersSetAsync()
    {
        loading = true;
        notFound = false;

        try
        {
            var seasons = await Catalog.GetSeasonsAsync();
            var catalogs = await Task.WhenAll(seasons
                .Where(item => item.HasCatalog)
                .Select(item => Catalog.GetAsync(item.Id)));

            appearances = catalogs
                .Select(item => new
                {
                    Catalog = item,
                    Detail = SpriteCatalogRoutes.FindVariant(item, FamilySlug, VariantSlug)
                })
                .Where(item => item.Detail is not null)
                .Select(item => new SpriteSeasonAppearance(item.Catalog, item.Detail!))
                .OrderByDescending(item => item.Catalog.Season.IsActive)
                .ThenByDescending(item => item.Catalog.Season.Chapter)
                .ThenByDescending(item => item.Catalog.Season.Number)
                .ToArray();

            selectedAppearance = appearances.FirstOrDefault();
            notFound = selectedAppearance is null;
        }
        catch (HttpRequestException)
        {
            notFound = true;
        }
        finally
        {
            loading = false;
        }
    }

    private void SelectAppearance(int seasonId)
    {
        selectedAppearance = appearances.FirstOrDefault(item => item.Catalog.Season.Id == seasonId)
            ?? selectedAppearance;
    }

    private sealed record SpriteSeasonAppearance(SpriteCatalogDto Catalog, SpriteVariantDetail Detail);
}
