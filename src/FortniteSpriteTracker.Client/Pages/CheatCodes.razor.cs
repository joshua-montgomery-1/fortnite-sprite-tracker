using FortniteSpriteTracker.Services;
using FortniteSpriteTracker.Shared.CheatCodes;
using FortniteSpriteTracker.Shared.Profiles;
using Microsoft.AspNetCore.Components;

namespace FortniteSpriteTracker.Pages;

public partial class CheatCodes
{
    private const string BrowserProgressKey = "sprite-scout-cheat-code-progress";
    private static readonly string[] UsageFilters = ["All", "Unused", "Used"];
    private readonly HashSet<int> usedIds = [];
    private readonly HashSet<int> localUsedIds = [];
    private readonly HashSet<int> savingIds = [];

    [Inject] private CheatCodeClient CheatCodesApi { get; set; } = null!;
    [Inject] private BrowserStorage Storage { get; set; } = null!;
    [Inject] private AccountState AccountState { get; set; } = null!;

    private CheatCodeCatalogDto? catalog;
    private bool catalogLoading = true;
    private int? selectedCategoryId;
    private string usageFilter = "All";
    private UserProfileDto? profile => AccountState.Profile;
    private int UsedCount => usedIds.Count;
    private IEnumerable<CheatCodeCategoryDto> VisibleCategories => (catalog?.Categories ?? [])
        .Where(item => selectedCategoryId is null || item.Id == selectedCategoryId)
        .Where(item => item.Codes.Any(IsVisible));

    protected override async Task OnInitializedAsync()
    {
        try
        {
            catalog = await CheatCodesApi.GetCatalogAsync();
        }
        catch (HttpRequestException)
        {
            catalog = null;
        }
        finally
        {
            catalogLoading = false;
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender || catalog is null)
        {
            return;
        }

        localUsedIds.UnionWith(await Storage.GetAsync(BrowserProgressKey, new HashSet<int>()));
        FilterToTrackable(localUsedIds);
        try
        {
            await AccountState.LoadAsync();
            if (profile is not null)
            {
                var serverIds = (await CheatCodesApi.GetProgressAsync()).Select(item => item.CheatCodeId).ToHashSet();
                FilterToTrackable(serverIds);
                usedIds.UnionWith(serverIds);
                foreach (var id in localUsedIds.Except(serverIds).ToArray())
                {
                    await CheatCodesApi.UpdateAsync(new UpdateCheatCodeProgressRequest { CheatCodeId = id, IsUsed = true });
                    serverIds.Add(id);
                }

                localUsedIds.Clear();
                await Storage.RemoveAsync(BrowserProgressKey);
                usedIds.UnionWith(serverIds);
            }
            else
            {
                usedIds.UnionWith(localUsedIds);
            }
        }
        catch (HttpRequestException)
        {
            usedIds.UnionWith(localUsedIds);
        }

        StateHasChanged();
    }

    private bool IsVisible(CheatCodeDto code) => usageFilter switch
    {
        "Used" => code.IsTrackable && usedIds.Contains(code.Id),
        "Unused" => code.IsTrackable && !usedIds.Contains(code.Id),
        _ => true
    };

    private async Task ToggleUsedAsync(CheatCodeDto code)
    {
        if (!code.IsTrackable || !savingIds.Add(code.Id))
        {
            return;
        }

        var isUsed = !usedIds.Contains(code.Id);
        if (isUsed) usedIds.Add(code.Id); else usedIds.Remove(code.Id);
        try
        {
            // Keep a device copy for every user. It makes an in-flight account save survive a refresh
            // and is reconciled with the account the next time the tracker loads.
            if (isUsed) localUsedIds.Add(code.Id); else localUsedIds.Remove(code.Id);
            await Storage.SetAsync(BrowserProgressKey, localUsedIds);

            if (profile is not null)
            {
                await CheatCodesApi.UpdateAsync(new UpdateCheatCodeProgressRequest { CheatCodeId = code.Id, IsUsed = isUsed });
            }
        }
        catch (HttpRequestException)
        {
            // The local copy is retained and will be merged when a connection is available.
        }
        finally
        {
            savingIds.Remove(code.Id);
        }
    }

    private void FilterToTrackable(HashSet<int> ids)
    {
        var trackableIds = catalog!.Categories.SelectMany(item => item.Codes).Where(item => item.IsTrackable).Select(item => item.Id).ToHashSet();
        ids.RemoveWhere(id => !trackableIds.Contains(id));
    }
}
