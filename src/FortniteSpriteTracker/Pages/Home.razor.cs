using FortniteSpriteTracker.Models;
using FortniteSpriteTracker.Services;
using Microsoft.AspNetCore.Components;

namespace FortniteSpriteTracker.Pages;

public partial class Home
{
    private static readonly string[] CollectionFilters = ["All", "Owned", "Missing", "Mastered"];
    private readonly HashSet<string> owned = [];
    private readonly HashSet<string> mastered = [];

    [Inject] private BrowserStorage Storage { get; set; } = null!;
    [Inject] private BrowserPrintService Printer { get; set; } = null!;

    private string filter = "All";
    private string rarity = "All";
    private string variant = "All";
    private string query = "";
    private string viewMode = "Checklist";
    private bool printBlank;

    private string CubeHeroUrl => SpriteData.VariantImageUrl("zeropoint", SpriteVariant.Cube);
    private int OwnedPercent => (int)Math.Round(owned.Count * 100d / SpriteData.TotalEntries);
    private int MasteredPercent => (int)Math.Round(mastered.Count * 100d / SpriteData.TotalEntries);

    private IEnumerable<SpriteVariant> ActiveVariants =>
        variant == "All"
            ? SpriteData.AllVariants
            : [Enum.Parse<SpriteVariant>(variant)];

    private IEnumerable<SpriteDefinition> VisibleSprites => SpriteData.Sprites.Where(SpriteIsVisible);

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
    }

    private async Task ToggleMastered(string key)
    {
        if (!mastered.Remove(key))
        {
            mastered.Add(key);
            owned.Add(key);
        }

        await SaveAsync();
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
