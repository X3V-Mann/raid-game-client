using GameApi.Services;

namespace GameApi.Models;

// ============================================================
//  СЕТКА РЕЙДА (20 × 15)
// ============================================================

public static class RaidTileTypes
{
    public const string Ground = "ground";
    public const string Tree = "tree";
    public const string Building = "building";
    public const string Exit = "exit";
    public const string Stash = "stash";
    public const string Transition = "transition";
    public const string Car = "car";
}

public class RaidTile
{
    public int X { get; set; }
    public int Y { get; set; }
    public string Type { get; set; } = RaidTileTypes.Ground;
    public string? BuildingId { get; set; }
}

public class RaidBuilding
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public string Name { get; set; } = "Здание";
    public string Kind { get; set; } = "house";
    public string Icon { get; set; } = "🏠";
    public string Description { get; set; } = "";
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; } = 1;
    public int Height { get; set; } = 1;
    public bool Entered { get; set; }
    public bool Cleared { get; set; }
    public bool Looted { get; set; }
    public bool HasEnemies { get; set; }
    public bool HasStash { get; set; }
    public bool StashLooted { get; set; }
    public List<Item> StashLoot { get; set; } = new();
    public int StashMoney { get; set; }
    public int StashXp { get; set; }
    public int Tier { get; set; } = 1;
    public List<Item> Loot { get; set; } = new();
    public int Money { get; set; }
    public int Xp { get; set; }
    /// <summary>Мебель/контейнеры внутри здания, которые можно обыскать отдельно.</summary>
    public List<RaidFurniture> Furniture { get; set; } = new();
}

public class RaidState
{
    public string LocationId { get; set; } = "";
    public string LocationName { get; set; } = "";
    public int Tier { get; set; } = 1;
    public int Width { get; set; } = 20;
    public int Height { get; set; } = 15;
    public List<RaidTile> Tiles { get; set; } = new();
    public List<RaidBuilding> Buildings { get; set; } = new();
    /// <summary>Тайники на карте — редкие объекты, которые можно обыскать.</summary>
    public List<RaidStash> Stashes { get; set; } = new();
    /// <summary>Автомобили на карте — можно обыскать и найти лут.</summary>
    public List<RaidCar> Cars { get; set; } = new();
    /// <summary>Мутанты на карте локации. Внутри зданий они не спаунятся.</summary>
    public List<RaidMutant> Mutants { get; set; } = new();
    /// <summary>Нейтральные/скриптовые NPC на карте.</summary>
    public List<RaidNpc> Npcs { get; set; } = new();
    public int PlayerX { get; set; }
    public int PlayerY { get; set; }
    public int ExitX { get; set; }
    public int ExitY { get; set; }
    /// <summary>Все выходы с локации (от 1 до 3).</summary>
    public List<(int X, int Y)> Exits { get; set; } = new();
    public int CombatReturnX { get; set; }
    public int CombatReturnY { get; set; }
    public CombatStateDto? Combat { get; set; }
    public bool Finished { get; set; }
    public bool PlayerDied { get; set; }
    public bool AtExit { get; set; }
    public int Sector { get; set; } = 1;
    public int MaxSectors { get; set; } = 1;
    public int CellsVisited { get; set; }
    public int EnemiesKilled { get; set; }
    public int MoneyFound { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.Now;
    public List<string> Log { get; set; } = new();
}

/// <summary>Тайник на карте рейда — можно обыскать и найти лут.</summary>
public class RaidStash
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public int X { get; set; }
    public int Y { get; set; }
    public bool Looted { get; set; }
    public List<Item> Loot { get; set; } = new();
    public int Money { get; set; }
    public int Xp { get; set; }
}

/// <summary>Автомобиль на карте рейда — можно обыскать.</summary>
public class RaidCar
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public int X { get; set; }
    public int Y { get; set; }
    public string Name { get; set; } = "Автомобиль";
    public bool Looted { get; set; }
    public List<Item> Loot { get; set; } = new();
    public int Money { get; set; }
    public int Xp { get; set; }
}

/// <summary>Мебель/контейнер внутри здания, который можно обыскать.</summary>
public class RaidFurniture
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public string Name { get; set; } = "Мебель";
    public string Icon { get; set; } = "📦";
    /// <summary>Опциональный путь к спрайту (например /items/furniture/bed.png). Если задан — UI может рендерить картинку.</summary>
    public string? Sprite { get; set; }
    public bool Looted { get; set; }
    public List<Item> Loot { get; set; } = new();
    public int Money { get; set; }
    public int Xp { get; set; }
}

public static class CombatModes
{
    public const string Combat = "combat";
    public const string Interior = "interior";
}

public static class CombatObjectKinds
{
    public const string Furniture = "furniture";
    public const string Stash = "stash";
    public const string MainLoot = "main_loot";
    public const string Exit = "exit";
}

/// <summary>Интерактивный объект внутри здания (мебель, тайник, выход).</summary>
public class CombatObjectDto
{
    public string Id { get; set; } = "";
    public string Kind { get; set; } = CombatObjectKinds.Furniture;
    public int X { get; set; }
    public int Y { get; set; }
    public string Name { get; set; } = "";
    public string Icon { get; set; } = "📦";
    public string? Sprite { get; set; }
    public bool Looted { get; set; }
    public bool BlocksMovement { get; set; } = true;
}

