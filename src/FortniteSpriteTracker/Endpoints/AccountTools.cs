using System.ComponentModel;
using System.Security.Claims;
using FortniteSpriteTracker.DataAccess;
using FortniteSpriteTracker.Server.Services;
using FortniteSpriteTracker.Shared.Profiles;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using OpenIddict.Abstractions;
using SpriteScout.Auth;

namespace FortniteSpriteTracker.Server.Endpoints;

[McpServerToolType]
public sealed class AccountTools(SpriteTrackerDbContext database, McpAccountService accounts)
{
    [McpServerTool(Name = "who_am_i", ReadOnly = true)]
    [Description("Read the connected Sprite Scout account's display name and public profile identifier.")]
    public async Task<AccountIdentity> WhoAmI(ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        var id = Guid.Parse(user.GetClaim(OpenIddictConstants.Claims.Subject)!);
        var profile = await database.Users.AsNoTracking()
            .Where(account => account.AccountId == id)
            .Select(account => new AccountIdentity(id, account.PublicId, account.DisplayName))
            .SingleOrDefaultAsync(cancellationToken);
        return profile ?? throw new McpException("Sign in to the Sprite Scout website first to create your profile.");
    }

    [McpServerTool(Name = "get_profile", ReadOnly = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Read your Sprite Scout profile, including display name, Epic Games display name and profile settings. Requires account:read.")]
    public async Task<UserProfileDto> GetProfile(ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        var userId = await accounts.GetUserIdAsync(user, AuthDefaults.AccountReadScope, cancellationToken);
        return await ReadProfileAsync(userId, cancellationToken);
    }

    [McpServerTool(Name = "update_profile", ReadOnly = false, Destructive = true, Idempotent = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("Edit your Sprite Scout display name and/or Epic Games display name. Omitted names stay unchanged. Requires account:write. Profile privacy and theme settings are preserved.")]
    public async Task<UserProfileDto> UpdateProfile(ClaimsPrincipal user, CancellationToken cancellationToken,
        [Description("New display name, 1–80 characters. Omit to leave unchanged.")] string? displayName = null,
        [Description("New Epic Games display name, 3–16 characters. Omit to leave unchanged.")] string? epicDisplayName = null,
        [Description("Set true to remove your Epic Games display name. Cannot be combined with epicDisplayName.")] bool clearEpicDisplayName = false)
    {
        var userId = await accounts.GetUserIdAsync(user, AuthDefaults.AccountWriteScope, cancellationToken);
        displayName = displayName?.Trim();
        epicDisplayName = epicDisplayName?.Trim();
        if (displayName is null && epicDisplayName is null && !clearEpicDisplayName)
            throw new McpException("Specify a name to update, or set clearEpicDisplayName to true.");
        if (displayName is not null && displayName.Length is < 1 or > 80)
            throw new McpException("Display name must be between 1 and 80 characters.");
        if (epicDisplayName is not null && epicDisplayName.Length is < 3 or > 16)
            throw new McpException("Epic Games display name must be between 3 and 16 characters. Use clearEpicDisplayName to remove it.");
        if (clearEpicDisplayName && epicDisplayName is not null)
            throw new McpException("Choose either an Epic Games display name or clearEpicDisplayName.");

        var profile = await database.Users.SingleAsync(item => item.Id == userId, cancellationToken);
        if (displayName is not null)
        {
            profile.DisplayName = displayName;
            database.Entry(profile).Property(item => item.DisplayName).IsModified = true;
        }
        if (epicDisplayName is not null || clearEpicDisplayName)
        {
            profile.EpicDisplayName = epicDisplayName;
            profile.NormalizedEpicDisplayName = CurrentUserService.NormalizeEpicDisplayName(epicDisplayName);
            database.Entry(profile).Property(item => item.EpicDisplayName).IsModified = true;
            database.Entry(profile).Property(item => item.NormalizedEpicDisplayName).IsModified = true;
        }
        profile.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await database.SaveChangesAsync(cancellationToken);
        return await ReadProfileAsync(userId, cancellationToken);
    }

    private Task<UserProfileDto> ReadProfileAsync(long userId, CancellationToken cancellationToken) =>
        database.Users.AsNoTracking().Where(item => item.Id == userId).Select(item => new UserProfileDto
        {
            PublicId = item.PublicId, DisplayName = item.DisplayName, EpicDisplayName = item.EpicDisplayName,
            IsCollectionPublic = item.IsCollectionPublic, ThemePreference = item.ThemePreference,
            IsProfileComplete = !string.IsNullOrWhiteSpace(item.EpicDisplayName)
        }).SingleAsync(cancellationToken);

    public sealed record AccountIdentity(Guid AccountId, Guid PublicId, string DisplayName);
}
