namespace FortniteSpriteTracker.DataAccess.Seeding;

public static class CatalogSeedData
{
    public const int SeasonId = 1;

    public static IReadOnlyList<VariantStyleSeed> VariantStyles { get; } =
    [
        new VariantStyleSeed
        {
            Id = 1,
            Name = "Normal",
            Color = "#a7a9ae",
            Bonus = "Core power",
            ImageSuffix = "basic",
            DisplayOrder = 1
        },
        new VariantStyleSeed
        {
            Id = 2,
            Name = "Gold",
            Color = "#f1bd38",
            Bonus = "Bonus Sprite XP",
            ImageSuffix = "gold",
            DisplayOrder = 2
        },
        new VariantStyleSeed
        {
            Id = 3,
            Name = "Gummy",
            Color = "#ff6da9",
            Bonus = "+20% Sprite Dust",
            ImageSuffix = "gummy",
            DisplayOrder = 3
        },
        new VariantStyleSeed
        {
            Id = 4,
            Name = "Galaxy",
            Color = "#7858ed",
            Bonus = "+30% ammunition",
            ImageSuffix = "galaxy",
            DisplayOrder = 4
        },
        new VariantStyleSeed
        {
            Id = 5,
            Name = "Holofoil",
            Color = "#67dff1",
            Bonus = "+5% rare finds",
            ImageSuffix = "holofoil",
            DisplayOrder = 5
        },
        new VariantStyleSeed
        {
            Id = 6,
            Name = "Gem",
            Color = "#60dca5",
            Bonus = "-30% fall damage",
            ImageSuffix = "gem",
            DisplayOrder = 6
        },
        new VariantStyleSeed
        {
            Id = 7,
            Name = "Cube",
            Color = "#a955de",
            Bonus = "Storm Overdrive",
            ImageSuffix = "cube",
            DisplayOrder = 7
        },
        new VariantStyleSeed
        {
            Id = 8,
            Name = "Quack",
            Color = "#ffd93f",
            Bonus = "Shared progress",
            ImageSuffix = "quack",
            DisplayOrder = 8
        },
    ];

