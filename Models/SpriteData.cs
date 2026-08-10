namespace FortniteSpriteTracker.Models;

public sealed record SpriteDefinition(string Name, string Slug, string Rarity, string Ability, string[] Variants, string Colors);

public static class SpriteData
{
    public static readonly string[] AllVariants = ["Normal", "Gold", "Gummy", "Galaxy", "Holofoil", "Gem", "Cube", "Quack"];
    public static readonly Dictionary<string, string> VariantColors = new() { ["Normal"]="#a7a9ae", ["Gold"]="#f1bd38", ["Gummy"]="#ff6da9", ["Galaxy"]="#7858ed", ["Holofoil"]="#67dff1", ["Gem"]="#60dca5", ["Cube"]="#a955de", ["Quack"]="#ffd93f" };
    public static readonly Dictionary<string, string> VariantBonuses = new() { ["Normal"]="Core power", ["Gold"]="Bonus Sprite XP", ["Gummy"]="+20% Sprite Dust", ["Galaxy"]="+30% ammunition", ["Holofoil"]="+5% rare finds", ["Gem"]="-30% fall damage", ["Cube"]="Storm Overdrive", ["Quack"]="Shared progress" };
    public static readonly Dictionary<string, string> RarityColors = new() { ["Rare"]="#58a6ff", ["Epic"]="#c780ff", ["Legendary"]="#ffb23f", ["Mythic"]="#ff5d7c" };
    private static readonly string[] Core = ["Normal", "Gold", "Gummy", "Galaxy"];
    private static readonly string[] Holo = ["Normal", "Gold", "Gummy", "Galaxy", "Holofoil"];

    public static readonly SpriteDefinition[] Sprites = [
        S("Water","water","Rare","Replenishes shields for you and nearby squadmates while in water.",[..Core,"Holofoil","Quack","Gem"],"#7be7ff,#347cff"),
        S("Earth","earth","Rare","May reveal extra rare items when you open chests.",[..Core,"Cube","Quack","Gem"],"#dbf29a,#5d8f45"),
        S("Fire","fire","Rare","Releases a fiery burst after you deal enough damage.",[..Core,"Holofoil","Cube","Quack"],"#ffc35b,#ff5538"),
        S("Fishy","fishy","Rare","Increases swim speed and boosts movement while under fire.",[..Core,"Cube"],"#7cefd8,#167fba"),
        S("Air","air","Rare","Increases sprint speed and jump height while equipped.",Holo,"#d7f8ff,#7abfd2"),
        S("Duck","duck","Epic","Emoting and jamming replenish your shields.",[..Core,"Gem"],"#fff36a,#ef9a35"),
        S("Ghost","ghost","Epic","Briefly cloaks you whenever you reload.",Holo,"#e7f7ff,#8b79dc"),
        S("Demon","demon","Epic","Siphons health and shields after eliminations.",[..Core,"Gem"],"#ff7272,#7a1f74"),
        S("King","king","Epic","Makes your pickaxe deal significantly more damage.",Holo,"#f7df77,#7c3fa8"),
        S("Aura","drifter","Epic","Grants a Shock Rock charge after enough damage.",[..Core,"Gem"],"#faaf6b,#f36baa"),
        S("Striker","soccer","Epic","Traversal actions trigger speed, reload, and fire-rate Overdrive.",Holo,"#95efff,#4a62f4"),
        S("Dream","dream","Legendary","Grants random loot each level and Legendary loot at max level.",[..Core,"Cube"],"#bfa6ff,#5367e7"),
        S("Punk","punk","Legendary","Can grant a short infinite-ammo effect at max mastery.",[..Core,"Cube"],"#ff6fd0,#633b93"),
        S("Boss","boss","Legendary","Boosts your maximum Health and Shield while equipped.",[..Core,"Cube"],"#ffc45d,#c04b31"),
        S("Seven","seven","Legendary","Reveals enemy foot trails for you and nearby squadmates.",Holo,"#f15d5d,#5062b9"),
        S("Llama","llama","Legendary","Opening Ammo Boxes has a chance to upgrade your weapon.",[..Core,"Gem"],"#f19ee4,#6a66dd"),
        S("Peely","peely","Legendary","Marks nearby rare Sprites or enemies carrying them - and you.",Holo,"#ffea61,#d99c36"),
        S("Grim Reaper","grimreaper","Mythic","Marks players who damage you and reveals their location.",[..Core,"Cube","Holofoil","Gem"],"#c8ff70,#313b43"),
        S("Zero Point","zeropoint","Mythic","Creates a Shield Bubble Jr. whenever you heal.",[..Core,"Holofoil","Cube","Quack","Gem"],"#f49dff,#6647ff"),
        S("Burnt Peanut","theburntpeanut","Mythic","May award extra or Mythic loot after eliminations.",["Normal"],"#ffb45d,#743c32"),
        S("Batman","batman","Mythic","Deploy the Bat Cape midair and improve rare-Sprite chest finds.",[..Core,"Holofoil","Cube"],"#ffe16b,#202735"),
        S("Pollo","pollo","Mythic","Replenishes nearby shields after an elimination.",["Normal"],"#f8efe4,#d95445"),
        S("Vini Jr.","vinijr","Mythic","Empowers destructive slides and boosts combat after slide-kicks.",["Normal"],"#83e8ff,#2361b9"),
        S("John Wick","johnwick","Mythic","Knocks and eliminations briefly reveal nearby enemies.",["Normal"],"#989da7,#292d34"),
        S("Ironmouse","ironmouse","Mythic","Regenerates low health with Cloak and low gravity.",["Normal"],"#ff87c4,#8a43a8")
    ];

    public static int TotalEntries => Sprites.Sum(s => s.Variants.Length);
    public static string Key(string sprite, string variant) => $"{sprite}::{variant}";
    public static string ImageUrl(string slug) => $"https://fortnitespritetracker.org/images/sprites/{slug}_basic.webp";
    public static string VariantImageUrl(string slug, string variant)
    {
        var suffix = variant switch { "Normal" => "basic", "Gummy" => "candy", "Holofoil" when slug is "air" or "ghost" => "holo", _ => variant.ToLowerInvariant() };
        return $"https://fortnitespritetracker.org/images/sprites/{slug}_{suffix}.webp";
    }
    private static SpriteDefinition S(string n,string s,string r,string a,string[] v,string c) => new(n,s,r,a,v,c);
}
