using FortniteSpriteTracker.Services;
using FortniteSpriteTracker.Shared.Catalog;
using FortniteSpriteTracker.Shared.Collections;
using FortniteSpriteTracker.Shared.Profiles;
using Microsoft.AspNetCore.Components;

namespace FortniteSpriteTracker.Pages;

public partial class Home : IAsyncDisposable
{
    private const string BrowserProgressKey = "sprite-scout-progress";
    private static readonly string[] CollectionFilters = ["All", "Owned", "Missing", "Mastered"];
    private readonly HashSet<int> owned = [];
    private readonly HashSet<int> mastered = [];
    private readonly Dictionary<int, LocalProgress> localProgress = [];
    private readonly HashSet<int> serverKeys = [];
    private readonly Dictionary<int, LocalProgress> pendingAccountUpdates = [];
    private readonly SemaphoreSlim saveLock = new(1, 1);
    private CancellationTokenSource? saveDelay;

    [Inject] private BrowserStorage Storage { get; set; } = null!;
    [Inject] private BrowserPrintService Printer { get; set; } = null!;
    [Inject] private AccountClient Account { get; set; } = null!;
    [Inject] private AccountState AccountState { get; set; } = null!;
    [Inject] private CollectionClient Collection { get; set; } = null!;
    [Inject] private CatalogClient Catalog { get; set; } = null!;

    [PersistentState]
    public HomeCatalogState? InitialCatalogState { get; set; }

    private SpriteCatalogDto? catalog;
    private IReadOnlyList<SeasonDto> seasons = [];
    private SeasonDto? selectedSeason;
    private bool browserProgressLoaded;
    private bool catalogLoading = true;
    private bool catalogEmpty;
    private string? catalogError;
    private string catalogEmptyTitle = "No Sprite guide is available yet";
    private string catalogEmptyMessage = "Check back when a seasonal collection has been published.";
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
    private string? saveStatus;
    private UserProfileDto? profile => AccountState.Profile;

    private string CubeHeroUrl => catalog?.Families
        .SelectMany(item => item.Variants)
        .FirstOrDefault(item => item.Style.Name == "Cube" && item.ImagePath.Contains("zeropoint"))?.ImagePath ?? "";
    private int OwnedPercent => Percent(owned.Count);
    private int MasteredPercent => Percent(mastered.Count);
    private IEnumerable<VariantStyleDto> ActiveStyles => variant == "All"
        ? catalog?.VariantStyles ?? []
        : catalog?.VariantStyles.Where(item => item.Name == variant) ?? [];
    private IEnumerable<SpriteFamilyDto> VisibleSprites => catalog?.Families.Where(SpriteIsVisible) ?? [];

