using System.Security.Claims;
using FortniteSpriteTracker.Server.Data;
using FortniteSpriteTracker.Server.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace FortniteSpriteTracker.Server.Services;

public sealed class CurrentUserService(SpriteTrackerDbContext database)
{
    public async Task<UserAccount> GetOrCreateAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var googleSubject = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("The authenticated Google account has no subject identifier.");

        var existingUser = await database.Users.SingleOrDefaultAsync(
            user => user.GoogleSubject == googleSubject,
            cancellationToken);

        if (existingUser is not null)
        {
            return existingUser;
        }

        var displayName = principal.FindFirstValue(ClaimTypes.Name)?.Trim();
        var user = new UserAccount
        {
            GoogleSubject = googleSubject,
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? "Sprite Scout" : displayName
        };

        database.Users.Add(user);
        await database.SaveChangesAsync(cancellationToken);
        return user;
    }

    public static string? NormalizeEpicDisplayName(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();
}
