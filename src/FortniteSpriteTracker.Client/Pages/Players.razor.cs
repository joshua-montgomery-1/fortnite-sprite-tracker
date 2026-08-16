using FortniteSpriteTracker.Services;
using FortniteSpriteTracker.Shared.Catalog;
using FortniteSpriteTracker.Shared.Collections;
using FortniteSpriteTracker.Shared.Players;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace FortniteSpriteTracker.Pages;

public partial class Players
{
    [Inject] private PlayerClient PlayerApi { get; set; } = null!;
    [Inject] private CatalogClient CatalogApi { get; set; } = null!;
    [Inject] private AccountState AccountState { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private IJSRuntime JavaScript { get; set; } = null!;

    [Parameter] public Guid? PublicId { get; set; }

    private PlayerCollectionDto? data;
    private SpriteCatalogDto? catalog;
    private string epicUsername = "";
    private string collectionQuery = "";
    private string mode = "collection";
    private string? searchMessage;
    private PlayerSummaryDto? searchResult;
    private string? catalogError;
    private bool searching;
    private bool loading;
    private bool copied;
    private bool changingTracking;
    private bool confirmingUntrack;

    private bool IsOwnProfile => data?.Viewer?.PublicId == data?.Player.PublicId;
    private string CanonicalUrl => new Uri(new Uri(Navigation.BaseUri), $"players/{data!.Player.PublicId:D}").AbsoluteUri;
    private string LoginUrl => $"auth/login?returnUrl={Uri.EscapeDataString(new Uri(Navigation.Uri).PathAndQuery)}";
    private HashSet<int> TargetKeys => Keys(data?.Collection ?? []);
    private HashSet<int> ViewerKeys => Keys(data?.ViewerCollection ?? []);
    private int TargetOnlyCount => TargetKeys.Except(ViewerKeys).Count();
    private int ViewerOnlyCount => ViewerKeys.Except(TargetKeys).Count();
    private int SharedCount => TargetKeys.Intersect(ViewerKeys).Count();

    private IEnumerable<ViewEntry> VisibleEntries
    {
        get
        {
            if (data is null)
            {
                return [];
            }

            var target = TargetKeys;
            var viewer = ViewerKeys;
            var keys = mode switch
            {
                "target" => target.Except(viewer),
                "viewer" => viewer.Except(target),
                "shared" => target.Intersect(viewer),
                _ => target
            };

            return keys.Select(ToEntry)
                .Where(entry => entry is not null && entry.Family.Name.Contains(collectionQuery, StringComparison.OrdinalIgnoreCase))
                .Select(entry => entry!)
                .OrderBy(entry => entry.Family.DisplayOrder)
                .ThenBy(entry => entry.Variant.Style.DisplayOrder);
        }
    }

    protected override async Task OnParametersSetAsync()
    {
        data = null;
        mode = "collection";
        catalogError = null;
        try
        {
            var seasons = await CatalogApi.GetSeasonsAsync();
            var catalogSeason = seasons.FirstOrDefault(item => item.IsActive && item.HasCatalog)
                ?? seasons.FirstOrDefault(item => item.HasCatalog);
            if (catalogSeason is null)
            {
                catalogError = "No seasonal Sprite guide is available yet.";
                return;
            }

            catalog ??= await CatalogApi.GetAsync(catalogSeason.Id);
            await AccountState.LoadAsync();
        }
        catch (HttpRequestException)
        {
            catalogError = "The current Sprite guide is temporarily unavailable. Please try again later.";
            return;
        }

        if (PublicId is null)
        {
            return;
        }

        loading = true;
        try
        {
            data = await PlayerApi.GetAsync(PublicId.Value);
            if (data?.Viewer is not null && !IsOwnProfile)
            {
                mode = "target";
            }
        }
        finally
        {
            loading = false;
        }
    }

    private async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(epicUsername))
        {
            return;
        }

        searching = true;
        searchMessage = null;
        searchResult = null;
        try
        {
            var result = await PlayerApi.FindByEpicUsernameAsync(epicUsername);
            if (result is null)
            {
                searchMessage = "No Sprite Scout account matched that exact Epic display name.";
            }
            else
            {
                searchResult = result;
            }
        }
        catch (HttpRequestException)
        {
            searchMessage = "Player search is temporarily unavailable.";
        }
        finally
        {
            searching = false;
        }
    }

    private Task BeginTrackingChangeAsync()
    {
        if (data?.IsTracked == true)
        {
            confirmingUntrack = true;
            return Task.CompletedTask;
        }

        return ToggleTrackingAsync();
    }

    private async Task ToggleTrackingAsync()
    {
        if (data is null)
        {
            return;
        }

        changingTracking = true;
        try
        {
            if (data.IsTracked)
            {
                await PlayerApi.UntrackAsync(data.Player.PublicId);
            }
            else
            {
                await PlayerApi.TrackAsync(data.Player.PublicId);
            }

            data = new PlayerCollectionDto
            {
                Player = data.Player,
                Collection = data.Collection,
                Viewer = data.Viewer,
                ViewerCollection = data.ViewerCollection,
                CanCompare = data.CanCompare,
                IsTracked = !data.IsTracked
            };
        }
        finally
        {
            changingTracking = false;
            confirmingUntrack = false;
        }
    }

    private static string PlayerName(PlayerSummaryDto player) => $"{player.DisplayName} ({player.EpicDisplayName})";

    private async Task CopyLinkAsync()
    {
        await JavaScript.InvokeVoidAsync("navigator.clipboard.writeText", CanonicalUrl);
        copied = true;
    }

    private static HashSet<int> Keys(IEnumerable<SpriteProgressDto> progress) =>
        progress.Where(item => item.IsOwned).Select(item => item.SpriteVariantId).ToHashSet();

    private ViewEntry? ToEntry(int key)
    {
        var family = catalog?.Families.FirstOrDefault(item => item.Variants.Any(candidate => candidate.Id == key));
        var variant = family?.Variants.FirstOrDefault(item => item.Id == key);
        var mastered = (mode == "viewer" ? data?.ViewerCollection : data?.Collection)
            ?.Any(item => item.IsMastered && item.SpriteVariantId == key) == true;
        return family is not null && variant is not null
            ? new ViewEntry(family, variant, mastered)
            : null;
    }

    private sealed record ViewEntry(SpriteFamilyDto Family, SpriteVariantDto Variant, bool IsMastered);
}
