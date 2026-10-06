using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace SpriteScout.Auth;

internal sealed class AccountRequirement : IAuthorizationRequirement;

internal sealed class AccountAuthorizationHandler(AuthDbContext database)
    : AuthorizationHandler<AccountRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context, AccountRequirement requirement)
    {
        if (Guid.TryParse(context.User.GetClaim(Claims.Subject), out var id) &&
            await database.Accounts.AnyAsync(account => account.Id == id))
        {
            context.Succeed(requirement);
        }
    }
}
