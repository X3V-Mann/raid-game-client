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
    public Item? Backpack { get; set; }

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

    // Статистика
    public PlayerStats Stats { get; set; } = new();

    /// <summary>Базовые слоты инвентаря без экипированной сумки.</summary>
    public const int BaseInventorySlots = 12;

    /// <summary>Суммируются: базовые слоты + слоты экипированной сумки.</summary>
    public int InventoryCapacity => BaseInventorySlots + (Backpack?.BackpackSlots ?? 0);
    public Item? Weapon => WeaponRanged ?? WeaponMelee;

    public double CurrentWeight
    {
        get
        {
            double sum = Inventory.Sum(i => i.Weight * Math.Max(1, i.Count));
            foreach (var e in new[] { WeaponMelee, WeaponRanged, Helmet, Armor, Backpack })
                if (e != null) sum += e.Weight;
            return Math.Round(sum, 1);
        }
    }

    public int MaxWeight => 50 + (Backpack?.BackpackSlots ?? 0);
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
