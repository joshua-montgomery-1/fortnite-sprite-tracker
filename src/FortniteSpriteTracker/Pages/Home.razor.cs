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
    private readonly Dictionary<string, LocalProgress> localProgress = new(StringComparer.Ordinal);

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
        var savedProgress = await Storage.GetAsync(
            "sprite-scout-progress-v2",
            new Dictionary<string, LocalProgress>());

        foreach (var item in savedProgress)
        {
            localProgress[item.Key] = item.Value;
        }

        foreach (var key in savedOwned.Union(savedMastered))
        {
            localProgress.TryAdd(key, new LocalProgress(
                savedOwned.Contains(key) || savedMastered.Contains(key),
                savedMastered.Contains(key),
                DateTimeOffset.MinValue,
                null,
                false));
        }

        ApplyLocalProgress();

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
        await Storage.SetAsync("sprite-scout-progress-v2", localProgress);
    }

    private async Task LoadAuthenticatedCollectionAsync()
    {
        try
        {
            var serverProgress = await Collection.GetAsync();
            var serverByKey = serverProgress
                .Select(item => ToLocalProgress(item, profile!.Id))
                .Where(item => item is not null)
                .ToDictionary(item => item!.Value.Key, item => item!.Value.Progress);

            foreach (var item in localProgress.ToArray())
            {
                if (serverByKey.TryGetValue(item.Key, out var serverItem))
                {
                    if (item.Value.AccountId == profile!.Id
                        && item.Value.UpdatedAtUtc > serverItem.UpdatedAtUtc)
                    {
                        await SaveToAccountAsync(item.Key, item.Value);
                    }
                    else
                    {
                        localProgress[item.Key] = serverItem;
                    }
                }
                else if (item.Value.AccountId is null
                    && (item.Value.IsOwned || item.Value.IsMastered))
                {
                    await SaveToAccountAsync(item.Key, item.Value);
                }
                else if (item.Value.AccountId == profile!.Id && item.Value.IsPending)
                {
                    await SaveToAccountAsync(item.Key, item.Value);
                }
                else if (item.Value.AccountId != profile!.Id)
                {
                    localProgress.Remove(item.Key);
                }
                else
                {
                    localProgress.Remove(item.Key);
                }
            }

            foreach (var item in serverByKey)
            {
                localProgress.TryAdd(item.Key, item.Value);
            }

            ApplyLocalProgress();
            await SaveAsync();
        }
        catch (HttpRequestException)
        {
            // Browser storage remains available if account synchronization is interrupted.
        }
    }

    private static (string Key, LocalProgress Progress)? ToLocalProgress(
        SpriteProgressDto item,
        Guid accountId)
    {
        var sprite = SpriteData.Sprites.FirstOrDefault(candidate => candidate.Slug == item.SpriteSlug);
        if (sprite is null || !Enum.TryParse<SpriteVariant>(item.Variant, out var spriteVariant))
        {
            return null;
        }

        return (
            SpriteData.Key(sprite.Name, spriteVariant),
            new LocalProgress(item.IsOwned, item.IsMastered, item.UpdatedAtUtc, accountId, false));
    }

    private void ApplyLocalProgress()
    {
        owned.Clear();
        mastered.Clear();

        foreach (var item in localProgress)
        {
            if (item.Value.IsOwned || item.Value.IsMastered)
            {
                owned.Add(item.Key);
            }

            if (item.Value.IsMastered)
            {
                mastered.Add(item.Key);
            }
        }
    }

    private async Task<bool> SaveToAccountAsync(string key, LocalProgress progress)
    {
        if (profile is null || !TryGetSpriteVariant(key, out var sprite, out var spriteVariant))
        {
            return profile is null;
        }

        try
        {
            var saved = await Collection.UpdateAsync(new UpdateSpriteProgressRequest(
                sprite.Slug,
                spriteVariant.ToString(),
                progress.IsOwned,
                progress.IsMastered));
            localProgress[key] = new LocalProgress(
                saved.IsOwned,
                saved.IsMastered,
                saved.UpdatedAtUtc,
                profile.Id,
                false);
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

        localProgress[key] = new LocalProgress(
            owned.Contains(key),
            mastered.Contains(key),
            DateTimeOffset.UtcNow,
            profile?.Id,
            profile is not null);
        await SaveAsync();
        if (profile is not null)
        {
            await SaveToAccountAsync(key, localProgress[key]);
            await SaveAsync();
        }
    }

    private async Task ToggleMastered(string key)
    {
        if (!mastered.Remove(key))
        {
            mastered.Add(key);
            owned.Add(key);
        }

        localProgress[key] = new LocalProgress(
            owned.Contains(key),
            mastered.Contains(key),
            DateTimeOffset.UtcNow,
            profile?.Id,
            profile is not null);
        await SaveAsync();
        if (profile is not null)
        {
            await SaveToAccountAsync(key, localProgress[key]);
            await SaveAsync();
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

    private sealed record LocalProgress(
        bool IsOwned,
        bool IsMastered,
        DateTimeOffset UpdatedAtUtc,
        Guid? AccountId,
        bool IsPending);
}
