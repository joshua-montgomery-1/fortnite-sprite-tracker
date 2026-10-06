using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SpriteScout.Auth;

public sealed class AuthDbContextFactory : IDesignTimeDbContextFactory<AuthDbContext>
{
    public AuthDbContext CreateDbContext(string[] args)
    {
        // Scaffolding migrations needs a provider, not a live database or production credentials.
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseNpgsql("Host=localhost;Database=sprite-scout-design;Username=design",
                postgres => postgres.MigrationsHistoryTable("__EFMigrationsHistory", "auth"))
            .UseOpenIddict()
            .Options;
        return new AuthDbContext(options);
    }
}
