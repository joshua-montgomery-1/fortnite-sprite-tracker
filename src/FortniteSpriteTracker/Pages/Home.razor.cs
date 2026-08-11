using FortniteSpriteTracker.Models;
using FortniteSpriteTracker.Services;
using FortniteSpriteTracker.Shared.Collections;
using FortniteSpriteTracker.Shared.Profiles;
using Microsoft.AspNetCore.Components;

namespace FortniteSpriteTracker.Pages;

public partial class Home
{
    private static readonly string[] CollectionFilters = ["All", "Owned", "Missing", "Mastered"];
    private readonly HashSet<string> owned = [];
    private readonly HashSet<string> mastered = [];

    [Inject] private BrowserStorage Storage { get; set; } = null!;
    [Inject] private BrowserPrintService Printer { get; set; } = null!;
    [Inject] private AccountClient Account { get; set; } = null!;
    [Inject] private CollectionClient Collection { get; set; } = null!;

    private string filter = "All";
    private string rarity = "All";
    private string variant = "All";
    private string query = "";
    private string viewMode = "Checklist";
    private bool printBlank;
    private UserProfileDto? profile;

    private string CubeHeroUrl => SpriteData.VariantImageUrl("zeropoint", SpriteVariant.Cube);
    private int OwnedPercent => (int)Math.Round(owned.Count * 100d / SpriteData.TotalEntries);
    private int MasteredPercent => (int)Math.Round(mastered.Count * 100d / SpriteData.TotalEntries);

    private IEnumerable<SpriteVariant> ActiveVariants =>
        variant == "All"
            ? SpriteData.AllVariants
            : [Enum.Parse<SpriteVariant>(variant)];

    private IEnumerable<SpriteDefinition> VisibleSprites => SpriteData.Sprites.Where(SpriteIsVisible);

