using System.Security.Claims;
using FortniteSpriteTracker.DataAccess;
using FortniteSpriteTracker.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace FortniteSpriteTracker.Server.Services;

public sealed class CurrentUserService(SpriteTrackerDbContext database)
{
    private const int MaximumDatabaseAttempts = 3;

    public async Task<UserAccount> GetOrCreateAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var googleSubject = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("The authenticated Google account has no subject identifier.");

        var existingUser = await FindUserWithRetryAsync(googleSubject, cancellationToken);

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

    private async Task<UserAccount?> FindUserWithRetryAsync(
        string googleSubject,
        CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return await database.Users.SingleOrDefaultAsync(
                    user => user.GoogleSubject == googleSubject,
                    cancellationToken);
            }
            catch (TaskCanceledException) when (
                !cancellationToken.IsCancellationRequested && attempt < MaximumDatabaseAttempts)
            {
                // Npgsql can surface a transient connection/SSL handshake timeout as a
                // TaskCanceledException. Retry those without masking a canceled HTTP request.
                await Task.Delay(TimeSpan.FromMilliseconds(200 * attempt), cancellationToken);
            }
        }
    }

    public static string? NormalizeEpicDisplayName(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();
}
