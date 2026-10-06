using System.Security.Claims;
using FortniteSpriteTracker.DataAccess;
using SpriteScout.Auth;

namespace FortniteSpriteTracker.Server.Services;

public sealed class CentralAccountProfileLinker(
    CurrentUserService currentUser, SpriteTrackerDbContext database) : ICentralAccountObserver
{
    public async Task GoogleSignedInAsync(
        Guid accountId, string googleSubject, string? displayName, CancellationToken cancellationToken)
    {
        var identity = new ClaimsIdentity("Google");
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, googleSubject));
        if (displayName is not null) identity.AddClaim(new Claim(ClaimTypes.Name, displayName));
        var user = await currentUser.GetOrCreateAsync(new ClaimsPrincipal(identity), cancellationToken);
        if (user.CentralAccountId is not null && user.CentralAccountId != accountId)
            throw new InvalidOperationException("The website account has a different central identity.");
        user.CentralAccountId = accountId;
        await database.SaveChangesAsync(cancellationToken);
    }
}
