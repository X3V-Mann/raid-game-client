using GameApi.Models;

namespace GameApi.Services;

public static class TraderLoyalty
{
    public const int MaxLevel = 10;
    private static readonly int[] Thresholds = { 0, 60, 160, 320, 550, 900, 1400, 2000, 2800, 3800 };

    public static int Level(Player p, string traderId)
    {
        int rep = p.TraderRep.TryGetValue(traderId, out var r) ? r : 0;
        int level = 1;
        for (int i = 1; i < Thresholds.Length; i++)
            if (rep >= Thresholds[i]) level = i + 1;
        return Math.Min(level, MaxLevel);
    }

    public static int Rep(Player p, string traderId) =>
        p.TraderRep.TryGetValue(traderId, out var r) ? r : 0;

    public static int CurrentLevelFloor(Player p, string traderId) =>
        Thresholds[Math.Max(0, Level(p, traderId) - 1)];

    public static int NextThreshold(Player p, string traderId)
    {
        int level = Level(p, traderId);
        return level >= MaxLevel ? 0 : Thresholds[level];
    }

    public static void Add(Player p, string traderId, int amount)
    {
        if (amount <= 0) return;
        p.TraderRep[traderId] = Rep(p, traderId) + amount;
    }

    /// <summary>Требуемая лояльность для не-оружия (оружие задаётся в WeaponCatalog).</summary>
    public static int RequiredFor(string code) => code switch
    {
        "helmet_tactical" => 2,
        "helmet_altyn" => 3,
        "helmet_exo" => 4,
        "vest" => 2,
        "combat_suit" => 3,
        "vest_heavy" => 3,
        "sphere" => 4,
        "exo" => 5,
        "backpack" => 2,
        "raidpack" => 3,
        "bag_large" => 3,
        "bag_expedition" => 4,
        "medkit" => 2,
        "antirad" => 2,
        "surgical_kit" => 4,
        "blood_bag" => 4,
        "battery" => 1,
        "electronics" => 1,
        "toolkit" => 1,
        "fuel" => 1,
        "artifact" => 1,
        "documents" => 1,
        _ => 1,
    };
}