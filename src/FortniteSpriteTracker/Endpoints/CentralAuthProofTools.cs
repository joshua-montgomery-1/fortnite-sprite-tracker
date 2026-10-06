using System.ComponentModel;
using System.Security.Claims;
using FortniteSpriteTracker.DataAccess;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using OpenIddict.Abstractions;

namespace FortniteSpriteTracker.Server.Endpoints;

[McpServerToolType]
public sealed class CentralAuthProofTools(SpriteTrackerDbContext database)
{
    [McpServerTool(Name = "who_am_i", ReadOnly = true)]
    [Description("Read the connected Sprite Scout account's display name and public profile identifier.")]
    public async Task<AccountIdentity> WhoAmI(ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        var id = Guid.Parse(user.GetClaim(OpenIddictConstants.Claims.Subject)!);
        var profile = await database.Users.AsNoTracking()
            .Where(account => account.CentralAccountId == id)
            .Select(account => new AccountIdentity(id, account.PublicId, account.DisplayName))
            .SingleOrDefaultAsync(cancellationToken);
        return profile ?? throw new McpException("Sign in to the Sprite Scout website first to create your profile.");
    }

    public sealed record AccountIdentity(Guid AccountId, Guid PublicId, string DisplayName);
}
