using System.Globalization;
using FortniteSpriteTracker.Services;
using FortniteSpriteTracker.Components;
using FortniteSpriteTracker.Shared.Catalog;
using FortniteSpriteTracker.Shared.Collections;
using FortniteSpriteTracker.Shared.Profiles;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace FortniteSpriteTracker.Pages;

public partial class Home : IAsyncDisposable
{
    private const string BrowserProgressKey = "sprite-scout-progress";
    private static readonly string[] CollectionFilters = ["All", "Owned", "Missing", "Mastered", "Unmastered"];
    private static readonly string[] Rarities = ["All", "Rare", "Epic", "Legendary", "Mythic"];
    private static readonly SchemaGraph HomeStructuredData = new()
    {
        Nodes =
        [
            new WebApplicationSchema
            {
                Id = SchemaBuilder.Url("#webapp"),
                Name = "Sprite Scout",
                Url = SchemaBuilder.Url("/"),
                Description = "Interactive Fortnite Sprite companion tracker and field guide. Track sprite variants, view rare locations, and share collection progress with squadmates.",
                Offer = new OfferSchema
                {
                    Price = "0",
                    Currency = "USD"
                }
            },
            new WebSiteSchema
            {
                Id = SchemaBuilder.Url("#website"),
                Name = "Sprite Scout",
                Url = SchemaBuilder.Url("/")
            }
        ]
    };
    private readonly HashSet<int> owned = [];
    private readonly HashSet<int> mastered = [];
    private readonly Dictionary<int, LocalProgress> localProgress = [];
    private readonly HashSet<int> serverKeys = [];
    private readonly Dictionary<int, LocalProgress> pendingAccountUpdates = [];
    private readonly SemaphoreSlim saveLock = new(1, 1);
    private CancellationTokenSource? saveDelay;

