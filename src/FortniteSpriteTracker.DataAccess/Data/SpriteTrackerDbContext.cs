using FortniteSpriteTracker.DataAccess.Entities;
using FortniteSpriteTracker.Shared.Profiles;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FortniteSpriteTracker.DataAccess;

public sealed class SpriteTrackerDbContext(DbContextOptions<SpriteTrackerDbContext> options) : DbContext(options), IDataProtectionKeyContext
{
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();
    public DbSet<UserAccount> Users => Set<UserAccount>();
    public DbSet<Season> Seasons => Set<Season>();
    public DbSet<SpriteFamily> SpriteFamilies => Set<SpriteFamily>();
    public DbSet<VariantStyle> VariantStyles => Set<VariantStyle>();
    public DbSet<SpriteVariant> SpriteVariants => Set<SpriteVariant>();
    public DbSet<SeasonSpriteFamily> SeasonSpriteFamilies => Set<SeasonSpriteFamily>();
    public DbSet<SeasonSpriteVariant> SeasonSpriteVariants => Set<SeasonSpriteVariant>();
    public DbSet<SpriteProgress> SpriteProgress => Set<SpriteProgress>();
    public DbSet<TrackedPlayer> TrackedPlayers => Set<TrackedPlayer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var users = modelBuilder.Entity<UserAccount>();
        users.HasKey(user => user.Id);
        users.Property(user => user.Id).ValueGeneratedOnAdd();
        users.HasIndex(user => user.PublicId).IsUnique();
        users.HasIndex(user => user.GoogleSubject).IsUnique();
        users.HasIndex(user => user.NormalizedEpicDisplayName);
        users.Property(user => user.GoogleSubject).HasMaxLength(255);
        users.Property(user => user.DisplayName).HasMaxLength(80);
        users.Property(user => user.EpicDisplayName).HasMaxLength(16);
        users.Property(user => user.NormalizedEpicDisplayName).HasMaxLength(16);
        users.Property(user => user.ThemePreference)
            .HasConversion(
                preference => preference.ToStorageValue(),
                value => ThemePreferenceExtensions.Parse(value))
            .HasMaxLength(20)
            .HasDefaultValue(ThemePreference.System);

        var seasons = modelBuilder.Entity<Season>();
        seasons.HasIndex(season => new { season.Chapter, season.Number }).IsUnique();
        seasons.Property(season => season.Name).HasMaxLength(80);

        var families = modelBuilder.Entity<SpriteFamily>();
        families.Property(family => family.Id).ValueGeneratedOnAdd();
        families.HasIndex(family => family.Slug).IsUnique();
        families.Property(family => family.Name).HasMaxLength(80);
        families.Property(family => family.Slug).HasMaxLength(80);

        var styles = modelBuilder.Entity<VariantStyle>();
        styles.Property(style => style.Id).ValueGeneratedOnAdd();
        styles.HasIndex(style => style.Name).IsUnique();
        styles.Property(style => style.Name).HasMaxLength(40);
        styles.Property(style => style.Color).HasMaxLength(20);
        styles.Property(style => style.Bonus).HasMaxLength(120);
        styles.Property(style => style.ImageSuffix).HasMaxLength(40);

        var variants = modelBuilder.Entity<SpriteVariant>();
        variants.Property(variant => variant.Id).ValueGeneratedOnAdd();
        variants.HasIndex(variant => new { variant.SpriteFamilyId, variant.VariantStyleId }).IsUnique();
        variants.Property(variant => variant.ImagePath).HasMaxLength(255);

        var seasonFamilies = modelBuilder.Entity<SeasonSpriteFamily>();
        seasonFamilies.HasKey(item => new { item.SeasonId, item.SpriteFamilyId });
        seasonFamilies.HasIndex(item => new { item.SeasonId, item.DisplayOrder });
        seasonFamilies.Property(item => item.Rarity).HasMaxLength(40);
        seasonFamilies.Property(item => item.RarityColor).HasMaxLength(20);
        seasonFamilies.Property(item => item.Ability).HasMaxLength(500);
        seasonFamilies.Property(item => item.PrimaryColor).HasMaxLength(20);
        seasonFamilies.Property(item => item.SecondaryColor).HasMaxLength(20);
        seasonFamilies.Property(item => item.ImagePath).HasMaxLength(300);

        var seasonVariants = modelBuilder.Entity<SeasonSpriteVariant>();
        seasonVariants.HasKey(item => new { item.SeasonId, item.SpriteVariantId });

        var progress = modelBuilder.Entity<SpriteProgress>();
        progress.HasKey(item => new { item.UserId, item.SpriteVariantId });
        progress.HasOne(item => item.User)
            .WithMany(user => user.SpriteProgress)
            .HasForeignKey(item => item.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        progress.HasOne(item => item.SpriteVariant)
            .WithMany(variant => variant.Progress)
            .HasForeignKey(item => item.SpriteVariantId)
            .OnDelete(DeleteBehavior.Restrict);

        var trackedPlayers = modelBuilder.Entity<TrackedPlayer>();
        trackedPlayers.HasKey(item => new { item.UserId, item.PlayerId });
        trackedPlayers.HasOne(item => item.User)
            .WithMany(user => user.TrackedPlayers)
            .HasForeignKey(item => item.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        trackedPlayers.HasOne(item => item.Player)
            .WithMany(user => user.TrackedBy)
            .HasForeignKey(item => item.PlayerId)
            .OnDelete(DeleteBehavior.Cascade);
        trackedPlayers.HasIndex(item => item.PlayerId);
    }
}
