namespace FortniteSpriteTracker.Models;

public enum SpriteRarity { Rare, Epic, Legendary, Mythic }
public enum SpriteVariant { Normal, Gold, Gummy, Galaxy, Holofoil, Gem, Cube, Quack }

public sealed record SpriteDefinition(
    string Name,
    string Slug,
    SpriteRarity Rarity,
    string Ability,
    IReadOnlyList<SpriteVariant> Variants,
    string PrimaryColor,
    string SecondaryColor);

public sealed record SpriteVariantMetadata(SpriteVariant Variant, string Color, string Bonus, string ImageSuffix);

public static class SpriteData
{
    public static readonly SpriteVariantMetadata[] VariantCatalog = [
        new(SpriteVariant.Normal,"#a7a9ae","Core power","basic"), new(SpriteVariant.Gold,"#f1bd38","Bonus Sprite XP","gold"),
        new(SpriteVariant.Gummy,"#ff6da9","+20% Sprite Dust","candy"), new(SpriteVariant.Galaxy,"#7858ed","+30% ammunition","galaxy"),
        new(SpriteVariant.Holofoil,"#67dff1","+5% rare finds","holofoil"), new(SpriteVariant.Gem,"#60dca5","-30% fall damage","gem"),
        new(SpriteVariant.Cube,"#a955de","Storm Overdrive","cube"), new(SpriteVariant.Quack,"#ffd93f","Shared progress","quack") ];
    public static readonly SpriteVariant[] AllVariants = VariantCatalog.Select(v => v.Variant).ToArray();
    public static readonly IReadOnlyDictionary<SpriteVariant, SpriteVariantMetadata> Variants = VariantCatalog.ToDictionary(v => v.Variant);
    public static readonly Dictionary<SpriteRarity, string> RarityColors = new() { [SpriteRarity.Rare]="#58a6ff", [SpriteRarity.Epic]="#c780ff", [SpriteRarity.Legendary]="#ffb23f", [SpriteRarity.Mythic]="#ff5d7c" };
    private static readonly string[] Core = ["Normal", "Gold", "Gummy", "Galaxy"];
    private static readonly string[] Holo = ["Normal", "Gold", "Gummy", "Galaxy", "Holofoil"];