    public static IReadOnlyList<SpriteFamilySeed> Families { get; } =
    [
        new SpriteFamilySeed
        {
            Id = 1,
            Name = "Water",
            ImagePath = "images/sprites/water_basic.webp",
            Slug = "water",
            Rarity = "Rare",
            RarityColor = "#58a6ff",
            Ability = "Replenishes shields for you and nearby squadmates while in water.",
            PrimaryColor = "#7be7ff",
            SecondaryColor = "#347cff",
            DisplayOrder = 1,
            Variants =
            [
                new SpriteVariantSeed
                {
                    Id = 1,
                    VariantStyleId = 1,
                    ImagePath = "images/sprites/water_basic.webp"
                },
                new SpriteVariantSeed
                {
                    Id = 2,
                    VariantStyleId = 2,
                    ImagePath = "images/sprites/water_gold.webp"
                },
                new SpriteVariantSeed
                {
                    Id = 3,
                    VariantStyleId = 3,
                    ImagePath = "images/sprites/water_gummy.webp"
                },
                new SpriteVariantSeed
                {
                    Id = 4,
                    VariantStyleId = 4,
                    ImagePath = "images/sprites/water_galaxy.webp"
                },
                new SpriteVariantSeed
                {
                    Id = 5,
                    VariantStyleId = 5,
                    ImagePath = "images/sprites/water_holofoil.webp"
                },
                new SpriteVariantSeed
                {
                    Id = 6,
                    VariantStyleId = 8,
                    ImagePath = "images/sprites/water_quack.webp"
                },
                new SpriteVariantSeed
                {
                    Id = 7,
                    VariantStyleId = 6,
                    ImagePath = "images/sprites/water_gem.webp"
                },
            ]
        },
        new SpriteFamilySeed
        {
            Id = 2,
            Name = "Earth",
            ImagePath = "images/sprites/earth_basic.webp",
            Slug = "earth",
            Rarity = "Rare",
            RarityColor = "#58a6ff",
            Ability = "May reveal extra rare items when you open chests.",
            PrimaryColor = "#dbf29a",
            SecondaryColor = "#5d8f45",
            DisplayOrder = 2,
            Variants =
            [
                new SpriteVariantSeed
                {
                    Id = 8,
                    VariantStyleId = 1,
                    ImagePath = "images/sprites/earth_basic.webp"
                },
                new SpriteVariantSeed
                {
                    Id = 9,
                    VariantStyleId = 2,
                    ImagePath = "images/sprites/earth_gold.webp"
                },
                new SpriteVariantSeed
                {
                    Id = 10,
                    VariantStyleId = 3,
                    ImagePath = "images/sprites/earth_gummy.webp"
                },
                new SpriteVariantSeed
                {
                    Id = 11,
                    VariantStyleId = 4,
                    ImagePath = "images/sprites/earth_galaxy.webp"
                },
                new SpriteVariantSeed
                {
                    Id = 12,
                    VariantStyleId = 7,
                    ImagePath = "images/sprites/earth_cube.webp"
                },
                new SpriteVariantSeed
                {
                    Id = 13,
                    VariantStyleId = 8,
                    ImagePath = "images/sprites/earth_quack.webp"
                },
                new SpriteVariantSeed
                {
                    Id = 14,
                    VariantStyleId = 6,
                    ImagePath = "images/sprites/earth_gem.webp"
                },
            ]
        },
        new SpriteFamilySeed
        {
            Id = 3,
            Name = "Fire",
            ImagePath = "images/sprites/fire_basic.webp",
            Slug = "fire",
            Rarity = "Rare",
            RarityColor = "#58a6ff",
            Ability = "Releases a fiery burst after you deal enough damage.",
            PrimaryColor = "#ffc35b",
            SecondaryColor = "#ff5538",
            DisplayOrder = 3,
            Variants =
            [
                new SpriteVariantSeed
                {
                    Id = 15,
                    VariantStyleId = 1,
                    ImagePath = "images/sprites/fire_basic.webp"
                },
                new SpriteVariantSeed
                {
                    Id = 16,
                    VariantStyleId = 2,
                    ImagePath = "images/sprites/fire_gold.webp"
                },
                new SpriteVariantSeed
                {
                    Id = 17,
                    VariantStyleId = 3,
                    ImagePath = "images/sprites/fire_gummy.webp"
                },
                new SpriteVariantSeed
                {
                    Id = 18,
                    VariantStyleId = 4,
                    ImagePath = "images/sprites/fire_galaxy.webp"
                },
                new SpriteVariantSeed
                {
                    Id = 19,
                    VariantStyleId = 5,
                    ImagePath = "images/sprites/fire_holofoil.webp"
                },
                new SpriteVariantSeed
                {
                    Id = 20,
                    VariantStyleId = 7,
                    ImagePath = "images/sprites/fire_cube.webp"
                },
                new SpriteVariantSeed
                {
                    Id = 21,
                    VariantStyleId = 8,
                    ImagePath = "images/sprites/fire_quack.webp"
                },
            ]
        },
        new SpriteFamilySeed
        {
            Id = 4,
            Name = "Fishy",
            ImagePath = "images/sprites/fishy_basic.webp",
            Slug = "fishy",
            Rarity = "Rare",
            RarityColor = "#58a6ff",
            Ability = "Increases swim speed and boosts movement while under fire.",
            PrimaryColor = "#7cefd8",
            SecondaryColor = "#167fba",
            DisplayOrder = 4,
            Variants =
            [
                new SpriteVariantSeed
                {
                    Id = 22,
                    VariantStyleId = 1,
                    ImagePath = "images/sprites/fishy_basic.webp"
                },
                new SpriteVariantSeed
                {
                    Id = 23,
                    VariantStyleId = 2,
                    ImagePath = "images/sprites/fishy_gold.webp"
                },
                new SpriteVariantSeed
                {
                    Id = 24,
                    VariantStyleId = 3,
                    ImagePath = "images/sprites/fishy_gummy.webp"
                },
                new SpriteVariantSeed
                {
                    Id = 25,
                    VariantStyleId = 4,
                    ImagePath = "images/sprites/fishy_galaxy.webp"
                },
                new SpriteVariantSeed
                {
                    Id = 26,
                    VariantStyleId = 7,
                    ImagePath = "images/sprites/fishy_cube.webp"
                },
            ]
        },
        new SpriteFamilySeed
        {
            Id = 5,
            Name = "Air",
            ImagePath = "images/sprites/air_basic.webp",
            Slug = "air",
            Rarity = "Rare",
            RarityColor = "#58a6ff",
            Ability = "Increases sprint speed and jump height while equipped.",
            PrimaryColor = "#d7f8ff",
            SecondaryColor = "#7abfd2",
            DisplayOrder = 5,
            Variants =
            [
                new SpriteVariantSeed
                {
                    Id = 27,
                    VariantStyleId = 1,
                    ImagePath = "images/sprites/air_basic.webp"
                },
                new SpriteVariantSeed
                {
                    Id = 28,
                    VariantStyleId = 2,
                    ImagePath = "images/sprites/air_gold.webp"
                },
                new SpriteVariantSeed
                {
                    Id = 29,
                    VariantStyleId = 3,
                    ImagePath = "images/sprites/air_gummy.webp"
                },
                new SpriteVariantSeed
                {
                    Id = 30,
                    VariantStyleId = 4,
                    ImagePath = "images/sprites/air_galaxy.webp"
                },
                new SpriteVariantSeed
                {
                    Id = 31,
                    VariantStyleId = 5,
                    ImagePath = "images/sprites/air_holofoil.webp"
                },
            ]
        },
        new SpriteFamilySeed
        {
            Id = 6,
            Name = "Duck",
            ImagePath = "images/sprites/duck_basic.webp",
            Slug = "duck",
            Rarity = "Epic",
            RarityColor = "#c780ff",
            Ability = "Emoting and jamming replenish your shields.",
            PrimaryColor = "#fff36a",
            SecondaryColor = "#ef9a35",
            DisplayOrder = 6,
            Variants =
            [
                new SpriteVariantSeed
                {
                    Id = 32,
                    VariantStyleId = 1,
                    ImagePath = "images/sprites/duck_basic.webp"
                },
                new SpriteVariantSeed
                {
                    Id = 33,
                    VariantStyleId = 2,
                    ImagePath = "images/sprites/duck_gold.webp"
                },
                new SpriteVariantSeed
                {
                    Id = 34,
                    VariantStyleId = 3,
                    ImagePath = "images/sprites/duck_gummy.webp"
                },
                new SpriteVariantSeed
                {
                    Id = 35,
                    VariantStyleId = 4,
                    ImagePath = "images/sprites/duck_galaxy.webp"
                },
                new SpriteVariantSeed
                {
                    Id = 36,
                    VariantStyleId = 6,
                    ImagePath = "images/sprites/duck_gem.webp"
                },
            ]
        },
        new SpriteFamilySeed
        {
            Id = 7,
            Name = "Ghost",
            ImagePath = "images/sprites/ghost_basic.webp",
            Slug = "ghost",
            Rarity = "Epic",
            RarityColor = "#c780ff",
            Ability = "Briefly cloaks you whenever you reload.",
            PrimaryColor = "#e7f7ff",
            SecondaryColor = "#8b79dc",
            DisplayOrder = 7,
            Variants =
            [
                new SpriteVariantSeed
                {
                    Id = 37,
                    VariantStyleId = 1,
                    ImagePath = "images/sprites/ghost_basic.webp"
                },
                new SpriteVariantSeed
                {
                    Id = 38,
                    VariantStyleId = 2,
                    ImagePath = "images/sprites/ghost_gold.webp"
                },
                new SpriteVariantSeed
                {
                    Id = 39,
                    VariantStyleId = 3,
                    ImagePath = "images/sprites/ghost_gummy.webp"
                },
                new SpriteVariantSeed
                {
                    Id = 40,
                    VariantStyleId = 4,
                    ImagePath = "images/sprites/ghost_galaxy.webp"
                },
                new SpriteVariantSeed
                {
                    Id = 41,
                    VariantStyleId = 5,
                    ImagePath = "images/sprites/ghost_holofoil.webp"
                },
            ]
        },
        new SpriteFamilySeed
        {
            Id = 8,
            Name = "Demon",
            ImagePath = "images/sprites/demon_basic.webp",
            Slug = "demon",
            Rarity = "Epic",
            RarityColor = "#c780ff",
            Ability = "Siphons health and shields after eliminations.",
            PrimaryColor = "#ff7272",
            SecondaryColor = "#7a1f74",
            DisplayOrder = 8,
            Variants =
            [
                new SpriteVariantSeed
                {
                    Id = 42,
                    VariantStyleId = 1,
                    ImagePath = "images/sprites/demon_basic.webp"
                },
                new SpriteVariantSeed
                {
                    Id = 43,
                    VariantStyleId = 2,
                    ImagePath = "images/sprites/demon_gold.webp"
                },
                new SpriteVariantSeed
                {
                    Id = 44,
                    VariantStyleId = 3,
                    ImagePath = "images/sprites/demon_gummy.webp"
                },
                new SpriteVariantSeed
                {
                    Id = 45,
                    VariantStyleId = 4,
                    ImagePath = "images/sprites/demon_galaxy.webp"
                },
                new SpriteVariantSeed
                {
                    Id = 46,
                    VariantStyleId = 6,
                    ImagePath = "images/sprites/demon_gem.webp"
                },
            ]
        },
        new SpriteFamilySeed
        {
            Id = 9,
            Name = "King",
            ImagePath = "images/sprites/king_basic.webp",
            Slug = "king",
            Rarity = "Epic",
            RarityColor = "#c780ff",
            Ability = "Makes your pickaxe deal significantly more damage.",
            PrimaryColor = "#f7df77",
            SecondaryColor = "#7c3fa8",
            DisplayOrder = 9,
            Variants =
            [
                new SpriteVariantSeed
                {
                    Id = 47,
                    VariantStyleId = 1,
                    ImagePath = "images/sprites/king_basic.webp"
                },
                new SpriteVariantSeed
                {
                    Id = 48,
                    VariantStyleId = 2,
                    ImagePath = "images/sprites/king_gold.webp"
                },
                new SpriteVariantSeed
                {
                    Id = 49,
                    VariantStyleId = 3,
                    ImagePath = "images/sprites/king_gummy.webp"
                },
                new SpriteVariantSeed
                {
                    Id = 50,
                    VariantStyleId = 4,
                    ImagePath = "images/sprites/king_galaxy.webp"
                },
                new SpriteVariantSeed
                {
                    Id = 51,
                    VariantStyleId = 5,
                    ImagePath = "images/sprites/king_holofoil.webp"
                },
            ]
        },
        new SpriteFamilySeed
        {
            Id = 10,
            Name = "Aura",
            ImagePath = "images/sprites/drifter_basic.webp",
            Slug = "drifter",
            Rarity = "Epic",
            RarityColor = "#c780ff",
            Ability = "Grants a Shock Rock charge after enough damage.",
            PrimaryColor = "#faaf6b",
            SecondaryColor = "#f36baa",
            DisplayOrder = 10,
            Variants =
            [
                new SpriteVariantSeed
                {
                    Id = 52,
                    VariantStyleId = 1,
                    ImagePath = "images/sprites/drifter_basic.webp"
                },
                new SpriteVariantSeed
                {
                    Id = 53,
                    VariantStyleId = 2,
                    ImagePath = "images/sprites/drifter_gold.webp"
                },
                new SpriteVariantSeed
                {
                    Id = 54,
                    VariantStyleId = 3,
                    ImagePath = "images/sprites/drifter_gummy.webp"
                },
                new SpriteVariantSeed
                {
                    Id = 55,
                    VariantStyleId = 4,
                    ImagePath = "images/sprites/drifter_galaxy.webp"
                },
                new SpriteVariantSeed
                {
                    Id = 56,
                    VariantStyleId = 6,
                    ImagePath = "images/sprites/drifter_gem.webp"
                },
            ]
        },
        new SpriteFamilySeed
        {
            Id = 11,
            Name = "Striker",
            ImagePath = "images/sprites/soccer_basic.webp",
            Slug = "soccer",
            Rarity = "Epic",
            RarityColor = "#c780ff",
            Ability = "Traversal actions trigger speed, reload, and fire-rate Overdrive.",
            PrimaryColor = "#95efff",
            SecondaryColor = "#4a62f4",
            DisplayOrder = 11,
            Variants =
            [
                new SpriteVariantSeed
                {
                    Id = 57,
                    VariantStyleId = 1,
                    ImagePath = "images/sprites/soccer_basic.webp"
                },
                new SpriteVariantSeed
                {
                    Id = 58,
                    VariantStyleId = 2,
                    ImagePath = "images/sprites/soccer_gold.webp"
                },
                new SpriteVariantSeed
                {
                    Id = 59,
                    VariantStyleId = 3,
                    ImagePath = "images/sprites/soccer_gummy.webp"
                },
                new SpriteVariantSeed
                {
                    Id = 60,
                    VariantStyleId = 4,
                    ImagePath = "images/sprites/soccer_galaxy.webp"
                },
                new SpriteVariantSeed
                {
                    Id = 61,
                    VariantStyleId = 5,
                    ImagePath = "images/sprites/soccer_holofoil.webp"
                },
            ]
        },
        new SpriteFamilySeed
        {
            Id = 12,
            Name = "Dream",
            ImagePath = "images/sprites/dream_basic.webp",
            Slug = "dream",
            Rarity = "Legendary",
            RarityColor = "#ffb23f",
            Ability = "Grants random loot each level and Legendary loot at max level.",
            PrimaryColor = "#bfa6ff",
            SecondaryColor = "#5367e7",
            DisplayOrder = 12,
            Variants =
            [
                new SpriteVariantSeed
                {
                    Id = 62,
                    VariantStyleId = 1,
                    ImagePath = "images/sprites/dream_basic.webp"
                },
                new SpriteVariantSeed
                {
                    Id = 63,
                    VariantStyleId = 2,
                    ImagePath = "images/sprites/dream_gold.webp"
                },
                new SpriteVariantSeed
                {
                    Id = 64,
                    VariantStyleId = 3,
                    ImagePath = "images/sprites/dream_gummy.webp"
                },
                new SpriteVariantSeed
                {
                    Id = 65,
                    VariantStyleId = 4,
                    ImagePath = "images/sprites/dream_galaxy.webp"
                },
                new SpriteVariantSeed
                {
                    Id = 66,
                    VariantStyleId = 7,
                    ImagePath = "images/sprites/dream_cube.webp"
                },
            ]
        },
        new SpriteFamilySeed
        {
            Id = 13,
            Name = "Punk",
            ImagePath = "images/sprites/punk_basic.webp",
            Slug = "punk",
            Rarity = "Legendary",
            RarityColor = "#ffb23f",
            Ability = "Can grant a short infinite-ammo effect at max mastery.",
            PrimaryColor = "#ff6fd0",
            SecondaryColor = "#633b93",
            DisplayOrder = 13,
            Variants =
            [
                new SpriteVariantSeed
                {
                    Id = 67,
                    VariantStyleId = 1,
                    ImagePath = "images/sprites/punk_basic.webp"
                },
                new SpriteVariantSeed
                {
                    Id = 68,
                    VariantStyleId = 2,
                    ImagePath = "images/sprites/punk_gold.webp"
                },
                new SpriteVariantSeed
                {
                    Id = 69,
                    VariantStyleId = 3,
                    ImagePath = "images/sprites/punk_gummy.webp"
                },
                new SpriteVariantSeed
                {
                    Id = 70,
                    VariantStyleId = 4,
                    ImagePath = "images/sprites/punk_galaxy.webp"
                },
                new SpriteVariantSeed
                {
                    Id = 71,
                    VariantStyleId = 7,
                    ImagePath = "images/sprites/punk_cube.webp"
                },
            ]
        },
        new SpriteFamilySeed
        {
            Id = 14,
            Name = "Boss",
            ImagePath = "images/sprites/boss_basic.webp",
            Slug = "boss",
            Rarity = "Legendary",
            RarityColor = "#ffb23f",
            Ability = "Boosts your maximum Health and Shield while equipped.",
            PrimaryColor = "#ffc45d",
            SecondaryColor = "#c04b31",
            DisplayOrder = 14,
            Variants =
            [
                new SpriteVariantSeed
                {
                    Id = 72,
                    VariantStyleId = 1,
                    ImagePath = "images/sprites/boss_basic.webp"
                },
                new SpriteVariantSeed
                {
                    Id = 73,
                    VariantStyleId = 2,
                    ImagePath = "images/sprites/boss_gold.webp"
                },
                new SpriteVariantSeed
                {
                    Id = 74,
                    VariantStyleId = 3,
                    ImagePath = "images/sprites/boss_gummy.webp"
                },
                new SpriteVariantSeed
                {
                    Id = 75,
                    VariantStyleId = 4,
                    ImagePath = "images/sprites/boss_galaxy.webp"
                },
                new SpriteVariantSeed
                {
                    Id = 76,
                    VariantStyleId = 7,
                    ImagePath = "images/sprites/boss_cube.webp"
                },
            ]
        },
        new SpriteFamilySeed
        {
            Id = 15,
            Name = "Seven",
            ImagePath = "images/sprites/seven_basic.webp",
            Slug = "seven",
            Rarity = "Legendary",
            RarityColor = "#ffb23f",
            Ability = "Reveals enemy foot trails for you and nearby squadmates.",
            PrimaryColor = "#f15d5d",
            SecondaryColor = "#5062b9",
            DisplayOrder = 15,
            Variants =
            [
                new SpriteVariantSeed
                {
                    Id = 77,
                    VariantStyleId = 1,
                    ImagePath = "images/sprites/seven_basic.webp"
                },
                new SpriteVariantSeed
                {
                    Id = 78,
                    VariantStyleId = 2,
                    ImagePath = "images/sprites/seven_gold.webp"
                },
                new SpriteVariantSeed
                {
                    Id = 79,
                    VariantStyleId = 3,
                    ImagePath = "images/sprites/seven_gummy.webp"
                },
                new SpriteVariantSeed
                {
                    Id = 80,
                    VariantStyleId = 4,
                    ImagePath = "images/sprites/seven_galaxy.webp"
                },
                new SpriteVariantSeed
                {
                    Id = 81,
                    VariantStyleId = 5,
                    ImagePath = "images/sprites/seven_holofoil.webp"
                },
            ]
        },
        new SpriteFamilySeed
        {
            Id = 16,
            Name = "Llama",
            ImagePath = "images/sprites/llama_basic.webp",
            Slug = "llama",
            Rarity = "Legendary",
            RarityColor = "#ffb23f",
            Ability = "Opening Ammo Boxes has a chance to upgrade your weapon.",
            PrimaryColor = "#f19ee4",
            SecondaryColor = "#6a66dd",
            DisplayOrder = 16,
            Variants =
            [
                new SpriteVariantSeed
                {
                    Id = 82,
                    VariantStyleId = 1,
                    ImagePath = "images/sprites/llama_basic.webp"
                },
                new SpriteVariantSeed
                {
                    Id = 83,
                    VariantStyleId = 2,
                    ImagePath = "images/sprites/llama_gold.webp"
                },
                new SpriteVariantSeed
                {
                    Id = 84,
                    VariantStyleId = 3,
                    ImagePath = "images/sprites/llama_gummy.webp"
                },
                new SpriteVariantSeed
                {
                    Id = 85,
                    VariantStyleId = 4,
                    ImagePath = "images/sprites/llama_galaxy.webp"
                },
                new SpriteVariantSeed
                {
                    Id = 86,
                    VariantStyleId = 6,
                    ImagePath = "images/sprites/llama_gem.webp"
                },
            ]
        },
        new SpriteFamilySeed
        {
            Id = 17,
            Name = "Peely",
            ImagePath = "images/sprites/peely_basic.webp",
            Slug = "peely",
            Rarity = "Legendary",
            RarityColor = "#ffb23f",
            Ability = "Marks nearby rare Sprites or enemies carrying them - and you.",
            PrimaryColor = "#ffea61",
            SecondaryColor = "#d99c36",
            DisplayOrder = 17,
            Variants =
            [
                new SpriteVariantSeed
                {
                    Id = 87,
                    VariantStyleId = 1,
                    ImagePath = "images/sprites/peely_basic.webp"
                },
                new SpriteVariantSeed
                {
                    Id = 88,
                    VariantStyleId = 2,
                    ImagePath = "images/sprites/peely_gold.webp"
                },
                new SpriteVariantSeed
                {
                    Id = 89,
                    VariantStyleId = 3,
                    ImagePath = "images/sprites/peely_gummy.webp"
                },
                new SpriteVariantSeed
                {
                    Id = 90,
                    VariantStyleId = 4,
                    ImagePath = "images/sprites/peely_galaxy.webp"
                },
                new SpriteVariantSeed
                {
                    Id = 91,
                    VariantStyleId = 5,
                    ImagePath = "images/sprites/peely_holofoil.webp"
                },
            ]
        },
        new SpriteFamilySeed
        {
            Id = 18,
            Name = "Grim Reaper",
            ImagePath = "images/sprites/grimreaper_basic.webp",
            Slug = "grimreaper",
            Rarity = "Mythic",
            RarityColor = "#ff5d7c",
            Ability = "Marks players who damage you and reveals their location.",
            PrimaryColor = "#c8ff70",
            SecondaryColor = "#313b43",
            DisplayOrder = 18,
            Variants =
            [
                new SpriteVariantSeed
                {
                    Id = 92,
                    VariantStyleId = 1,
                    ImagePath = "images/sprites/grimreaper_basic.webp"
                },
                new SpriteVariantSeed
                {
                    Id = 93,
                    VariantStyleId = 2,
                    ImagePath = "images/sprites/grimreaper_gold.webp"
                },
                new SpriteVariantSeed
                {
                    Id = 94,
                    VariantStyleId = 3,
                    ImagePath = "images/sprites/grimreaper_gummy.webp"
                },
                new SpriteVariantSeed
                {
                    Id = 95,
                    VariantStyleId = 4,
                    ImagePath = "images/sprites/grimreaper_galaxy.webp"
                },
                new SpriteVariantSeed
                {
                    Id = 96,
                    VariantStyleId = 7,
                    ImagePath = "images/sprites/grimreaper_cube.webp"
                },
                new SpriteVariantSeed
                {
                    Id = 97,
                    VariantStyleId = 5,
                    ImagePath = "images/sprites/grimreaper_holofoil.webp"
                },
                new SpriteVariantSeed
                {
                    Id = 98,
                    VariantStyleId = 6,
                    ImagePath = "images/sprites/grimreaper_gem.webp"
                },
            ]
        },
        new SpriteFamilySeed
        {
            Id = 19,
            Name = "Zero Point",
            ImagePath = "images/sprites/zeropoint_basic.webp",
            Slug = "zeropoint",
            Rarity = "Mythic",
            RarityColor = "#ff5d7c",
            Ability = "Creates a Shield Bubble Jr. whenever you heal.",
            PrimaryColor = "#f49dff",
            SecondaryColor = "#6647ff",
            DisplayOrder = 19,
            Variants =
            [
                new SpriteVariantSeed
                {
                    Id = 99,
                    VariantStyleId = 1,
                    ImagePath = "images/sprites/zeropoint_basic.webp"
                },
                new SpriteVariantSeed
                {
                    Id = 100,
                    VariantStyleId = 2,
                    ImagePath = "images/sprites/zeropoint_gold.webp"
                },
                new SpriteVariantSeed
                {
                    Id = 101,
                    VariantStyleId = 3,
                    ImagePath = "images/sprites/zeropoint_gummy.webp"
                },
                new SpriteVariantSeed
                {
                    Id = 102,
                    VariantStyleId = 4,
                    ImagePath = "images/sprites/zeropoint_galaxy.webp"
                },
                new SpriteVariantSeed
                {
                    Id = 103,
                    VariantStyleId = 5,
                    ImagePath = "images/sprites/zeropoint_holofoil.webp"
                },
                new SpriteVariantSeed
                {
                    Id = 104,
                    VariantStyleId = 7,
                    ImagePath = "images/sprites/zeropoint_cube.webp"
                },
                new SpriteVariantSeed
                {
                    Id = 105,
                    VariantStyleId = 8,
                    ImagePath = "images/sprites/zeropoint_quack.webp"
                },
                new SpriteVariantSeed
                {
                    Id = 106,
                    VariantStyleId = 6,
                    ImagePath = "images/sprites/zeropoint_gem.webp"
                },
            ]
        },
        new SpriteFamilySeed
        {
            Id = 20,
            Name = "Burnt Peanut",
            ImagePath = "images/sprites/theburntpeanut_basic.webp",
            Slug = "theburntpeanut",
            Rarity = "Mythic",
            RarityColor = "#ff5d7c",
            Ability = "May award extra or Mythic loot after eliminations.",
            PrimaryColor = "#ffb45d",
            SecondaryColor = "#743c32",
            DisplayOrder = 20,
            Variants =
            [
                new SpriteVariantSeed
                {
                    Id = 107,
                    VariantStyleId = 1,
                    ImagePath = "images/sprites/theburntpeanut_basic.webp"
                },
            ]
        },
        new SpriteFamilySeed
        {
            Id = 21,
            Name = "Batman",
            ImagePath = "images/sprites/batman_basic.webp",
            Slug = "batman",
            Rarity = "Mythic",
            RarityColor = "#ff5d7c",
            Ability = "Deploy the Bat Cape midair and improve rare-Sprite chest finds.",
            PrimaryColor = "#ffe16b",
            SecondaryColor = "#202735",
            DisplayOrder = 21,
            Variants =
            [
                new SpriteVariantSeed
                {
                    Id = 108,
                    VariantStyleId = 1,
                    ImagePath = "images/sprites/batman_basic.webp"
                },
                new SpriteVariantSeed
                {
                    Id = 109,
                    VariantStyleId = 2,
                    ImagePath = "images/sprites/batman_gold.webp"
                },
                new SpriteVariantSeed
                {
                    Id = 110,
                    VariantStyleId = 3,
                    ImagePath = "images/sprites/batman_gummy.webp"
                },
                new SpriteVariantSeed
                {
                    Id = 111,
                    VariantStyleId = 4,
                    ImagePath = "images/sprites/batman_galaxy.webp"
                },
                new SpriteVariantSeed
                {
                    Id = 112,
                    VariantStyleId = 5,
                    ImagePath = "images/sprites/batman_holofoil.webp"
                },
                new SpriteVariantSeed
                {
                    Id = 113,
                    VariantStyleId = 7,
                    ImagePath = "images/sprites/batman_cube.webp"
                },
            ]
        },
        new SpriteFamilySeed
        {
            Id = 22,
            Name = "Pollo",
            ImagePath = "images/sprites/pollo_basic.webp",
            Slug = "pollo",
            Rarity = "Mythic",
            RarityColor = "#ff5d7c",
            Ability = "Replenishes nearby shields after an elimination.",
            PrimaryColor = "#f8efe4",
            SecondaryColor = "#d95445",
            DisplayOrder = 22,
            Variants =
            [
                new SpriteVariantSeed
                {
                    Id = 114,
                    VariantStyleId = 1,
                    ImagePath = "images/sprites/pollo_basic.webp"
                },
            ]
        },
        new SpriteFamilySeed
        {
            Id = 23,
            Name = "Vini Jr.",
            ImagePath = "images/sprites/vinijr_basic.webp",
            Slug = "vinijr",
            Rarity = "Mythic",
            RarityColor = "#ff5d7c",
            Ability = "Empowers destructive slides and boosts combat after slide-kicks.",
            PrimaryColor = "#83e8ff",
            SecondaryColor = "#2361b9",
            DisplayOrder = 23,
            Variants =
            [
                new SpriteVariantSeed
                {
                    Id = 115,
                    VariantStyleId = 1,
                    ImagePath = "images/sprites/vinijr_basic.webp"
                },
            ]
        },
        new SpriteFamilySeed
        {
            Id = 24,
            Name = "John Wick",
            ImagePath = "images/sprites/johnwick_basic.webp",
            Slug = "johnwick",
            Rarity = "Mythic",
            RarityColor = "#ff5d7c",
            Ability = "Knocks and eliminations briefly reveal nearby enemies.",
            PrimaryColor = "#989da7",
            SecondaryColor = "#292d34",
            DisplayOrder = 24,
            Variants =
            [
                new SpriteVariantSeed
                {
                    Id = 116,
                    VariantStyleId = 1,
                    ImagePath = "images/sprites/johnwick_basic.webp"
                },
            ]
        },
        new SpriteFamilySeed
        {
            Id = 25,
            Name = "Ironmouse",
            ImagePath = "images/sprites/ironmouse_basic.webp",
            Slug = "ironmouse",
            Rarity = "Mythic",
            RarityColor = "#ff5d7c",
            Ability = "Regenerates low health with Cloak and low gravity.",
            PrimaryColor = "#ff87c4",
            SecondaryColor = "#8a43a8",
            DisplayOrder = 25,
            Variants =
            [
                new SpriteVariantSeed
                {
                    Id = 117,
                    VariantStyleId = 1,
                    ImagePath = "images/sprites/ironmouse_basic.webp"
                },
            ]
        },
    ];
}
