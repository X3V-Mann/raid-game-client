namespace GameApi.Models;

public enum ItemType { WeaponMelee, WeaponRanged, Helmet, Armor, Backpack, Food, Drink, Medicine, Material, Misc, Cassette, Ammo }
public enum Rarity { Common, Uncommon, Rare, Epic, Legendary }

public class Item
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Icon { get; set; } = "";
    public ItemType Type { get; set; }
    public Rarity Rarity { get; set; } = Rarity.Common;
    public int Price { get; set; }
    public int Damage { get; set; }        // оружие
    public int Defense { get; set; }       // броня/шлем
    public int BackpackSlots { get; set; } // рюкзак
    public int Restore { get; set; }       // сила восстановления
    public string Restores { get; set; } = ""; // hp | hunger | thirst
    public int Count { get; set; } = 1;
    public string Caliber { get; set; } = ""; // калибр патронов, если предмет — боеприпас/оружие

    /// <summary>Минимальный тир локации, на котором предмет может выпасть.</summary>
    public int LootTier { get; set; } = 1;

    /// <summary>Требуемый уровень лояльности торговца, чтобы купить предмет.</summary>
    public int RequiredLoyalty { get; set; } = 0;

    /// <summary>Вес одной единицы, кг.</summary>
    public double Weight { get; set; } = 0.5;

    /// <summary>Может ли предмет выпасть в рейде (квестовые предметы — нет).</summary>
    public bool Lootable { get; set; } = true;

    /// <summary>Граната ли (взрывное устройство, используется в бою).</summary>
    public bool IsGrenade { get; set; }
}
