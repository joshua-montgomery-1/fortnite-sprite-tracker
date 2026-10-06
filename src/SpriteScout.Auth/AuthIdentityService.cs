using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace SpriteScout.Auth;

public sealed class AuthIdentityService(AuthDbContext database)
{
    public async Task<Guid> GetOrCreateGoogleAsync(string subject, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);
        var existing = await FindAsync(subject, cancellationToken);
        if (existing is not null)
        {
            return existing.Value;
        }

        var account = new AuthAccount();
        database.Accounts.Add(account);
        database.ExternalIdentities.Add(new ExternalIdentity
        {
            AccountId = account.Id,
            Provider = "Google",
            Issuer = AuthDefaults.GoogleIssuer,
            Subject = subject
        });
        try
        {
            await database.SaveChangesAsync(cancellationToken);
            return account.Id;
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // Another sign-in may have created the same provider identity concurrently.
            database.ChangeTracker.Clear();
            var concurrent = await FindAsync(subject, cancellationToken);
            if (concurrent is null)
            {
                throw;
            }
            return concurrent.Value;
        }
    }

    private Task<Guid?> FindAsync(string subject, CancellationToken cancellationToken) =>
        database.ExternalIdentities
            .Where(identity => identity.Provider == "Google" &&
                identity.Issuer == AuthDefaults.GoogleIssuer && identity.Subject == subject)
            .Select(identity => (Guid?)identity.AccountId)
            .SingleOrDefaultAsync(cancellationToken);
}
