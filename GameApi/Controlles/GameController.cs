using GameApi.Models;
using GameApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace GameApi.Controllers;

[ApiController]
[Route("api")]
public class GameController : ControllerBase
{
    private readonly GameStateService _state;
    private readonly ItemCatalog _catalog;
    private readonly QuestService _questSvc;

    public GameController(GameStateService s, ItemCatalog c, QuestService q)
        => (_state, _catalog, _questSvc) = (s, c, q);

    // --- Игрок ---
    [HttpGet("player")]
    public Player GetPlayer()
    {
        AchievementCatalog.Evaluate(_state);
        return _state.Player;
    }

    [HttpGet("achievements")]
    public object GetAchievements()
    {
        AchievementCatalog.Evaluate(_state);
        var p = _state.Player;

        return new
        {
            unlocked = p.Achievements.Count,
            total = AchievementCatalog.All.Count,
            definitions = AchievementCatalog.All.Select(a => new
            {
                a.Id,
                a.Name,
                a.Icon,
                a.Description,
                unlocked = p.Achievements.Contains(a.Id),
            }),
        };
    }

    [HttpPost("player/avatar")]
    public IActionResult SetAvatar([FromBody] AvatarDto dto)
    {
        var p = _state.Player;
        if (string.IsNullOrWhiteSpace(dto.Avatar)) return BadRequest("Пустой id аватара");
        p.Avatar = dto.Avatar;
        return Ok(p);
    }

