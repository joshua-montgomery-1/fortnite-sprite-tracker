namespace FortniteSpriteTracker.DataAccess.Seeding;

public sealed record CheatCodeCategorySeed(int Id, string Name, int DisplayOrder);

public sealed record CheatCodeSeed(
    int Id,
    int CategoryId,
    string Code,
    string Description,
    string? Requirement,
    bool IsTrackable,
    int DisplayOrder);

public static class CheatCodeSeedData
{
    public const int SeasonId = 2;

    public static readonly IReadOnlyList<CheatCodeCategorySeed> Categories =
    [
        new(1, "Cosmetics", 1),
        new(2, "Sprites", 2),
        new(3, "Sprite Dust", 3),
        new(4, "Gizmos & Supplies", 4),
        new(5, "XP", 5),
        new(6, "Lobby Effects", 6)
    ];

    public static readonly IReadOnlyList<CheatCodeSeed> Codes =
    [
        new(1001, 1, "REACHYOURIMPOSSIBLE", "Unlocks the Block Party Loading Screen.", null, true, 1),
        new(1002, 1, "BEMOREALIEN", "Unlocks the Override Ready Loading Screen.", null, true, 2),

        new(1101, 2, "JONESYISGOLDEN", "Unlocks the Gold Jonesy Sprite.", null, true, 1),
        new(1102, 2, "GATHERANDCRAFT", "Unlocks the Cheat Master Bush Sprite.", "Complete Wrixel's Story Quest before redeeming this code.", true, 2),
        new(1103, 2, "PLAY4ALL", "Unlocks the Cheat Master Jonesy Sprite.", null, true, 3),
        new(1104, 2, "GOTTAGOFAST", "Unlocks the Cheat Master Sonic Sprite.", null, true, 4),
        new(1105, 2, "IWANNAFLYHIGH", "Unlocks the Cheat Master Tails Sprite.", null, true, 5),
        new(1106, 2, "8BITBLAST", "Unlocks the Cheat Master 8-Bit Sprite.", null, true, 6),
        new(1107, 2, "BORN2PLAY", "Unlocks the Cheat Master Adventure Sprite.", null, true, 7),

        new(1201, 3, "H0P0NVC", "Grants 2,000 Sprite Dust.", null, true, 1),
        new(1202, 3, "MAGILUME", "Grants 2,000 Sprite Dust.", null, true, 2),
        new(1203, 3, "CHISPAMBO", "Grants 2,000 Sprite Dust.", null, true, 3),
        new(1204, 3, "ABGESTAUBT", "Grants 2,000 Sprite Dust.", null, true, 4),
        new(1205, 3, "PERLIMPINPIN", "Grants 2,000 Sprite Dust.", null, true, 5),

        new(1301, 4, "O2OVERRIDE", "Grants one Llama Supply Drop and five Portable Extractors.", null, true, 1),
        new(1302, 4, "TAKEYOURHEART", "Grants two Extraction Accelerators.", null, true, 2),
        new(1303, 4, "SURVIVETHENIGHT", "Grants two Cheat Code Locators.", null, true, 3),
        new(1304, 4, "FINDITCHAT", "Grants two Cheat Code Locators.", null, true, 4),
        new(1305, 4, "PERFECTORDER", "Grants four Spicy Tacos.", null, true, 5),

        new(1401, 5, "OVERRIDEXP", "Grants 40,000 Battle Pass XP.", null, true, 1),

        new(1501, 6, "LETSBLOCKANDROLL", "Repeatable: temporarily transforms your lobby character into a Tetris block.", null, false, 1),
        new(1502, 6, "DONTBLOCKME", "Repeatable: temporarily transforms your lobby character into a Tetris block.", null, false, 2)
    ];
}
