using FortniteSpriteTracker.Server.Data.Entities;
using FortniteSpriteTracker.Models;
using Microsoft.EntityFrameworkCore;
using CatalogSpriteVariant = FortniteSpriteTracker.Models.SpriteVariant;
using SpriteEntity = FortniteSpriteTracker.Server.Data.Entities.Sprite;
using SpriteVariantEntity = FortniteSpriteTracker.Server.Data.Entities.SpriteVariant;

namespace FortniteSpriteTracker.Server.Data;

public sealed class SpriteTrackerDbContext(DbContextOptions<SpriteTrackerDbContext> options) : DbContext(options)
{
    public DbSet<UserAccount> Users => Set<UserAccount>();
    public DbSet<Season> Seasons => Set<Season>();
    public DbSet<SpriteEntity> Sprites => Set<SpriteEntity>();
    public DbSet<SpriteVariantEntity> SpriteVariants => Set<SpriteVariantEntity>();
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

        var seasons = modelBuilder.Entity<Season>();
        seasons.HasIndex(season => new { season.Chapter, season.Number }).IsUnique();
        seasons.Property(season => season.Name).HasMaxLength(80);

        var sprites = modelBuilder.Entity<SpriteEntity>();
        sprites.HasIndex(sprite => sprite.Slug).IsUnique();
        sprites.HasIndex(sprite => new { sprite.SeasonId, sprite.Name }).IsUnique();
        sprites.Property(sprite => sprite.Name).HasMaxLength(80);
        sprites.Property(sprite => sprite.Slug).HasMaxLength(80);
        sprites.Property(sprite => sprite.Rarity).HasMaxLength(40);
        sprites.Property(sprite => sprite.Ability).HasMaxLength(500);

        var variants = modelBuilder.Entity<SpriteVariantEntity>();
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
        Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<SpriteEntity> sprites,
        Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<SpriteVariantEntity> variants)
    {
        seasons.HasData(new Season
        {
            Id = 1,
            Chapter = 7,
            Number = 3,
            Name = "Chapter 7 · Season 3"
        });

        var variantId = 1;
        for (var spriteIndex = 0; spriteIndex < SpriteData.Sprites.Length; spriteIndex++)
        {
            var definition = SpriteData.Sprites[spriteIndex];
            var spriteId = spriteIndex + 1;

            sprites.HasData(new SpriteEntity
            {
                Id = spriteId,
                SeasonId = 1,
                Name = definition.Name,
                Slug = definition.Slug,
                Rarity = definition.Rarity.ToString(),
                Ability = definition.Ability
            });

            foreach (CatalogSpriteVariant variant in definition.Variants)
            {
                variants.HasData(new SpriteVariantEntity
                {
                    Id = variantId++,
                    SpriteId = spriteId,
                    Name = variant.ToString(),
                    ImagePath = SpriteData.VariantImageUrl(definition.Slug, variant)
                });
            }
        }
    }
}
