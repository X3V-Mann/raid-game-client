using GameApi.Services;

namespace GameApi.Models;

public class ShelterState
{
    public int MedBlockLevel { get; set; }
    public int RestZoneLevel { get; set; }
    public int KitchenLevel { get; set; }
    public int StashLevel { get; set; }
    public int StashSlots { get; set; } = 50; // 50 ячеек по умолчанию
    public int WorkshopLevel { get; set; }
    public int GeneratorLevel { get; set; }
    public int WaterLevel { get; set; }
    public int ArmoryLevel { get; set; }
    public int GymLevel { get; set; }
    public List<Item> Stash { get; set; } = new();
}

public class Player
{
    public int Level { get; set; } = 1;
    public int Xp { get; set; }
    public int PerkPoints { get; set; }
    public int Money { get; set; } = 15;
    public int Hp { get; set; } = 100;
    public int MaxHp { get; set; } = 100;
    public int Stamina { get; set; } = 100;
    public int MaxStamina { get; set; } = 100;
    public int Hunger { get; set; } = 100;
    public int Thirst { get; set; } = 100;

    public string Name { get; set; } = "";
    public string Avatar { get; set; } = "messer";
    public bool Onboarded { get; set; } = false;
    public bool GameCompleted { get; set; } = false;

    // Экипировка
    public Item? WeaponMelee { get; set; }
    public Item? WeaponRanged { get; set; }
    public Item? Helmet { get; set; }
    public Item? Armor { get; set; }
    public Item? Suit { get; set; }
    public Item? Backpack { get; set; }

    // === Характеристики ГГ ===
    /// <summary>Удача — влияет на качество и количество лута.</summary>
    public int Luck { get; set; } = 1;
    /// <summary>Ближний бой — бонус к урону и шанс попадания в misch.</summary>
    public int MeleeSkill { get; set; } = 1;
    /// <summary>Дальний бой — бонус к урону и меткости огнестрела.</summary>
    public int RangedSkill { get; set; } = 1;
    /// <summary>Меткость — снижает шанс промаха и улучшает прицельный выстрел.</summary>
    public int Accuracy { get; set; } = 1;
    /// <summary>Ловкость — влияет на AP и общее проворство.</summary>
    public int Agility { get; set; } = 1;
    /// <summary>Уклонение — шанс увернуться от удара/выстрела.</summary>
    public int Dodge { get; set; } = 1;
    /// <summary>Парирование — шанс частично заблокировать удар.</summary>
    public int Parry { get; set; } = 1;
    /// <summary>Обращение с оружием — устойчивость и контроль при стрельбе/ударах.</summary>
    public int WeaponHandling { get; set; } = 1;
    /// <summary>Очки для прокачки характеристик.</summary>
    public int SkillPoints { get; set; } = 0;

    /// <summary>Бонус к шансу уклонения от удара (0..1).</summary>
    public double DodgeChance => Math.Min(0.60, (Agility - 1) * 0.02 + (Dodge - 1) * 0.05);

    /// <summary>Шанс парирования/блока (0..1).</summary>
    public double ParryChance => Math.Min(0.45, (Parry - 1) * 0.035 + (Agility - 1) * 0.01);

    /// <summary>Бонус к AP в бою за ловкость (характеристика).</summary>
    public int AgilityApBonus => (Agility - 1) / 2;

    /// <summary>Шанс найти дополнительный предмет при обыске (0..1).</summary>
    public double LootBonus => Math.Min(0.40, (Luck - 1) * 0.05 + PerkEffects.ExtraLootRolls(this) * 0.1);

    /// <summary>Бонус к урону ближнего боя.</summary>
    public int MeleeDamageBonus => (MeleeSkill - 1) * 2;

    /// <summary>Бонус к урону дальнего боя.</summary>
    public int RangedDamageBonus => (RangedSkill - 1) * 2;

    /// <summary>Бонус к меткости (снижение шанса промаха, %).</summary>
    public double AccuracyBonus => Math.Min(0.30, (Accuracy - 1) * 0.03);

    public List<Item> Inventory { get; set; } = new();
    public ShelterState Shelter { get; set; } = new();
    public List<string> UnlockedLocations { get; set; } = new() { "city" };
    public Dictionary<string, int> Perks { get; set; } = new();

    /// <summary>Очки репутации у торговцев (ключ — id торговца).</summary>
    public Dictionary<string, int> TraderRep { get; set; } = new();

    /// <summary>Найденные кассеты (коды), доступные в проигрывателе убежища.</summary>
    public List<string> Cassettes { get; set; } = new();

    /// <summary>Свободная игра после финала.</summary>
    public bool Freeplay { get; set; } = false;

    /// <summary>Открытые достижения (id).</summary>
    public List<string> Achievements { get; set; } = new();

    /// <summary>Разблокированные торговцы (прогрессия сюжета). В начале доступны скупщик и бармен.</summary>
    public List<string> UnlockedTraders { get; set; } = new() { "buyer", "bartender" };

    /// <summary>Игровое время в минутах от старта (День 1, 08:00 = 480).</summary>
    public long GameTimeMinutes { get; set; } = 480;

    /// <summary>Мысли главного героя (автоматические/сюжетные записи).</summary>
    public List<string> JournalThoughts { get; set; } = new();

    /// <summary>Заметки игрока (отдельная вкладка дневника).</summary>
    public List<string> JournalNotes { get; set; } = new();

    /// <summary>Финальный выбор: detonated/hidden/null.</summary>
    public string? EndingChoice { get; set; }
    /// <summary>Бомба спрятана в убежище/локации (спрайт доступен при hidden).</summary>
    public bool BombHidden { get; set; }

    // Статистика
    public PlayerStats Stats { get; set; } = new();

    /// <summary>Базовые слоты инвентаря без экипированной сумки.</summary>
    public const int BaseInventorySlots = 12;

    /// <summary>Доп. слоты от снаряжения (разгрузки/жилеты в слоте брони).</summary>
    public int EquipmentBonusSlots => (Armor != null && !Armor.IsSuit ? Armor.BackpackSlots : 0);

    /// <summary>Суммируются: базовые слоты + слоты сумки + слоты разгрузки/жилета.</summary>
    public int InventoryCapacity => BaseInventorySlots + (Backpack?.BackpackSlots ?? 0) + EquipmentBonusSlots;
    public Item? Weapon => WeaponRanged ?? WeaponMelee;

    public double CurrentWeight
    {
        get
        {
            double sum = Inventory.Sum(i => i.Weight * Math.Max(1, i.Count));
            foreach (var e in new[] { WeaponMelee, WeaponRanged, Helmet, Armor, Suit, Backpack })
                if (e != null) sum += e.Weight;
            return Math.Round(sum, 1);
        }
    }

    public int MaxWeight => 50 + (Backpack?.BackpackSlots ?? 0) + EquipmentBonusSlots;
    public bool Overweight => CurrentWeight > MaxWeight;

    public int PerkLevel(string id) => Perks.TryGetValue(id, out var lvl) ? lvl : 0;

    // Прогресс до следующего уровня (только для отображения)
    public int XpForNextLevel => 100 + (Level - 1) * 75;
    public int XpIntoLevel
    {
        get
        {
            int spent = 0;
            for (int l = 1; l < Level; l++) spent += 100 + (l - 1) * 75;
            return Math.Max(0, Xp - spent);
        }
    }
}
