using FortniteSpriteTracker.Server.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace FortniteSpriteTracker.Server.Data;

public sealed class SpriteTrackerDbContext(DbContextOptions<SpriteTrackerDbContext> options) : DbContext(options)
{
    public DbSet<UserAccount> Users => Set<UserAccount>();
    public DbSet<SpriteProgress> SpriteProgress => Set<SpriteProgress>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var users = modelBuilder.Entity<UserAccount>();
        users.HasKey(user => user.Id);
        users.HasIndex(user => user.GoogleSubject).IsUnique();
        users.HasIndex(user => user.NormalizedEpicDisplayName);
        users.Property(user => user.GoogleSubject).HasMaxLength(255);
        users.Property(user => user.DisplayName).HasMaxLength(80);
        users.Property(user => user.EpicDisplayName).HasMaxLength(16);
        users.Property(user => user.NormalizedEpicDisplayName).HasMaxLength(16);

        var progress = modelBuilder.Entity<SpriteProgress>();
        progress.HasKey(item => new { item.UserId, item.SpriteSlug, item.Variant });
        progress.Property(item => item.SpriteSlug).HasMaxLength(80);
        progress.Property(item => item.Variant).HasMaxLength(40);
        progress.HasOne(item => item.User)
            .WithMany(user => user.SpriteProgress)
            .HasForeignKey(item => item.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
