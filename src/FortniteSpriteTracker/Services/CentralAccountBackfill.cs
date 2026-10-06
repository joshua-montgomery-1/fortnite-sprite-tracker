using FortniteSpriteTracker.DataAccess;
using Microsoft.EntityFrameworkCore;
using SpriteScout.Auth;

namespace FortniteSpriteTracker.Server.Services;

public sealed class CentralAccountBackfill(SpriteTrackerDbContext website, AuthIdentityService identities)
{
    public async Task<int> RunAsync(CancellationToken cancellationToken = default)
    {
        var users = await website.Users.Where(user => user.CentralAccountId == null)
            .OrderBy(user => user.Id).ToListAsync(cancellationToken);
        foreach (var user in users)
        {
            // Persist identity first: retrying after interruption reuses the same provider mapping.
            user.CentralAccountId = await identities.GetOrCreateGoogleAsync(user.GoogleSubject, cancellationToken);
            await website.SaveChangesAsync(cancellationToken);
        }
        return users.Count;
    }
}

public sealed class CentralAccountBackfillInitializer(
    IServiceScopeFactory scopeFactory, ILogger<CentralAccountBackfillInitializer> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var linked = await scope.ServiceProvider.GetRequiredService<CentralAccountBackfill>().RunAsync(cancellationToken);
        logger.LogInformation("Linked {Count} existing website accounts to central auth.", linked);
    }
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