    [Inject] private BrowserStorage Storage { get; set; } = null!;
    [Inject] private AccountClient Account { get; set; } = null!;
    [Inject] private AccountState AccountState { get; set; } = null!;
    [Inject] private CollectionClient Collection { get; set; } = null!;
    [Inject] private CatalogClient Catalog { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private IJSRuntime JS { get; set; } = null!;

    [SupplyParameterFromQuery(Name = "season")] private string? SeasonQuery { get; set; }
    [SupplyParameterFromQuery(Name = "status")] private string? StatusQuery { get; set; }
    [SupplyParameterFromQuery(Name = "rarity")] private string? RarityQuery { get; set; }
    [SupplyParameterFromQuery(Name = "variant")] private string? VariantQuery { get; set; }
    [SupplyParameterFromQuery(Name = "q")] private string? SearchQuery { get; set; }
    [SupplyParameterFromQuery(Name = "unreleased")] private string? UnreleasedQuery { get; set; }

    [PersistentState]
    public HomeCatalogState? InitialCatalogState { get; set; }

    private SpriteCatalogDto? catalog;
    private IReadOnlyList<SeasonDto> seasons = [];
    private SeasonDto? selectedSeason;
    private SeasonDto? DefaultSeason => seasons.FirstOrDefault(item => item.IsActive) ?? seasons.FirstOrDefault();
    private bool seasonsLoaded;
    private bool progressReady;
    private bool seasonResolved;
    private int seasonVersion;
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
    private bool showUnreleased;
    private bool showAnonymousWarning;
    private bool showImportPrompt;
    private bool importingAnonymousProgress;
    private string? syncError;
    private string? saveStatus;
    private UserProfileDto? profile => AccountState.Profile;
    private string CollectionHref => new Uri(Navigation.Uri).PathAndQuery + "#collection";

    private HeroSpriteViewModel? HeroSprite => HeroSpriteSelector.Select(catalog);
    private bool SelectedSeasonIsActive
    {
        get
        {
            var now = DateTimeOffset.UtcNow;
            return selectedSeason is not null &&
                selectedSeason.StartAt <= now &&
                (selectedSeason.EndAt is null || now < selectedSeason.EndAt);
        }
    }
    private bool SelectedSeasonIsUpcoming => selectedSeason is not null && DateTimeOffset.UtcNow < selectedSeason.StartAt;
    private string SelectedSeasonStatus
    {
        get
        {
            if (selectedSeason is null)
            {
                return "";
            }

            var now = DateTimeOffset.UtcNow;
            if (now < selectedSeason.StartAt)
            {
                return "UPCOMING";
            }

            return selectedSeason.EndAt is null || now < selectedSeason.EndAt
                ? "LIVE"
                : "PREVIOUS";
        }
    }
    private int ReleasedEntryCount => catalog?.Families
        .SelectMany(item => item.Variants)
        .Count(IsReleased) ?? 0;
    private int OwnedReleasedCount => catalog?.Families
        .SelectMany(item => item.Variants)
        .Count(item => IsReleased(item) && owned.Contains(item.Id)) ?? 0;
    private int MasteredReleasedCount => catalog?.Families
        .SelectMany(item => item.Variants)
        .Count(item => IsReleased(item) && mastered.Contains(item.Id)) ?? 0;
    private int OwnedPercent => Percent(OwnedReleasedCount);
    private int MasteredPercent => Percent(MasteredReleasedCount);
    private IEnumerable<VariantStyleDto> ActiveStyles => variant == "All"
        ? catalog?.VariantStyles ?? []
        : catalog?.VariantStyles.Where(item => item.Name == variant) ?? [];
    private IEnumerable<SpriteFamilyDto> VisibleSprites => catalog?.Families.Where(SpriteIsVisible) ?? [];

    protected override async Task OnInitializedAsync()
    {
        if (InitialCatalogState is not null)
        {
            seasons = InitialCatalogState.Seasons;
            seasonsLoaded = true;
            return;
        }

        try
        {
            seasons = await Catalog.GetSeasonsAsync();
            seasonsLoaded = true;
        }
        catch (HttpRequestException)
        {
            catalogError = "The current Sprite guide is temporarily unavailable. Please try again later.";
            catalogLoading = false;
        }
    }

    protected override Task OnParametersSetAsync()
    {
        filter = ValidateOption(StatusQuery, CollectionFilters);
        rarity = ValidateOption(RarityQuery, Rarities);
        query = SearchQuery ?? "";
        showUnreleased = bool.TryParse(UnreleasedQuery, out var unreleased) && unreleased;

        if (!seasonsLoaded)
        {
            return Task.CompletedTask;
        }

        var requestedSeason = int.TryParse(SeasonQuery, NumberStyles.None, CultureInfo.InvariantCulture, out var seasonId)
            ? seasons.FirstOrDefault(item => item.Id == seasonId)
            : null;
        var season = requestedSeason ?? DefaultSeason;
        if (seasonResolved && selectedSeason?.Id == season?.Id)
        {
            ApplyVariantFromUrl();
            return Task.CompletedTask;
        }

        return LoadSeasonAsync(season);
    }

    private async Task LoadSeasonAsync(SeasonDto? season)
    {
        var version = ++seasonVersion;
        var isFirstSelection = !seasonResolved;
        seasonResolved = true;
        selectedSeason = season;
        catalog = null;
        catalogError = null;
        catalogEmpty = false;
        catalogLoading = true;
        variant = "All";
        owned.Clear();
        mastered.Clear();
        serverKeys.Clear();
        showImportPrompt = false;

        try
        {
            if (isFirstSelection && InitialCatalogState is { } initial && initial.SelectedSeasonId == season?.Id)
            {
                catalog = initial.Catalog;
            }
            else if (season is { HasCatalog: true })
            {
                var loadedCatalog = await Catalog.GetAsync(season.Id);
                if (version != seasonVersion)
                {
                    return;
                }

                catalog = loadedCatalog;
            }

            catalogEmpty = catalog is null;
            if (catalogEmpty)
            {
                SetCatalogEmptyMessage();
            }

            catalogLoading = false;
            ApplyVariantFromUrl();
            InitialCatalogState ??= new HomeCatalogState
            {
                Seasons = seasons.ToArray(),
                SelectedSeasonId = season?.Id,
                Catalog = catalog
            };

            if (progressReady)
            {
                await RefreshProgressAsync();
            }
        }
        catch (HttpRequestException)
        {
            if (version == seasonVersion)
            {
                catalogError = isFirstSelection
                    ? "The current Sprite guide is temporarily unavailable. Please try again later."
                    : "This Sprite guide is temporarily unavailable. Please try again later.";
                catalogLoading = false;
            }
        }
    }

    private static string ValidateOption(string? value, IEnumerable<string> options) =>
        options.FirstOrDefault(item => string.Equals(item, value, StringComparison.OrdinalIgnoreCase)) ?? "All";

    private void ApplyVariantFromUrl() =>
        variant = ValidateOption(VariantQuery, catalog?.VariantStyles.Select(item => item.Name) ?? []);

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        await LoadBrowserProgressAsync();
        var accountLoaded = await LoadAccountAsync();
        progressReady = true;
        await RefreshProgressAsync(accountLoaded);
        StateHasChanged();
    }

