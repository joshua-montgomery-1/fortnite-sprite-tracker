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
    private readonly HashSet<string> serverKeys = [];

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
    private bool showAnonymousWarning;
    private bool showImportPrompt;
    private bool importingAnonymousProgress;
    private string? syncError;
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
            if (item.Value.AccountId is null)
            {
                localProgress[item.Key] = item.Value;
            }
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

        if (profile is not null)
        {
            showImportPrompt = localProgress.Values.Any(item => item.IsOwned || item.IsMastered);
            await LoadAuthenticatedCollectionAsync();
        }
        else
        {
            ApplyLocalProgress();
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
        var anonymousOwned = localProgress
            .Where(item => item.Value.IsOwned || item.Value.IsMastered)
            .Select(item => item.Key)
            .ToArray();
        var anonymousMastered = localProgress
            .Where(item => item.Value.IsMastered)
            .Select(item => item.Key)
            .ToArray();

        await Storage.SetAsync("sprite-scout-owned", anonymousOwned);
        await Storage.SetAsync("sprite-scout-mastered", anonymousMastered);
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

            serverKeys.Clear();
            serverKeys.UnionWith(serverByKey.Keys);
            owned.Clear();
            mastered.Clear();
            ApplyProgress(serverByKey);
            syncError = null;
        }
        catch (HttpRequestException)
        {
            owned.Clear();
            mastered.Clear();
            syncError = "We couldn't load your saved collection. Your database progress was not changed.";
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

        ApplyProgress(localProgress);
    }

    private void ApplyProgress(IEnumerable<KeyValuePair<string, LocalProgress>> progress)
    {
        foreach (var item in progress)
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

    private async Task ImportAnonymousProgressAsync()
    {
        if (profile is null || importingAnonymousProgress)
        {
            return;
        }

        importingAnonymousProgress = true;
        syncError = null;

        try
        {
            foreach (var item in localProgress.Where(item =>
                         !serverKeys.Contains(item.Key)
                         && (item.Value.IsOwned || item.Value.IsMastered)).ToArray())
            {
                if (!await SaveToAccountAsync(item.Key, item.Value))
                {
                    throw new HttpRequestException("Anonymous progress import failed.");
                }
            }

            localProgress.Clear();
            await SaveAsync();
            showImportPrompt = false;
            await LoadAuthenticatedCollectionAsync();
        }
        catch (HttpRequestException)
        {
            syncError = "We couldn't import the browser selections. They are still saved on this device.";
        }
        finally
        {
            importingAnonymousProgress = false;
        }
    }

    private void DismissImportPrompt() => showImportPrompt = false;

    private async Task<bool> SaveToAccountAsync(string key, LocalProgress progress)
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
                progress.IsOwned,
                progress.IsMastered));
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
        var wasOwned = owned.Contains(key);
        var wasMastered = mastered.Contains(key);

        if (!owned.Remove(key))
        {
            owned.Add(key);
        }
        else
        {
            mastered.Remove(key);
        }

        if (profile is null)
        {
            localProgress[key] = new LocalProgress(
                owned.Contains(key), mastered.Contains(key), DateTimeOffset.UtcNow, null, false);
            await SaveAsync();
            showAnonymousWarning = true;
            return;
        }

        var progress = new LocalProgress(
            owned.Contains(key), mastered.Contains(key), DateTimeOffset.UtcNow, profile.Id, true);
        if (!await SaveToAccountAsync(key, progress))
        {
            SetProgress(key, wasOwned, wasMastered);
            syncError = "That change couldn't be saved, so your database collection was restored.";
        }
    }

    private async Task ToggleMastered(string key)
    {
        var wasOwned = owned.Contains(key);
        var wasMastered = mastered.Contains(key);

        if (!mastered.Remove(key))
        {
            mastered.Add(key);
            owned.Add(key);
        }

        if (profile is null)
        {
            localProgress[key] = new LocalProgress(
                owned.Contains(key), mastered.Contains(key), DateTimeOffset.UtcNow, null, false);
            await SaveAsync();
            showAnonymousWarning = true;
            return;
        }

        var progress = new LocalProgress(
            owned.Contains(key), mastered.Contains(key), DateTimeOffset.UtcNow, profile.Id, true);
        if (!await SaveToAccountAsync(key, progress))
        {
            SetProgress(key, wasOwned, wasMastered);
            syncError = "That change couldn't be saved, so your database collection was restored.";
        }
    }

    private void SetProgress(string key, bool isOwned, bool isMastered)
    {
        owned.Remove(key);
        mastered.Remove(key);

        if (isOwned || isMastered)
        {
            owned.Add(key);
        }

        if (isMastered)
        {
            mastered.Add(key);
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
