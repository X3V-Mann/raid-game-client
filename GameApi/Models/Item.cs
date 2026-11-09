namespace GameApi.Models;

public enum ItemType { WeaponMelee, WeaponRanged, Helmet, Armor, Backpack, Food, Drink, Medicine, Material, Misc, Cassette, Ammo, Stash }
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
    public int Defense { get; set; }       // броня/шлем (общая)

    // === Новые параметры брони (стиль S.T.A.L.K.E.R.) ===
    /// <summary>Замедление движения, % (0-100).</summary>
    public int Slowdown { get; set; }
    /// <summary>Защита от взрыва/разрыва, % (0-100).</summary>
    public int BlastProtection { get; set; }
    /// <summary>Защита от пуль/выстрелов, % (0-100).</summary>
    public int BulletProtection { get; set; }
    /// <summary>Защита от физ. урона (нож, кулаки), % (0-100).</summary>
    public int PhysicalProtection { get; set; }

    // === Новые параметры оружия ===
    /// <summary>Меткость оружия (снижает шанс промаха), 0-100.</summary>
    public int Accuracy { get; set; }
    /// <summary>Эффективная дальность, клетки.</summary>
    public int RangeEffective { get; set; }
    /// <summary>Максимальная дальность, клетки.</summary>
    public int RangeMax { get; set; }
    /// <summary>Скорострельность (выстрелы в минуту).</summary>
    public int Rpm { get; set; }
    /// <summary>Требуемый уровень навыка владения оружием (0 = без ограничений).</summary>
    public int RequiredSkill { get; set; }

    public int BackpackSlots { get; set; } // рюкзак
    public int Restore { get; set; }       // сила восстановления
    public string Restores { get; set; } = ""; // hp | hunger | thirst
    /// <summary>Дополнительное восстановление HP при использовании (еда/напитки с лечебным эффектом).</summary>
    public int RestoreHpBonus { get; set; } = 0;
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

    /// <summary>Костюм (одежда), занимает слот костюма.</summary>
    public bool IsSuit { get; set; }

    /// <summary>Тайник — редкий объект в зданиях, можно обыскать.</summary>
    public bool IsStash { get; set; }

    /// <summary>Предмет вынесен из рейда — только такие можно сдавать торговцам.</summary>
    public bool FoundInRaid { get; set; }
}
