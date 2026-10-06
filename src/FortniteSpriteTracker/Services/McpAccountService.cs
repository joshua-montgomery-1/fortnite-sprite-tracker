using System.Security.Claims;
using FortniteSpriteTracker.DataAccess;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol;
using OpenIddict.Abstractions;

namespace FortniteSpriteTracker.Server.Services;

public sealed class McpAccountService(SpriteTrackerDbContext database)
{
    public async Task<long> GetUserIdAsync(ClaimsPrincipal principal, string scope, CancellationToken cancellationToken)
    {
        if (!principal.HasScope(scope))
            throw new McpException($"Reconnect to Sprite Scout and approve the '{scope}' permission to use this tool.");
        if (!Guid.TryParse(principal.GetClaim(OpenIddictConstants.Claims.Subject), out var accountId))
            throw new McpException("Sign in to Sprite Scout to use this tool.");
        return await database.Users.AsNoTracking().Where(user => user.AccountId == accountId)
            .Select(user => (long?)user.Id).SingleOrDefaultAsync(cancellationToken)
            ?? throw new McpException("Sign in to the Sprite Scout website first to create your profile.");
    }
}