    protected override async Task OnInitializedAsync()
    {
        if (InitialCatalogState is not null)
        {
            seasons = InitialCatalogState.Seasons;
            selectedSeason = seasons.FirstOrDefault(item => item.Id == InitialCatalogState.SelectedSeasonId);
            catalog = InitialCatalogState.Catalog;
            RestoreCatalogDisplayState();
            return;
        }

        try
        {
            seasons = await Catalog.GetSeasonsAsync();
            selectedSeason = seasons.FirstOrDefault(item => item.IsActive)
                ?? seasons.FirstOrDefault();
            await LoadSelectedCatalogAsync();
            InitialCatalogState = new HomeCatalogState
            {
                Seasons = seasons.ToArray(),
                SelectedSeasonId = selectedSeason?.Id,
                Catalog = catalog
            };
        }
        catch (HttpRequestException)
        {
            catalogError = "The current Sprite guide is temporarily unavailable. Please try again later.";
            catalogLoading = false;
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        if (!browserProgressLoaded)
        {
            await LoadBrowserProgressAsync();
            browserProgressLoaded = true;
        }

        var accountLoaded = await LoadAccountAsync();
        if (!accountLoaded)
        {
            ApplyLocalProgress();
            StateHasChanged();
            return;
        }

        if (profile is not null)
        {
            await LoadAuthenticatedCollectionAsync();
        }
        else
        {
            ApplyLocalProgress();
        }

        StateHasChanged();
    }

    private void RestoreCatalogDisplayState()
    {
        catalogLoading = false;
        catalogEmpty = catalog is null;
        catalogError = null;
        if (catalogEmpty)
        {
            SetCatalogEmptyMessage();
        }
    }

    private async Task LoadSelectedCatalogAsync()
    {
        catalogLoading = true;
        catalog = null;
        catalogEmpty = false;
        catalogError = null;

        if (selectedSeason is null)
        {
            catalogEmpty = true;
            SetCatalogEmptyMessage();
            catalogLoading = false;
            return;
        }

        if (!selectedSeason.HasCatalog)
        {
            catalogEmpty = true;
            SetCatalogEmptyMessage();
            catalogLoading = false;
            return;
        }

        catalog = await Catalog.GetAsync(selectedSeason.Id);
        catalogLoading = false;
    }

    private void SetCatalogEmptyMessage()
    {
        if (selectedSeason is null)
        {
            catalogEmptyTitle = "No Sprite guide is available yet";
            catalogEmptyMessage = "Check back when a seasonal collection has been published.";
            return;
        }

        catalogEmptyTitle = selectedSeason.IsActive
            ? $"The {selectedSeason.Name} guide is coming soon"
            : $"No guide was published for {selectedSeason.Name}";
        catalogEmptyMessage = selectedSeason.IsActive
            ? "This season is live. Its Sprite collection will appear here once it is ready."
            : "Choose another season to explore its Sprite collection.";
    }

    private async Task SeasonChangedAsync(ChangeEventArgs args)
    {
        if (!int.TryParse(args.Value?.ToString(), out var seasonId))
        {
            return;
        }

        selectedSeason = seasons.FirstOrDefault(item => item.Id == seasonId);
        filter = "All";
        rarity = "All";
        variant = "All";
        query = "";
        try
        {
            await LoadSelectedCatalogAsync();
            if (catalog is not null && profile is not null)
            {
                await LoadAuthenticatedCollectionAsync();
            }
            else
            {
                ApplyLocalProgress();
            }
        }
        catch (HttpRequestException)
        {
            catalogError = "This Sprite guide is temporarily unavailable. Please try again later.";
            catalogLoading = false;
        }
    }

    private async Task LoadBrowserProgressAsync()
    {
        var savedProgress = await Storage.GetAsync(
            BrowserProgressKey,
            new Dictionary<int, LocalProgress>());

        foreach (var item in savedProgress)
        {
            if (FindVariant(item.Key) is not null)
            {
                localProgress[item.Key] = item.Value;
            }
        }
    }

    private int Percent(int count) => catalog?.TotalEntries > 0
        ? (int)Math.Round(count * 100d / catalog.TotalEntries)
        : 0;

    private bool SpriteIsVisible(SpriteFamilyDto sprite)
    {
        var matchesStatus = filter switch
        {
            "Owned" => sprite.Variants.Any(item => owned.Contains(item.Id)),
            "Missing" => sprite.Variants.Any(item => !owned.Contains(item.Id)),
            "Mastered" => sprite.Variants.Any(item => mastered.Contains(item.Id)),
            _ => true
        };
        var matchesRarity = rarity == "All" || sprite.Rarity == rarity;
        var matchesVariant = variant == "All" || sprite.Variants.Any(item => item.Style.Name == variant);
        var matchesQuery = $"{sprite.Name} {sprite.Ability}".Contains(query, StringComparison.OrdinalIgnoreCase);
        return matchesStatus && matchesRarity && matchesVariant && matchesQuery;
    }

    private async Task SaveAsync()
    {
        if (localProgress.Count == 0)
        {
            await Storage.RemoveAsync(BrowserProgressKey);
            return;
        }

        await Storage.SetAsync(BrowserProgressKey, localProgress);
    }

    private async Task<bool> LoadAccountAsync()
    {
        const int maximumAttempts = 3;
        for (var attempt = 1; attempt <= maximumAttempts; attempt++)
        {
            try
            {
                await AccountState.LoadAsync();
                syncError = null;
                return true;
            }
            catch (HttpRequestException) when (attempt < maximumAttempts)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(250 * attempt));
            }
            catch (HttpRequestException)
            {
                syncError = "We couldn't verify your account. Browser selections are shown temporarily.";
                return false;
            }
        }