/// <summary>
/// Мутант, который живёт прямо на карте локации (не в здании).
/// Видит игрока в радиусе <see cref="VisionRadius"/> клеток, но здания и деревья
/// перекрывают ему обзор — такого мутанта можно обойти стороной.
/// </summary>
public class RaidMutant
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public string Name { get; set; } = "Мутант";
    public string Icon { get; set; } = "/enemies/flesh.png";
    public int X { get; set; }
    public int Y { get; set; }
    public int Hp { get; set; } = 20;
    public int MaxHp { get; set; } = 20;
    public int Damage { get; set; } = 5;
    public int Defense { get; set; }
    public bool Ranged { get; set; }
    public int VisionRadius { get; set; } = 3;
    /// <summary>Мутант уже побеждён и больше не появляется на карте.</summary>
    public bool Defeated { get; set; }
    /// <summary>Ходов игрока до того, как мутант снова сможет заметить его (после побега).</summary>
    public int Cooldown { get; set; }
}

/// <summary>Нейтральный/дружественный/скриптовый NPC на карте рейда.</summary>
public class RaidNpc
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public string Name { get; set; } = "";
    public string Icon { get; set; } = ""; // /neutrals_frendly/...
    public int X { get; set; }
    public int Y { get; set; }
    public bool Scripted { get; set; }
    public string? GivesItemCode { get; set; } // выдаёт при разговоре (если квест активен)
    public string? RequiresQuestId { get; set; } // только если этот квест принят
    public bool GaveItem { get; set; }
    public bool Talked { get; set; }
    public List<string> Dialogue { get; set; } = new();
    public bool CanTrade { get; set; }
    public List<string> TradeCodes { get; set; } = new(); // коды товаров (покупка за деньги)
}

// ============================================================
//  ТАКТИЧЕСКИЙ БОЙ (8 × 8)
// ============================================================

public static class CombatTileTypes
{
    public const string Empty = "empty";
    public const string Sandbag = "sandbag";
    public const string Wall = "wall";
}

public class CombatTile
{
    public int X { get; set; }
    public int Y { get; set; }
    public string Type { get; set; } = CombatTileTypes.Empty;
}

public class CombatStateDto
{
    public string? BuildingId { get; set; }
    /// <summary>Если бой начался с мутантом на карте — его Id (для отметки побеждённого).</summary>
    public string? MutantId { get; set; }
    public string Mode { get; set; } = CombatModes.Combat;
    public string Title { get; set; } = "Бой";
    public int Tier { get; set; } = 1;
    public int Width { get; set; } = 20;
    public int Height { get; set; } = 20;
    public List<CombatTile> Tiles { get; set; } = new();
    public List<CombatObjectDto> Objects { get; set; } = new();

    public int PlayerX { get; set; }
    public int PlayerY { get; set; }
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
    public int CoverTurns { get; set; } = 0;

    public int PlayerAp { get; set; } = CombatRules.BaseAp;
    public int PlayerApMax { get; set; } = CombatRules.BaseAp;
    public bool IsPlayerTurn { get; set; } = true;
    public List<string> PendingEnemyIds { get; set; } = new();

    public List<Enemy> Enemies { get; set; } = new();
    public List<string> Log { get; set; } = new();
    public bool Finished { get; set; }
    public bool PlayerWon { get; set; }
    public bool PlayerFled { get; set; }
    public string? LastEvent { get; set; }
    /// <summary>Id противника, который последним атаковал игрока (для подсветки в UI).</summary>
    public string? LastAttackerId { get; set; }

    /// <summary>Есть ли на поле трупы, которые ещё можно обыскать.</summary>
    public bool CorpsesAvailable => Enemies.Any(e => e.Hp <= 0 && !e.Looted && (e.Loot.Count > 0 || e.Money > 0));
}

/// <summary>Базовые правила боя и стоимость действий в AP.</summary>
public static class CombatRules
{
    public const int BaseAp = 6;
    public const int MaxAp = 20;

    public const int MoveCost = 1;
    public const int AttackCost = 2;
    public const int AimedCost = 3;
    public const int BurstCost = 4;
    public const int GrenadeCost = 3;
    public const int HealCost = 2;
    public const int DefendCost = 1;
    public const int FleeCost = 3;
    public const int LootCost = 1;

    public const int GrenadeRange = 6;
    public const int GrenadeRadius = 2;
    public const int RangedRange = 12;

    public static int ApMax(Player p) =>
        Math.Min(MaxAp, BaseAp + PerkEffects.ApBonus(p));
}

// ============================================================
//  DTO запросов/ответов
// ============================================================

public class RaidStartDto
{
    public int Tier { get; set; }
    public string LocationId { get; set; } = "";
}

public class RaidActionResult
{
    public RaidState? Raid { get; set; }
    public CombatStateDto? State { get; set; }
    public Player? Player { get; set; }
    public List<Item> DroppedLoot { get; set; } = new();
    public int DroppedMoney { get; set; }
    public int DroppedXp { get; set; }
    public int LevelsGained { get; set; }
    public bool CombatStarted { get; set; }
    public bool CombatFinished { get; set; }
    public bool RaidFinished { get; set; }
    public string? Message { get; set; }
}