    private async Task RefreshProgressAsync(bool accountLoaded = true)
    {
        if (catalog is null)
        {
            return;
        }

        if (accountLoaded && profile is not null)
        {
            await LoadAuthenticatedCollectionAsync();
        }
        else
        {
            ApplyLocalProgress();
        }

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

    private void SeasonChanged(ChangeEventArgs args)
    {
        if (!int.TryParse(args.Value?.ToString(), out var seasonId) ||
            seasonId == selectedSeason?.Id || !seasons.Any(item => item.Id == seasonId))
        {
            return;
        }

        NavigateWithFilters(seasonId, "All", "All", "All", "", showUnreleased);
    }

    private void UpdateFilterUrl(bool replace = false) =>
        NavigateWithFilters(selectedSeason?.Id, filter, rarity,
            catalogLoading ? VariantQuery : variant, query, showUnreleased, replace);

    private void NavigateWithFilters(int? seasonId, string status, string rarityValue, string? variantValue,
        string search, bool unreleased, bool replace = false)
    {
        // Blazor handles fragment-only anchor clicks without updating Navigation.Uri.
        var currentUri = JS is IJSInProcessRuntime browser
            ? browser.Invoke<string>("location.toString")
            : Navigation.Uri;
        var uri = Navigation.GetUriWithQueryParameters(currentUri, new Dictionary<string, object?>
        {
            ["season"] = seasonId == DefaultSeason?.Id ? null : seasonId,
            ["status"] = status == "All" ? null : status,
            ["rarity"] = rarityValue == "All" ? null : rarityValue,
            ["variant"] = variantValue == "All" ? null : variantValue,
            ["q"] = string.IsNullOrEmpty(search) ? null : search,
            ["unreleased"] = unreleased ? "true" : null
        });
        if (uri != currentUri)
        {
            Navigation.NavigateTo(uri, replace: replace);
        }
    }

    private async Task LoadBrowserProgressAsync()
    {
        var savedProgress = await Storage.GetAsync(
            BrowserProgressKey,
            new Dictionary<int, LocalProgress>());

        foreach (var item in savedProgress)
        {
            localProgress[item.Key] = item.Value;
        }
    }

    private int Percent(int count) => ReleasedEntryCount > 0
        ? (int)Math.Round(count * 100d / ReleasedEntryCount)
        : 0;

    private static bool IsReleased(SpriteVariantDto variant) =>
        variant.ReleasedAt is null || variant.ReleasedAt <= DateTimeOffset.UtcNow;

    private bool SpriteIsVisible(SpriteFamilyDto sprite)
    {
        bool IsIncluded(SpriteVariantDto item) => showUnreleased || IsReleased(item);

        // When a specific variant style (Type) is selected, require the status to apply to that same variant.
        var matchesStatus = filter switch
        {
            "Owned" => sprite.Variants.Any(item => IsIncluded(item) && owned.Contains(item.Id) && (variant == "All" || item.Style.Name == variant)),
            "Missing" => sprite.Variants.Any(item => IsIncluded(item) && !owned.Contains(item.Id) && (variant == "All" || item.Style.Name == variant)),
            "Mastered" => sprite.Variants.Any(item => IsIncluded(item) && mastered.Contains(item.Id) && (variant == "All" || item.Style.Name == variant)),
            "Unmastered" => sprite.Variants.Any(item => IsIncluded(item) && !mastered.Contains(item.Id) && (variant == "All" || item.Style.Name == variant)),
            _ => sprite.Variants.Any(item => IsIncluded(item) && (variant == "All" || item.Style.Name == variant))
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
        var version = seasonVersion;
        try
        {
            var serverProgress = await Collection.GetAsync();
            if (version != seasonVersion)
            {
                return;
            }

            var serverByKey = serverProgress
                .Where(item => FindVariant(item.SpriteVariantId) is not null)
                .ToDictionary(
                    item => item.SpriteVariantId,
                    item => new LocalProgress(
                        item.IsOwned,
                        item.IsMastered));
            serverKeys.Clear();
            serverKeys.UnionWith(serverByKey.Keys);
            var localProgressChanged = false;
            foreach (var localItem in localProgress.ToArray())
            {
                if (serverByKey.TryGetValue(localItem.Key, out var serverItem) &&
                    (!localItem.Value.IsOwned || serverItem.IsOwned) &&
                    (!localItem.Value.IsMastered || serverItem.IsMastered))
                {
                    localProgress.Remove(localItem.Key);
                    localProgressChanged = true;
                }
            }

            showImportPrompt = localProgress.Any(item =>
                FindVariant(item.Key) is not null && (item.Value.IsOwned || item.Value.IsMastered));
            owned.Clear();
            mastered.Clear();
            ApplyProgress(serverByKey);
            syncError = null;
            if (localProgressChanged)
            {
                await SaveAsync();
            }
        }
        catch (HttpRequestException)
        {
            if (version == seasonVersion)
            {
                owned.Clear();
                mastered.Clear();
                syncError = "We couldn't load your saved collection. Your database progress was not changed.";
            }
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
        var seasonEntries = localProgress.Where(item => FindVariant(item.Key) is not null).ToArray();
        try
        {
            foreach (var item in seasonEntries.Where(item => !serverKeys.Contains(item.Key) && (item.Value.IsOwned || item.Value.IsMastered)))
            {
                if (!await SaveToAccountAsync(item.Key, item.Value))
                {
                    throw new HttpRequestException("Anonymous progress import failed.");
                }
            }

            foreach (var item in seasonEntries)
            {
                localProgress.Remove(item.Key);
            }
            await SaveAsync();
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
        foreach (var id in localProgress.Keys.Where(id => FindVariant(id) is not null).ToArray())
        {
            localProgress.Remove(id);
        }
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
        if (SelectedSeasonIsUpcoming)
        {
            return;
        }

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
        if (SelectedSeasonIsUpcoming)
        {
            return;
        }

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

    private void SetStatus(string status)
    {
        filter = status;
        UpdateFilterUrl();
    }

    private void ShowMissing() => SetStatus("Missing");

    private void ToggleVariant(VariantStyleDto item)
    {
        variant = variant == item.Name ? "All" : item.Name;
        UpdateFilterUrl();
    }

    private void UpdateSearchUrl() => UpdateFilterUrl(replace: true);

    private void UpdateFilterUrlAfterBinding() => UpdateFilterUrl();
    private void ShowChecklist() => viewMode = "Checklist";
    private void ShowFieldGuide() => viewMode = "Field Guide";

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
        seasonVersion++;
        saveDelay?.Cancel();
        saveDelay?.Dispose();
        saveLock.Dispose();
        return ValueTask.CompletedTask;
    }
}
