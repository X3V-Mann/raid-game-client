using GameApi.Models;
using GameApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace GameApi.Controllers;

[ApiController]
[Route("api/raid")]
public class RaidController : ControllerBase
{
    private const int MapWidth = 20;
    private const int MapHeight = 15;
    private const int MutantVisionRadius = 3;
    private const int MutantFleeCooldown = 6;

    private readonly LootService _loot;
    private readonly GameStateService _state;
    private readonly ItemCatalog _catalog;
    private readonly LocationCatalog _locations;
    private readonly Random _rng = new();

    private static readonly string[] _enemySprites =
    {
        "bandit", "bomzh", "dikiy", "maroder", "naemnik", "odinochka", "otmorozok",
        "raider", "serzhant", "snaiper", "specnaz", "voenny",
    };

    private static readonly string[] _mutantNames = { "Пёс", "Плоть", "Кабан", "Мутант" };

    private static bool IsMutantTemplate(EnemyTemplate t) =>
        _mutantNames.Contains(t.Name) ||
        t.Icon.Contains("pes.png", StringComparison.OrdinalIgnoreCase) ||
        t.Icon.Contains("flesh.png", StringComparison.OrdinalIgnoreCase) ||
        t.Icon.Contains("boar.png", StringComparison.OrdinalIgnoreCase);

    private static readonly (int Dx, int Dy)[] Dirs8 =
    {
        (-1, -1), (0, -1), (1, -1),
        (-1, 0),           (1, 0),
        (-1, 1),  (0, 1),  (1, 1),
    };

    public RaidController(LootService loot, GameStateService state, ItemCatalog catalog, LocationCatalog locations)
        => (_loot, _state, _catalog, _locations) = (loot, state, catalog, locations);

    // ============================================================
    //  ЛОКАЦИИ / СОСТОЯНИЕ РЕЙДА
    // ============================================================

    [HttpGet("locations")]
    public object GetLocations()
    {
        _state.EnsureQuestBasedLocationUnlocks();
        var p = _state.Player;
        return _locations.All.Select(l => new
        {
            l.Id,
            l.Name,
            l.Tier,
            l.Description,
            l.Image,
            l.Icon,
            l.BossChance,
            unlocked = _locations.IsUnlocked(p, l),
            unlockHint = l.UnlockHint,
        }).ToList();
    }

    [HttpGet("state")]
    public object GetState()
    {
        var raid = _state.Raid;
        if (raid == null || raid.Finished)
            return new { active = false, player = _state.Player };

        _state.CurrentCombat = raid.Combat;
        return new { active = true, raid, player = _state.Player };
    }

    [HttpPost("start")]
    public ActionResult<RaidActionResult> Start([FromBody] RaidStartDto dto)
    {
        var player = _state.Player;
        if (player.Hp <= 0)
            return BadRequest("Ты без сил. Отдохни в убежище или подлечись.");

        // Активный рейд не пересоздаём — клиент должен продолжить его.
        if (_state.Raid is { Finished: false })
        {
            return new RaidActionResult
            {
                Raid = _state.Raid,
                State = _state.Raid.Combat,
                Player = player,
                Message = "Активный рейд восстановлен.",
            };
        }

        var loc = _locations.Get(dto.LocationId) ?? _locations.All[0];
        if (!_locations.IsUnlocked(player, loc))
            return BadRequest($"Локация закрыта. {loc.UnlockHint}");

        // Предметы, взятые в рейд, теряют статус «из рейда».
        ClearFoundInRaidStatus(player);

        int effectiveTier = Math.Min(loc.Tier, 1 + player.Level / 2);
        var raid = GenerateRaid(loc, effectiveTier);
        ApplyRaidProgress(raid, player, QuestType.VisitLocation);
        _state.Raid = raid;
        _state.CurrentCombat = null;
        _state.Persist();

        return new RaidActionResult { Raid = raid, Player = player };
    }

    public record MoveDto(int X, int Y);

    [HttpPost("move")]
    public ActionResult<RaidActionResult> Move([FromBody] MoveDto dto)
    {
        var raid = _state.Raid;
        var p = _state.Player;
        if (raid == null || raid.Finished) return BadRequest("Рейд не активен.");
        if (raid.Combat != null) return BadRequest("Сначала завершите бой.");

        int dx = Math.Abs(dto.X - raid.PlayerX);
        int dy = Math.Abs(dto.Y - raid.PlayerY);
        if (dx > 1 || dy > 1 || (dx == 0 && dy == 0))
            return BadRequest("Можно двигаться только на соседнюю клетку.");

        var tile = GetTile(raid, dto.X, dto.Y);
        if (tile == null) return BadRequest("За пределами карты.");
        if (!IsWalkable(tile)) return BadRequest("Эта клетка непроходима.");

        raid.PlayerX = dto.X;
        raid.PlayerY = dto.Y;
        raid.CellsVisited++;
        raid.AtExit = tile.Type == RaidTileTypes.Exit;

        // Мутанты на карте постепенно "остывают" после побега игрока.
        foreach (var m in raid.Mutants.Where(m => m.Cooldown > 0))
            m.Cooldown--;

        int armorSlowdown = (p.Helmet?.Slowdown ?? 0) + (p.Armor?.Slowdown ?? 0) + (p.Suit?.Slowdown ?? 0);
        int staminaCost = p.Overweight ? 4 : 2;
        staminaCost += armorSlowdown / 10; // тяжёлая броня тратит больше выносливости
        p.Stamina = Math.Max(0, p.Stamina - staminaCost);
        p.Hunger = Math.Max(0, p.Hunger - 1);
        p.Thirst = Math.Max(0, p.Thirst - 1);

        if (p.Stamina <= 0)
        {
            int dmg = Math.Max(1, 2 + effectiveTierSafe(raid) / 2);
            p.Hp = Math.Max(1, p.Hp - dmg);
            AddLog(raid, $"💀 Истощение: −{dmg} HP");
        }

        if (tile.Type == RaidTileTypes.Transition && raid.Sector < raid.MaxSectors)
        {
            var loc = _locations.Get(raid.LocationId);
            if (loc != null)
                AdvanceToNextSector(raid, loc);
            raid.AtExit = false;
            _state.Persist();
            return new RaidActionResult { Raid = raid, Player = p, Message = $"Переход в сектор {raid.Sector}." };
        }

        if (raid.AtExit)
        {
            AddLog(raid, "🚪 Ты достиг выхода из рейда.");
            return CompleteRaid(raid, survived: true, abandoned: false);
        }

        // Мутанты замечают игрока в радиусе 3 клеток, но только при прямой видимости:
        // здания, деревья и тайники перекрывают им обзор, поэтому их можно обойти.
        var watcher = FindMutantWatcher(raid);
        if (watcher != null)
        {
            AddLog(raid, $"👁 {watcher.Name} заметил тебя!");
            StartMutantCombat(raid, watcher);
            _state.CurrentCombat = raid.Combat;
            _state.Persist();
            return new RaidActionResult
            {
                Raid = raid,
                State = raid.Combat,
                Player = p,
                CombatStarted = true,
                Message = $"{watcher.Name} заметил тебя!",
            };
        }

        _state.Persist();
        return new RaidActionResult { Raid = raid, Player = p };
    }

    public record EnterBuildingDto(string BuildingId);
    public record LootBuildingDto(string BuildingId);

    [HttpPost("enter")]
    public ActionResult<RaidActionResult> EnterBuilding([FromBody] EnterBuildingDto dto)
    {
        var raid = _state.Raid;
        var p = _state.Player;
        if (raid == null || raid.Finished) return BadRequest("Рейд не активен.");
        if (raid.Combat != null) return BadRequest("Сначала завершите бой.");

        var b = raid.Buildings.FirstOrDefault(x => x.Id == dto.BuildingId);
        if (b == null) return NotFound("Здание не найдено.");
        if (!IsAdjacentToBuilding(raid, b))
            return BadRequest("Подойдите к зданию ближе.");

        b.Entered = true;
        ApplyRaidProgress(raid, p, QuestType.EnterBuilding, b.Kind);

        if (b.HasEnemies && !b.Cleared)
        {
            if (raid.Combat == null)
                StartBuildingCombat(raid, b);
            _state.CurrentCombat = raid.Combat;
            _state.Persist();
            return new RaidActionResult
            {
                Raid = raid,
                State = raid.Combat,
                Player = p,
                CombatStarted = true,
                Message = $"Внутри {b.Name} враги!",
            };
        }

        StartBuildingInterior(raid, b);
        _state.CurrentCombat = raid.Combat;
        _state.Persist();
        return new RaidActionResult
        {
            Raid = raid,
            State = raid.Combat,
            Player = p,
            CombatStarted = true,
            Message = $"Вы вошли в {b.Name}.",
        };
    }

    [HttpPost("loot-building")]
    public ActionResult<RaidActionResult> LootBuilding([FromBody] LootBuildingDto dto)
    {
        var raid = _state.Raid;
        var p = _state.Player;
        if (raid == null || raid.Finished) return BadRequest("Рейд не активен.");
        if (raid.Combat != null) return BadRequest("Сначала завершите бой.");

        var b = raid.Buildings.FirstOrDefault(x => x.Id == dto.BuildingId);
        if (b == null) return NotFound("Здание не найдено.");
        if (!IsAdjacentToBuilding(raid, b))
            return BadRequest("Подойдите к зданию ближе.");
        if (b.HasEnemies && !b.Cleared)
            return BadRequest("Сначала зачистите здание.");

        var taken = new List<Item>();
        if (!b.Looted)
        {
            foreach (var item in b.Loot.ToList())
            {
                item.FoundInRaid = true;
                if (InventoryHelper.AddToInventory(p, item))
                    taken.Add(item);
                else
                    AddLog(raid, "🎒 Инвентарь полон — часть добычи осталась.");
            }
            b.Loot.RemoveAll(i => taken.Any(t => t.Id == i.Id));

            if (b.Loot.Count == 0)
            {
                p.Money += b.Money;
                p.Xp += b.Xp;
                b.Money = 0;
                b.Xp = 0;
                b.Looted = true;
            }
        }

        PlayerProgression.Apply(p);
        _state.Persist();
        return new RaidActionResult
        {
            Raid = raid,
            Player = p,
            DroppedLoot = taken,
            Message = taken.Count > 0 ? "Добыча забрана." : "Забирать нечего.",
        };
    }

    [HttpPost("loot-building-stash")]
    public ActionResult<RaidActionResult> LootBuildingStash([FromBody] LootBuildingDto dto)
    {
        var raid = _state.Raid;
        var p = _state.Player;
        if (raid == null || raid.Finished) return BadRequest("Рейд не активен.");
        if (raid.Combat != null) return BadRequest("Сначала завершите бой.");

        var b = raid.Buildings.FirstOrDefault(x => x.Id == dto.BuildingId);
        if (b == null) return NotFound("Здание не найдено.");
        if (!IsAdjacentToBuilding(raid, b))
            return BadRequest("Подойдите к зданию ближе.");
        if (!b.HasStash || b.StashLooted)
            return BadRequest("Тайника здесь нет или он уже обыскан.");
        if (b.HasEnemies && !b.Cleared)
            return BadRequest("Сначала зачистите здание.");

        var taken = new List<Item>();
        foreach (var item in b.StashLoot.ToList())
        {
            item.FoundInRaid = true;
            if (InventoryHelper.AddToInventory(p, item))
                taken.Add(item);
            else
                AddLog(raid, "🎒 Инвентарь полон — часть добычи из тайника осталась.");
        }
        b.StashLoot.RemoveAll(i => taken.Any(t => t.Id == i.Id));

        if (b.StashLoot.Count == 0)
        {
            p.Money += b.StashMoney;
            p.Xp += b.StashXp;
            b.StashMoney = 0;
            b.StashXp = 0;
            b.StashLooted = true;
        }

        p.Stats.StashesUsed += 1;
        AddLog(raid, taken.Count > 0 ? $"🗄 Тайник в {b.Name} обыскан." : $"🗄 Тайник в {b.Name} пуст.");
        PlayerProgression.Apply(p);
        _state.Persist();
        return new RaidActionResult
        {
            Raid = raid,
            Player = p,
            DroppedLoot = taken,
            Message = taken.Count > 0 ? "Тайник обыскан — добыча забрана." : "Тайник пуст.",
        };
    }

    public record LootStashDto(int X, int Y);

    [HttpPost("loot-stash")]
    public ActionResult<RaidActionResult> LootStash([FromBody] LootStashDto dto)
    {
        var raid = _state.Raid;
        var p = _state.Player;
        if (raid == null || raid.Finished) return BadRequest("Рейд не активен.");
        if (raid.Combat != null) return BadRequest("Сначала завершите бой.");

        var stash = raid.Stashes.FirstOrDefault(s => s.X == dto.X && s.Y == dto.Y && !s.Looted);
        if (stash == null) return NotFound("Тайник не найден или уже обыскан.");

        // Проверка adjacency — игрок должен быть рядом
        int dx = Math.Abs(dto.X - raid.PlayerX);
        int dy = Math.Abs(dto.Y - raid.PlayerY);
        if (dx > 1 || dy > 1 || (dx == 0 && dy == 0))
            return BadRequest("Подойдите к тайнику ближе.");

        var taken = new List<Item>();
        foreach (var item in stash.Loot.ToList())
        {
            item.FoundInRaid = true;
            if (InventoryHelper.AddToInventory(p, item))
                taken.Add(item);
            else
                AddLog(raid, "🎒 Инвентарь полон — часть добычи осталась.");
        }
        stash.Loot.RemoveAll(i => taken.Any(t => t.Id == i.Id));

        if (stash.Loot.Count == 0)
        {
            p.Money += stash.Money;
            p.Xp += stash.Xp;
            stash.Money = 0;
            stash.Xp = 0;
            stash.Looted = true;
        }

        PlayerProgression.Apply(p);
        _state.Persist();
        return new RaidActionResult
        {
            Raid = raid,
            Player = p,
            DroppedLoot = taken,
            DroppedMoney = stash.Money,
            DroppedXp = stash.Xp,
            Message = taken.Count > 0 ? "Тайник обыскан." : "Тайник пуст.",
        };
    }

    public record LootCarDto(int X, int Y);

    [HttpPost("loot-car")]
    public ActionResult<RaidActionResult> LootCar([FromBody] LootCarDto dto)
    {
        var raid = _state.Raid;
        var p = _state.Player;
        if (raid == null || raid.Finished) return BadRequest("Рейд не активен.");
        if (raid.Combat != null) return BadRequest("Сначала завершите бой.");

        var car = raid.Cars.FirstOrDefault(c => c.X == dto.X && c.Y == dto.Y && !c.Looted);
        if (car == null) return NotFound("Автомобиль не найден или уже обыскан.");

        int dx = Math.Abs(dto.X - raid.PlayerX);
        int dy = Math.Abs(dto.Y - raid.PlayerY);
        if (dx > 1 || dy > 1 || (dx == 0 && dy == 0))
            return BadRequest("Подойдите к автомобилю ближе.");

        var taken = new List<Item>();
        foreach (var item in car.Loot.ToList())
        {
            item.FoundInRaid = true;
            if (InventoryHelper.AddToInventory(p, item))
                taken.Add(item);
            else
                AddLog(raid, "🎒 Инвентарь полон — часть добычи из автомобиля осталась.");
        }
        car.Loot.RemoveAll(i => taken.Any(t => t.Id == i.Id));

        if (car.Loot.Count == 0)
        {
            p.Money += car.Money;
            p.Xp += car.Xp;
            car.Money = 0;
            car.Xp = 0;
            car.Looted = true;
        }

        AddLog(raid, taken.Count > 0 ? $"🚗 {car.Name}: найдено ценное." : $"🚗 {car.Name}: пусто.");
        PlayerProgression.Apply(p);
        _state.Persist();
        return new RaidActionResult
        {
            Raid = raid,
            Player = p,
            DroppedLoot = taken,
            Message = taken.Count > 0 ? $"{car.Name} обыскан." : $"{car.Name} пуст.",
        };
    }

    public record LootFurnitureDto(string BuildingId, string FurnitureId);

    [HttpPost("loot-furniture")]
    public ActionResult<RaidActionResult> LootFurniture([FromBody] LootFurnitureDto dto)
    {
        var raid = _state.Raid;
        var p = _state.Player;
        if (raid == null || raid.Finished) return BadRequest("Рейд не активен.");
        if (raid.Combat != null) return BadRequest("Сначала завершите бой.");

        var b = raid.Buildings.FirstOrDefault(x => x.Id == dto.BuildingId);
        if (b == null) return NotFound("Здание не найдено.");
        if (!IsAdjacentToBuilding(raid, b))
            return BadRequest("Подойдите к зданию ближе.");
        if (b.HasEnemies && !b.Cleared)
            return BadRequest("Сначала зачистите здание.");

        var f = b.Furniture.FirstOrDefault(x => x.Id == dto.FurnitureId && !x.Looted);
        if (f == null) return NotFound("Мебель не найдена или уже обыскана.");

        var taken = new List<Item>();
        foreach (var item in f.Loot.ToList())
        {
            item.FoundInRaid = true;
            if (InventoryHelper.AddToInventory(p, item))
                taken.Add(item);
            else
                AddLog(raid, "🎒 Инвентарь полон — часть добычи осталась.");
        }
        f.Loot.RemoveAll(i => taken.Any(t => t.Id == i.Id));

        if (f.Loot.Count == 0)
        {
            p.Money += f.Money;
            p.Xp += f.Xp;
            f.Money = 0;
            f.Xp = 0;
            f.Looted = true;
        }

        AddLog(raid, taken.Count > 0 ? $"{f.Icon} {f.Name} обыскан(а)." : $"{f.Icon} {f.Name}: пусто.");
        PlayerProgression.Apply(p);
        _state.Persist();
        return new RaidActionResult
        {
            Raid = raid,
            Player = p,
            DroppedLoot = taken,
            Message = taken.Count > 0 ? $"{f.Name}: добыча забрана." : $"{f.Name}: пусто.",
        };
    }

