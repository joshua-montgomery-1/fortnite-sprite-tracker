using FortniteSpriteTracker.Shared.Catalog;

var catalog = BuildCatalog();

AssertEqual("/sprites/jonesy/loot", SpriteCatalogRoutes.VariantPath(catalog.Families[0], catalog.Families[0].Variants[1]), "variant path uses family slug and style token");

var detail = SpriteCatalogRoutes.FindVariant(catalog, "jonesy", "loot");
AssertNotNull(detail, "known sprite variant resolves");
AssertEqual("Jonesy", detail!.Family.Name, "resolved variant keeps the Jonesy family");
AssertEqual("Loot", detail.Variant.Style.Name, "resolved variant selects the requested style");
AssertEqual(3, detail.RelatedVariants.Count, "resolved variant exposes every sibling variant in the family");

var caseInsensitive = SpriteCatalogRoutes.FindVariant(catalog, "JONESY", "CHEAT-MASTER");
AssertNotNull(caseInsensitive, "slug matching is case-insensitive");
AssertEqual("Jonesy Cheat Master", caseInsensitive!.Variant.Style.Name, "case-insensitive slug resolves the exact variant");

AssertNull(SpriteCatalogRoutes.FindVariant(catalog, "jonesy", "missing"), "unknown variant slug is not resolved");
AssertNull(SpriteCatalogRoutes.FindVariant(catalog, "missing", "normal"), "unknown family slug is not resolved");

Console.WriteLine("Sprite catalog route tests passed.");

static SpriteCatalogDto BuildCatalog()
{
    var normal = new VariantStyleDto
    {
        Id = 1,
        Name = "Normal",
        Slug = "normal",
        Color = "#4db4ff",
        Bonus = "Base variant",
        DisplayOrder = 1
    };
    var loot = new VariantStyleDto
    {
        Id = 2,
        Name = "Loot",
        Slug = "loot",
        Color = "#ffd166",
        Bonus = "Loot variant",
        DisplayOrder = 2
    };
    var cheatMaster = new VariantStyleDto
    {
        Id = 3,
        Name = "Jonesy Cheat Master",
        Slug = "cheat-master",
        Color = "#9b5cff",
        Bonus = "Code variant",
        DisplayOrder = 3
    };

    var jonesy = new SpriteFamilyDto
    {
        Id = 10,
        Name = "Jonesy",
        Slug = "jonesy",
        Rarity = "Epic",
        RarityColor = "#b15cff",
        Ability = "Reveals nearby loot.",
        PrimaryColor = "#2b79ff",
        SecondaryColor = "#ffb03b",
        DisplayOrder = 1,
        ImageUrl = "images/sprites/jonesy_basic.webp",
        Variants =
        [
            new SpriteVariantDto { Id = 100, ImagePath = "images/sprites/jonesy_basic.webp", Style = normal },
            new SpriteVariantDto { Id = 101, ImagePath = "images/sprites/jonesy_loot.webp", Style = loot },
            new SpriteVariantDto { Id = 102, ImagePath = "images/sprites/jonesy_cheatmaster.webp", Style = cheatMaster }
        ]
    };

    return new SpriteCatalogDto
    {
        Season = new SeasonDto
        {
            Id = 1,
            Chapter = 7,
            Number = 4,
            Name = "Test Season",
            StartAt = DateTimeOffset.UtcNow.AddDays(-1),
            IsActive = true,
            HasCatalog = true,
            HasCheatCodes = false
        },
        VariantStyles = [normal, loot, cheatMaster],
        Families = [jonesy],
        TotalEntries = jonesy.Variants.Count
    };
}

static void AssertEqual<T>(T expected, T actual, string name)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"{name}: expected '{expected}', got '{actual}'.");
    }
}

static void AssertNotNull(object? value, string name)
{
    if (value is null)
    {
        throw new InvalidOperationException($"{name}: expected a value.");
    }
}

static void AssertNull(object? value, string name)
{
    if (value is not null)
    {
        throw new InvalidOperationException($"{name}: expected no value.");
    }
}
