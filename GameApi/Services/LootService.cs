using GameApi.Models;

namespace GameApi.Services;

public class LootService
{
    private readonly ItemCatalog _catalog;
    private readonly Random _rng = new();

    public LootService(ItemCatalog catalog) => _catalog = catalog;

    public LootResult Generate(int tier, int kills, bool boss = false, int extraRolls = 0)
    {
        var items = new List<Item>();
        int rolls = 1 + kills + _rng.Next(0, 2) + Math.Max(0, extraRolls);
        for (int i = 0; i < rolls; i++)
        {
            var rarity = RollRarity(tier);
            var pool = _catalog.All
                .Where(t => t.Lootable && t.LootTier <= tier && t.LootTier >= Math.Max(1, tier - 2) && t.Rarity == rarity)
                .ToList();
            if (pool.Count == 0)
                pool = _catalog.All.Where(t => t.Lootable && t.LootTier <= tier).ToList();
            items.Add(_catalog.Create(pool[_rng.Next(pool.Count)].Code));
        }

        var cassette = RollCassette();
        if (cassette != null) items.Add(cassette);

        // Бонус за босса: гарантированный редкий предмет + усиленные деньги и XP
        if (boss)
        {
            var rarePool = _catalog.All
                .Where(t => t.Lootable && t.Rarity >= Rarity.Rare && t.LootTier <= tier)
                .ToList();
            if (rarePool.Count > 0)
                items.Add(_catalog.Create(rarePool[_rng.Next(rarePool.Count)].Code));
        }

        var grouped = new List<Item>();
        foreach (var it in items)
        {
            if (InventoryHelper.IsStackable(it))
            {
                var ex = grouped.FirstOrDefault(g => g.Code == it.Code);
                if (ex != null) { ex.Count += it.Count; continue; }
            }
            grouped.Add(it);
        }

        List<Item> finalItems = grouped;

        // Мизерный шанс выпадения оружия (милишка или огнестрел) — по тиру локации.
        if (_rng.NextDouble() < 0.08 && tier >= 1)
        {
            var weaponPool = _catalog.All
                .Where(i => (i.Type == ItemType.WeaponMelee || i.Type == ItemType.WeaponRanged)
                            && i.Lootable
                            && i.LootTier <= tier)
                .ToList();
            if (weaponPool.Count > 0)
            {
                var weapon = _catalog.Create(weaponPool[_rng.Next(weaponPool.Count)].Code);
                if (InventoryHelper.IsStackable(weapon)) weapon.Count = 1;
                finalItems.Add(weapon);
            }
        }

        var result = new LootResult
        {
            Items = finalItems,
            Money = _rng.Next(10, 40) * tier + kills * _rng.Next(5, 20),
            Xp = kills * (15 + tier * 5)
        };

        if (boss)
        {
            result.Money *= 3;
            result.Xp *= 3;
        }

        return result;
    }

    private Rarity RollRarity(int tier)
    {
        int total = 45 + 28 + (16 + tier) + (8 + tier * 2) + (3 + tier);
        int roll = _rng.Next(total);
        if ((roll -= 45) < 0) return Rarity.Common;
        if ((roll -= 28) < 0) return Rarity.Uncommon;
        if ((roll -= (16 + tier)) < 0) return Rarity.Rare;
        if ((roll -= (8 + tier * 2)) < 0) return Rarity.Epic;
        return Rarity.Legendary;
    }

    private static readonly string[] CassetteCodes = { "cassette_1", "cassette_2", "cassette_3" };

    /// <summary>Редкая находка: кассета для проигрывателя в убежище.</summary>
    public Item? RollCassette()
    {
        if (_rng.NextDouble() > 0.05) return null;
        return _catalog.Create(CassetteCodes[_rng.Next(CassetteCodes.Length)]);
    }
}