    public record OnboardDto(string Name, string Avatar);
    [HttpPost("player/onboard")]
    public IActionResult Onboard([FromBody] OnboardDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name) || dto.Name.Length < 2 || dto.Name.Length > 20)
            return BadRequest("Имя должно быть от 2 до 20 символов");
        var p = _state.Player;
        p.Name = dto.Name.Trim();
        if (!string.IsNullOrWhiteSpace(dto.Avatar)) p.Avatar = dto.Avatar;
        p.Onboarded = true;
        return Ok(p);
    }

    public record AvatarDto(string Avatar);

    [HttpPost("dev/reset")]
    public Player ResetGame()
    {
        _state.Reset();
        return _state.Player;
    }

    [HttpPost("player/sync")]
    public Player SyncPlayer([FromBody] Player p)
    {
        var cur = _state.Player;

        int moneyBefore = cur.Money;
        int hpBefore = cur.Hp;

        cur.Hp = Math.Clamp(p.Hp, 0, cur.MaxHp);
        cur.Stamina = Math.Clamp(p.Stamina, 0, cur.MaxStamina);
        cur.Hunger = Math.Clamp(p.Hunger, 0, 100);
        cur.Thirst = Math.Clamp(p.Thirst, 0, 100);

        cur.Money = Math.Max(0, p.Money);
        cur.Xp = Math.Max(cur.Xp, p.Xp); // опыт не откатывается назад

        if (p.Inventory != null) cur.Inventory = p.Inventory;

        // Найденные кассеты остаются в коллекции даже после продажи/смерти.
        foreach (var cassette in cur.Inventory
                     .Concat(cur.Shelter.Stash)
                     .Where(i => i.Type == ItemType.Cassette))
        {
            if (!cur.Cassettes.Contains(cassette.Code))
                cur.Cassettes.Add(cassette.Code);
        }

        cur.WeaponMelee = p.WeaponMelee;
        cur.WeaponRanged = p.WeaponRanged;
        cur.Helmet = p.Helmet;
        cur.Armor = p.Armor;
        cur.Backpack = p.Backpack;

        int earned = cur.Money - moneyBefore;
        if (earned > 0) cur.Stats.MoneyEarned += earned;

        int damaged = hpBefore - cur.Hp;
        if (damaged > 0) cur.Stats.HpDamaged += damaged;

        PlayerProgression.Apply(cur);
        return cur;
    }

    [HttpPost("player/die")]
    public Player Die()
    {
        var p = _state.Player;
        // Ближний бой сохраняется, остальное теряется.
        p.WeaponRanged = null;
        p.Helmet = null; p.Armor = null; p.Backpack = null;
        p.Inventory.Clear();
        if (p.WeaponMelee != null) p.Inventory.Add(p.WeaponMelee);
        p.WeaponMelee = null;
        p.Hp = Math.Max(1, p.MaxHp / 10);
        p.Stamina = Math.Max(10, p.MaxStamina / 3);
        p.Hunger = Math.Max(0, p.Hunger - 20);
        p.Thirst = Math.Max(0, p.Thirst - 20);
        p.Stats.RaidsFailed += 1;

        _state.CurrentCombat = null;
        _state.PendingEnemy = null;
        AchievementCatalog.Evaluate(_state);
        return p;
    }

    // --- Инвентарь / экипировка ---
    [HttpPost("equip/{itemId}")]
    public IActionResult Equip(string itemId)
    {
        var p = _state.Player;
        var item = p.Inventory.FirstOrDefault(i => i.Id == itemId);
        if (item == null) return NotFound("Предмет не найден");

        p.Inventory.Remove(item);
        switch (item.Type)
        {
            case ItemType.WeaponMelee:
                if (p.WeaponMelee != null) p.Inventory.Add(p.WeaponMelee);
                p.WeaponMelee = item; break;
            case ItemType.WeaponRanged:
                if (p.WeaponRanged != null) p.Inventory.Add(p.WeaponRanged);
                p.WeaponRanged = item; break;
            case ItemType.Helmet:
                if (p.Helmet != null) p.Inventory.Add(p.Helmet);
                p.Helmet = item; break;
            case ItemType.Armor:
                if (p.Armor != null) p.Inventory.Add(p.Armor);
                p.Armor = item; break;
            case ItemType.Backpack:
                int newCapacity = Player.BaseInventorySlots + (item.BackpackSlots > 0 ? item.BackpackSlots : 0);
                if (p.Inventory.Count > newCapacity)
                {
                    p.Inventory.Add(item);
                    return BadRequest("В этом рюкзаке не хватит места для вещей");
                }
                if (p.Backpack != null) p.Inventory.Add(p.Backpack);
                p.Backpack = item; break;
            default:
                p.Inventory.Add(item); return BadRequest("Нельзя экипировать");
        }
        return Ok(p);
    }

    [HttpPost("unequip/{slot}")]
    public IActionResult Unequip(string slot)
    {
        var p = _state.Player;
        Item? item = slot switch
        {
            "weaponMelee" => p.WeaponMelee,
            "weaponRanged" => p.WeaponRanged,
            "helmet" => p.Helmet,
            "armor" => p.Armor,
            "backpack" => p.Backpack,
            _ => null
        };
        if (item == null) return NotFound();
        if (p.Inventory.Count >= p.InventoryCapacity) return BadRequest("Инвентарь полон");

        switch (slot)
        {
            case "weaponMelee": p.WeaponMelee = null; break;
            case "weaponRanged": p.WeaponRanged = null; break;
            case "helmet": p.Helmet = null; break;
            case "armor": p.Armor = null; break;
            case "backpack": p.Backpack = null; break;
        }
        p.Inventory.Add(item);
        return Ok(p);
    }

    [HttpPost("consume/{itemId}")]
    public IActionResult Consume(string itemId)
    {
        var p = _state.Player;
        var item = p.Inventory.FirstOrDefault(i => i.Id == itemId);
        if (item == null) return NotFound();
        if (item.Type is not (ItemType.Food or ItemType.Drink or ItemType.Medicine))
            return BadRequest("Это нельзя использовать");
        if (string.IsNullOrWhiteSpace(item.Restores))
            return BadRequest("Это нельзя использовать");

        switch (item.Restores)
        {
            case "hp":
                if (p.Hp >= p.MaxHp) return BadRequest("Здоровье уже полное");
                int healPower = Math.Max(1, (int)Math.Round(item.Restore * PerkEffects.HealMultiplier(p)));
                int healed = Math.Min(healPower, p.MaxHp - p.Hp);
                p.Hp += healed; p.Stats.HpHealed += healed; break;
            case "hunger":
                if (p.Hunger >= 100) return BadRequest("Ты не голоден");
                p.Hunger = Math.Min(100, p.Hunger + item.Restore);
                p.Stats.FoodEaten += 1; break;
            case "thirst":
                if (p.Thirst >= 100) return BadRequest("Ты не хочешь пить");
                p.Thirst = Math.Min(100, p.Thirst + item.Restore); break;
            default:
                return BadRequest("Это нельзя использовать");
        }

        InventoryHelper.RemoveFromInventory(p, itemId, 1);
        return Ok(p);
    }

    // --- Продажа (с учётом правил торговца) ---
    public record SellDto(string ItemId, int Count = 1, string? TraderId = null);

    private Trader? TraderById(string? id) =>
        _state.Traders.FirstOrDefault(t => t.Id == (string.IsNullOrWhiteSpace(id) ? "buyer" : id));

    private static bool TraderBuys(Trader trader, Item item) =>
        trader.BuysEverything || trader.Buys.Contains(item.Type);

    private static int SellUnitPrice(Item item, Trader trader, Player p) =>
        Math.Max(1, (int)Math.Floor(item.Price * trader.BuyRate * PerkEffects.SellMultiplier(p)));

    [HttpPost("sell/{itemId}")]
    public IActionResult Sell(string itemId, [FromQuery] string? traderId = null)
    {
        var p = _state.Player;
        var item = p.Inventory.FirstOrDefault(i => i.Id == itemId);
        if (item == null) return NotFound();

        var trader = TraderById(traderId);
        if (trader == null) return NotFound("Торговец не найден");
        if (!TraderBuys(trader, item))
            return BadRequest($"{trader.Name} не покупает такие вещи");

        int earned = SellUnitPrice(item, trader, p) * item.Count;
        p.Money += earned;
        p.Stats.MoneyEarned += earned;
        p.Stats.SalesMade += 1;
        p.Inventory.Remove(item);
        TraderLoyalty.Add(p, trader.Id, Math.Max(1, earned / 200));
        return Ok(p);
    }

    [HttpPost("sell-partial")]
    public IActionResult SellPartial([FromBody] SellDto dto)
    {
        var p = _state.Player;
        var item = p.Inventory.FirstOrDefault(i => i.Id == dto.ItemId);
        if (item == null) return NotFound();

        var trader = TraderById(dto.TraderId);
        if (trader == null) return NotFound("Торговец не найден");
        if (!TraderBuys(trader, item))
            return BadRequest($"{trader.Name} не покупает такие вещи");
        if (dto.Count < 1 || dto.Count > item.Count) return BadRequest("Неверное количество");

        int earned = SellUnitPrice(item, trader, p) * dto.Count;
        item.Count -= dto.Count;
        if (item.Count <= 0) p.Inventory.Remove(item);
        p.Money += earned; p.Stats.MoneyEarned += earned; p.Stats.SalesMade += 1;
        return Ok(p);
    }

    public record AddItemsDto(List<Item> Items);
    [HttpPost("inventory/add")]
    public Player AddItems([FromBody] AddItemsDto dto)
    {
        var p = _state.Player;
        foreach (var it in dto.Items) InventoryHelper.AddToInventory(p, it);
        return p;
    }

    [HttpPost("inventory/drop/{itemId}")]
    public IActionResult DropItem(string itemId)
    {
        var p = _state.Player;
        var item = p.Inventory.FirstOrDefault(i => i.Id == itemId);
        if (item == null) return NotFound("Предмет не найден");

        InventoryHelper.RemoveFromInventory(p, itemId, item.Count);
        return Ok(p);
    }

    // --- Торговцы ---
    [HttpGet("traders")]
    public object GetTraders()
    {
        var p = _state.Player;
        foreach (var t in _state.Traders)
        {
            t.LoyaltyLevel = TraderLoyalty.Level(p, t.Id);
            t.LoyaltyRep = TraderLoyalty.Rep(p, t.Id);
            t.LoyaltyCurrent = TraderLoyalty.CurrentLevelFloor(p, t.Id);
            t.LoyaltyNext = TraderLoyalty.NextThreshold(p, t.Id);
        }

        return new
        {
            traders = _state.Traders,
            buyMultiplier = PerkEffects.BuyMultiplier(p),
            sellMultiplier = PerkEffects.SellMultiplier(p),
        };
    }

    public record BuyBatchEntry(string Code, int Count);
    [HttpPost("traders/{traderId}/buy-batch")]
    public IActionResult BuyBatch(string traderId, [FromBody] List<BuyBatchEntry> entries)
    {
        var trader = _state.Traders.FirstOrDefault(t => t.Id == traderId);
        var p = _state.Player;
        if (trader == null) return NotFound("Торговец не найден");
        if (entries == null || entries.Count == 0) return BadRequest("Пустой заказ");

        int totalCost = 0;
        var toBuy = new List<(Item template, int count)>();

        foreach (var e in entries)
        {
            var stock = trader.Stock.FirstOrDefault(i => i.Code == e.Code);
            if (stock == null) return BadRequest($"Нет в продаже: {e.Code}");
            if (e.Count < 1) continue;

            int loyalty = TraderLoyalty.Level(p, trader.Id);
            if (stock.RequiredLoyalty > loyalty)
                return BadRequest($"Нужна лояльность {stock.RequiredLoyalty} у торговца {trader.Name}");

            totalCost += BuyPrice(stock, trader, p) * e.Count;
            toBuy.Add((stock, e.Count));
        }

        if (p.Money < totalCost) return BadRequest($"Не хватает денег: нужно {totalCost} ₽");

        var backup = CloneInventory(p);

        foreach (var (stock, count) in toBuy)
        {
            for (int i = 0; i < count; i++)
            {
                var fresh = _catalog.Create(stock.Code);
                if (!InventoryHelper.AddToInventory(p, fresh))
                {
                    p.Inventory.Clear(); p.Inventory.AddRange(backup);
                    return BadRequest("Инвентарь полон");
                }
            }
        }

        p.Money -= totalCost;
        p.Stats.MoneySpent += totalCost;
        TraderLoyalty.Add(p, trader.Id, Math.Max(1, totalCost / 500));
        return Ok(p);
    }

    private static int BuyPrice(Item stock, Trader trader, Player p) =>
        Math.Max(1, (int)Math.Round(stock.Price * trader.PriceMultiplier * PerkEffects.BuyMultiplier(p)));

    [HttpPost("traders/{traderId}/barter/{offerId}")]
    public IActionResult Barter(string traderId, string offerId)
    {
        var trader = _state.Traders.FirstOrDefault(t => t.Id == traderId);
        var p = _state.Player;
        if (trader == null) return NotFound("Торговец не найден");

        var offer = trader.Barter.FirstOrDefault(b => b.Id == offerId);
        if (offer == null) return NotFound("Обмен не найден");

        int barterLoyalty = TraderLoyalty.Level(p, trader.Id);
        if (offer.RequiredLoyalty > barterLoyalty)
            return BadRequest($"Нужна лояльность {offer.RequiredLoyalty} у торговца {trader.Name}");

        foreach (var cost in offer.Cost)
        {
            if (InventoryHelper.CountByCode(p, cost.Code) < cost.Count)
            {
                var name = _catalog.Get(cost.Code)?.Name ?? cost.Code;
                return BadRequest($"Не хватает: {name} ×{cost.Count}");
            }
        }
        if (p.Money < offer.Money) return BadRequest($"Нужно {offer.Money} ₽");

        // Проверяем место под награду заранее.
        var gift = _catalog.Create(offer.GivesCode);
        bool willStack = InventoryHelper.IsStackable(gift) && p.Inventory.Any(i => i.Code == gift.Code);
        if (!willStack && p.Inventory.Count >= p.InventoryCapacity)
            return BadRequest("Инвентарь полон");

        foreach (var cost in offer.Cost)
            InventoryHelper.RemoveAllByCode(p, cost.Code, cost.Count);

        p.Money -= offer.Money;
        p.Stats.MoneySpent += offer.Money;
        TraderLoyalty.Add(p, trader.Id, 10);

        for (int i = 0; i < Math.Max(1, offer.GivesCount); i++)
        {
            var item = _catalog.Create(offer.GivesCode);
            if (!InventoryHelper.AddToInventory(p, item))
            {
                // Возвращаем деньги, если предмет не влез (ресурсы уже списаны — компенсируем деньгами).
                p.Money += offer.Money;
                return BadRequest("Инвентарь полон");
            }
        }

        return Ok(p);
    }

    // --- Убежище ---
    private const int MaxShelterLevel = 5;

    private static readonly Dictionary<string, (string Name, string Icon, string Effect, Func<ShelterState, int> Get)> ShelterModules = new()
    {
        ["medblock"] = ("Мед. блок", "💉", "+5 HP к пассивному восстановлению за уровень", s => s.MedBlockLevel),
        ["rest"] = ("Зона отдыха", "🛏", "+1 выносливости за тик", s => s.RestZoneLevel),
        ["kitchen"] = ("Кухня", "🍳", "+1 голода за тик", s => s.KitchenLevel),
        ["water"] = ("Водоочистка", "💧", "+1 жажды за тик", s => s.WaterLevel),
        ["stash"] = ("Схрон", "📦", "+8 слотов хранилища", s => s.StashLevel),
        ["workshop"] = ("Мастерская", "🔧", "+3% к продаже, −2% к покупке", s => s.WorkshopLevel),
        ["generator"] = ("Генератор", "⚡", "−5% к стоимости улучшений", s => s.GeneratorLevel),
        ["armory"] = ("Оружейная", "🔫", "+1 урон за 2 уровня", s => s.ArmoryLevel),
        ["gym"] = ("Спортзал", "🏋", "+5 к макс. выносливости за уровень", s => s.GymLevel),
    };

    private static int ShelterLevel(ShelterState s, string id) => ShelterModules[id].Get(s);

    private static void SetShelterLevel(ShelterState s, string id, int level)
    {
        switch (id)
        {
            case "medblock": s.MedBlockLevel = level; break;
            case "rest": s.RestZoneLevel = level; break;
            case "kitchen": s.KitchenLevel = level; break;
            case "water": s.WaterLevel = level; break;
            case "stash": s.StashLevel = level; s.StashSlots += 8; break;
            case "workshop": s.WorkshopLevel = level; break;
            case "generator": s.GeneratorLevel = level; break;
            case "armory": s.ArmoryLevel = level; break;
            case "gym": s.GymLevel = level; break;
        }
    }

    private static int UpgradeMoneyCost(ShelterState s, string id)
    {
        int lvl = ShelterLevel(s, id);
        double discount = Math.Max(0.5, 1.0 - s.GeneratorLevel * 0.05);
        return Math.Max(50, (int)Math.Round(200 * (lvl + 1) * discount));
    }

    // Разные модули требуют разные материалы и разное их количество.
    private static readonly Dictionary<string, List<(string Code, int Need)>> ModuleMaterials = new()
    {
        ["medblock"] = new() { ("scrap", 2), ("cloth", 1), ("wire", 1) },
        ["rest"]     = new() { ("cloth", 3), ("scrap", 2) },
        ["kitchen"]  = new() { ("scrap", 3), ("cloth", 2), ("wire", 1) },
        ["water"]    = new() { ("scrap", 2), ("wire", 2), ("battery", 1) },
        ["stash"]    = new() { ("scrap", 2), ("wire", 2), ("glass", 1) },
        ["workshop"] = new() { ("electronics", 2), ("scrap", 2), ("wire", 1) },
        ["generator"]= new() { ("battery", 2), ("scrap", 2), ("wire", 1), ("fuel", 1) },
        ["armory"]   = new() { ("scrap", 3), ("wire", 2), ("electronics", 1) },
        ["gym"]      = new() { ("scrap", 2), ("cloth", 2), ("cement", 1) },
    };

    private static List<(string Code, int Need)> UpgradeMaterials(ShelterState s, string id)
    {
        int lvl = ShelterLevel(s, id);
        int need = lvl + 1;
        if (ModuleMaterials.TryGetValue(id, out var list) && list.Count > 0)
            return list;
        return new List<(string Code, int Need)> { ("scrap", need), ("cloth", need), ("wire", need) };
    }

    [HttpGet("shelter/info")]
    public object GetShelterInfo()
    {
        var p = _state.Player;
        var s = p.Shelter;

        var modules = ShelterModules.Select(kv =>
        {
            int lvl = ShelterLevel(s, kv.Key);
            var reqs = UpgradeMaterials(s, kv.Key).Select(r => new
            {
                code = r.Code,
                name = _catalog.Get(r.Code)?.Name ?? r.Code,
                need = r.Need,
                have = InventoryHelper.CountByCode(p, r.Code),
            }).ToList();

            return new
            {
                id = kv.Key,
                name = kv.Value.Name,
                icon = kv.Value.Icon,
                effect = kv.Value.Effect,
                level = lvl,
                maxLevel = MaxShelterLevel,
                nextCost = lvl >= MaxShelterLevel ? 0 : UpgradeMoneyCost(s, kv.Key),
                requirements = reqs,
            };
        }).ToList();

        return new { modules, regen = _state.GetRegenInfo(), stashSlots = s.StashSlots };
    }

    [HttpPost("shelter/upgrade/{module}")]
    public IActionResult UpgradeShelter(string module)
    {
        if (!ShelterModules.ContainsKey(module)) return NotFound("Неизвестный модуль");

        var p = _state.Player;
        var s = p.Shelter;
        int lvl = ShelterLevel(s, module);
        if (lvl >= MaxShelterLevel) return BadRequest("Максимальный уровень улучшения");

        int moneyCost = UpgradeMoneyCost(s, module);
        var materials = UpgradeMaterials(s, module);

        foreach (var (code, need) in materials)
        {
            if (InventoryHelper.CountByCode(p, code) < need)
                return BadRequest($"Не хватает: {_catalog.Get(code)?.Name ?? code} ×{need}");
        }
        if (p.Money < moneyCost) return BadRequest($"Нужно {moneyCost} ₽");

        p.Money -= moneyCost; p.Stats.MoneySpent += moneyCost;
        foreach (var (code, need) in materials) InventoryHelper.RemoveAllByCode(p, code, need);

        SetShelterLevel(s, module, lvl + 1);
        PlayerProgression.Apply(p, grantPerkPoints: false);
        AchievementCatalog.Evaluate(_state);
        return Ok(p);
    }

    [HttpGet("shelter/regen-info")]
    public object GetRegenInfo() => _state.GetRegenInfo();

    [HttpPost("stash/store/{itemId}")]
    public IActionResult StoreStash(string itemId)
    {
        var p = _state.Player;
        var item = p.Inventory.FirstOrDefault(i => i.Id == itemId);
        if (item == null) return NotFound();

        if (InventoryHelper.IsStackable(item))
        {
            var existing = p.Shelter.Stash.FirstOrDefault(i => i.Code == item.Code);
            if (existing != null) { existing.Count += item.Count; p.Inventory.Remove(item); return Ok(p); }
        }

        if (p.Shelter.Stash.Count >= p.Shelter.StashSlots) return BadRequest("Схрон полон");
        p.Inventory.Remove(item);
        p.Shelter.Stash.Add(item);
        p.Stats.StashesUsed += 1;
        return Ok(p);
    }

    [HttpPost("stash/take/{itemId}")]
    public IActionResult TakeStash(string itemId)
    {
        var p = _state.Player;
        var item = p.Shelter.Stash.FirstOrDefault(i => i.Id == itemId);
        if (item == null) return NotFound();

        p.Shelter.Stash.Remove(item);
        if (!InventoryHelper.AddToInventory(p, item))
        {
            p.Shelter.Stash.Add(item);
            return BadRequest("Инвентарь полон");
        }
        return Ok(p);
    }

    // --- Перки ---
    [HttpGet("perks")]
    public object GetPerks()
    {
        var p = _state.Player;
        return new
        {
            definitions = PerkCatalog.All,
            levels = PerkCatalog.All.ToDictionary(d => d.Id, d => p.PerkLevel(d.Id)),
            points = p.PerkPoints,
        };
    }

    [HttpPost("perks/{id}")]
    public IActionResult BuyPerk(string id)
    {
        var p = _state.Player;
        var def = PerkCatalog.Get(id);
        if (def == null) return NotFound("Неизвестный навык");

        int lvl = p.PerkLevel(id);
        if (lvl >= def.MaxLevel) return BadRequest("Навык уже максимального уровня");
        if (p.PerkPoints < 1) return BadRequest("Нет очков навыков");

        p.PerkPoints -= 1;
        p.Perks[id] = lvl + 1;
        PlayerProgression.Apply(p, grantPerkPoints: false);
        AchievementCatalog.Evaluate(_state);

        return Ok(new
        {
            player = p,
            definitions = PerkCatalog.All,
            levels = PerkCatalog.All.ToDictionary(d => d.Id, d => p.PerkLevel(d.Id)),
            points = p.PerkPoints,
        });
    }

    // --- Задания ---
    private static readonly Dictionary<int, string> ChapterTitles = new()
    {
        [1] = "Бункер",
        [2] = "Слухи",
        [3] = "Завод",
        [4] = "Военная база",
        [5] = "Лес",
        [6] = "Окраины",
        [7] = "Пригород",
        [8] = "Правда",
        [9] = "Ложь",
        [10] = "Градирни",
    };

    private bool IsChainReady(Quest q)
    {
        if (string.IsNullOrEmpty(q.RequiresQuestId)) return true;
        var prev = _state.Quests.FirstOrDefault(x => x.Id == q.RequiresQuestId);
        return prev == null || prev.IsCompleted;
    }

    [HttpGet("quests")]
    public object GetQuests()
    {
        _state.RefreshDailies();
        var list = _state.Quests
            .Where(q => q.IsRepeatable || !q.IsCompleted)
            .Where(q => !q.IsDaily || q.AvailableAt == null || q.AvailableAt <= DateTime.Now)
            .Where(IsChainReady)
            .ToList();
        return new { quests = list };
    }

    [HttpGet("quests/trader/{traderId}")]
    public object GetQuestsByTrader(string traderId)
    {
        _state.RefreshDailies();
        var list = _state.Quests
            .Where(q => q.TraderId == traderId && (q.IsRepeatable || !q.IsCompleted) && IsChainReady(q))
            .Where(q => !q.IsDaily || q.AvailableAt == null || q.AvailableAt <= DateTime.Now)
            .ToList();
        return new { quests = list };
    }

    [HttpGet("story")]
    public object GetStory()
    {
        _state.RefreshDailies();
        return StoryView();
    }

    private object StoryView()
    {
        var chapters = _state.StoryQuests
            .GroupBy(q => q.Chapter)
            .OrderBy(g => g.Key)
            .Select(g =>
            {
                var current = g.OrderBy(q => q.Order).FirstOrDefault(q => !q.IsCompleted);
                return new
                {
                    chapter = g.Key,
                    title = ChapterTitles.TryGetValue(g.Key, out var t) ? t : $"Глава {g.Key}",
                    total = g.Count(),
                    completed = g.Count(q => q.IsCompleted),
                    currentOrder = current?.Order ?? 0,
                };
            })
            .ToList();

        return new
        {
            current = _questSvc.GetCurrentStory(_state.StoryQuests),
            chapterQuests = _state.StoryQuests
                .Where(q => q.Chapter == (_questSvc.GetCurrentStory(_state.StoryQuests)?.Chapter ?? 0))
                .OrderBy(q => q.Order)
                .ToList(),
            completed = _state.StoryQuests.Where(q => q.IsCompleted).OrderBy(q => q.Chapter).ThenBy(q => q.Order).ToList(),
            allCount = _state.StoryQuests.Count,
            chapters,
            gameCompleted = _state.Player.GameCompleted,
        };
    }

    [HttpPost("quests/{id}/accept")]
    public IActionResult AcceptQuest(string id)
    {
        var q = FindQuest(id);
        if (q == null) return NotFound();
        if (q.IsRepeatable && q.IsCompleted)
        {
            // Повторяемое задание после сдачи можно принимать заново.
            q.IsCompleted = false;
            q.IsAccepted = false;
            q.Progress = 0;
        }
        if (q.IsCompleted) return BadRequest("Задание уже выполнено");

        if (q.IsStory)
        {
            var current = _questSvc.GetCurrentStory(_state.StoryQuests);
            if (current == null || current.Id != q.Id)
                return BadRequest("Сначала завершите предыдущую главу");
        }
        else if (!IsChainReady(q))
        {
            return BadRequest("Сначала завершите предыдущее задание торговца");
        }
        if (q.IsDaily && q.AvailableAt.HasValue && q.AvailableAt > DateTime.Now)
            return BadRequest("Это задание станет доступно позже");

        q.IsAccepted = true;
        return Ok(new { accepted = true });
    }

    public record ProgressDto(string Type, string? Code, int Count);
    [HttpPost("quests/progress")]
    public IActionResult Progress([FromBody] ProgressDto dto)
    {
        int addCount = dto.Type == "survive" ? 1 : Math.Max(0, dto.Count);

        foreach (var q in _state.Quests.Concat(_state.StoryQuests).Where(q => q.IsAccepted && !q.IsCompleted))
        {
            if (dto.Type == "kill" && q.Type == QuestType.Kill && !q.TargetBoss) q.Progress += addCount;
            if (dto.Type == "survive" && q.Type == QuestType.Survive) q.Progress += addCount;
        }
        return Ok(new { ok = true });
    }

    [HttpPost("quests/{id}/complete")]
    public IActionResult CompleteQuest(string id)
    {
        var q = FindQuest(id);
        var p = _state.Player;
        if (q == null || !q.IsAccepted || q.IsCompleted) return BadRequest("Недоступно");

        if (q.IsStory)
        {
            var current = _questSvc.GetCurrentStory(_state.StoryQuests);
            if (current == null || current.Id != q.Id)
                return BadRequest("Сначала завершите предыдущую главу");
        }
        else if (!IsChainReady(q))
        {
            return BadRequest("Сначала завершите предыдущее задание торговца");
        }

        if (q.Type == QuestType.Fetch)
        {
            if (string.IsNullOrEmpty(q.TargetItemCode)) return BadRequest("Ошибка квеста");
            int have = InventoryHelper.CountByCode(p, q.TargetItemCode);
            if (have < q.TargetCount) return BadRequest("Нет предметов");
        }
        else if (q.Progress < q.TargetCount) return BadRequest("Не выполнено");

        var backup = CloneInventory(p);
        if (q.Type == QuestType.Fetch)
            InventoryHelper.RemoveAllByCode(p, q.TargetItemCode!, q.TargetCount);

        foreach (var item in q.Reward.Items)
        {
            if (!InventoryHelper.AddToInventory(p, item))
            {
                p.Inventory.Clear();
                p.Inventory.AddRange(backup);
                return BadRequest("Инвентарь полон — освободите место под награду");
            }
        }

        q.IsCompleted = true;
        p.Money += q.Reward.Money;
        p.Stats.MoneyEarned += q.Reward.Money;
        p.Stats.QuestsCompleted += 1;
        p.Xp += q.Reward.Xp;

        if (!string.IsNullOrEmpty(q.Reward.UnlockLocation)
            && !p.UnlockedLocations.Contains(q.Reward.UnlockLocation))
            p.UnlockedLocations.Add(q.Reward.UnlockLocation);

        if (!string.IsNullOrEmpty(q.TraderId))
            TraderLoyalty.Add(p, q.TraderId, q.IsDaily ? 15 : 50);

        if (q.IsDaily)
            q.AvailableAt = DateTime.Now.Date.AddDays(1);

        if (q.IsStory)
        {
            var next = _questSvc.GetCurrentStory(_state.StoryQuests);
            if (next != null) next.IsAccepted = true;
            if (q.Id.EndsWith("_10_10"))
            {
                p.GameCompleted = true;
                p.Freeplay = true;
            }
        }

        PlayerProgression.Apply(p);

        // Клиент после сдачи всё равно перезапрашивает задания, поэтому не гоняем
        // весь список квестов в ответе.
        AchievementCatalog.Evaluate(_state);

        return Ok(new
        {
            player = p,
            gameCompleted = p.GameCompleted,
        });
    }

    private List<Item> CloneInventory(Player p) =>
        p.Inventory.Select(i => new Item
        {
            Id = i.Id, Code = i.Code, Name = i.Name, Description = i.Description, Icon = i.Icon,
            Type = i.Type, Rarity = i.Rarity, Price = i.Price, Damage = i.Damage, Defense = i.Defense,
            BackpackSlots = i.BackpackSlots, Restore = i.Restore, Restores = i.Restores,
            Count = i.Count, Lootable = i.Lootable, LootTier = i.LootTier,
            RequiredLoyalty = i.RequiredLoyalty, Weight = i.Weight,
            IsGrenade = i.IsGrenade, Caliber = i.Caliber,
        }).ToList();

    private Quest? FindQuest(string id) => _state.Quests.Concat(_state.StoryQuests).FirstOrDefault(q => q.Id == id);

    // --- Статистика ---
    public record StatsSyncDto(int Cells, bool Survived);
    [HttpPost("stats/sync")]
    public Player SyncStats([FromBody] StatsSyncDto dto)
    {
        var p = _state.Player;

        // Клетки считаются и за провальные рейды; выживание — только по флагу.
        p.Stats.CellsPassed += Math.Max(0, dto.Cells);
        if (dto.Survived) p.Stats.RaidsSurvived += 1;

        AchievementCatalog.Evaluate(_state);
        return p;
    }
}