    public static readonly SpriteDefinition[] Sprites = [
        CreateSprite("Water","water","Rare","Replenishes shields for you and nearby squadmates while in water.",[..Core,"Holofoil","Quack","Gem"],"#7be7ff,#347cff"),
        CreateSprite("Earth","earth","Rare","May reveal extra rare items when you open chests.",[..Core,"Cube","Quack","Gem"],"#dbf29a,#5d8f45"),
        CreateSprite("Fire","fire","Rare","Releases a fiery burst after you deal enough damage.",[..Core,"Holofoil","Cube","Quack"],"#ffc35b,#ff5538"),
        CreateSprite("Fishy","fishy","Rare","Increases swim speed and boosts movement while under fire.",[..Core,"Cube"],"#7cefd8,#167fba"),
        CreateSprite("Air","air","Rare","Increases sprint speed and jump height while equipped.",Holo,"#d7f8ff,#7abfd2"),
        CreateSprite("Duck","duck","Epic","Emoting and jamming replenish your shields.",[..Core,"Gem"],"#fff36a,#ef9a35"),
        CreateSprite("Ghost","ghost","Epic","Briefly cloaks you whenever you reload.",Holo,"#e7f7ff,#8b79dc"),
        CreateSprite("Demon","demon","Epic","Siphons health and shields after eliminations.",[..Core,"Gem"],"#ff7272,#7a1f74"),
        CreateSprite("King","king","Epic","Makes your pickaxe deal significantly more damage.",Holo,"#f7df77,#7c3fa8"),
        CreateSprite("Aura","drifter","Epic","Grants a Shock Rock charge after enough damage.",[..Core,"Gem"],"#faaf6b,#f36baa"),
        CreateSprite("Striker","soccer","Epic","Traversal actions trigger speed, reload, and fire-rate Overdrive.",Holo,"#95efff,#4a62f4"),
        CreateSprite("Dream","dream","Legendary","Grants random loot each level and Legendary loot at max level.",[..Core,"Cube"],"#bfa6ff,#5367e7"),
        CreateSprite("Punk","punk","Legendary","Can grant a short infinite-ammo effect at max mastery.",[..Core,"Cube"],"#ff6fd0,#633b93"),
        CreateSprite("Boss","boss","Legendary","Boosts your maximum Health and Shield while equipped.",[..Core,"Cube"],"#ffc45d,#c04b31"),
        CreateSprite("Seven","seven","Legendary","Reveals enemy foot trails for you and nearby squadmates.",Holo,"#f15d5d,#5062b9"),
        CreateSprite("Llama","llama","Legendary","Opening Ammo Boxes has a chance to upgrade your weapon.",[..Core,"Gem"],"#f19ee4,#6a66dd"),
        CreateSprite("Peely","peely","Legendary","Marks nearby rare Sprites or enemies carrying them - and you.",Holo,"#ffea61,#d99c36"),
        CreateSprite("Grim Reaper","grimreaper","Mythic","Marks players who damage you and reveals their location.",[..Core,"Cube","Holofoil","Gem"],"#c8ff70,#313b43"),
        CreateSprite("Zero Point","zeropoint","Mythic","Creates a Shield Bubble Jr. whenever you heal.",[..Core,"Holofoil","Cube","Quack","Gem"],"#f49dff,#6647ff"),
        CreateSprite("Burnt Peanut","theburntpeanut","Mythic","May award extra or Mythic loot after eliminations.",["Normal"],"#ffb45d,#743c32"),
        CreateSprite("Batman","batman","Mythic","Deploy the Bat Cape midair and improve rare-Sprite chest finds.",[..Core,"Holofoil","Cube"],"#ffe16b,#202735"),
        CreateSprite("Pollo","pollo","Mythic","Replenishes nearby shields after an elimination.",["Normal"],"#f8efe4,#d95445"),
        CreateSprite("Vini Jr.","vinijr","Mythic","Empowers destructive slides and boosts combat after slide-kicks.",["Normal"],"#83e8ff,#2361b9"),
        CreateSprite("John Wick","johnwick","Mythic","Knocks and eliminations briefly reveal nearby enemies.",["Normal"],"#989da7,#292d34"),
        CreateSprite("Ironmouse","ironmouse","Mythic","Regenerates low health with Cloak and low gravity.",["Normal"],"#ff87c4,#8a43a8")
    ];

    public static int TotalEntries => Sprites.Sum(s => s.Variants.Count);
    public static string Key(string sprite, SpriteVariant variant) => $"{sprite}::{variant}";
    public static string ImageUrl(string slug) => $"images/sprites/{slug}_basic.webp";
    public static string ExternalImageUrl(string slug) => $"https://fortnitespritetracker.org/images/sprites/{slug}_basic.webp";
    public static string VariantImageUrl(string slug, SpriteVariant variant) =>
        $"images/sprites/{slug}_{Variants[variant].ImageSuffix}.webp";
    public static string ExternalVariantImageUrl(string slug, SpriteVariant variant)
    {
        // The upstream source predates the Holofoil naming convention for these two assets.
        var sourceSuffix = variant == SpriteVariant.Holofoil && slug is "air" or "ghost"
            ? "holo"
            : Variants[variant].ImageSuffix;
        return $"https://fortnitespritetracker.org/images/sprites/{slug}_{sourceSuffix}.webp";
    }
    private static SpriteDefinition CreateSprite(string name, string slug, string rarity, string ability, string[] variants, string colors)
    {
        var palette = colors.Split(',', 2);
        return new(name, slug, Enum.Parse<SpriteRarity>(rarity), ability,
            variants.Select(Enum.Parse<SpriteVariant>).ToArray(), palette[0], palette[1]);
    }
}
