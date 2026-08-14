using FortniteSpriteTracker.DataAccess.Entities;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Catalog = FortniteSpriteTracker.Models;

namespace FortniteSpriteTracker.DataAccess;

public sealed class SpriteTrackerDbContext(DbContextOptions<SpriteTrackerDbContext> options) :
    DbContext(options),
    IDataProtectionKeyContext
{
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();
    public DbSet<UserAccount> Users => Set<UserAccount>();
    public DbSet<Season> Seasons => Set<Season>();
    public DbSet<Sprite> Sprites => Set<Sprite>();
    public DbSet<SpriteVariant> SpriteVariants => Set<SpriteVariant>();
    public DbSet<SpriteProgress> SpriteProgress => Set<SpriteProgress>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var users = modelBuilder.Entity<UserAccount>();
        users.HasKey(user => user.Id);
        users.HasIndex(user => user.PublicId).IsUnique();
        users.HasIndex(user => user.GoogleSubject).IsUnique();
        users.HasIndex(user => user.NormalizedEpicDisplayName);
        users.Property(user => user.GoogleSubject).HasMaxLength(255);
        users.Property(user => user.DisplayName).HasMaxLength(80);
        users.Property(user => user.EpicDisplayName).HasMaxLength(16);
        users.Property(user => user.NormalizedEpicDisplayName).HasMaxLength(16);

        var seasons = modelBuilder.Entity<Season>();
        seasons.HasIndex(season => new { season.Chapter, season.Number }).IsUnique();
        seasons.Property(season => season.Name).HasMaxLength(80);

        var sprites = modelBuilder.Entity<Sprite>();
        sprites.HasIndex(sprite => sprite.Slug).IsUnique();
        sprites.HasIndex(sprite => new { sprite.SeasonId, sprite.Name }).IsUnique();
        sprites.Property(sprite => sprite.Name).HasMaxLength(80);
        sprites.Property(sprite => sprite.Slug).HasMaxLength(80);
        sprites.Property(sprite => sprite.Rarity).HasMaxLength(40);
        sprites.Property(sprite => sprite.Ability).HasMaxLength(500);

        var variants = modelBuilder.Entity<SpriteVariant>();
        variants.HasIndex(variant => new { variant.SpriteId, variant.Name }).IsUnique();
        variants.Property(variant => variant.Name).HasMaxLength(40);
        variants.Property(variant => variant.ImagePath).HasMaxLength(255);

        var progress = modelBuilder.Entity<SpriteProgress>();
        progress.HasKey(item => new { item.UserId, item.SpriteVariantId });
        progress.HasOne(item => item.User)
            .WithMany(user => user.SpriteProgress)
            .HasForeignKey(item => item.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        progress.HasOne(item => item.SpriteVariant)
            .WithMany(variant => variant.Progress)
            .HasForeignKey(item => item.SpriteVariantId)
            .OnDelete(DeleteBehavior.Cascade);

        SeedCatalog(seasons, sprites, variants);
    }

    private static void SeedCatalog(
        Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<Season> seasons,
        Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<Sprite> sprites,
        Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<SpriteVariant> variants)
    {
        seasons.HasData(new Season
        {
            Id = 1,
            Chapter = 7,
            Number = 3,
            Name = "Chapter 7 · Season 3"
        });

        var variantId = 1;
        for (var spriteIndex = 0; spriteIndex < Catalog.SpriteData.Sprites.Length; spriteIndex++)
        {
            var definition = Catalog.SpriteData.Sprites[spriteIndex];
            var spriteId = spriteIndex + 1;

            sprites.HasData(new Sprite
            {
                Id = spriteId,
                SeasonId = 1,
                Name = definition.Name,
                Slug = definition.Slug,
                Rarity = definition.Rarity.ToString(),
                Ability = definition.Ability
            });

            foreach (Catalog.SpriteVariant variant in definition.Variants)
            {
                variants.HasData(new SpriteVariant
                {
                    Id = variantId++,
                    SpriteId = spriteId,
                    Name = variant.ToString(),
                    ImagePath = Catalog.SpriteData.VariantImageUrl(definition.Slug, variant)
                });
            }
        }
    }
}
