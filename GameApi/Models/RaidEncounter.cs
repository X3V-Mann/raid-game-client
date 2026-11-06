namespace GameApi.Models;

public enum EncounterType
{
    Empty,       // ничего
    Loot,        // находка
    Combat,      // бой
    Scene,       // сценка (текст)
    Trader,      // торговец (опционально)
    Trap,        // ловушка (урон)
}

public class RaidEncounter
{
    public int Index { get; set; }           // 1..10
    public EncounterType Type { get; set; }
    public string? Title { get; set; }       // "Ржавый тайник"
    public string? Description { get; set; } // "Ты замечаешь ящик за мусором"
    public string? Icon { get; set; }        // "📦"
    public CombatStateDto? Combat { get; set; } // если Combat — заполнено
    public List<Item>? LootItems { get; set; }  // если Loot/Combat — предметы
    public int MoneyReward { get; set; }
    public int XpReward { get; set; }
    public int Damage { get; set; }          // если Trap — урон
}

public class CombatStateDto
{
    public List<Enemy> Enemies { get; set; } = new();   // ← было Enemy Enemy
    public int PlayerHp { get; set; }
    public int PlayerMaxHp { get; set; }
    public int PlayerStamina { get; set; }
    public int PlayerMaxStamina { get; set; }
    public int PlayerHunger { get; set; }
    public int PlayerThirst { get; set; }
    public double PlayerWeight { get; set; }
    public int PlayerMaxWeight { get; set; }
    public int PlayerDefense { get; set; }
    public int PlayerDamage { get; set; }
    public bool IsDefending { get; set; }
    public List<string> Log { get; set; } = new();
    public bool Finished { get; set; }
    public bool PlayerWon { get; set; }
    public bool PlayerFled { get; set; }
    public int Tier { get; set; } = 1;                  // тир локации, нужен для лута
    public int CoverTurns { get; set; } = 0;          // ходы защиты от перка «Укрытие»
}

public class RaidStartDto
{
    public int Tier { get; set; }
    public string LocationId { get; set; } = "";
}

public class RaidActionResult
{
    public CombatStateDto State { get; set; } = new();
    public List<Item> DroppedLoot { get; set; } = new();
    public int DroppedMoney { get; set; }
    public int DroppedXp { get; set; }
    /// <summary>Актуальное состояние игрока (HP, инвентарь, опыт) после действия.</summary>
    public Player? Player { get; set; }
    public int LevelsGained { get; set; }
}