    protected override async Task OnInitializedAsync()
    {
        try
        {
            profile = await Account.GetProfileAsync();
        }
        catch (HttpRequestException)
        {
            // The client remains usable when running without the account backend.
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        var savedOwned = await Storage.GetAsync("sprite-scout-owned", Array.Empty<string>());
        var savedMastered = await Storage.GetAsync("sprite-scout-mastered", Array.Empty<string>());

        owned.UnionWith(savedOwned);
        mastered.UnionWith(savedMastered);
        owned.UnionWith(mastered);

        if (profile is not null)
        {
            await LoadAuthenticatedCollectionAsync();
        }

        StateHasChanged();
    }

    private bool SpriteIsVisible(SpriteDefinition sprite)
    {
        var matchesStatus = filter switch
        {
            "Owned" => sprite.Variants.Any(item => owned.Contains(SpriteData.Key(sprite.Name, item))),
            "Missing" => sprite.Variants.Any(item => !owned.Contains(SpriteData.Key(sprite.Name, item))),
            "Mastered" => sprite.Variants.Any(item => mastered.Contains(SpriteData.Key(sprite.Name, item))),
            _ => true
        };

        var matchesRarity = rarity == "All" || sprite.Rarity.ToString() == rarity;
        var matchesVariant = variant == "All" || sprite.Variants.Contains(Enum.Parse<SpriteVariant>(variant));
        var matchesQuery = $"{sprite.Name} {sprite.Ability}".Contains(query, StringComparison.OrdinalIgnoreCase);

        return matchesStatus && matchesRarity && matchesVariant && matchesQuery;
    }

    private async Task SaveAsync()
    {
        await Storage.SetAsync("sprite-scout-owned", owned);
        await Storage.SetAsync("sprite-scout-mastered", mastered);
    }

    private async Task LoadAuthenticatedCollectionAsync()
    {
        try
        {
            var serverProgress = await Collection.GetAsync();
            var migrationKey = $"sprite-scout-migrated-{profile!.Id}";
            var pendingKey = $"sprite-scout-sync-pending-{profile.Id}";
            var hasMigrated = await Storage.GetAsync(migrationKey, false);
            var hasPendingChanges = await Storage.GetAsync(pendingKey, false);

            if (hasMigrated && !hasPendingChanges)
            {
                owned.Clear();
                mastered.Clear();
                ApplyServerProgress(serverProgress);
            }
            else
            {
                ApplyServerProgress(serverProgress);
                var synchronized = true;

                foreach (var key in owned.Union(mastered).ToArray())
                {
                    synchronized &= await SaveToAccountAsync(key);
                }

                await Storage.SetAsync(migrationKey, synchronized);
                await Storage.SetAsync(pendingKey, !synchronized);
            }

            await SaveAsync();
        }
        catch (HttpRequestException)
        {
            // Browser storage remains available if account synchronization is interrupted.
        }
    }

    private void ApplyServerProgress(IEnumerable<SpriteProgressDto> progress)
    {
        foreach (var item in progress)
        {
            var sprite = SpriteData.Sprites.FirstOrDefault(candidate => candidate.Slug == item.SpriteSlug);
            if (sprite is null || !Enum.TryParse<SpriteVariant>(item.Variant, out var spriteVariant))
            {
                continue;
            }

            var key = SpriteData.Key(sprite.Name, spriteVariant);
            if (item.IsOwned)
            {
                owned.Add(key);
            }

            if (item.IsMastered)
            {
                mastered.Add(key);
                owned.Add(key);
            }
        }
    }

    private async Task<bool> SaveToAccountAsync(string key)
    {
        if (profile is null || !TryGetSpriteVariant(key, out var sprite, out var spriteVariant))
        {
            return profile is null;
        }

        try
        {
            await Collection.UpdateAsync(new UpdateSpriteProgressRequest(
                sprite.Slug,
                spriteVariant.ToString(),
                owned.Contains(key),
                mastered.Contains(key)));
            return true;
        }
        catch (HttpRequestException)
        {
            // The local selection remains saved and can be synchronized on a later visit.
            return false;
        }
    }

    private static bool TryGetSpriteVariant(
        string key,
        out SpriteDefinition sprite,
        out SpriteVariant spriteVariant)
    {
        var separatorIndex = key.LastIndexOf("::", StringComparison.Ordinal);
        var spriteName = separatorIndex > 0 ? key[..separatorIndex] : string.Empty;
        var variantName = separatorIndex > 0 ? key[(separatorIndex + 2)..] : string.Empty;

        spriteVariant = default;
        sprite = SpriteData.Sprites.FirstOrDefault(candidate => candidate.Name == spriteName)!;
        return sprite is not null && Enum.TryParse(variantName, out spriteVariant);
    }

    private async Task SaveProfileAsync(UpdateUserProfileRequest request)
    {
        profile = await Account.UpdateProfileAsync(request);
    }

    private async Task ToggleOwned(string key)
    {
        if (!owned.Remove(key))
        {
            owned.Add(key);
        }
        else
        {
            mastered.Remove(key);
        }

        await SaveAsync();
        if (!await SaveToAccountAsync(key) && profile is not null)
        {
            await Storage.SetAsync($"sprite-scout-sync-pending-{profile.Id}", true);
        }
    }

    private async Task ToggleMastered(string key)
    {
        if (!mastered.Remove(key))
        {
            mastered.Add(key);
            owned.Add(key);
        }

        await SaveAsync();
        if (!await SaveToAccountAsync(key) && profile is not null)
        {
            await Storage.SetAsync($"sprite-scout-sync-pending-{profile.Id}", true);
        }
    }

    private void ShowMissing() => filter = "Missing";

    private void ToggleVariant(SpriteVariant item) =>
        variant = variant == item.ToString() ? "All" : item.ToString();

    private void ShowChecklist() => viewMode = "Checklist";

    private void ShowFieldGuide() => viewMode = "Field Guide";

    private async Task PrintChecklist(bool blank)
    {
        var previousState = (viewMode, filter, rarity, variant, query);

        printBlank = blank;
        viewMode = "Checklist";
        filter = "All";
        rarity = "All";
        variant = "All";
        query = "";
        StateHasChanged();

        await Task.Delay(350);
        await Printer.PrintAsync();

        printBlank = false;
        (viewMode, filter, rarity, variant, query) = previousState;
    }
}
