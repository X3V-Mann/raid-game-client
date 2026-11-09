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

    /// <summary>Скидка за лояльность: 2.5% за каждый уровень выше первого.</summary>
    public static double PriceMultiplier(Player p, string traderId) =>
        1.0 - 0.025 * (Level(p, traderId) - 1);

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
        // Новые шлемы
        "helmet_bulletproof" => 3,
        "helmet_riot" => 4,
        "helmet_stalker" => 3,
        // Новые костюмы
        "suit_seva" => 5,
        "suit_gilly" => 4,
        "suit_armor" => 4,
        "suit_tactical" => 4,
        // Новые бронежилеты
        "vest_steel" => 4,
        "vest_bulletproof" => 5,
        "vest_military" => 4,
        "vest_ceramic" => 4,
        // Новые рюкзаки
        "bag_huge" => 4,
        "bag_cargo" => 4,
        "bag_milpack" => 3,
        "bag_tactpack" => 3,
        "backpack" => 2,
        "raidpack" => 3,
        "bag_large" => 3,
        "bag_expedition" => 4,
        // Медицина: чем сильнее лечение, тем выше требуемая лояльность.
        // 1 ур. — до 35 HP, 2 ур. — 40–60 HP, 3 ур. — 61–70 HP, 4 ур. — 71+ HP.
        "medkit_car" => 2,
        "morphine" => 2,
        "stimulant" => 2,
        "bandage_pack" => 2,
        "medkit" => 2,
        "medkit_army" => 3,
        "blood_bag" => 4,
        "surgical_kit" => 4,
        "battery" => 1,
        "electronics" => 1,
        "toolkit" => 1,
        "fuel" => 1,
        "artifact" => 1,
        "documents" => 1,
        _ => 1,
    };
}