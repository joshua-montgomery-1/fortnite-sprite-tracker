using Microsoft.EntityFrameworkCore;

namespace SpriteScout.Auth;

public sealed class AuthDbContext(DbContextOptions<AuthDbContext> options) : DbContext(options)
{
    public DbSet<AuthAccount> Accounts => Set<AuthAccount>();
    public DbSet<ExternalIdentity> ExternalIdentities => Set<ExternalIdentity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("sprite_scout_auth");
        modelBuilder.UseOpenIddict();
        modelBuilder.Entity<AuthAccount>().HasKey(account => account.Id);
        var identities = modelBuilder.Entity<ExternalIdentity>();
        identities.HasKey(identity => identity.Id);
        identities.Property(identity => identity.Provider).HasMaxLength(80);
        identities.Property(identity => identity.Issuer).HasMaxLength(255);
        identities.Property(identity => identity.Subject).HasMaxLength(255);
        identities.HasIndex(identity => new { identity.Provider, identity.Issuer, identity.Subject }).IsUnique();
        identities.HasOne<AuthAccount>().WithMany().HasForeignKey(identity => identity.AccountId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