    [HttpPost("exit")]
    public ActionResult<RaidActionResult> ExitRaid()
    {
        var raid = _state.Raid;
        if (raid == null || raid.Finished) return BadRequest("Рейд не активен.");
        if (raid.Combat != null) return BadRequest("Сначала завершите бой.");
        if (!raid.AtExit) return BadRequest("Выход находится в другой клетке.");

        return CompleteRaid(raid, survived: true, abandoned: false);
    }

    public record NpcTalkDto(string NpcId);
    public record NpcBuyDto(string NpcId, string Code, int Count = 1);
    public record NpcSellDto(string NpcId, string ItemId, int Count = 1);

    [HttpPost("npc/talk")]
    public ActionResult<RaidActionResult> TalkNpc([FromBody] NpcTalkDto dto)
    {
        var raid = _state.Raid;
        var p = _state.Player;
        if (raid == null || raid.Finished) return BadRequest("Рейд не активен.");
        if (raid.Combat != null) return BadRequest("Сначала завершите бой.");
        var npc = raid.Npcs.FirstOrDefault(n => n.Id == dto.NpcId);
        if (npc == null) return NotFound("NPC не найден.");
        if (Math.Abs(npc.X - raid.PlayerX) > 1 || Math.Abs(npc.Y - raid.PlayerY) > 1 || (npc.X == raid.PlayerX && npc.Y == raid.PlayerY))
            return BadRequest("Подойдите к персонажу ближе.");

        npc.Talked = true;
        var given = new List<Item>();

        bool questActive = IsNpcQuestActive(npc);
        bool questRelated = npc.Scripted && (!string.IsNullOrEmpty(npc.RequiresQuestId) || !string.IsNullOrEmpty(npc.GivesItemCode));

        // Выдача квестового предмета — только когда задание активно/нужно
        if (!npc.GaveItem && !string.IsNullOrEmpty(npc.GivesItemCode))
        {
            bool okToGive = false;
            if (string.IsNullOrEmpty(npc.RequiresQuestId))
            {
                okToGive = _state.AllQuests().Any(q => q.IsAccepted && !q.IsCompleted && !q.IsFailed && q.Type == QuestType.Fetch
                    && string.Equals(q.TargetItemCode, npc.GivesItemCode, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                okToGive = _state.AllQuests().Any(q => q.Id == npc.RequiresQuestId && q.IsAccepted && !q.IsCompleted && !q.IsFailed);
                if (!okToGive) okToGive = _state.AllQuests().Any(q => q.IsAccepted && !q.IsCompleted && !q.IsFailed && q.Type == QuestType.Fetch
                    && string.Equals(q.TargetItemCode, npc.GivesItemCode, StringComparison.OrdinalIgnoreCase));
            }

            if (okToGive)
            {
                var item = _catalog.Create(npc.GivesItemCode);
                item.FoundInRaid = true;
                if (InventoryHelper.AddToInventory(p, item))
                {
                    npc.GaveItem = true;
                    given.Add(item);
                    AddLog(raid, $"🤝 {npc.Name} передал: {item.Name}.");
                }
                else AddLog(raid, "🎒 Инвентарь полон — NPC не смог передать предмет.");
            }
        }

        // Прогресс Talk-квестов — только когда задание активно (для квестовых NPC)
        bool canProgressTalk = !questRelated || questActive;
        if (canProgressTalk)
        {
            foreach (var q in _state.AllQuests().Where(q => q.IsAccepted && !q.IsCompleted && !q.IsFailed && q.Type == QuestType.Talk))
            {
                if (!string.IsNullOrEmpty(q.TargetNpcName) && npc.Name.Contains(q.TargetNpcName, StringComparison.OrdinalIgnoreCase))
                    q.Progress = Math.Max(q.Progress, q.TargetCount <= 0 ? 1 : q.TargetCount);
                if (!string.IsNullOrEmpty(npc.RequiresQuestId) && string.Equals(q.Id, npc.RequiresQuestId, StringComparison.OrdinalIgnoreCase))
                    q.Progress = Math.Max(q.Progress, q.TargetCount <= 0 ? 1 : q.TargetCount);
            }
        }

        string reply;
        if (given.Count > 0)
            reply = $"Держи, это тебе пригодится. {npc.Name} передал: {given[0].Name}.";
        else if (questRelated && !questActive)
            reply = GenericNpcReply();
        else if (npc.Dialogue.Count > 0)
            reply = npc.Dialogue[_rng.Next(npc.Dialogue.Count)];
        else
            reply = GenericNpcReply();

        AddLog(raid, $"💬 {npc.Name}: {reply}");
        _state.Persist();
        return new RaidActionResult
        {
            Raid = raid,
            Player = p,
            DroppedLoot = given,
            Message = given.Count > 0 ? $"{npc.Name} передал предмет." : $"Разговор: {npc.Name}.",
            NpcReply = reply,
        };
    }

    private bool IsNpcQuestActive(RaidNpc npc)
    {
        if (!string.IsNullOrEmpty(npc.RequiresQuestId))
        {
            if (_state.AllQuests().Any(q => q.Id == npc.RequiresQuestId && q.IsAccepted && !q.IsCompleted && !q.IsFailed))
                return true;
        }
        if (!string.IsNullOrEmpty(npc.GivesItemCode))
        {
            if (_state.AllQuests().Any(q => q.IsAccepted && !q.IsCompleted && !q.IsFailed && q.Type == QuestType.Fetch
                && string.Equals(q.TargetItemCode, npc.GivesItemCode, StringComparison.OrdinalIgnoreCase)))
                return true;
        }
        // Talk-квесты по имени NPC
        if (npc.Scripted && _state.AllQuests().Any(q => q.IsAccepted && !q.IsCompleted && !q.IsFailed && q.Type == QuestType.Talk
            && !string.IsNullOrEmpty(q.TargetNpcName) && npc.Name.Contains(q.TargetNpcName, StringComparison.OrdinalIgnoreCase)))
            return true;
        return false;
    }

    private string GenericNpcReply()
    {
        string[] replies = {
            "Да как все... выживаем понемногу.",
            "Тихо пока. Не хотелось бы шума лишнего.",
            "Осматриваюсь, хожу тут. Бывай осторожнее.",
            "Бывало и хуже. Главное — патроны целы и голова на месте.",
            "Ничего нового. Зона как Зона.",
        };
        return replies[_rng.Next(replies.Length)];
    }

    [HttpPost("npc/buy")]
    public ActionResult<RaidActionResult> BuyFromNpc([FromBody] NpcBuyDto dto)
    {
        var raid = _state.Raid;
        var p = _state.Player;
        if (raid == null || raid.Finished) return BadRequest("Рейд не активен.");
        if (raid.Combat != null) return BadRequest("Сначала завершите бой.");
        var npc = raid.Npcs.FirstOrDefault(n => n.Id == dto.NpcId);
        if (npc == null) return NotFound("NPC не найден.");
        if (!npc.CanTrade) return BadRequest("Этот персонаж не торгует.");
        if (Math.Abs(npc.X - raid.PlayerX) > 1 || Math.Abs(npc.Y - raid.PlayerY) > 1) return BadRequest("Подойдите ближе.");
        if (string.IsNullOrWhiteSpace(dto.Code) || !npc.TradeCodes.Contains(dto.Code, StringComparer.OrdinalIgnoreCase))
            return BadRequest("У NPC нет такого товара.");
        int count = Math.Clamp(dto.Count, 1, 10);
        var tmpl = _catalog.Get(dto.Code);
        if (tmpl == null) return BadRequest("Предмет не найден.");
        int cost = Math.Max(1, tmpl.Price) * count;
        if (p.Money < cost) return BadRequest($"Нужно {cost} ₽");
        var taken = new List<Item>();
        for (int i = 0; i < count; i++)
        {
            var it = _catalog.Create(dto.Code); it.FoundInRaid = false;
            if (InventoryHelper.AddToInventory(p, it)) taken.Add(it); else break;
        }
        if (taken.Count == 0) return BadRequest("Инвентарь полон.");
        int paid = Math.Max(1, tmpl.Price) * taken.Count;
        p.Money -= paid; p.Stats.MoneySpent += paid;
        AddLog(raid, $"🤝 Куплено у {npc.Name}: {tmpl.Name} ×{taken.Count} (-{paid} ₽).");
        _state.Persist();
        return new RaidActionResult { Raid = raid, Player = p, DroppedLoot = taken, Message = "Сделка завершена." };
    }

    [HttpPost("npc/sell")]
    public ActionResult<RaidActionResult> SellToNpc([FromBody] NpcSellDto dto)
    {
        var raid = _state.Raid;
        var p = _state.Player;
        if (raid == null || raid.Finished) return BadRequest("Рейд не активен.");
        if (raid.Combat != null) return BadRequest("Сначала завершите бой.");
        var npc = raid.Npcs.FirstOrDefault(n => n.Id == dto.NpcId);
        if (npc == null) return NotFound("NPC не найден.");
        if (!npc.CanTrade) return BadRequest("Этот персонаж не торгует.");
        if (Math.Abs(npc.X - raid.PlayerX) > 1 || Math.Abs(npc.Y - raid.PlayerY) > 1) return BadRequest("Подойдите ближе.");
        if (string.IsNullOrWhiteSpace(dto.ItemId)) return BadRequest("Предмет не выбран.");

        var item = p.Inventory.FirstOrDefault(i => i.Id == dto.ItemId);
        if (item == null) return NotFound("Предмет не найден в инвентаре.");
        bool isQuestItem = item.Code.StartsWith("quest_", StringComparison.OrdinalIgnoreCase)
            || item.Code.StartsWith("q_", StringComparison.OrdinalIgnoreCase)
            || item.Code.StartsWith("boss_", StringComparison.OrdinalIgnoreCase)
            || item.Code.StartsWith("art_", StringComparison.OrdinalIgnoreCase);
        if (isQuestItem) return BadRequest("Квестовые предметы нельзя продать.");

        int count = Math.Clamp(dto.Count, 1, Math.Max(1, item.Count));
        int unitPrice = Math.Max(1, (int)Math.Floor(item.Price * 0.4));
        int gained = unitPrice * count;

        InventoryHelper.RemoveFromInventory(p, item.Id, count);
        p.Money += gained;
        p.Stats.MoneyEarned += gained;

        AddLog(raid, $"💰 Продано {npc.Name}: {item.Name} ×{count} (+{gained} ₽).");
        _state.Persist();
        return new RaidActionResult { Raid = raid, Player = p, Message = $"Продано: {item.Name} ×{count}." };
    }

    [HttpPost("abandon")]
    public ActionResult<RaidActionResult> AbandonRaid()
    {
        var raid = _state.Raid;
        if (raid == null || raid.Finished) return BadRequest("Рейд не активен.");
        if (raid.Combat != null) return BadRequest("Сначала завершите бой.");

        _state.Player.Hp = Math.Max(1, _state.Player.Hp - 5);
        return CompleteRaid(raid, survived: true, abandoned: true);
    }

    private ActionResult<RaidActionResult> CompleteRaid(RaidState raid, bool survived, bool abandoned)
    {
        var p = _state.Player;
        p.Stats.CellsPassed += raid.CellsVisited;
        if (survived) p.Stats.RaidsSurvived += 1;

        if (survived)
        {
            ApplyRaidProgress(raid, p, QuestType.Survive);
            if (!abandoned)
                ApplyRaidProgress(raid, p, QuestType.ExtractAlive);
        }

        _state.Raid = null;
        _state.CurrentCombat = null;
        _state.Persist();

        return new RaidActionResult
        {
            Player = p,
            RaidFinished = true,
            Message = abandoned ? "Ты покинул рейд." : "Ты вернулся в убежище.",
        };
    }

    // ============================================================
    //  БОЙ
    // ============================================================

    public record CombatActionDto(
        string Action,
        string? EnemyId = null,
        string? ItemId = null,
        int? TargetX = null,
        int? TargetY = null);

    public record LootCorpseDto(string EnemyId, string? ItemId = null);

    [HttpPost("combat/action")]
    public ActionResult<RaidActionResult> CombatAction([FromBody] CombatActionDto dto)
    {
        var raid = _state.Raid;
        var p = _state.Player;
        var c = raid?.Combat;
        if (raid == null || c == null || c.Finished)
            return BadRequest("Бой не активен.");
        bool interior = string.Equals(c.Mode, CombatModes.Interior, StringComparison.OrdinalIgnoreCase);
        if (!c.IsPlayerTurn && !interior)
            return BadRequest("Сейчас ход противников.");

        string action = (dto.Action ?? "").ToLowerInvariant();
        switch (action)
        {
            case "move":
            {
                if (!interior && c.PlayerAp < CombatRules.MoveCost)
                    return BadRequest("Не хватает AP.");
                if (dto.TargetX is not int tx || dto.TargetY is not int ty)
                    return BadRequest("Не указана клетка.");
                if (!CanEnterCombatCell(c, tx, ty))
                    return BadRequest("Клетка занята или непроходима.");
                int dx = Math.Abs(tx - c.PlayerX);
                int dy = Math.Abs(ty - c.PlayerY);
                if (dx > 1 || dy > 1 || (dx == 0 && dy == 0))
                    return BadRequest("Можно двигаться только по диагонали или в сторону.");
                c.PlayerX = tx;
                c.PlayerY = ty;
                if (!interior) c.PlayerAp -= CombatRules.MoveCost;
                if (interior)
                {
                    var exitObj = c.Objects.FirstOrDefault(o => o.Kind == CombatObjectKinds.Exit && o.X == c.PlayerX && o.Y == c.PlayerY);
                    if (exitObj != null)
                    {
                        c.Finished = true;
                        c.PlayerWon = true;
                        raid.Combat = null;
                        _state.CurrentCombat = null;
                        _state.Persist();
                        return new RaidActionResult { Raid = raid, Player = p, Message = $"Вы вышли из {c.Title}." };
                    }
                }
                break;
            }

            case "interact":
            case "search":
            {
                if (!interior) return BadRequest("Здесь нечего обыскать.");
                if (dto.TargetX is not int ix || dto.TargetY is not int iy)
                    return BadRequest("Не указана цель.");
                var obj = c.Objects.FirstOrDefault(o => o.X == ix && o.Y == iy);
                if (obj == null) return BadRequest("Здесь ничего нет.");
                int cdx = Math.Abs(ix - c.PlayerX);
                int cdy = Math.Abs(iy - c.PlayerY);
                if (cdx > 1 || cdy > 1)
                    return BadRequest("Подойдите ближе.");
                var (ok, msg, loot) = ResolveInteriorInteract(raid, c, p, obj);
                if (!ok) return BadRequest(msg);
                _state.CurrentCombat = c;
                _state.Persist();
                return new RaidActionResult { Raid = raid, State = c, Player = p, DroppedLoot = loot, Message = msg };
            }

            case "leave":
            case "close":
                if (!interior) return BadRequest("Сейчас нельзя выйти так.");
                c.Finished = true;
                c.PlayerWon = true;
                raid.Combat = null;
                _state.CurrentCombat = null;
                _state.Persist();
                return new RaidActionResult { Raid = raid, Player = p, Message = $"Вы вышли из {c.Title}." };

            case "attack":
            case "aim":
            case "burst":
                if (ResolveAttack(raid, c, p, action, dto.EnemyId) is { } err)
                    return BadRequest(err);
                break;

            case "defend":
            {
                if (c.PlayerAp < CombatRules.DefendCost) return BadRequest("Не хватает AP.");
                c.PlayerAp -= CombatRules.DefendCost;
                c.IsDefending = true;
                c.CoverTurns = 1;
                AddLog(c, "🛡 Ты приготовился к удару — входящий урон снижен.");
                break;
            }

            case "item":
            {
                if (c.PlayerAp < CombatRules.HealCost) return BadRequest("Не хватает AP.");
                if (ResolveCombatItem(c, p, dto.ItemId) is { } itemErr) return BadRequest(itemErr);
                c.PlayerAp -= CombatRules.HealCost;
                break;
            }

            case "grenade":
            {
                if (c.PlayerAp < CombatRules.GrenadeCost) return BadRequest("Не хватает AP.");
                if (ResolveGrenade(raid, c, p, dto.ItemId, dto.TargetX, dto.TargetY) is { } grenadeErr)
                    return BadRequest(grenadeErr);
                c.PlayerAp -= CombatRules.GrenadeCost;
                break;
            }

            case "flee":
            {
                if (c.PlayerAp < CombatRules.FleeCost) return BadRequest("Не хватает AP.");
                c.PlayerAp -= CombatRules.FleeCost;
                double chance = 0.55 - (p.Overweight ? 0.15 : 0);
                if (_rng.NextDouble() < chance)
                {
                    c.Finished = true;
                    c.PlayerFled = true;
                    AddLog(c, "🏃 Ты вырвался из боя.");
                    FinishCombat(raid, c, p, fled: true);
                    return new RaidActionResult { Raid = raid, State = c, Player = p, CombatFinished = true };
                }
                AddLog(c, "❌ Побег не удался.");
                break;
            }

            case "end":
            {
                c.PlayerAp = 0;
                AddLog(c, "⏭ Ход завершён.");
                break;
            }

            default:
                return BadRequest("Неизвестное действие.");
        }

        if (interior)
        {
            c.IsPlayerTurn = true;
            c.PlayerAp = c.PlayerApMax;
            _state.CurrentCombat = c;
            _state.Persist();
            return new RaidActionResult { Raid = raid, State = c, Player = p };
        }

        if (c.Enemies.All(e => e.Hp <= 0))
        {
            FinishCombat(raid, c, p, fled: false);
            return new RaidActionResult
            {
                Raid = raid,
                State = c,
                Player = p,
                CombatFinished = true,
                Message = "Победа!",
            };
        }

        // Пока есть AP, игрок продолжает действовать. Ход переходит врагам,
        // когда AP закончились или игрок завершил ход вручную.
        if (c.PlayerAp <= 0)
            BeginEnemyPhase(c);

        _state.CurrentCombat = c;
        _state.Persist();

        return new RaidActionResult { Raid = raid, State = c, Player = p };
    }

    private static void BeginEnemyPhase(CombatStateDto c)
    {
        c.IsPlayerTurn = false;
        c.PendingEnemyIds = c.Enemies
            .Where(e => e.Hp > 0)
            .OrderBy(e => Chebyshev(e.X, e.Y, c.PlayerX, c.PlayerY))
            .Select(e => e.Id)
            .ToList();
    }

    /// <summary>Обрабатывает ход одного противника. Клиент вызывает подряд с задержкой.</summary>
    [HttpPost("combat/enemy")]
    public ActionResult<RaidActionResult> CombatEnemyTurn()
    {
        var raid = _state.Raid;
        var p = _state.Player;
        var c = raid?.Combat;
        if (raid == null || c == null || c.Finished)
            return BadRequest("Бой не активен.");
        if (string.Equals(c.Mode, CombatModes.Interior, StringComparison.OrdinalIgnoreCase))
        {
            c.IsPlayerTurn = true;
            c.PlayerAp = c.PlayerApMax;
            c.PendingEnemyIds.Clear();
            _state.CurrentCombat = c;
            _state.Persist();
            return new RaidActionResult { Raid = raid, State = c, Player = p };
        }

        if (c.PendingEnemyIds.Count == 0)
        {
            c.IsPlayerTurn = true;
            c.PlayerAp = c.PlayerApMax;
            c.IsDefending = false;
            _state.Persist();
            return new RaidActionResult { Raid = raid, State = c, Player = p };
        }

        string enemyId = c.PendingEnemyIds[0];
        c.PendingEnemyIds.RemoveAt(0);
        var enemy = c.Enemies.FirstOrDefault(e => e.Id == enemyId && e.Hp > 0);

        if (enemy != null)
            RunEnemyTurn(raid, c, p, enemy);

        if (c.PlayerHp <= 0)
        {
            c.PlayerHp = 0;
            c.Finished = true;
            c.PlayerWon = false;
            c.IsPlayerTurn = true;
            AddLog(c, "☠️ Ты погиб.");
            _state.KillPlayer();
            return new RaidActionResult
            {
                State = c,
                Player = _state.Player,
                CombatFinished = true,
                Message = "Ты погиб.",
            };
        }

        if (c.PendingEnemyIds.Count == 0)
        {
            c.IsPlayerTurn = true;
            c.PlayerAp = c.PlayerApMax;
            c.IsDefending = false;
        }

        p.Hp = c.PlayerHp;
        _state.CurrentCombat = c;
        _state.Persist();

        return new RaidActionResult { Raid = raid, State = c, Player = p };
    }

    [HttpPost("combat/loot")]
    public ActionResult<RaidActionResult> LootCorpse([FromBody] LootCorpseDto dto)
    {
        var raid = _state.Raid;
        var p = _state.Player;
        var c = raid?.Combat;
        if (raid == null || c == null) return BadRequest("Бой не активен.");

        var enemy = c.Enemies.FirstOrDefault(e => e.Id == dto.EnemyId && e.Hp <= 0);
        if (enemy == null) return NotFound("Труп не найден.");
        if (enemy.Looted) return BadRequest("Этот труп уже обыскан.");
        if (!c.Finished && Chebyshev(enemy.X, enemy.Y, c.PlayerX, c.PlayerY) > 1)
            return BadRequest("Подойдите к трупу ближе.");
        if (c.PlayerAp < CombatRules.LootCost && !c.Finished)
            return BadRequest("Не хватает AP.");

        var taken = new List<Item>();
        if (string.IsNullOrEmpty(dto.ItemId))
        {
            foreach (var item in enemy.Loot.ToList())
            {
                item.FoundInRaid = true;
                if (InventoryHelper.AddToInventory(p, item))
                    taken.Add(item);
                else
                    AddLog(c, "🎒 Инвентарь полон — часть добычи осталась.");
            }
            enemy.Loot.RemoveAll(i => taken.Any(t => t.Id == i.Id));

            if (enemy.Loot.Count == 0)
            {
                p.Money += enemy.Money;
                enemy.Money = 0;
                enemy.Looted = true;
            }
        }
        else
        {
            var item = enemy.Loot.FirstOrDefault(i => i.Id == dto.ItemId);
            if (item == null) return NotFound("Предмет не найден.");
            if (!InventoryHelper.AddToInventory(p, item))
                return BadRequest("Инвентарь полон.");
            taken.Add(item);
            enemy.Loot.Remove(item);
            if (enemy.Loot.Count == 0 && enemy.Money <= 0) enemy.Looted = true;
        }

        if (!c.Finished) c.PlayerAp -= CombatRules.LootCost;
        AddLog(c, taken.Count > 0 ? $"🎒 Обыскал {enemy.Name}: {taken.Count} предм." : "🎒 Труп пуст.");
        c.LastEvent = taken.Count > 0 ? $"Обыск: {enemy.Name}" : "Труп пуст";

        p.Stats.StashesUsed += 0;
        _state.CurrentCombat = c;
        _state.Persist();
        return new RaidActionResult { Raid = raid, State = c, Player = p, DroppedLoot = taken };
    }

    [HttpPost("combat/close")]
    public ActionResult<RaidActionResult> CloseCombat()
    {
        var raid = _state.Raid;
        var c = raid?.Combat;
        if (raid == null) return BadRequest("Рейд не активен.");
        if (c == null)
        {
            _state.CurrentCombat = null;
            _state.Persist();
            return new RaidActionResult { Raid = raid, Player = _state.Player };
        }
        bool interior = string.Equals(c.Mode, CombatModes.Interior, StringComparison.OrdinalIgnoreCase);
        if (!c.Finished && !interior) return BadRequest("Бой ещё не завершён.");

        var building = raid.Buildings.FirstOrDefault(b => b.Id == c.BuildingId);
        if (interior)
        {
            c.Finished = true;
            c.PlayerWon = true;
        }
        else if (c.PlayerWon)
        {
            if (building != null) building.Cleared = true;
        }
        else if (c.PlayerFled)
        {
            raid.PlayerX = raid.CombatReturnX;
            raid.PlayerY = raid.CombatReturnY;
            raid.AtExit = GetTile(raid, raid.PlayerX, raid.PlayerY)?.Type == RaidTileTypes.Exit;
        }

        raid.Combat = null;
        _state.CurrentCombat = null;
        _state.Persist();
        return new RaidActionResult { Raid = raid, Player = _state.Player };
    }

    // ============================================================
    //  АТАКИ ИГРОКА
    // ============================================================

    private string? ResolveAttack(RaidState raid, CombatStateDto c, Player p, string action, string? enemyId)
    {
        var target = c.Enemies.FirstOrDefault(e => e.Hp > 0 && e.Id == enemyId)
                     ?? c.Enemies.FirstOrDefault(e => e.Hp > 0);
        if (target == null) return "Нет цели.";

        int cost = action switch
        {
            "aim" => CombatRules.AimedCost,
            "burst" => CombatRules.BurstCost,
            _ => CombatRules.AttackCost,
        };
        // Бонус AP за ловкость.
        int agilityApBonus = p.AgilityApBonus;
        if (c.PlayerAp < cost) return "Не хватает AP.";

        int dist = Chebyshev(c.PlayerX, c.PlayerY, target.X, target.Y);
        var ranged = p.WeaponRanged;
        var melee = p.WeaponMelee;
        bool hasAmmo = ranged != null && CountAmmo(p, ranged.Caliber) > 0;
        bool forceRanged = action is "aim" or "burst";
        bool useRanged = forceRanged || (hasAmmo && (dist > 1 || melee == null));

        // Проверка навыка владения оружием.
        if (useRanged && ranged != null && ranged.RequiredSkill > p.RangedSkill)
            return $"Недостаточно навыка «Дальний бой» для {ranged.Name} (требуется {ranged.RequiredSkill}, у вас {p.RangedSkill}).";
        if (!useRanged && melee != null && melee.RequiredSkill > p.MeleeSkill)
            return $"Недостаточно навыка «Ближний бой» для {melee.Name} (требуется {melee.RequiredSkill}, у вас {p.MeleeSkill}).";

        if (useRanged)
        {
            if (ranged == null) return "Нет огнестрельного оружия.";
            if (!hasAmmo) return $"Нет патронов под {ranged.Name} ({ranged.Caliber}).";
            if (dist > ranged.RangeMax) return "Цель слишком далеко.";
            if (dist > ranged.RangeEffective && _rng.NextDouble() < 0.30)
                return "Цель за пределами эффективной дальности — промах!";
            if (!HasLineOfSight(c, c.PlayerX, c.PlayerY, target.X, target.Y))
                return "Цель за укрытием — нет линии видимости.";
        }
        else
        {
            if (dist > 1) return "Слишком далеко для ближнего боя.";
            if (melee == null && ranged == null) { /* кулаки */ }
        }

        int shots = action == "burst" ? 3 : 1;
        int ammoPerShot = useRanged ? WeaponCatalog.AmmoPerShot(ranged) : 1;
        if (useRanged && CountAmmo(p, ranged!.Caliber) < ammoPerShot * shots)
            return "Не хватает патронов.";
        if (useRanged && !ConsumeAmmo(p, ranged!.Caliber, shots * ammoPerShot))
            return "Не хватает патронов.";

        c.PlayerAp -= cost;
        int weaponDamage = useRanged ? ranged!.Damage : (melee?.Damage ?? 5);
        int skillBonus = useRanged ? p.RangedDamageBonus : p.MeleeDamageBonus;
        int baseDamage = Math.Max(1, weaponDamage + skillBonus + PerkEffects.DamageBonus(p));

        int hitCount = 0;
        for (int i = 0; i < shots; i++)
        {
            if (target.Hp <= 0)
            {
                target = c.Enemies.FirstOrDefault(e => e.Hp > 0);
                if (target == null) break;
            }

            double missChance = useRanged
                ? (action == "burst" ? 0.22 : action == "aim" ? 0.03 : 0.10 + dist * 0.02)
                : 0.05;
            // Меткость оружия и навыка снижают шанс промаха.
            missChance -= (useRanged ? ranged?.Accuracy ?? 0 : melee?.Accuracy ?? 0) * 0.001;
            missChance -= p.AccuracyBonus;
            missChance -= (p.WeaponHandling - 1) * 0.012;
            if (p.Overweight) missChance += 0.05;
            missChance = Math.Clamp(missChance, 0, 0.95);
            if (_rng.NextDouble() < missChance)
            {
                AddLog(c, $"💨 {target.Name}: промах!");
                continue;
            }

            double dmg = action == "aim" ? baseDamage * 1.5
                : action == "burst" ? baseDamage * 0.55
                : baseDamage;

            if (useRanged)
            {
                double cover = CoverReductionAt(c, target.X, target.Y, null);
                dmg *= 1.0 - cover;
            }

            dmg = Math.Max(1, dmg - target.Defense);
            if (action != "burst" && _rng.NextDouble() < 0.10)
            {
                dmg *= 1.6;
                AddLog(c, "💥 КРИТ!");
            }

            int final = Math.Max(1, (int)Math.Round(dmg));
            target.Hp = Math.Max(0, target.Hp - final);
            hitCount++;
            AddLog(c, $"⚔️ {target.Name}: −{final} HP");
            if (target.Hp <= 0) OnEnemyKilled(raid, c, p, target);
        }

        if (action == "aim" && hitCount > 0) AddLog(c, "🎯 Прицельный выстрел.");
        if (action == "burst") AddLog(c, "🔥 Очередь.");
        return null;
    }

    private string? ResolveCombatItem(CombatStateDto c, Player p, string? itemId)
    {
        var item = !string.IsNullOrEmpty(itemId)
            ? p.Inventory.FirstOrDefault(i => i.Id == itemId)
            : p.Inventory.FirstOrDefault(i => i.Restores == "hp");

        if (item == null) return "Нет подходящего предмета.";
        if (item.IsGrenade) return "Гранату нужно бросить в клетку.";
        if (item.Type is not (ItemType.Food or ItemType.Drink or ItemType.Medicine))
            return "Это нельзя использовать в бою.";
        if (string.IsNullOrWhiteSpace(item.Restores))
            return "Это нельзя использовать в бою.";

        switch (item.Restores)
        {
            case "hp":
                if (c.PlayerHp >= c.PlayerMaxHp) return "Здоровье уже полное.";
                int heal = Math.Min(
                    Math.Max(1, (int)Math.Round(item.Restore * PerkEffects.HealMultiplier(p))),
                    c.PlayerMaxHp - c.PlayerHp);
                c.PlayerHp += heal;
                p.Hp = c.PlayerHp;
                p.Stats.HpHealed += heal;
                AddLog(c, $"🩹 {item.Name}: +{heal} HP");
                c.LastEvent = $"+{heal} HP";
                break;

            case "hunger":
            {
                bool canEat = c.PlayerHunger < 100;
                bool canHeal = item.RestoreHpBonus > 0 && c.PlayerHp < c.PlayerMaxHp;
                if (!canEat && !canHeal) return c.PlayerHunger >= 100 ? "Ты не голоден." : "Здоровье уже полное.";
                var parts = new List<string>();
                if (canEat)
                {
                    c.PlayerHunger = Math.Min(100, c.PlayerHunger + item.Restore);
                    p.Hunger = c.PlayerHunger;
                    p.Stats.FoodEaten += 1;
                    parts.Add($"+{item.Restore} сытости");
                }
                if (canHeal)
                {
                    int bonus = Math.Min(item.RestoreHpBonus, c.PlayerMaxHp - c.PlayerHp);
                    c.PlayerHp += bonus; p.Hp = c.PlayerHp; p.Stats.HpHealed += bonus;
                    parts.Add($"+{bonus} HP");
                }
                var msg = string.Join(", ", parts);
                AddLog(c, $"🍖 {item.Name}: {msg}");
                c.LastEvent = msg;
                break;
            }

            case "thirst":
            {
                bool canDrink = c.PlayerThirst < 100;
                bool canHeal = item.RestoreHpBonus > 0 && c.PlayerHp < c.PlayerMaxHp;
                if (!canDrink && !canHeal) return c.PlayerThirst >= 100 ? "Ты не хочешь пить." : "Здоровье уже полное.";
                var parts = new List<string>();
                if (canDrink)
                {
                    c.PlayerThirst = Math.Min(100, c.PlayerThirst + item.Restore);
                    p.Thirst = c.PlayerThirst;
                    parts.Add($"+{item.Restore} воды");
                }
                if (canHeal)
                {
                    int bonus = Math.Min(item.RestoreHpBonus, c.PlayerMaxHp - c.PlayerHp);
                    c.PlayerHp += bonus; p.Hp = c.PlayerHp; p.Stats.HpHealed += bonus;
                    parts.Add($"+{bonus} HP");
                }
                var msg = string.Join(", ", parts);
                AddLog(c, $"💧 {item.Name}: {msg}");
                c.LastEvent = msg;
                break;
            }

            default:
                return "Это нельзя использовать в бою.";
        }

        InventoryHelper.RemoveFromInventory(p, item.Id, 1);
        return null;
    }

    private string? ResolveGrenade(RaidState raid, CombatStateDto c, Player p, string? itemId, int? tx, int? ty)
    {
        var grenade = !string.IsNullOrEmpty(itemId)
            ? p.Inventory.FirstOrDefault(i => i.Id == itemId && i.IsGrenade)
            : p.Inventory.FirstOrDefault(i => i.IsGrenade);
        if (grenade == null) return "Нет гранат.";
        if (tx is not int x || ty is not int y) return "Укажите клетку броска.";
        if (x < 0 || y < 0 || x >= c.Width || y >= c.Height) return "Клетка за пределами поля.";
        if (Chebyshev(c.PlayerX, c.PlayerY, x, y) > CombatRules.GrenadeRange)
            return $"Слишком далеко (максимум {CombatRules.GrenadeRange} клетки).";

        bool isSmoke = grenade.Damage <= 0
            || (grenade.Code?.Contains("smoke", StringComparison.OrdinalIgnoreCase) ?? false)
            || (grenade.Code?.Contains("дым", StringComparison.OrdinalIgnoreCase) ?? false)
            || (grenade.Name?.Contains("Дымов", StringComparison.OrdinalIgnoreCase) ?? false)
            || (grenade.Name?.Contains("Smoke", StringComparison.OrdinalIgnoreCase) ?? false);

        int hit = 0;
        if (isSmoke)
        {
            foreach (var e in c.Enemies.Where(e => e.Hp > 0
                     && Chebyshev(e.X, e.Y, x, y) <= CombatRules.GrenadeRadius))
            {
                e.SkipTurns = Math.Max(e.SkipTurns, 2);
                hit++;
                AddLog(c, $"💨 {e.Name}: в дыму (пропустит 2 хода)");
            }
            InventoryHelper.RemoveFromInventory(p, grenade.Id, 1);
            AddLog(c, $"💨 {grenade.Name}: завеса дыма (задето {hit}).");
            c.LastEvent = hit > 0 ? $"Дым: {hit} цел." : "Дымовая завеса.";
            return null;
        }

        int blast = Math.Max(12, 20 + c.Tier * 5);
        foreach (var e in c.Enemies.Where(e => e.Hp > 0
                 && Chebyshev(e.X, e.Y, x, y) <= CombatRules.GrenadeRadius))
        {
            int dmg = Math.Max(1, blast - e.Defense);
            e.Hp = Math.Max(0, e.Hp - dmg);
            hit++;
            AddLog(c, $"💣 {e.Name}: −{dmg} HP");
            if (e.Hp <= 0) OnEnemyKilled(raid, c, p, e);
        }

        InventoryHelper.RemoveFromInventory(p, grenade.Id, 1);
        AddLog(c, $"💣 {grenade.Name}: взрыв (задето {hit}).");
        c.LastEvent = $"Взрыв: {hit} цел.";
        return null;
    }

    // ============================================================
    //  ХОД ПРОТИВНИКОВ
    // ============================================================

    private void RunEnemyTurn(RaidState raid, CombatStateDto c, Player p, Enemy enemy)
    {
        enemy.Ap = enemy.ApMax;
        c.LastAttackerId = null;

        if (enemy.SkipTurns > 0)
        {
            enemy.SkipTurns--;
            AddLog(c, $"💨 {enemy.Name} пропускает ход (дым).");
            c.LastEvent = $"{enemy.Name}: пропуск хода";
            return;
        }

        int dist = Chebyshev(enemy.X, enemy.Y, c.PlayerX, c.PlayerY);
        bool los = HasLineOfSight(c, enemy.X, enemy.Y, c.PlayerX, c.PlayerY);

        bool rangedReady = enemy.Ranged && los && dist <= enemy.Range;
        if (!rangedReady)
        {
            // Резервируем AP на атаку, чтобы подойти и ударить/выстрелить в тот же ход.
            int attackReserve = enemy.Ranged ? 2 : 1;
            int steps = Math.Max(0, enemy.Ap - attackReserve);
            var path = FindPath(c, enemy, c.PlayerX, c.PlayerY, allowTarget: enemy.Ranged);
            int taken = 0;
            foreach (var (nx, ny) in path)
            {
                if (taken >= steps) break;
                if (nx == c.PlayerX && ny == c.PlayerY) break;
                enemy.X = nx;
                enemy.Y = ny;
                taken++;
                dist = Chebyshev(enemy.X, enemy.Y, c.PlayerX, c.PlayerY);
                los = HasLineOfSight(c, enemy.X, enemy.Y, c.PlayerX, c.PlayerY);
                if (enemy.Ranged && los && dist <= enemy.Range) break;
                if (!enemy.Ranged && dist <= 1) break;
            }
            enemy.Ap -= taken;
        }

        dist = Chebyshev(enemy.X, enemy.Y, c.PlayerX, c.PlayerY);
        los = HasLineOfSight(c, enemy.X, enemy.Y, c.PlayerX, c.PlayerY);

        bool canAttack = enemy.Ranged
            ? los && dist <= enemy.Range && enemy.Ap >= 2
            : dist <= 1 && enemy.Ap >= 1;

        if (!canAttack) return;

        int playerDefense = (p.Helmet?.Defense ?? 0) + (p.Armor?.Defense ?? 0) + (p.Suit?.Defense ?? 0) + PerkEffects.DefenseBonus(p);
        int enemyHit = enemy.Damage + (enemy.Ranged ? 4 : 0);
        // Защита снижает урон, но не обнуляет его — враги должны ощущаться.
        int mitigated = enemyHit - (playerDefense * 2 / 3);
        int raw = Math.Max(Math.Max(2, enemyHit / 4), mitigated);

        // Процентная защита брони в зависимости от типа урона.
        int armorPct = enemy.Ranged
            ? (p.Armor?.BulletProtection ?? 0) + (p.Helmet?.BulletProtection ?? 0) + (p.Suit?.BulletProtection ?? 0)
            : (p.Armor?.PhysicalProtection ?? 0) + (p.Helmet?.PhysicalProtection ?? 0) + (p.Suit?.PhysicalProtection ?? 0);
        if (armorPct > 0)
            raw = Math.Max(1, raw * (100 - Math.Min(armorPct, 80)) / 100);

        raw = ApplyCoverToPlayer(c, p, raw, enemy.Ranged);
        if (c.IsDefending) raw = Math.Max(1, raw / 2);

        c.LastAttackerId = enemy.Id;

        double dodge = p.DodgeChance; // уже включает Agility + навык уклонения + перк
        if (dodge > 0 && _rng.NextDouble() < dodge)
        {
            AddLog(c, $"💨 {enemy.Name}: уклонение!");
            c.LastEvent = $"{enemy.Name}: промах";
            enemy.Ap -= 2;
            return;
        }

        if (p.ParryChance > 0 && _rng.NextDouble() < p.ParryChance)
        {
            raw = Math.Max(1, raw / 2);
            AddLog(c, "🛡 Парирование: урон снижен.");
        }

        c.LastAttackerId = enemy.Id;
        int before = c.PlayerHp;
        c.PlayerHp = Math.Max(0, c.PlayerHp - raw);
        p.Stats.HpDamaged += Math.Min(before, raw);
        p.Hp = c.PlayerHp;
        enemy.Ap -= 2;
        AddLog(c, $"💥 {enemy.Name}: −{raw} HP");
        c.LastEvent = $"{enemy.Name}: −{raw} HP";
    }

    private int ApplyCoverToPlayer(CombatStateDto c, Player p, int damage, bool ranged)
    {
        if (!ranged) return damage;
        double cover = CoverReductionAt(c, c.PlayerX, c.PlayerY, p);
        return Math.Max(1, (int)Math.Round(damage * (1.0 - cover)));
    }

    // ============================================================
    //  ПОБЕДА / КИЛЛЫ
    // ============================================================

    private bool IsMutantEnemy(Enemy enemy)
        => _mutantNames.Contains(enemy.Name) || enemy.Name.Contains("Мутант", StringComparison.OrdinalIgnoreCase);

    private List<Item> CreateMutantParts(Enemy enemy)
    {
        Item Part(string code, int count = 1)
        {
            var it = _catalog.Create(code);
            it.Count = count;
            it.FoundInRaid = true;
            return it;
        }
        if (enemy.Name.Contains("Пёс", StringComparison.OrdinalIgnoreCase))
            return new() { Part("dog_tail", 1) };
        if (enemy.Name.Contains("Плоть", StringComparison.OrdinalIgnoreCase))
            return new() { Part("flesh_eye", 1), Part("flesh_fat", _rng.Next(1, 3)) };
        if (enemy.Name.Contains("Кабан", StringComparison.OrdinalIgnoreCase))
            return new() { Part("boar_meat", _rng.Next(1, 3)), Part("boar_hoof", 1) };
        // Прочие мутанты
        var pool = new[] { "flesh_fat", "dog_tail", "boar_meat" };
        return new() { Part(pool[_rng.Next(pool.Length)], 1) };
    }

    private bool QuestTimeOk(Quest q, Player p)
    {
        bool isNight = GameStateService.IsNight(p.GameTimeMinutes);
        if (q.TimeOfDay == QuestTimeOfDay.Day && isNight) return false;
        if (q.TimeOfDay == QuestTimeOfDay.Night && !isNight) return false;
        return true;
    }

    private void ApplyRaidProgress(RaidState raid, Player p, QuestType type, string? buildingKind = null, int amount = 1)
    {
        foreach (var q in _state.AllQuests().Where(q => q.IsAccepted && !q.IsCompleted && !q.IsFailed && q.Type == type))
        {
            if (!QuestTimeOk(q, p)) continue;
            if (!string.IsNullOrEmpty(q.TargetLocationId) && !string.Equals(q.TargetLocationId, raid.LocationId, StringComparison.OrdinalIgnoreCase)) continue;
            if (type == QuestType.EnterBuilding && !string.IsNullOrEmpty(q.TargetBuildingKind)
                && !string.Equals(q.TargetBuildingKind, buildingKind, StringComparison.OrdinalIgnoreCase)) continue;
            q.Progress += amount;
        }
    }

    private void OnEnemyKilled(RaidState raid, CombatStateDto c, Player p, Enemy enemy)
    {
        AddLog(c, $"☠️ {enemy.Name} повержен.");
        p.Stats.EnemiesKilled += 1;
        raid.EnemiesKilled += 1;

        if (enemy.Loot.Count == 0 && enemy.Money == 0)
        {
            if (IsMutantEnemy(enemy))
            {
                enemy.Loot = CreateMutantParts(enemy);
                enemy.Money = 0;
            }
            else
            {
                int luckRolls = Math.Max(0, (p.Luck - 1) / 3);
                var loot = _loot.Generate(c.Tier, 1, enemy.IsBoss, PerkEffects.ExtraLootRolls(p) + luckRolls);
                enemy.Loot = loot.Items;
                enemy.Money = loot.Money;
            }
        }

        if (enemy.IsBoss)
            EnsureBossIntelDrop(enemy, raid.LocationId);


        bool isNight = GameStateService.IsNight(p.GameTimeMinutes);
        foreach (var q in _state.AllQuests().Where(q => q.IsAccepted && !q.IsCompleted && !q.IsFailed))
        {
            if (q.TimeOfDay == QuestTimeOfDay.Day && isNight) continue;
            if (q.TimeOfDay == QuestTimeOfDay.Night && !isNight) continue;
            if (!string.IsNullOrEmpty(q.TargetLocationId) && !string.Equals(q.TargetLocationId, raid.LocationId, StringComparison.OrdinalIgnoreCase)) continue;
            if (!string.IsNullOrEmpty(q.TargetEnemyName) && !enemy.Name.Contains(q.TargetEnemyName, StringComparison.OrdinalIgnoreCase)) continue;

            if (q.Type == QuestType.Kill || q.Type == QuestType.KillBoss)
            {
                if (q.TargetBoss && !enemy.IsBoss) continue;
                if (q.Type == QuestType.KillBoss && !enemy.IsBoss) continue;
                q.Progress += 1;
            }
        }
    }

    private void FinishCombat(RaidState raid, CombatStateDto c, Player p, bool fled)
    {
        c.Finished = true;
        c.IsPlayerTurn = true;
        if (!fled)
        {
            c.PlayerWon = true;
            c.PlayerAp = c.PlayerApMax;
            AddLog(c, "🏆 Победа!");
            var building = raid.Buildings.FirstOrDefault(b => b.Id == c.BuildingId);
            if (building != null) building.Cleared = true;
        }

        // Бой с мутантом на карте: побеждённые исчезают с карты,
        // а после побега мутант получает передышку и не бросается сразу.
        if (c.MutantId != null)
        {
            foreach (var enemy in c.Enemies)
            {
                var mutant = raid.Mutants.FirstOrDefault(m => m.Id == enemy.Id);
                if (mutant == null) continue;
                mutant.Hp = Math.Max(0, enemy.Hp);
                if (!fled && enemy.Hp <= 0) mutant.Defeated = true;
            }

            if (fled)
            {
                foreach (var mutant in raid.Mutants.Where(m =>
                             !m.Defeated && c.Enemies.Any(e => e.Id == m.Id)))
                    mutant.Cooldown = MutantFleeCooldown;
            }
        }

        p.Hp = Math.Max(0, c.PlayerHp);
        p.Stats.RaidsSurvived += 0; // выживание рейда считаем на выходе
        _state.CurrentCombat = c;
        _state.Persist();
    }

    // ============================================================
    //  ГЕНЕРАЦИЯ РЕЙДА
    // ============================================================

    private static int MaxSectorsFor(Location loc) => (loc.Tier >= 2 || loc.Id == "kordon") ? 2 : 1;

    private RaidState GenerateRaid(Location loc, int tier)
    {
        RaidState raid = new();
        int max = MaxSectorsFor(loc);
        for (int attempt = 0; attempt < 30; attempt++)
        {
            raid = GenerateRaidAttempt(loc, tier, sector: 1, maxSectors: max);
            if (IsRaidReachable(raid)) break;
            if (attempt == 29)
            {
                // Аварийный вариант: убираем деревья, чтобы карта точно была проходима.
                foreach (var t in raid.Tiles.Where(t => t.Type == RaidTileTypes.Tree))
                    t.Type = RaidTileTypes.Ground;
                raid = GenerateRaidAttempt(loc, tier, noTrees: true, sector: 1, maxSectors: max);
            }
        }
        InjectQuestItems(raid, loc);
        return raid;
    }

    private RaidState GenerateRaidAttempt(Location loc, int tier, bool noTrees = false, int sector = 1, int maxSectors = 1)
    {
        var raid = new RaidState
        {
            LocationId = loc.Id,
            LocationName = loc.Name,
            Tier = tier,
            Sector = sector,
            MaxSectors = maxSectors,
            Width = MapWidth,
            Height = MapHeight,
            StartedAt = DateTime.Now,
        };

        for (int y = 0; y < MapHeight; y++)
            for (int x = 0; x < MapWidth; x++)
                raid.Tiles.Add(new RaidTile { X = x, Y = y, Type = RaidTileTypes.Ground });

        // Игрок входит слева.
        raid.PlayerX = 0;
        raid.PlayerY = _rng.Next(1, MapHeight - 1);

        // Выходы справа — от 1 до 3 случайных.
        int exitCount = _rng.Next(1, 4);
        var usedExitY = new HashSet<int>();
        for (int i = 0; i < exitCount; i++)
        {
            int ey;
            do { ey = _rng.Next(1, MapHeight - 1); } while (usedExitY.Contains(ey));
            usedExitY.Add(ey);
            int ex = MapWidth - 1;
            var exitTile = GetTile(raid, ex, ey)!;
            exitTile.Type = RaidTileTypes.Exit;
            raid.Exits.Add((ex, ey));
            if (i == 0) { raid.ExitX = ex; raid.ExitY = ey; } // первый выход — основной для совместимости
        }

        // Здания.
        var plan = BuildPlan(loc);
        foreach (var theme in plan)
        {
            for (int tries = 0; tries < 160; tries++)
            {
                int bx = _rng.Next(1, MapWidth - theme.W);
                int by = _rng.Next(1, MapHeight - theme.H);
                if (bx + theme.W > MapWidth - 1) continue;
                if (by + theme.H > MapHeight - 1) continue;
                if (!AreaFree(raid, bx, by, theme.W, theme.H)) continue;

                var b = new RaidBuilding
                {
                    Name = theme.Name,
                    Kind = theme.Kind,
                    Icon = theme.Icon,
                    Description = theme.Description,
                    X = bx, Y = by, Width = theme.W, Height = theme.H,
                    Tier = tier,
                    HasEnemies = _rng.NextDouble() < EnemyChanceForBuilding(loc, theme.Kind),
                };

                var loot = _loot.Generate(tier, 1, false, PerkEffects.ExtraLootRolls(_state.Player));
                b.Loot = loot.Items;
                b.Money = loot.Money + _rng.Next(0, 20) * tier;
                b.Xp = loot.Xp;

                // Очень редкий тайник внутри здания.
                if (_rng.NextDouble() < 0.05)
                {
                    b.HasStash = true;
                    var stashLoot = _loot.Generate(Math.Min(7, tier + 1), 1, false, 1 + PerkEffects.ExtraLootRolls(_state.Player));
                    b.StashLoot = stashLoot.Items;
                    b.StashMoney = stashLoot.Money + _rng.Next(20, 80) * tier;
                    b.StashXp = stashLoot.Xp + 10 * tier;
                }

                GenerateBuildingFurniture(b, theme.Kind, tier);
                raid.Buildings.Add(b);
                for (int y = by; y < by + theme.H; y++)
                    for (int x = bx; x < bx + theme.W; x++)
                    {
                        var t = GetTile(raid, x, y)!;
                        t.Type = RaidTileTypes.Building;
                        t.BuildingId = b.Id;
                    }
                break;
            }
        }

        // Деревья / мусор.
        double treeChance = noTrees ? 0 : TreeChance(loc);
        if (treeChance > 0)
        {
            for (int y = 0; y < MapHeight; y++)
                for (int x = 0; x < MapWidth; x++)
                {
                    if (x == raid.PlayerX && y == raid.PlayerY) continue;
                    if (x == raid.ExitX && y == raid.ExitY) continue;
                    if (Math.Abs(x - raid.PlayerX) + Math.Abs(y - raid.PlayerY) <= 1) continue;
                    var t = GetTile(raid, x, y)!;
                    if (t.Type != RaidTileTypes.Ground) continue;
                    if (_rng.NextDouble() < treeChance) t.Type = RaidTileTypes.Tree;
                }
        }

        // Тайники — редко встречающиеся объекты с хорошим лутом.
        int stashCount = _rng.Next(0, 3); // 0-2 тайника
        if (tier >= 3) stashCount += _rng.Next(0, 2); // на высоких тайрах чаще
        for (int i = 0; i < stashCount; i++)
        {
            for (int tries = 0; tries < 100; tries++)
            {
                int sx = _rng.Next(1, MapWidth - 1);
                int sy = _rng.Next(1, MapHeight - 1);
                if (sx == raid.PlayerX && sy == raid.PlayerY) continue;
                if (sx == raid.ExitX && sy == raid.ExitY) continue;
                var t = GetTile(raid, sx, sy)!;
                if (t.Type != RaidTileTypes.Ground) continue;
                if (raid.Stashes.Any(s => s.X == sx && s.Y == sy)) continue;

                t.Type = RaidTileTypes.Stash;
                var loot = _loot.Generate(tier, 0, false, 0);
                var stash = new RaidStash
                {
                    X = sx,
                    Y = sy,
                    Loot = loot.Items,
                    Money = loot.Money,
                    Xp = loot.Xp,
                };
                raid.Stashes.Add(stash);
                raid.Log.Add($"🗄 Найден тайник в клетке ({sx},{sy}).");
                break;
            }
        }

        SpawnRaidCars(raid, loc, tier);
        SpawnRaidNpcs(raid, loc, tier);

        // Переход в следующий сектор локации (на некоторых локациях).
        if (raid.Sector < raid.MaxSectors)
        {
            for (int tries = 0; tries < 140; tries++)
            {
                int tx = _rng.Next(MapWidth / 2, MapWidth - 1);
                int ty = _rng.Next(1, MapHeight - 1);
                if (tx == raid.PlayerX && ty == raid.PlayerY) continue;
                if (raid.Exits.Any(e => e.X == tx && e.Y == ty)) continue;
                var tt = GetTile(raid, tx, ty);
                if (tt == null || tt.Type != RaidTileTypes.Ground) continue;
                tt.Type = RaidTileTypes.Transition;
                break;
            }
        }

        // Мутанты — только на открытой местности локации, никогда внутри зданий.
        SpawnRaidMutants(raid, loc);

        string sec = raid.MaxSectors > 1 ? $" Сектор {raid.Sector}/{raid.MaxSectors}." : "";
        raid.Log.Add($"🌆 Рейд начался: {loc.Name}.{sec} Зданий: {raid.Buildings.Count}. Тайников: {raid.Stashes.Count}. Автомобилей: {raid.Cars.Count}.");
        if (raid.Mutants.Count > 0)
            raid.Log.Add($"🐺 На локации замечены мутанты: {raid.Mutants.Count}. Они видят на {MutantVisionRadius} клетки, но здания и деревья закрывают им обзор.");
        if (raid.Sector < raid.MaxSectors)
            raid.Log.Add("🧭 На этой локации можно пройти дальше (ищите переход ➜).");
        return raid;
    }

    private record ScriptNpcDef(string Name, string Icon, string[] Locations, string[] Dialogue, string? GivesItemCode = null, string? RequiresQuestId = null, bool CanTrade = false);
    private static readonly ScriptNpcDef[] ScriptNpcs = new[]
    {
        new ScriptNpcDef("Борян", "/neutrals_frendly/script/Борян.jpg", new[]{"city","kordon"}, new[]{"Эй, сталкер! Не стреляй, я свой.","Слышал, на Кордоне нашли чей-то груз. Погляди там.","Если принесёшь мне записку — помогу чем смогу."}, "q_note", "npc_talk_boryan"),
        new ScriptNpcDef("Варшава", "/neutrals_frendly/script/Варшава.jpg", new[]{"kordon","factory"}, new[]{"Тихо тут... слишком тихо.","На заводе видели людей в форме. Не наши.","Держи папку — сам нашёл, мне она не нужна."}, "q_intel_folder", "npc_talk_varshava"),
        new ScriptNpcDef("Доктор", "/neutrals_frendly/script/Доктор.jpg", new[]{"city","mall"}, new[]{"Раны лечу, вопросы — потом.","В этой зоне память лечить сложнее, чем пулевые.","Возьми документы, они медицинские — вдруг пригодятся."}, "q_docs_folder", "npc_talk_doctor"),
        new ScriptNpcDef("Дух", "/neutrals_frendly/script/Дух.png", new[]{"forest","predbannik"}, new[]{"Лес всё слышит. И всё помнит.","Ночью сюда лучше не ходить.","Я нашёл старые бумаги у тропы."}, "q_old_docs", "npc_talk_duh"),
        new ScriptNpcDef("Клавишник", "/neutrals_frendly/script/Клавишник.jpg", new[]{"factory","military"}, new[]{"Электроника — мой конёк.","На базе хранится что-то важное. Флешки, коды...","Могу передать техническую документацию."}, "q_tech_docs", "npc_talk_klav"),
        new ScriptNpcDef("Майор Маршин", "/neutrals_frendly/script/Майор Маршин.jpg", new[]{"military","outskirts"}, new[]{"Стой. Документы.","Я знаю про правительственный груз. И знаю, кто за ним охотился.","Вот закрытый кейс — передай тем, кому доверяешь."}, "q_closed_case", "npc_talk_marshin"),
        new ScriptNpcDef("Немец", "/neutrals_frendly/script/Немец.png", new[]{"mall","outskirts"}, new[]{"Аккуратность прежде всего.","Тут всё имеет цену. Даже информация.","Стальной ящик — берёшь, отвечаешь."}, "q_steel_box", "npc_talk_nemets"),
        new ScriptNpcDef("Оззи", "/neutrals_frendly/script/Оззи.jpg", new[]{"suburb","gradirni"}, new[]{"Курьер прибыл. Опоздал, но прибыл.","На Градирнях неспокойно. Наёмники нервничают.","Кейс с артефактами — не открывать без нужды."}, "q_artifact_case", "npc_talk_ozzi"),
        new ScriptNpcDef("Перец", "/neutrals_frendly/script/Перец.png", new[]{"kordon","forest"}, new[]{"Жарко сегодня для Зоны, а?","У меня есть припасы — могу поменяться.",}, null, "npc_talk_perets", true),
        new ScriptNpcDef("Пётр", "/neutrals_frendly/script/Пётр.jpg", new[]{"city","predbannik"}, new[]{"Работаю тут давно. Видел всякое.","Твой груз? Может, и видел. Но память штука ненадёжная."}, null, "npc_talk_petr"),
        new ScriptNpcDef("Умник", "/neutrals_frendly/script/Умник.jpg", new[]{"factory","lab" /*lab not a loc id, harmless*/,"mall"}, new[]{"Логика проста: если груз секретный — его спрятали надёжно.","Документацию найти можно. Люди всегда оставляют следы."}, "q_tech_docs", "npc_talk_umnik"),
        new ScriptNpcDef("Учёный", "/neutrals_frendly/script/Учёный.jpg", new[]{"predbannik","gradirni","forest"}, new[]{"Аномальная активность растёт.","Артефакты нужно хранить в контейнерах. Вот образец."}, "art_kaplya", "npc_talk_ucheniy"),
    };

    private void SpawnRaidNpcs(RaidState raid, Location loc, int tier)
    {
        var genericImgs = new List<string>();
        foreach (var ext in new[] { "png", "jpg" })
            for (int i = 1; i <= 24; i++) { /* placeholder list built below */ }
        var imgs = new[] {
            "/neutrals_frendly/1.png","/neutrals_frendly/2.jpg","/neutrals_frendly/3.png","/neutrals_frendly/4.png",
            "/neutrals_frendly/5.png","/neutrals_frendly/6.png","/neutrals_frendly/7.png","/neutrals_frendly/8.png",
            "/neutrals_frendly/9.png","/neutrals_frendly/10.png","/neutrals_frendly/11.png","/neutrals_frendly/12.png",
            "/neutrals_frendly/13.png","/neutrals_frendly/14.png","/neutrals_frendly/15.png","/neutrals_frendly/16.png",
            "/neutrals_frendly/17.png","/neutrals_frendly/18.jpg","/neutrals_frendly/19.jpg","/neutrals_frendly/20.jpg",
            "/neutrals_frendly/21.jpg","/neutrals_frendly/22.jpg","/neutrals_frendly/23.jpg","/neutrals_frendly/24.jpg",
        };
        string[] gNames = { "Сталкер", "Одиночка", "Бродяга", "Охотник", "Выживший", "Путник", "Диггер", "Разведчик" };
        string[] gLines = { "Не шуми — тут рядом твари.", "Могу обменяться, если есть что предложить.", "Видел врагов на севере.", "Удачи в рейде.", "Я просто прохожу мимо." };

        bool IsFree(int x, int y) => GetTile(raid, x, y)?.Type == RaidTileTypes.Ground
            && !raid.Mutants.Any(m => m.X == x && m.Y == y) && !raid.Npcs.Any(n => n.X == x && n.Y == y)
            && !raid.Cars.Any(c => c.X == x && c.Y == y) && !raid.Stashes.Any(s => s.X == x && s.Y == y)
            && !(Math.Abs(x - raid.PlayerX) + Math.Abs(y - raid.PlayerY) < 3);

        (int x,int y)? Pick()
        {
            for (int t = 0; t < 120; t++) { int x=_rng.Next(1,raid.Width-1), y=_rng.Next(1,raid.Height-1); if (IsFree(x,y)) return (x,y); }
            return null;
        }

        // Скриптовые NPC — если их локация совпадает и (нет требования квеста ИЛИ квест принят/актуален)
        foreach (var sn in ScriptNpcs.Where(s => s.Locations.Contains(loc.Id, StringComparer.OrdinalIgnoreCase)).Take(2))
        {
            bool relevant = sn.RequiresQuestId == null || _state.AllQuests().Any(q => q.Id == sn.RequiresQuestId && (q.IsAccepted || !q.IsCompleted));
            // Спавним с шансом; если квест принят — почти всегда
            bool questAccepted = sn.RequiresQuestId != null && _state.AllQuests().Any(q => q.Id == sn.RequiresQuestId && q.IsAccepted && !q.IsCompleted);
            if (!relevant && !questAccepted) continue;
            if (!questAccepted && _rng.NextDouble() > 0.45) continue;
            var pos = Pick(); if (pos == null) continue;
            raid.Npcs.Add(new RaidNpc
            {
                Name = sn.Name, Icon = sn.Icon, X = pos.Value.x, Y = pos.Value.y, Scripted = true,
                Dialogue = sn.Dialogue.ToList(), GivesItemCode = sn.GivesItemCode, RequiresQuestId = sn.RequiresQuestId,
                CanTrade = sn.CanTrade, TradeCodes = sn.CanTrade ? new List<string>{"bandage","water","bread","canned"} : new()
            });
        }

        // Обычные нейтральные
        int gCount = _rng.NextDouble() < 0.7 ? _rng.Next(1, 3) : 0;
        for (int i = 0; i < gCount; i++)
        {
            var pos = Pick(); if (pos == null) break;
            var img = imgs[_rng.Next(imgs.Length)];
            bool canTrade = _rng.NextDouble() < 0.35;
            raid.Npcs.Add(new RaidNpc
            {
                Name = gNames[_rng.Next(gNames.Length)], Icon = img, X = pos.Value.x, Y = pos.Value.y, Scripted = false,
                Dialogue = Enumerable.Range(0, _rng.Next(1,3)).Select(_ => gLines[_rng.Next(gLines.Length)]).ToList(),
                CanTrade = canTrade, TradeCodes = canTrade ? new List<string>{"bandage","water","bread","scrap","bolt","nuts"} : new()
            });
        }
    }

    private void SpawnRaidCars(RaidState raid, Location loc, int tier)
    {
        int count = loc.Id switch
        {
            "city" => _rng.Next(1, 4),
            "kordon" => _rng.Next(1, 3),
            "military" => _rng.Next(1, 3),
            "factory" => _rng.Next(0, 3),
            "mall" => _rng.Next(0, 2),
            "forest" => _rng.Next(0, 2),
            _ => _rng.Next(0, 3),
        };
        var names = new[] { "Старая легковушка", "Брошенный фургон", "Ржавая Нива", "Армейский УАЗ", "Разбитая иномарка", "Грузовик" };

        for (int i = 0; i < count; i++)
        {
            for (int tries = 0; tries < 140; tries++)
            {
                int cx = _rng.Next(1, MapWidth - 1);
                int cy = _rng.Next(1, MapHeight - 1);
                if (Math.Abs(cx - raid.PlayerX) + Math.Abs(cy - raid.PlayerY) <= 2) continue;
                if (raid.Exits.Any(e => e.X == cx && e.Y == cy)) continue;
                var t = GetTile(raid, cx, cy);
                if (t == null || t.Type != RaidTileTypes.Ground) continue;
                if (raid.Stashes.Any(s => s.X == cx && s.Y == cy)) continue;
                if (raid.Cars.Any(c => c.X == cx && c.Y == cy)) continue;

                t.Type = RaidTileTypes.Car;
                var loot = _loot.Generate(tier, 0, false, 0);
                // Cars often have materials/tools/fuel-like finds; bias by re-rolling rarity sometimes not possible here,
                // but base loot + small money is fine.
                raid.Cars.Add(new RaidCar
                {
                    X = cx,
                    Y = cy,
                    Name = names[_rng.Next(names.Length)],
                    Loot = loot.Items,
                    Money = loot.Money + _rng.Next(0, 25) * Math.Max(1, tier),
                    Xp = Math.Max(2, loot.Xp / 2),
                });
                break;
            }
        }
    }

    private void GenerateBuildingFurniture(RaidBuilding b, string kind, int tier)
    {
        (string name, string icon)[] pool = kind switch
        {
            "house" or "hut" or "bath" or "barn" => new[] { ("Шкаф", "🗄️"), ("Тумбочка", "🪑"), ("Кровать", "🛏️"), ("Стол", "🪵"), ("Комод", "📦"), ("Печь", "🔥") },
            "shop" or "department" or "cafe" => new[] { ("Прилавок", "🧾"), ("Стеллаж", "🗄️"), ("Касса", "💰"), ("Деревянный ящик", "📦"), ("Холодильник", "🧊") },
            "warehouse" or "garage" or "workshop" or "factory" or "boiler" => new[] { ("Верстак", "🔧"), ("Ящик с инструментами", "🧰"), ("Стеллаж", "🗄️"), ("Металлический шкаф", "🗄️"), ("Бочка", "🛢️") },
            "armory" or "barracks" or "checkpoint" or "military" => new[] { ("Оружейный шкаф", "🗄️"), ("Тумбочка", "🪑"), ("Койка", "🛏️"), ("Металлический ящик", "📦"), ("Сейф", "🔒") },
            "medbay" => new[] { ("Медицинский шкаф", "💊"), ("Кушетка", "🛏️"), ("Стол", "🪵"), ("Деревянный ящик", "📦") },
            "lab" or "office" or "school" => new[] { ("Стол", "🪵"), ("Шкаф", "🗄️"), ("Сейф", "🔒"), ("Полка", "📚"), ("Тумба", "📦") },
            _ => new[] { ("Шкаф", "🗄️"), ("Деревянный ящик", "📦"), ("Стол", "🪵"), ("Тумбочка", "🪑") },
        };

        int maxBySize = Math.Clamp(b.Width * b.Height, 1, 4);
        int count = Math.Max(1, _rng.Next(1, Math.Min(4, maxBySize + 1)));
        // Some buildings empty-ish
        if (kind is "well" or "checkpoint" && _rng.NextDouble() < 0.35) count = 0;

        for (int i = 0; i < count; i++)
        {
            var pick = pool[_rng.Next(pool.Length)];
            var loot = _loot.Generate(Math.Max(1, tier - ( _rng.NextDouble() < 0.6 ? 1 : 0)), 0, false, 0);
            // Furniture gives smaller money, sometimes just junk
            if (_rng.NextDouble() < 0.25)
            {
                loot.Items.Clear();
                loot.Money = _rng.Next(0, 8) * Math.Max(1, tier);
                loot.Xp = 1;
            }
            b.Furniture.Add(new RaidFurniture
            {
                Name = pick.name,
                Icon = pick.icon,
                Sprite = FurnitureSprite(pick.name),
                Loot = loot.Items,
                Money = Math.Max(0, loot.Money / 2),
                Xp = Math.Max(1, loot.Xp / 3),
            });
        }
    }

    private static string? FurnitureSprite(string name) => name switch
    {
        "Шкаф" or "Металлический шкаф" => "/blocks/шкаф.png",
        "Тумбочка" or "Комод" or "Тумба" => "/blocks/тумба.png",
        "Кровать" or "Койка" or "Кушетка" => "/blocks/кровать.png",
        "Стол" => "/blocks/стол.png",
        "Печь" => "/blocks/печь.png",
        "Прилавок" => "/blocks/прилавок.png",
        "Стеллаж" => "/blocks/стеллаж.png",
        "Касса" => "/blocks/касса.png",
        "Холодильник" => "/blocks/холодильник.png",
        "Верстак" => "/blocks/верстак.png",
        "Ящик с инструментами" => "/blocks/ящик с инструментами.png",
        "Деревянный ящик" or "Ящик" => "/blocks/деревянный_ящик.png",
        "Металлический ящик" => "/blocks/Металлический_ящик.png",
        "Бочка" => "/blocks/бочка.png",
        "Оружейный шкаф" => "/blocks/оружейный_шкаф.png",
        "Сейф" => "/blocks/сейф.png",
        "Медицинский шкаф" => "/blocks/медицинский шкаф.png",
        "Полка" => "/blocks/полка.png",
        _ => null,
    };

    private void SpawnRaidMutants(RaidState raid, Location loc)
    {
        var templates = loc.Enemies.Where(IsMutantTemplate).ToList();
        if (templates.Count == 0) return;

        int count = _rng.Next(1, 4); // 1–3 мутанта на локацию

        for (int i = 0; i < count; i++)
        {
            // Взвешенный выбор шаблона мутанта.
            double total = templates.Sum(t => t.Weight);
            double roll = _rng.NextDouble() * total;
            double acc = 0;
            EnemyTemplate chosen = templates[0];
            foreach (var template in templates)
            {
                acc += template.Weight;
                if (roll <= acc) { chosen = template; break; }
            }

            for (int tries = 0; tries < 400; tries++)
            {
                int x = _rng.Next(1, MapWidth - 1);
                int y = _rng.Next(1, MapHeight - 1);
                if (Chebyshev(x, y, raid.PlayerX, raid.PlayerY) < MutantVisionRadius + 2) continue;

                var tile = GetTile(raid, x, y);
                if (tile == null || tile.Type != RaidTileTypes.Ground) continue;
                if (raid.Stashes.Any(s => s.X == x && s.Y == y)) continue;
                if (raid.Mutants.Any(m => m.X == x && m.Y == y)) continue;

                double levelScale = 1.0 + Math.Max(0, _state.Player.Level - 1) * 0.045;
                int hp = (int)Math.Round((chosen.BaseHp + (raid.Tier - 1) * 10 + _rng.Next(0, 8)) * levelScale);
                int dmg = Math.Max(2, (int)Math.Round((chosen.BaseDamage + (raid.Tier - 1) * 3 + _rng.Next(0, 4)) * levelScale));

                raid.Mutants.Add(new RaidMutant
                {
                    Name = chosen.Name,
                    Icon = string.IsNullOrWhiteSpace(chosen.Icon) ? "/enemies/flesh.png" : chosen.Icon,
                    X = x,
                    Y = y,
                    Hp = hp,
                    MaxHp = hp,
                    Damage = dmg,
                    Defense = chosen.BaseDefense + Math.Max(0, raid.Tier - 1),
                    Ranged = chosen.Ranged,
                    VisionRadius = MutantVisionRadius,
                });
                break;
            }
        }
    }

    private void AdvanceToNextSector(RaidState raid, Location loc)
    {
        if (raid.Sector >= raid.MaxSectors) return;
        int next = raid.Sector + 1;
        RaidState fresh = new();
        for (int attempt = 0; attempt < 30; attempt++)
        {
            fresh = GenerateRaidAttempt(loc, raid.Tier, sector: next, maxSectors: raid.MaxSectors);
            if (IsRaidReachable(fresh)) break;
            if (attempt == 29)
            {
                foreach (var t in fresh.Tiles.Where(t => t.Type == RaidTileTypes.Tree))
                    t.Type = RaidTileTypes.Ground;
                fresh = GenerateRaidAttempt(loc, raid.Tier, noTrees: true, sector: next, maxSectors: raid.MaxSectors);
            }
        }

        raid.Sector = fresh.Sector;
        raid.Width = fresh.Width;
        raid.Height = fresh.Height;
        raid.Tiles = fresh.Tiles;
        raid.Buildings = fresh.Buildings;
        raid.Stashes = fresh.Stashes;
        raid.Cars = fresh.Cars;
        raid.Mutants = fresh.Mutants;
        raid.Exits = fresh.Exits;
        raid.ExitX = fresh.ExitX;
        raid.ExitY = fresh.ExitY;
        raid.PlayerX = fresh.PlayerX;
        raid.PlayerY = fresh.PlayerY;
        raid.AtExit = false;
        raid.Combat = null;
        raid.CombatReturnX = 0;
        raid.CombatReturnY = 0;
        _state.CurrentCombat = null;
        InjectQuestItems(raid, loc);
        raid.Log.Add($"🧭 Вы продвинулись дальше: {loc.Name}, сектор {raid.Sector}/{raid.MaxSectors}.");
        foreach (var line in fresh.Log.Where(l => l.Contains("Зданий") || l.Contains("Тайников") || l.Contains("Автомоб") || l.Contains("переход") || l.Contains("➜")))
            raid.Log.Add(line);
    }

    /// <summary>
    /// Подкладывает в лут рейда квестовые предметы активных заданий (1 шт. на рейд/сектор при необходимости),
    /// чтобы их можно было найти. Не спаунит, если предмет уже есть у игрока/в схроне или задание закрыто.
    /// </summary>
    private void InjectQuestItems(RaidState raid, Location loc)
    {
        var p = _state.Player;
        if (p == null) return;

        var injectedCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var q in _state.AllQuests()
                     .Where(q => q.IsAccepted && !q.IsCompleted && !q.IsFailed
                                 && q.Type == QuestType.Fetch
                                 && !string.IsNullOrWhiteSpace(q.TargetItemCode)))
        {
            var code = q.TargetItemCode!;
            if (!injectedCodes.Add(code)) continue;

            // Локация: если у квеста задана цель — только она; иначе где угодно (с точечными исключениями).
            if (!string.IsNullOrWhiteSpace(q.TargetLocationId))
            {
                if (!string.Equals(q.TargetLocationId, raid.LocationId, StringComparison.OrdinalIgnoreCase))
                    continue;
            }
            else if (string.Equals(code, "quest_bunker_key", StringComparison.OrdinalIgnoreCase))
            {
                var rid = raid.LocationId ?? "";
                if (!rid.Equals("suburb", StringComparison.OrdinalIgnoreCase)
                    && !rid.Equals("gradirni", StringComparison.OrdinalIgnoreCase))
                    continue;
            }

            // Уже получен/имеется — не спаунить повторно.
            int have = InventoryHelper.CountByCode(p, code)
                       + p.Shelter.Stash.Where(i => string.Equals(i.Code, code, StringComparison.OrdinalIgnoreCase))
                                        .Sum(i => Math.Max(1, i.Count));
            if (have >= Math.Max(1, q.TargetCount)) continue;

            if (_catalog.Get(code) == null) continue;

            var item = _catalog.Create(code);
            item.Count = 1;
            item.FoundInRaid = true;

            if (!TryPlaceQuestItem(raid, item))
            {
                // fallback: тайник на земле
                var pos = FindFreeGround(raid);
                if (pos != null)
                {
                    var (sx, sy) = pos.Value;
                    var tile = GetTile(raid, sx, sy);
                    if (tile != null && tile.Type == RaidTileTypes.Ground)
                        tile.Type = RaidTileTypes.Stash;
                    raid.Stashes.Add(new RaidStash
                    {
                        X = sx, Y = sy,
                        Loot = new List<Item> { item },
                        Money = 0, Xp = 0,
                    });
                }
                else
                {
                    // крайний случай: положить в первый попавшийся лут-контейнер здания
                    var anyB = raid.Buildings.FirstOrDefault();
                    if (anyB != null) anyB.Loot.Add(item);
                }
            }
        }
    }

    private bool TryPlaceQuestItem(RaidState raid, Item item)
    {
        var containers = new List<Action<Item>>();
        foreach (var b in raid.Buildings.Where(b => !b.Looted))
            containers.Add(it => b.Loot.Add(it));
        foreach (var b in raid.Buildings.Where(b => b.HasStash && !b.StashLooted))
            containers.Add(it => b.StashLoot.Add(it));
        foreach (var s in raid.Stashes.Where(s => !s.Looted))
            containers.Add(it => s.Loot.Add(it));
        foreach (var c in raid.Cars.Where(c => !c.Looted))
            containers.Add(it => c.Loot.Add(it));
        foreach (var b in raid.Buildings)
            foreach (var f in b.Furniture.Where(f => !f.Looted))
                containers.Add(it => f.Loot.Add(it));

        if (containers.Count == 0) return false;
        containers[_rng.Next(containers.Count)](item);
        return true;
    }

    private (int x, int y)? FindFreeGround(RaidState raid)
    {
        for (int tries = 0; tries < 120; tries++)
        {
            int x = _rng.Next(1, Math.Max(2, raid.Width - 1));
            int y = _rng.Next(1, Math.Max(2, raid.Height - 1));
            var t = GetTile(raid, x, y);
            if (t == null || t.Type != RaidTileTypes.Ground) continue;
            if (x == raid.PlayerX && y == raid.PlayerY) continue;
            if (raid.Exits.Any(e => e.X == x && e.Y == y)) continue;
            return (x, y);
        }
        return null;
    }

    private record BuildingTheme(string Name, string Kind, string Icon, int W, int H, string Description);

    private List<BuildingTheme> BuildPlan(Location loc)
    {
        var result = new List<BuildingTheme>();
        void Add(string name, string icon, int w, int h, string kind = "house", string desc = "Заброшенное строение.")
            => result.Add(new BuildingTheme(name, kind, icon, w, h, desc));

        switch (loc.Id)
        {
            case "mall":
                Add("Электроника", "📺", 3, 2, "department", "Витрины с техникой, большая часть уже разграблена.");
                Add("Одежда", "👕", 2, 2, "department", "Манекены без одежды и рассыпанные вешалки.");
                Add("Продукты", "🛒", 3, 2, "department", "Пустые полки и перевёрнутые тележки.");
                Add("Аптека", "/blocks/аптека.jpg", 2, 2, "department", "Разбитые витрины с лекарствами.");
                Add("Спорттовары", "🏋", 2, 3, "department", "Тренажёры и стойки с инвентарём.");
                Add("Книги", "📚", 2, 2, "department", "Тихий отдел с высокими стеллажами.");
                Add("Игрушки", "🧸", 1, 1, "department", "Маленький магазин игрушек.");
                Add("Кафе", "/blocks/Кафе.jpg", 2, 2, "department", "Стойка, столики и запах старого кофе.");
                Add("Кинотеатр", "🎬", 3, 3, "department", "Тёмный зал с рядами кресел.");
                break;

            case "forest":
                Add("Сторожка", "/blocks/wooden_house.png", 1, 1, "hut", "Крошечная сторожка лесника.");
                Add("Охотничий домик", "/blocks/house.png", 2, 2, "hut", "Домик с заколоченными окнами.");
                Add("Сарай", "/blocks/сарай.png", 1, 2, "shed", "Старый сарай с инструментами.");
                Add("Землянка", "/blocks/землянка.jpg", 1, 1, "hut", "Землянка, пахнущая сыростью.");
                break;

            case "military":
                Add("Казарма", "/blocks/kazarma.png", 3, 2, "barracks", "Ряды коек и оружейные пирамиды.");
                Add("Казарма", "/blocks/kazarma.png", 2, 3, "barracks", "Ещё одна казарма, следы недавнего присутствия.");
                Add("Штаб", "🏢", 2, 2, "office", "Карты, рации и тяжёлые сейфы.");
                Add("Арсенал", "/blocks/арсенал.jpg", 2, 3, "armory", "Оружейная комната с усиленной дверью.");
                Add("Склад", "/blocks/storage.jpg", 3, 3, "warehouse", "Армейские ящики и запчасти.");
                Add("КПП", "/blocks/blockpost_kpp.png", 1, 1, "checkpoint", "Маленький блокпост у входа.");
                Add("Медсанчасть", "/blocks/медсанчасть.jpg", 2, 2, "medbay", "Медицинские шкафы и кушетки.");
                Add("Гараж", "/blocks/garage.png", 3, 2, "garage", "Брошенная техника под брезентом.");
                break;

            case "factory":
                Add("Цех", "/blocks/цех.jpg", 3, 3, "workshop", "Огромный цех с ржавыми станками.");
                Add("Склад", "/blocks/storage.jpg", 2, 3, "warehouse", "Стеллажи с ящиками и деталями.");
                Add("Мастерская", "/blocks/мастерская.png", 2, 2, "workshop", "Верстаки, инструменты, запах масла.");
                Add("Лаборатория", "/blocks/лаборатория.png", 2, 2, "lab", "Странное оборудование и колбы.");
                Add("Бытовка", "/blocks/бытовка.png", 1, 1, "office", "Комната отдыха рабочих.");
                Add("Котельная", "/blocks/Котельная.png", 2, 2, "boiler", "Гулкое помещение с трубами.");
                break;

            case "kordon":
                Add("Жилой дом", "/blocks/house.png", 2, 1, "house", "Жилой дом. Внутри можно найти мебель и вещи.");
                Add("Обветшалый дом", "/blocks/dilapidated_house.png", 1, 1, "house", "Обветшалый дом. Дверь не заперта, но внутри может быть опасно.");
                Add("Деревянный дом", "/blocks/wooden_house.png", 2, 1, "house", "Старый деревянный дом.");
                Add("Дом", "/blocks/house.png", 2, 2, "house", "Деревенский дом с печью.");
                Add("Сарай", "/blocks/сарай.png", 1, 1, "shed", "Покосившийся сарай.");
                Add("Блокпост", "/blocks/blockpost_kpp.png", 1, 2, "checkpoint", "Мешки с песком и старый УАЗ.");
                Add("Колодец", "/blocks/колодец.jpg", 1, 1, "well", "Колодец с чистой водой.");
                Add("Амбар", "/blocks/амбар.png", 2, 2, "barn", "Амбар, набитый сеном и хламом.");
                Add("Баня", "/blocks/banya.jpg", 1, 1, "bath", "Маленькая баня.");
                Add("Магазин", "/blocks/store.png", 2, 2, "shop", "Деревенский магазин.");
                break;

            case "city":
                Add("Жилой дом", "/blocks/house.png", 2, 1, "house", "Жилой дом. Можно зайти и обыскать мебель.");
                Add("Обветшалый дом", "/blocks/dilapidated_house.png", 1, 1, "house", "Обветшалый дом с выбитыми окнами.");
                Add("Деревянный дом", "/blocks/wooden_house.png", 1, 1, "house", "Небольшой деревянный дом.");
                Add("Панелька", "/blocks/panelka.png", 3, 2, "house", "Обрушенный жилой дом.");
                Add("Магазин", "/blocks/store.png", 2, 2, "shop", "Небольшой магазин.");
                Add("Аптека", "/blocks/аптека.jpg", 1, 1, "shop", "Аптека с разбитой витриной.");
                Add("Гараж", "/blocks/garage.png", 2, 2, "garage", "Гараж с брошенной машиной.");
                Add("Склад", "/blocks/storage.jpg", 2, 2, "warehouse", "Запертый склад.");
                Add("Котельная", "/blocks/Котельная.png", 1, 1, "boiler", "Котельная в подвале.");
                Add("Школа", "/blocks/school.jpg", 3, 2, "school", "Школа с длинными коридорами.");
                Add("Кафе", "/blocks/Кафе.jpg", 1, 1, "shop", "Маленькое кафе.");
                break;

            default:
                Add("Жилой дом", "/blocks/house.png", 2, 1, "house", "Жилой дом. Можно зайти и обыскать мебель.");
                Add("Обветшалый дом", "/blocks/dilapidated_house.png", 1, 1, "house", "Обветшалый дом. Внутри тихо — или нет.");
                Add("Деревянный дом", "/blocks/wooden_house.png", 2, 1, "house", "Деревянный дом с покосившейся крышей.");
                Add("Магазин", "/blocks/store.png", 2, 2, "shop", "Небольшой магазин.");
                Add("Панелька", "/blocks/panelka.png", 3, 2, "house", "Обрушенный жилой дом.");
                Add("Аптека", "/blocks/аптека.jpg", 1, 1, "shop", "Аптека с разбитой витриной.");
                Add("Гараж", "/blocks/garage.png", 2, 2, "garage", "Гараж с брошенной машиной.");
                Add("Склад", "/blocks/storage.jpg", 2, 2, "warehouse", "Запертый склад.");
                Add("Котельная", "/blocks/Котельная.png", 1, 1, "boiler", "Котельная в подвале.");
                Add("Школа", "/blocks/school.jpg", 3, 2, "school", "Школа с длинными коридорами.");
                Add("Кафе", "/blocks/Кафе.jpg", 1, 1, "shop", "Маленькое кафе.");
                break;
        }

        return result;
    }

    private static double TreeChance(Location loc) => loc.Id switch
    {
        "forest" => 0.30,
        "kordon" => 0.16,
        "city" => 0.10,
        "mall" => 0.01,
        "military" => 0.05,
        "factory" => 0.04,
        _ => 0.12,
    };

    private static double EnemyChance(Location loc) => loc.Id switch
    {
        "forest" => 0.60,
        "military" => 0.85,
        "mall" => 0.70,
        "factory" => 0.75,
        "kordon" => 0.55,
        _ => 0.62,
    };

    private static double EnemyChanceForBuilding(Location loc, string kind)
        => 0.5; // 50/50 во всех зданиях

    private bool IsRaidReachable(RaidState raid)
    {
        var start = (raid.PlayerX, raid.PlayerY);
        var seen = BfsRaid(raid, start);
        var exit = (raid.ExitX, raid.ExitY);
        if (!seen.Contains(exit)) return false;

        foreach (var b in raid.Buildings)
        {
            bool adjacent = false;
            for (int y = b.Y - 1; y <= b.Y + b.Height && !adjacent; y++)
                for (int x = b.X - 1; x <= b.X + b.Width && !adjacent; x++)
                {
                    if (!InMap(raid, x, y)) continue;
                    var t = GetTile(raid, x, y)!;
                    if (t.Type == RaidTileTypes.Building) continue;
                    if (seen.Contains((x, y))) adjacent = true;
                }
            if (!adjacent) return false;
        }
        return true;
    }

    private HashSet<(int X, int Y)> BfsRaid(RaidState raid, (int X, int Y) start)
    {
        var seen = new HashSet<(int X, int Y)> { start };
        var queue = new Queue<(int X, int Y)>();
        queue.Enqueue(start);
        while (queue.Count > 0)
        {
            var (x, y) = queue.Dequeue();
            foreach (var (dx, dy) in Dirs8)
            {
                int nx = x + dx, ny = y + dy;
                if (!InMap(raid, nx, ny)) continue;
                if (seen.Contains((nx, ny))) continue;
                var t = GetTile(raid, nx, ny)!;
                if (!IsWalkable(t)) continue;
                seen.Add((nx, ny));
                queue.Enqueue((nx, ny));
            }
        }
        return seen;
    }

    private static bool AreaFree(RaidState raid, int x, int y, int w, int h)
    {
        if (x < 1 || y < 1 || x + w > raid.Width - 1 || y + h > raid.Height - 1) return false;
        for (int yy = y - 1; yy <= y + h; yy++)
            for (int xx = x - 1; xx <= x + w; xx++)
            {
                if (!InMap(raid, xx, yy)) continue;
                var t = GetTile(raid, xx, yy)!;
                if (t.Type is RaidTileTypes.Building or RaidTileTypes.Exit) return false;
            }
        return true;
    }

    private static bool InMap(RaidState raid, int x, int y) =>
        x >= 0 && y >= 0 && x < raid.Width && y < raid.Height;

    private static RaidTile? GetTile(RaidState raid, int x, int y) =>
        raid.Tiles.FirstOrDefault(t => t.X == x && t.Y == y);

    private static bool IsWalkable(RaidTile tile) =>
        tile.Type is RaidTileTypes.Ground or RaidTileTypes.Exit or RaidTileTypes.Transition;

    private static bool IsAdjacentToBuilding(RaidState raid, RaidBuilding b) =>
        raid.PlayerX >= b.X - 1 && raid.PlayerX <= b.X + b.Width &&
        raid.PlayerY >= b.Y - 1 && raid.PlayerY <= b.Y + b.Height &&
        !(raid.PlayerX >= b.X && raid.PlayerX < b.X + b.Width &&
          raid.PlayerY >= b.Y && raid.PlayerY < b.Y + b.Height);

    private static void AddLog(RaidState raid, string line)
    {
        raid.Log.Add(line);
        if (raid.Log.Count > 30) raid.Log = raid.Log.TakeLast(30).ToList();
    }

    private static void AddLog(CombatStateDto c, string line)
    {
        c.Log.Add(line);
        if (c.Log.Count > 40) c.Log = c.Log.TakeLast(40).ToList();
    }

    private static int effectiveTierSafe(RaidState raid) => Math.Max(1, raid.Tier);

    // ============================================================
    //  БОЙ: ГЕНЕРАЦИЯ
    // ============================================================

    private static (int w, int h) CombatSizeForBuilding(RaidBuilding b)
    {
        int bw = Math.Max(1, b.Width);
        int bh = Math.Max(1, b.Height);
        // По ТЗ: 1 клетка → 5x5, 2x2 → 8x8, 3x2 → 8x10.
        if (bw == 1 && bh == 1) return (5, 5);
        if (bw == 2 && bh == 2) return (8, 8);
        if ((bw == 3 && bh == 2) || (bw == 2 && bh == 3)) return (8, 10);
        // Остальные размеры: пропорционально площади здания.
        int area = bw * bh;
        int w = Math.Clamp(4 + (int)Math.Ceiling(Math.Sqrt(area) * 2.2), 5, 12);
        int h = Math.Clamp(4 + (int)Math.Ceiling(Math.Sqrt(area) * 2.0), 5, 12);
        return (w, h);
    }

    private RaidMutant? FindMutantWatcher(RaidState raid) =>
        raid.Mutants
            .Where(m => !m.Defeated && m.Cooldown <= 0)
            .Where(m => Chebyshev(m.X, m.Y, raid.PlayerX, raid.PlayerY) <= m.VisionRadius)
            .Where(m => HasRaidLineOfSight(raid, m.X, m.Y, raid.PlayerX, raid.PlayerY))
            .OrderBy(m => Chebyshev(m.X, m.Y, raid.PlayerX, raid.PlayerY))
            .FirstOrDefault();

    /// <summary>Линия видимости на карте рейда: здания, деревья и прочие препятствия её перекрывают.</summary>
    private static bool HasRaidLineOfSight(RaidState raid, int x0, int y0, int x1, int y1)
    {
        int dx = Math.Abs(x1 - x0);
        int dy = Math.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;
        int cx = x0, cy = y0;

        while (!(cx == x1 && cy == y1))
        {
            int e2 = 2 * err;
            if (e2 > -dy) { err -= dy; cx += sx; }
            if (e2 < dx) { err += dx; cy += sy; }
            if (cx == x1 && cy == y1) break;

            var t = GetTile(raid, cx, cy);
            if (t == null || !IsWalkable(t)) return false;
        }
        return true;
    }

    private void StartMutantCombat(RaidState raid, RaidMutant trigger)
    {
        var p = _state.Player;

        // Все мутанты, которые в этот момент реально видят игрока, вступают в бой.
        var group = raid.Mutants
            .Where(m => !m.Defeated && m.Cooldown <= 0)
            .Where(m => Chebyshev(m.X, m.Y, raid.PlayerX, raid.PlayerY) <= m.VisionRadius)
            .Where(m => HasRaidLineOfSight(raid, m.X, m.Y, raid.PlayerX, raid.PlayerY))
            .OrderBy(m => Chebyshev(m.X, m.Y, raid.PlayerX, raid.PlayerY))
            .Take(3)
            .ToList();
        if (group.Count == 0) group.Add(trigger);

        int cw = group.Count >= 3 ? 9 : 8;
        int ch = cw;

        int armorSlowdown = (p.Helmet?.Slowdown ?? 0) + (p.Armor?.Slowdown ?? 0) + (p.Suit?.Slowdown ?? 0);
        int baseApMax = Math.Min(CombatRules.MaxAp, CombatRules.BaseAp + PerkEffects.ApBonus(p) + p.AgilityApBonus - armorSlowdown / 10);

        var combat = new CombatStateDto
        {
            MutantId = trigger.Id,
            Title = group.Count == 1 ? group[0].Name : $"Стая мутантов ({group.Count})",
            Tier = raid.Tier,
            Width = cw,
            Height = ch,
            PlayerMaxHp = p.MaxHp,
            PlayerHp = p.Hp,
            PlayerApMax = baseApMax,
            PlayerAp = baseApMax,
            IsPlayerTurn = true,
            PlayerStamina = p.Stamina,
            PlayerMaxStamina = p.MaxStamina,
            PlayerHunger = p.Hunger,
            PlayerThirst = p.Thirst,
            PlayerWeight = p.CurrentWeight,
            PlayerMaxWeight = p.MaxWeight,
            PlayerDefense = (p.Helmet?.Defense ?? 0) + (p.Armor?.Defense ?? 0) + (p.Suit?.Defense ?? 0) + PerkEffects.DefenseBonus(p),
            PlayerDamage = 5 + (p.Weapon?.Damage ?? 0) + PerkEffects.DamageBonus(p),
        };

        for (int y = 0; y < ch; y++)
            for (int x = 0; x < cw; x++)
                combat.Tiles.Add(new CombatTile { X = x, Y = y });

        int corner = _rng.Next(4);
        combat.PlayerX = corner is 0 or 2 ? 1 : Math.Max(1, cw - 2);
        combat.PlayerY = corner is 0 or 1 ? 1 : Math.Max(1, ch - 2);

        raid.CombatReturnX = raid.PlayerX;
        raid.CombatReturnY = raid.PlayerY;

        // Естественные укрытия на открытой местности — пни, камни, ямы.
        int safeR = Math.Max(1, Math.Min(cw, ch) / 4);
        PlaceWallShapes(combat, cw, ch, safeR, Math.Clamp(2 + raid.Tier / 2, 2, 6));
        PlaceCoverSingles(combat, cw, ch, safeR, CombatTileTypes.Sandbag, Math.Clamp(2 + raid.Tier / 2, 2, 5));

        int enemySafeR = Math.Max(2, Math.Min(cw, ch) / 2 - 1);
        foreach (var mutant in group)
        {
            var enemy = new Enemy
            {
                Id = mutant.Id,
                Name = mutant.Name,
                Icon = mutant.Icon,
                Hp = Math.Max(1, mutant.Hp),
                MaxHp = Math.Max(1, mutant.MaxHp),
                Damage = mutant.Damage,
                Defense = mutant.Defense,
                Ranged = mutant.Ranged,
                Range = mutant.Ranged ? CombatRules.RangedRange : 1,
                ApMax = 5,
                Ap = 5,
            };

            for (int tries = 0; tries < 250; tries++)
            {
                int x = _rng.Next(0, cw);
                int y = _rng.Next(0, ch);
                if (Math.Abs(x - combat.PlayerX) <= enemySafeR && Math.Abs(y - combat.PlayerY) <= enemySafeR) continue;
                var t = GetCombatTile(combat, x, y);
                if (t == null || t.Type != CombatTileTypes.Empty) continue;
                if (combat.Enemies.Any(e => e.X == x && e.Y == y)) continue;
                enemy.X = x;
                enemy.Y = y;
                combat.Enemies.Add(enemy);
                break;
            }
        }

        combat.Log.Add(group.Count == 1
            ? $"🐺 {group[0].Name} атакует! Он заметил тебя на открытой местности."
            : $"🐺 Стая мутантов ({group.Count}) атакует!");
        combat.Log.Add($"⚡ AP: {combat.PlayerAp}/{combat.PlayerApMax}. Первым ходит игрок.");

        raid.Combat = combat;
        _state.CurrentCombat = combat;
    }

    private void StartBuildingCombat(RaidState raid, RaidBuilding b)
    {
        var p = _state.Player;
        var loc = _locations.Get(raid.LocationId) ?? _locations.All[0];

        var (cw, ch) = CombatSizeForBuilding(b);

        int groupSize = 1 + _rng.Next(0, 4); // 1–4 противника в здании
        bool bossInGroup = _rng.NextDouble() < loc.BossChance;

        int armorSlowdown = (p.Helmet?.Slowdown ?? 0) + (p.Armor?.Slowdown ?? 0) + (p.Suit?.Slowdown ?? 0);
        int baseApMax = Math.Min(CombatRules.MaxAp, CombatRules.BaseAp + PerkEffects.ApBonus(p) + p.AgilityApBonus - armorSlowdown / 10);

        var combat = new CombatStateDto
        {
            BuildingId = b.Id,
            Title = b.Name,
            Tier = raid.Tier,
            Width = cw,
            Height = ch,
            PlayerMaxHp = p.MaxHp,
            PlayerHp = p.Hp,
            PlayerApMax = baseApMax,
            PlayerAp = baseApMax,
            IsPlayerTurn = true,
            PlayerStamina = p.Stamina,
            PlayerMaxStamina = p.MaxStamina,
            PlayerHunger = p.Hunger,
            PlayerThirst = p.Thirst,
            PlayerWeight = p.CurrentWeight,
            PlayerMaxWeight = p.MaxWeight,
            PlayerDefense = (p.Helmet?.Defense ?? 0) + (p.Armor?.Defense ?? 0) + (p.Suit?.Defense ?? 0) + PerkEffects.DefenseBonus(p),
            PlayerDamage = 5 + (p.Weapon?.Damage ?? 0) + PerkEffects.DamageBonus(p),
        };

        for (int y = 0; y < ch; y++)
            for (int x = 0; x < cw; x++)
                combat.Tiles.Add(new CombatTile { X = x, Y = y });

        // Стартовые позиции: игрок в одном из углов, враги — на противоположной стороне.
        int corner = _rng.Next(4);
        combat.PlayerX = corner is 0 or 2 ? 1 : Math.Max(1, cw - 2);
        combat.PlayerY = corner is 0 or 1 ? 1 : Math.Max(1, ch - 2);

        raid.CombatReturnX = raid.PlayerX;
        raid.CombatReturnY = raid.PlayerY;

        // Укрытия — масштаб от размера карты.
        double areaScale = (cw * ch) / 64.0; // 8x8 как базовая плотность
        int walls = Math.Clamp((int)Math.Round((3 + raid.Tier / 2.0) * areaScale), 1, Math.Max(2, cw * ch / 8));
        int sandbags = Math.Clamp((int)Math.Round((4 + raid.Tier / 2.0) * areaScale), 1, Math.Max(2, cw * ch / 6));
        int safeR = Math.Max(1, Math.Min(cw, ch) / 4);

        PlaceWallShapes(combat, cw, ch, safeR, walls);
        PlaceCoverSingles(combat, cw, ch, safeR, CombatTileTypes.Sandbag, sandbags);

        // Враги.
        int enemySafeR = Math.Max(2, Math.Min(cw, ch) / 2 - 1);
        for (int i = 0; i < groupSize; i++)
        {
            bool isBoss = bossInGroup && i == 0;
            var enemy = GenerateEnemy(loc, raid.Tier, isBoss);
            enemy.Range = enemy.Ranged ? Math.Min(CombatRules.RangedRange, Math.Max(3, cw + ch)) : 1;
            enemy.ApMax = enemy.IsBoss ? 6 : 5;
            enemy.Ap = enemy.ApMax;

            for (int tries = 0; tries < 250; tries++)
            {
                int x = _rng.Next(0, cw);
                int y = _rng.Next(0, ch);
                if (Math.Abs(x - combat.PlayerX) <= enemySafeR && Math.Abs(y - combat.PlayerY) <= enemySafeR) continue;
                var t = GetCombatTile(combat, x, y);
                if (t == null || t.Type != CombatTileTypes.Empty) continue;
                if (combat.Enemies.Any(e => e.X == x && e.Y == y)) continue;
                enemy.X = x;
                enemy.Y = y;
                combat.Enemies.Add(enemy);
                break;
            }
        }

        combat.Log.Add($"⚔️ Внутри {b.Name} ({cw}×{ch}): {combat.Enemies.Count} противник(а).");
        combat.Log.Add($"⚡ AP: {combat.PlayerAp}/{combat.PlayerApMax}. Первым ходит игрок.");

        raid.Combat = combat;
        _state.CurrentCombat = combat;
    }

    private void StartBuildingInterior(RaidState raid, RaidBuilding b)
    {
        var p = _state.Player;
        var (cw, ch) = CombatSizeForBuilding(b);
        var combat = new CombatStateDto
        {
            BuildingId = b.Id,
            Mode = CombatModes.Interior,
            Title = b.Name,
            Tier = raid.Tier,
            Width = cw,
            Height = ch,
            PlayerMaxHp = p.MaxHp,
            PlayerHp = p.Hp,
            PlayerApMax = Math.Max(6, CombatRules.BaseAp),
            PlayerAp = Math.Max(6, CombatRules.BaseAp),
            IsPlayerTurn = true,
            PlayerStamina = p.Stamina,
            PlayerMaxStamina = p.MaxStamina,
            PlayerHunger = p.Hunger,
            PlayerThirst = p.Thirst,
            PlayerWeight = p.CurrentWeight,
            PlayerMaxWeight = p.MaxWeight,
            PlayerDefense = (p.Helmet?.Defense ?? 0) + (p.Armor?.Defense ?? 0) + (p.Suit?.Defense ?? 0) + PerkEffects.DefenseBonus(p),
            PlayerDamage = 5 + (p.Weapon?.Damage ?? 0) + PerkEffects.DamageBonus(p),
        };

        for (int y = 0; y < ch; y++)
            for (int x = 0; x < cw; x++)
                combat.Tiles.Add(new CombatTile { X = x, Y = y });

        // Игрок появляется у "входа" (нижняя сторона), выход там же.
        combat.PlayerX = Math.Clamp(cw / 2, 1, Math.Max(1, cw - 2));
        combat.PlayerY = Math.Max(0, ch - 2);
        if (!IsCombatTileWalkable(combat, combat.PlayerX, combat.PlayerY))
        {
            combat.PlayerX = 1; combat.PlayerY = 1;
        }
        raid.CombatReturnX = raid.PlayerX;
        raid.CombatReturnY = raid.PlayerY;

        // Немного обстановки/укрытий для вида (не блокировать всё).
        int decor = Math.Clamp((cw * ch) / 18, 0, 4);
        for (int i = 0; i < decor; i++)
        {
            for (int tries = 0; tries < 60; tries++)
            {
                int x = _rng.Next(0, cw);
                int y = _rng.Next(0, ch);
                if (Math.Abs(x - combat.PlayerX) <= 1 && Math.Abs(y - combat.PlayerY) <= 1) continue;
                var t = GetCombatTile(combat, x, y);
                if (t == null || t.Type != CombatTileTypes.Empty) continue;
                t.Type = _rng.NextDouble() < 0.5 ? CombatTileTypes.Sandbag : CombatTileTypes.Wall;
                break;
            }
        }

        bool PlaceObject(CombatObjectDto obj, bool preferInner = true)
        {
            for (int tries = 0; tries < 220; tries++)
            {
                int x = preferInner ? _rng.Next(0, cw) : _rng.Next(0, cw);
                int y = preferInner ? _rng.Next(0, Math.Max(1, ch - 1)) : _rng.Next(0, ch);
                if (Math.Abs(x - combat.PlayerX) + Math.Abs(y - combat.PlayerY) <= 1) continue;
                if (!IsCombatTileWalkable(combat, x, y)) continue;
                if (combat.Objects.Any(o => o.X == x && o.Y == y)) continue;
                obj.X = x; obj.Y = y;
                combat.Objects.Add(obj);
                return true;
            }
            return false;
        }

        foreach (var f in b.Furniture)
        {
            PlaceObject(new CombatObjectDto
            {
                Id = f.Id,
                Kind = CombatObjectKinds.Furniture,
                Name = f.Name,
                Icon = f.Icon,
                Sprite = f.Sprite,
                Looted = f.Looted,
                BlocksMovement = true,
            });
        }

        if (b.HasStash && !b.StashLooted)
        {
            PlaceObject(new CombatObjectDto
            {
                Id = "building_stash",
                Kind = CombatObjectKinds.Stash,
                Name = "Тайник",
                Icon = "🗄️",
                Sprite = "/blocks/stash.png",
                Looted = false,
                BlocksMovement = false,
            });
        }

        if (!b.Looted && (b.Loot.Count > 0 || b.Money > 0 || b.Xp > 0))
        {
            PlaceObject(new CombatObjectDto
            {
                Id = "main_loot",
                Kind = CombatObjectKinds.MainLoot,
                Name = "Полезный хлам",
                Icon = "📦",
                Sprite = "/blocks/деревянный_ящик.png",
                Looted = false,
                BlocksMovement = false,
            });
        }

        var exitObj = new CombatObjectDto
        {
            Id = "exit",
            Kind = CombatObjectKinds.Exit,
            Name = "Выход",
            Icon = "🚪",
            Sprite = "/blocks/exit.png",
            Looted = false,
            BlocksMovement = false,
            X = combat.PlayerX,
            Y = Math.Clamp(combat.PlayerY + 1, 0, ch - 1),
        };
        // Дверь всегда у края и доступна: ставим у нижнего края рядом с точкой входа.
        exitObj.Y = ch - 1;
        exitObj.X = Math.Clamp(combat.PlayerX, 0, cw - 1);
        // Убедимся, что клетка двери проходима и не занята.
        var exitTile = GetCombatTile(combat, exitObj.X, exitObj.Y);
        if (exitTile != null) exitTile.Type = CombatTileTypes.Empty;
        combat.Objects.RemoveAll(o => o.X == exitObj.X && o.Y == exitObj.Y);
        // Очистим путь от игрока до двери.
        int py = combat.PlayerY;
        while (py <= exitObj.Y)
        {
            var pt = GetCombatTile(combat, exitObj.X, py);
            if (pt != null) pt.Type = CombatTileTypes.Empty;
            combat.Objects.RemoveAll(o => o.X == exitObj.X && o.Y == py && o.Id != "exit");
            py++;
        }
        combat.Objects.Add(exitObj);

        b.Entered = true;
        combat.Log.Add($"🚪 Вы вошли в {b.Name}.");
        if (combat.Objects.Any(o => o.Kind == CombatObjectKinds.Furniture && !o.Looted))
            combat.Log.Add("Подойдите к мебели и обыщите её.");
        if (combat.Objects.Any(o => o.Kind == CombatObjectKinds.Stash && !o.Looted))
            combat.Log.Add("🗄 Внутри есть тайник.");

        raid.Combat = combat;
    }

    private (bool ok, string message, List<Item> loot) ResolveInteriorInteract(RaidState raid, CombatStateDto c, Player p, CombatObjectDto obj)
    {
        var b = raid.Buildings.FirstOrDefault(x => x.Id == c.BuildingId);
        if (b == null) return (false, "Здание не найдено.", new List<Item>());

        if (obj.Kind == CombatObjectKinds.Exit)
        {
            c.Finished = true;
            c.PlayerWon = true;
            raid.Combat = null;
            _state.CurrentCombat = null;
            return (true, $"Вы вышли из {b.Name}.", new List<Item>());
        }

        if (obj.Looted) return (false, "Здесь уже обыскано.", new List<Item>());

        var taken = new List<Item>();
        if (obj.Kind == CombatObjectKinds.Furniture)
        {
            var f = b.Furniture.FirstOrDefault(x => x.Id == obj.Id);
            if (f == null) return (false, "Объект не найден.", new List<Item>());
            foreach (var item in f.Loot.ToList())
            {
                item.FoundInRaid = true;
                if (InventoryHelper.AddToInventory(p, item)) taken.Add(item);
                else AddLog(c, "🎒 Инвентарь полон — часть добычи осталась.");
            }
            f.Loot.RemoveAll(i => taken.Any(t => t.Id == i.Id));
            if (f.Loot.Count == 0)
            {
                p.Money += f.Money; p.Xp += f.Xp; f.Money = 0; f.Xp = 0; f.Looted = true; obj.Looted = true;
            }
            PlayerProgression.Apply(p);
            AddLog(c, taken.Count > 0 ? $"{obj.Icon} {obj.Name}: найдено {taken.Count} предмет(ов)." : $"{obj.Icon} {obj.Name}: пусто.");
            return (true, taken.Count > 0 ? $"{obj.Name}: добыча забрана." : $"{obj.Name}: пусто.", taken);
        }

        if (obj.Kind == CombatObjectKinds.Stash)
        {
            if (!b.HasStash || b.StashLooted) { obj.Looted = true; return (false, "Тайник уже обыскан.", new List<Item>()); }
            foreach (var item in b.StashLoot.ToList())
            {
                item.FoundInRaid = true;
                if (InventoryHelper.AddToInventory(p, item)) taken.Add(item);
                else AddLog(c, "🎒 Инвентарь полон — часть добычи осталась.");
            }
            b.StashLoot.RemoveAll(i => taken.Any(t => t.Id == i.Id));
            if (b.StashLoot.Count == 0)
            {
                p.Money += b.StashMoney; p.Xp += b.StashXp; b.StashMoney = 0; b.StashXp = 0; b.StashLooted = true; obj.Looted = true; p.Stats.StashesUsed += 1;
            }
            PlayerProgression.Apply(p);
            AddLog(c, taken.Count > 0 ? $"🗄 Тайник обыскан." : "🗄 Тайник пуст.");
            return (true, taken.Count > 0 ? "Тайник обыскан." : "Тайник пуст.", taken);
        }

        if (obj.Kind == CombatObjectKinds.MainLoot)
        {
            if (b.Looted) { obj.Looted = true; return (false, "Здесь уже всё обыскано.", new List<Item>()); }
            foreach (var item in b.Loot.ToList())
            {
                item.FoundInRaid = true;
                if (InventoryHelper.AddToInventory(p, item)) taken.Add(item);
                else AddLog(c, "🎒 Инвентарь полон — часть добычи осталась.");
            }
            b.Loot.RemoveAll(i => taken.Any(t => t.Id == i.Id));
            if (b.Loot.Count == 0)
            {
                p.Money += b.Money; p.Xp += b.Xp; b.Money = 0; b.Xp = 0; b.Looted = true; obj.Looted = true;
            }
            PlayerProgression.Apply(p);
            AddLog(c, taken.Count > 0 ? $"📦 Найдены вещи ({taken.Count})." : "📦 Здесь пусто.");
            return (true, taken.Count > 0 ? "Добыча забрана." : "Забирать нечего.", taken);
        }

        return (false, "Не удалось взаимодействовать.", new List<Item>());
    }

    private void EnsureBossIntelDrop(Enemy enemy, string locationId)
    {
        string? code = (locationId ?? "").ToLowerInvariant() switch
        {
            "city" => "boss_pda_city",
            "kordon" => "boss_flash_kordon",
            "factory" => "boss_pda_factory",
            "mall" => "boss_flash_mall",
            "military" => "boss_pda_military",
            "predbannik" => "boss_flash_predbannik",
            "forest" => "boss_pda_forest",
            "outskirts" => "boss_flash_outskirts",
            "suburb" => "boss_pda_suburb",
            "gradirni" => "boss_flash_gradirni",
            _ => null,
        };
        if (code == null) return;
        if (enemy.Loot.Any(i => string.Equals(i.Code, code, StringComparison.OrdinalIgnoreCase))) return;
        var item = _catalog.Create(code);
        if (item == null) return;
        item.FoundInRaid = true;
        enemy.Loot.Insert(0, item);
    }

    private Enemy GenerateEnemy(Location loc, int t, bool isBoss = false)
    {
        double levelScale = 1.0 + Math.Max(0, _state.Player.Level - 1) * 0.045;

        if (isBoss)
        {
            int hp = (int)Math.Round((60 + t * 30 + _rng.Next(0, 20)) * levelScale);
            return new Enemy
            {
                Name = string.IsNullOrWhiteSpace(loc.BossName) ? "БОСС" : loc.BossName,
                Icon = string.IsNullOrWhiteSpace(loc.BossIcon) ? "/enemies/boss.png" : loc.BossIcon,
                Hp = hp, MaxHp = hp,
                Damage = Math.Max(8, (int)Math.Round((14 + t * 6) * levelScale)),
                Defense = 2 + t, IsBoss = true, Ranged = t >= 4, Range = t >= 4 ? CombatRules.RangedRange : 1,
                ApMax = 6, Ap = 6,
            };
        }

        // Мутанты живут только на карте локации и не должны попадаться внутри зданий.
        var pool = loc.Enemies.Where(e => !IsMutantTemplate(e)).ToList();
        if (pool.Count == 0) pool = new List<EnemyTemplate> { new() { Name = "Враг", Icon = "/enemies/bandit.png" } };

        double total = pool.Sum(e => e.Weight);
        double roll = _rng.NextDouble() * total;
        double acc = 0;
        EnemyTemplate chosen = pool[0];
        foreach (var e in pool) { acc += e.Weight; if (roll <= acc) { chosen = e; break; } }

        int hpN = (int)Math.Round((chosen.BaseHp + (t - 1) * 10 + _rng.Next(0, 8)) * levelScale);
        int dmg = Math.Max(2, (int)Math.Round((chosen.BaseDamage + (t - 1) * 3 + _rng.Next(0, 4)) * levelScale));

        // Используем иконку шаблона (важно для мутантов/типовых врагов), иначе случайная.
        string icon = !string.IsNullOrWhiteSpace(chosen.Icon)
            ? chosen.Icon
            : $"/enemies/{_enemySprites[_rng.Next(_enemySprites.Length)]}.png";

        return new Enemy
        {
            Name = chosen.Name, Icon = icon, Hp = hpN, MaxHp = hpN, Damage = dmg,
            Defense = chosen.BaseDefense + Math.Max(0, t - 1), Ranged = chosen.Ranged,
            Range = chosen.Ranged ? CombatRules.RangedRange : 1, ApMax = 5, Ap = 5,
        };
    }

    // ============================================================
    //  БОЙ: ГЕОМЕТРИЯ И ПОИСК ПУТИ
    // ============================================================

    private static int Chebyshev(int x0, int y0, int x1, int y1) =>
        Math.Max(Math.Abs(x1 - x0), Math.Abs(y1 - y0));

    private static CombatTile? GetCombatTile(CombatStateDto c, int x, int y) =>
        c.Tiles.FirstOrDefault(t => t.X == x && t.Y == y);

    private static bool IsCombatTileWalkable(CombatStateDto c, int x, int y)
    {
        if (x < 0 || y < 0 || x >= c.Width || y >= c.Height) return false;
        var t = GetCombatTile(c, x, y);
        return t != null && t.Type == CombatTileTypes.Empty;
    }

    private static bool IsCellBlockedByObject(CombatStateDto c, int x, int y) =>
        c.Objects.Any(o => o.BlocksMovement && o.X == x && o.Y == y);

    private static bool CanEnterCombatCell(CombatStateDto c, int x, int y)
    {
        if (!IsCombatTileWalkable(c, x, y)) return false;
        if (x == c.PlayerX && y == c.PlayerY) return false;
        if (IsCellBlockedByObject(c, x, y)) return false;
        return !c.Enemies.Any(e => e.X == x && e.Y == y);
    }

    private static bool HasLineOfSight(CombatStateDto c, int x0, int y0, int x1, int y1)
    {
        int dx = Math.Abs(x1 - x0);
        int dy = Math.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;
        int cx = x0, cy = y0;

        while (!(cx == x1 && cy == y1))
        {
            int e2 = 2 * err;
            if (e2 > -dy) { err -= dy; cx += sx; }
            if (e2 < dx) { err += dx; cy += sy; }
            if (cx == x1 && cy == y1) break;

            var t = GetCombatTile(c, cx, cy);
            if (t == null || t.Type is CombatTileTypes.Wall or CombatTileTypes.Sandbag)
                return false;

            // Укрытия/объекты интерьера перекрывают линию огня.
            if (c.Objects.Any(o => o.BlocksMovement && o.X == cx && o.Y == cy))
                return false;
        }
        return true;
    }


    private void PlaceWallShapes(CombatStateDto combat, int cw, int ch, int safeR, int tileBudget)
    {
        (int dx, int dy)[][] shapes = {
            new[]{(0,0),(1,0)}, new[]{(0,0),(0,1)}, // ряд из 2
            new[]{(0,0),(1,0),(0,1)}, new[]{(0,0),(1,0),(1,1)}, // уголки из 3
            new[]{(0,0),(0,1),(1,1)}, new[]{(0,0),(0,1),(-1,1)},
            new[]{(0,0),(1,0),(0,-1)}, new[]{(0,0),(-1,0),(0,1)},
        };
        int placed = 0;
        for (int tries = 0; tries < Math.Max(200, tileBudget * 120) && placed < tileBudget; tries++)
        {
            var shape = shapes[_rng.Next(shapes.Length)];
            int remaining = tileBudget - placed;
            if (shape.Length > remaining)
            {
                if (remaining < 2) break;
                shape = _rng.Next(2) == 0 ? new[] { (0, 0), (1, 0) } : new[] { (0, 0), (0, 1) };
            }
            int ax = _rng.Next(0, cw), ay = _rng.Next(0, ch);
            bool ok = true;
            foreach (var (dx, dy) in shape)
            {
                int x = ax + dx, y = ay + dy;
                if (x < 0 || y < 0 || x >= cw || y >= ch) { ok = false; break; }
                if (x == combat.PlayerX && y == combat.PlayerY) { ok = false; break; }
                if (Math.Abs(x - combat.PlayerX) <= safeR && Math.Abs(y - combat.PlayerY) <= safeR) { ok = false; break; }
                var t = GetCombatTile(combat, x, y);
                if (t == null || t.Type != CombatTileTypes.Empty) { ok = false; break; }
            }
            if (!ok) continue;
            foreach (var (dx, dy) in shape)
                GetCombatTile(combat, ax + dx, ay + dy)!.Type = CombatTileTypes.Wall;
            placed += shape.Length;
        }
    }

    private void PlaceCoverSingles(CombatStateDto combat, int cw, int ch, int safeR, string type, int count)
    {
        for (int i = 0; i < count; i++)
        {
            for (int tries = 0; tries < 160; tries++)
            {
                int x = _rng.Next(0, cw);
                int y = _rng.Next(0, ch);
                if (x == combat.PlayerX && y == combat.PlayerY) continue;
                if (Math.Abs(x - combat.PlayerX) <= safeR && Math.Abs(y - combat.PlayerY) <= safeR) continue;
                var t = GetCombatTile(combat, x, y);
                if (t == null || t.Type != CombatTileTypes.Empty) continue;
                t.Type = type;
                break;
            }
        }
    }

    private static double CoverReductionAt(CombatStateDto c, int x, int y, Player? p)
    {
        bool sandbag = false, wall = false;
        foreach (var (dx, dy) in Dirs8)
        {
            var t = GetCombatTile(c, x + dx, y + dy);
            if (t == null) continue;
            if (t.Type == CombatTileTypes.Sandbag) sandbag = true;
            if (t.Type == CombatTileTypes.Wall) wall = true;
        }

        double reduction = wall ? 0.35 : sandbag ? 0.25 : 0;
        if (p != null) reduction += PerkEffects.CoverReduction(p);
        return Math.Min(0.60, reduction);
    }

    private static List<(int X, int Y)> FindPath(CombatStateDto c, Enemy enemy, int targetX, int targetY, bool allowTarget)
    {
        var start = (enemy.X, enemy.Y);
        var goal = (X: targetX, Y: targetY);
        var came = new Dictionary<(int X, int Y), (int X, int Y)>();
        var seen = new HashSet<(int X, int Y)> { start };
        var queue = new Queue<(int X, int Y)>();
        queue.Enqueue(start);

        // Ближний бой: цель — подойти вплотную к игроку, а не встать на его клетку.
        bool ReachedGoal(int x, int y) => allowTarget
            ? x == goal.X && y == goal.Y
            : !(x == start.X && y == start.Y) && Chebyshev(x, y, goal.X, goal.Y) <= 1;

        (int X, int Y)? end = null;
        if (ReachedGoal(start.X, start.Y)) end = start;

        while (end == null && queue.Count > 0)
        {
            var (x, y) = queue.Dequeue();

            foreach (var (dx, dy) in Dirs8)
            {
                int nx = x + dx, ny = y + dy;
                if (nx < 0 || ny < 0 || nx >= c.Width || ny >= c.Height) continue;
                if (seen.Contains((nx, ny))) continue;
                if (!allowTarget && nx == goal.X && ny == goal.Y) continue;

                var t = GetCombatTile(c, nx, ny);
                if (t == null || t.Type != CombatTileTypes.Empty) continue;
                if (c.Enemies.Any(e => e.Hp > 0 && e.X == nx && e.Y == ny)) continue;

                seen.Add((nx, ny));
                came[(nx, ny)] = (x, y);
                if (ReachedGoal(nx, ny))
                {
                    end = (nx, ny);
                    break;
                }
                queue.Enqueue((nx, ny));
            }
        }

        if (end is not { } final) return new List<(int X, int Y)>();

        var path = new List<(int X, int Y)>();
        var cur = final;
        while (cur != start)
        {
            path.Add(cur);
            if (!came.TryGetValue(cur, out var prev)) return new List<(int X, int Y)>();
            cur = prev;
        }
        path.Reverse();
        return path;
    }

    // ============================================================
    //  УТИЛИТЫ
    // ============================================================

    private static void ClearFoundInRaidStatus(Player p)
    {
        foreach (var i in p.Inventory)
            i.FoundInRaid = false;

        Item?[] equipped = { p.WeaponMelee, p.WeaponRanged, p.Helmet, p.Armor, p.Suit, p.Backpack };
        foreach (var e in equipped)
            if (e != null) e.FoundInRaid = false;
    }

    private static int CountAmmo(Player p, string? caliber) =>
        p.Inventory.Where(i => i.Type == ItemType.Ammo
                              && string.Equals(i.Caliber, caliber, StringComparison.OrdinalIgnoreCase))
                   .Sum(i => Math.Max(1, i.Count));

    private static bool ConsumeAmmo(Player p, string? caliber, int count)
    {
        if (CountAmmo(p, caliber) < count) return false;
        int left = count;
        foreach (var stack in p.Inventory
                     .Where(i => i.Type == ItemType.Ammo
                                 && string.Equals(i.Caliber, caliber, StringComparison.OrdinalIgnoreCase))
                     .ToList())
        {
            if (left <= 0) break;
            int take = Math.Min(left, Math.Max(1, stack.Count));
            InventoryHelper.RemoveFromInventory(p, stack.Id, take);
            left -= take;
        }
        return true;
    }
}