        return false;
    }

    private async Task LoadAuthenticatedCollectionAsync()
    {
        try
        {
            var serverProgress = await Collection.GetAsync();
            var serverByKey = serverProgress
                .Where(item => FindVariant(item.SpriteVariantId) is not null)
                .ToDictionary(
                    item => item.SpriteVariantId,
                    item => new LocalProgress(
                        item.IsOwned,
                        item.IsMastered));
            serverKeys.Clear();
            serverKeys.UnionWith(serverByKey.Keys);
            foreach (var localItem in localProgress.ToArray())
            {
                if (serverByKey.TryGetValue(localItem.Key, out var serverItem) &&
                    (!localItem.Value.IsOwned || serverItem.IsOwned) &&
                    (!localItem.Value.IsMastered || serverItem.IsMastered))
                {
                    localProgress.Remove(localItem.Key);
                }
            }

            await SaveAsync();
            showImportPrompt = localProgress.Values.Any(item => item.IsOwned || item.IsMastered);
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

    private SpriteVariantDto? FindVariant(int id) => catalog?.Families
        .SelectMany(item => item.Variants)
        .FirstOrDefault(item => item.Id == id);

    private void ApplyLocalProgress()
    {
        owned.Clear();
        mastered.Clear();
        ApplyProgress(localProgress);
    }

    private void ApplyProgress(IEnumerable<KeyValuePair<int, LocalProgress>> progress)
    {
        foreach (var item in progress)
        {
            if (FindVariant(item.Key) is null)
            {
                continue;
            }

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
            foreach (var item in localProgress.Where(item => !serverKeys.Contains(item.Key) && (item.Value.IsOwned || item.Value.IsMastered)).ToArray())
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

    private async Task DiscardAnonymousProgressAsync()
    {
        localProgress.Clear();
        showImportPrompt = false;
        await SaveAsync();
    }

    private async Task<bool> SaveToAccountAsync(int id, LocalProgress progress)
    {
        if (profile is null || FindVariant(id) is null)
        {
            return profile is null;
        }

        try
        {
            await Collection.UpdateAsync(new UpdateSpriteProgressRequest
            {
                SpriteVariantId = id,
                IsOwned = progress.IsOwned,
                IsMastered = progress.IsMastered
            });
            return true;
        }
        catch (HttpRequestException)
        {
            return false;
        }
    }

    private async Task SaveProfileAsync(UpdateUserProfileRequest request) =>
        AccountState.SetProfile(await Account.UpdateProfileAsync(request));

    private async Task ToggleOwned(int id)
    {
        if (!owned.Remove(id))
        {
            owned.Add(id);
        }
        else
        {
            mastered.Remove(id);
        }

        await RecordChangeAsync(id);
    }

    private async Task ToggleMastered(int id)
    {
        if (!mastered.Remove(id))
        {
            mastered.Add(id);
            owned.Add(id);
        }

        await RecordChangeAsync(id);
    }

    private async Task RecordChangeAsync(int id)
    {
        var progress = new LocalProgress(
            owned.Contains(id),
            mastered.Contains(id));
        if (profile is null)
        {
            localProgress[id] = progress;
            await SaveAsync();
            showAnonymousWarning = true;
            return;
        }

        pendingAccountUpdates[id] = progress;
        saveStatus = "Saving…";
        saveDelay?.Cancel();
        saveDelay?.Dispose();
        saveDelay = new CancellationTokenSource();
        _ = FlushAccountUpdatesAfterDelayAsync(saveDelay.Token);
    }

    private async Task FlushAccountUpdatesAfterDelayAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
            await FlushAccountUpdatesAsync(CancellationToken.None);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
    }

    private async Task FlushAccountUpdatesAsync(CancellationToken cancellationToken)
    {
        await saveLock.WaitAsync(cancellationToken);
        try
        {
            if (profile is null || pendingAccountUpdates.Count == 0)
            {
                return;
            }

            var batch = pendingAccountUpdates.ToArray();
            pendingAccountUpdates.Clear();
            var requests = batch.Select(item => new UpdateSpriteProgressRequest
            {
                SpriteVariantId = item.Key,
                IsOwned = item.Value.IsOwned,
                IsMastered = item.Value.IsMastered
            }).ToArray();
            await Collection.UpdateBatchAsync(requests, cancellationToken);
            serverKeys.UnionWith(batch.Where(item => item.Value.IsOwned).Select(item => item.Key));
            serverKeys.ExceptWith(batch.Where(item => !item.Value.IsOwned).Select(item => item.Key));
            saveStatus = pendingAccountUpdates.Count == 0 ? "Saved" : "Saving…";
            syncError = null;
            await InvokeAsync(StateHasChanged);
            if (saveStatus == "Saved")
            {
                _ = HideSavedStatusAsync();
            }
        }
        catch (HttpRequestException)
        {
            pendingAccountUpdates.Clear();
            saveStatus = null;
            await LoadAuthenticatedCollectionAsync();
            syncError = "Those changes couldn't be saved, so your database collection was restored.";
            await InvokeAsync(StateHasChanged);
        }
        finally
        {
            saveLock.Release();
        }
    }

    private async Task HideSavedStatusAsync()
    {
        await Task.Delay(TimeSpan.FromSeconds(2));
        if (saveStatus == "Saved" && pendingAccountUpdates.Count == 0)
        {
            saveStatus = null;
            await InvokeAsync(StateHasChanged);
        }
    }

    private void ShowMissing() => filter = "Missing";
    private void ToggleVariant(VariantStyleDto item) => variant = variant == item.Name ? "All" : item.Name;
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
        bool IsMastered);

    public sealed class HomeCatalogState
    {
        public required SeasonDto[] Seasons { get; init; }
        public int? SelectedSeasonId { get; init; }
        public SpriteCatalogDto? Catalog { get; init; }
    }

    public ValueTask DisposeAsync()
    {
        saveDelay?.Cancel();
        saveDelay?.Dispose();
        saveLock.Dispose();
        return ValueTask.CompletedTask;
    }
}
