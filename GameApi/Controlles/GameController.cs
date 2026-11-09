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
    private readonly LocationCatalog _locations;

    public GameController(GameStateService s, ItemCatalog c, QuestService q, LocationCatalog l)
        => (_state, _catalog, _questSvc, _locations) = (s, c, q, l);

    // --- Игрок ---
    [HttpGet("player")]
    public Player GetPlayer()
    {
        // Чиним старые сохранения: урон оружия и найденные кассеты.
        WeaponCatalog.RefreshPlayerWeapons(_state.Player);
        InventoryHelper.SyncCassettes(_state.Player);
        InventoryHelper.SyncLegacyNames(_state.Player);
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
        InventoryHelper.SyncCassettes(cur);
        InventoryHelper.SyncLegacyNames(cur);

        cur.WeaponMelee = p.WeaponMelee;
        cur.WeaponRanged = p.WeaponRanged;
        cur.Helmet = p.Helmet;
        cur.Armor = p.Armor;
        cur.Suit = p.Suit;
        cur.Backpack = p.Backpack;

        int earned = cur.Money - moneyBefore;
        if (earned > 0) cur.Stats.MoneyEarned += earned;

        int damaged = hpBefore - cur.Hp;
        if (damaged > 0) cur.Stats.HpDamaged += damaged;

        PlayerProgression.Apply(cur);
        return cur;
    }

    [HttpPost("player/die")]
    public Player Die() => _state.KillPlayer();

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
                if (item.RequiredSkill > p.MeleeSkill) return BadRequest($"Недостаточно уровня навыка «Ближний бой» (требуется {item.RequiredSkill}, у вас {p.MeleeSkill}).");
                if (p.WeaponMelee != null) p.Inventory.Add(p.WeaponMelee);
                p.WeaponMelee = item; break;
            case ItemType.WeaponRanged:
                if (item.RequiredSkill > p.RangedSkill) return BadRequest($"Недостаточно уровня навыка «Дальний бой» (требуется {item.RequiredSkill}, у вас {p.RangedSkill}).");
                if (p.WeaponRanged != null) p.Inventory.Add(p.WeaponRanged);
                p.WeaponRanged = item; break;
            case ItemType.Helmet:
                if (p.Armor != null && IsHeavyArmor(p.Armor)) return BadRequest("Тяжёлая броня не даёт носить шлем");
                if (p.Helmet != null) p.Inventory.Add(p.Helmet);
                p.Helmet = item; break;
            case ItemType.Armor:
                if (item.IsSuit)
                {
                    if (p.Suit != null) p.Inventory.Add(p.Suit);
                    p.Suit = item;
                }
                else
                {
                    int capAfterArmor = Player.BaseInventorySlots + (p.Backpack?.BackpackSlots ?? 0) + (item.BackpackSlots > 0 ? item.BackpackSlots : 0);
                    int countAfterArmor = p.Inventory.Count + (p.Armor != null ? 1 : 0);
                    if (countAfterArmor > capAfterArmor)
                    {
                        p.Inventory.Add(item);
                        return BadRequest("Недостаточно места в инвентаре для этой экипировки");
                    }
                    if (p.Armor != null) p.Inventory.Add(p.Armor);
                    p.Armor = item;
                }
                break;
            case ItemType.Backpack:
            {
                int capAfterBag = Player.BaseInventorySlots + (item.BackpackSlots > 0 ? item.BackpackSlots : 0) + p.EquipmentBonusSlots;
                int countAfterBag = p.Inventory.Count + (p.Backpack != null ? 1 : 0);
                if (countAfterBag > capAfterBag)
                {
                    p.Inventory.Add(item);
                    return BadRequest("В этом рюкзаке не хватит места для вещей");
                }
                if (p.Backpack != null) p.Inventory.Add(p.Backpack);
                p.Backpack = item; break;
            }
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
            "suit" => p.Suit,
            "backpack" => p.Backpack,
            _ => null
        };
        if (item == null) return NotFound();

        int capAfter = p.InventoryCapacity;
        if (slot == "backpack") capAfter = Player.BaseInventorySlots + p.EquipmentBonusSlots;
        else if (slot == "armor") capAfter = Player.BaseInventorySlots + (p.Backpack?.BackpackSlots ?? 0);
        if (p.Inventory.Count + 1 > capAfter) return BadRequest("Инвентарь полон");

        switch (slot)
        {
            case "weaponMelee": p.WeaponMelee = null; break;
            case "weaponRanged": p.WeaponRanged = null; break;
            case "helmet": p.Helmet = null; break;
            case "armor": p.Armor = null; break;
            case "suit": p.Suit = null; break;
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
            {
                bool canEat = p.Hunger < 100;
                bool canHeal = item.RestoreHpBonus > 0 && p.Hp < p.MaxHp;
                if (!canEat && !canHeal) return BadRequest(p.Hunger >= 100 ? "Ты не голоден" : "Здоровье уже полное");
                if (canEat) { p.Hunger = Math.Min(100, p.Hunger + item.Restore); p.Stats.FoodEaten += 1; }
                if (canHeal)
                {
                    int bonus = Math.Min(item.RestoreHpBonus, p.MaxHp - p.Hp);
                    p.Hp += bonus; p.Stats.HpHealed += bonus;
                }
                break;
            }
            case "thirst":
            {
                bool canDrink = p.Thirst < 100;
                bool canHeal = item.RestoreHpBonus > 0 && p.Hp < p.MaxHp;
                if (!canDrink && !canHeal) return BadRequest(p.Thirst >= 100 ? "Ты не хочешь пить" : "Здоровье уже полное");
                if (canDrink) p.Thirst = Math.Min(100, p.Thirst + item.Restore);
                if (canHeal)
                {
                    int bonus = Math.Min(item.RestoreHpBonus, p.MaxHp - p.Hp);
                    p.Hp += bonus; p.Stats.HpHealed += bonus;
                }
                break;
            }
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

    /// <summary>Тяжёлая броня (сфера / экзоскелет) — не даёт носить шлем.</summary>
    private static bool IsHeavyArmor(Item? i) =>
        i != null && (string.Equals(i.Code, "sphere", StringComparison.OrdinalIgnoreCase)
                      || string.Equals(i.Code, "exo", StringComparison.OrdinalIgnoreCase));

    private static readonly HashSet<string> MutantPartCodes = new(StringComparer.OrdinalIgnoreCase)
        { "dog_tail", "flesh_eye", "flesh_fat", "boar_meat", "boar_hoof" };

    private static bool TraderBuys(Trader trader, Item item) =>
        trader.BuysEverything
        || (MutantPartCodes.Contains(item.Code) && (trader.Id == "bartender" || trader.Id == "buyer"))
        || trader.Buys.Contains(item.Type);

    private static int SellUnitPrice(Item item, Trader trader, Player p) =>
        Math.Max(1, (int)Math.Floor(item.Price * trader.BuyRate * PerkEffects.SellMultiplier(p)));

    [HttpPost("sell/{itemId}")]
    public IActionResult Sell(string itemId, [FromQuery] string? traderId = null)
    {
        var p = _state.Player;
        // Ищем в инвентаре, а потом в схроне убежища.
        var item = p.Inventory.FirstOrDefault(i => i.Id == itemId)
                 ?? p.Shelter.Stash.FirstOrDefault(i => i.Id == itemId);
        if (item == null) return NotFound();

        var trader = TraderById(traderId);
        if (trader == null) return NotFound("Торговец не найден");
        if (!TraderBuys(trader, item))
            return BadRequest($"{trader.Name} не покупает такие вещи");

        int earned = SellUnitPrice(item, trader, p) * item.Count;
        p.Money += earned;
        p.Stats.MoneyEarned += earned;
        p.Stats.SalesMade += 1;
        if (p.Inventory.Contains(item)) p.Inventory.Remove(item);
        else p.Shelter.Stash.Remove(item);
        TraderLoyalty.Add(p, trader.Id, Math.Max(1, earned / 200));
        return Ok(p);
    }

    [HttpPost("sell-partial")]
    public IActionResult SellPartial([FromBody] SellDto dto)
    {
        var p = _state.Player;
        var item = p.Inventory.FirstOrDefault(i => i.Id == dto.ItemId)
                 ?? p.Shelter.Stash.FirstOrDefault(i => i.Id == dto.ItemId);
        if (item == null) return NotFound();

        var trader = TraderById(dto.TraderId);
        if (trader == null) return NotFound("Торговец не найден");
        if (!TraderBuys(trader, item))
            return BadRequest($"{trader.Name} не покупает такие вещи");
        if (dto.Count < 1 || dto.Count > item.Count) return BadRequest("Неверное количество");

        int earned = SellUnitPrice(item, trader, p) * dto.Count;
        item.Count -= dto.Count;
        if (item.Count <= 0)
        {
            if (p.Inventory.Contains(item)) p.Inventory.Remove(item);
            else p.Shelter.Stash.Remove(item);
        }
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
            t.IsUnlocked = p.UnlockedTraders.Contains(t.Id);
        }

        return new
        {
            traders = _state.Traders,
            buyMultiplier = PerkEffects.BuyMultiplier(p),
            sellMultiplier = PerkEffects.SellMultiplier(p),
            gameTime = _state.GetGameTimeInfo(),
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

        if (p.Money < totalCost) return BadRequest($"Не хватает денег: нужно {totalCost} руб.");

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
        Math.Max(1, (int)Math.Round(
            stock.Price
            * trader.PriceMultiplier
            * PerkEffects.BuyMultiplier(p)
            * TraderLoyalty.PriceMultiplier(p, trader.Id)));

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
        if (p.Money < offer.Money) return BadRequest($"Нужно {offer.Money} руб.");

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
            item.FoundInRaid = true; // предметы по бартеру считаются «из рейда»
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

    public record ShelterReq(string Code, int Need, bool FromRaid = false);

    // Каждый модуль требует свой набор предметов (2–5 видов, 1–10 шт.); часть — только из рейда.
    private static readonly Dictionary<string, List<ShelterReq>> ModuleMaterials = new()
    {
        ["medblock"] = new() { new("cloth", 6), new("bandage", 4), new("mineral_water", 3), new("medkit_army", 1, true) },
        ["rest"]     = new() { new("fleece_fabric", 5), new("cloth", 8), new("bolts", 10), new("paracord", 2) },
        ["kitchen"]  = new() { new("scrap", 7), new("glass", 4), new("duct_tape", 3), new("canned", 2, true), new("salt", 5) },
        ["water"]    = new() { new("rubber", 4), new("wire", 6), new("capacitors", 2), new("mineral_water", 5) },
        ["stash"]    = new() { new("scrap", 10), new("nails", 8), new("duct_tape", 4), new("zipties", 6) },
        ["workshop"] = new() { new("toolkit", 1), new("electronics", 3), new("rosin", 2), new("resistors", 4), new("screwdriver_set", 1) },
        ["generator"]= new() { new("battery", 1, true), new("wire", 8), new("fuel", 2), new("capacitors", 3), new("textolite", 1) },
        ["armory"]   = new() { new("scrap", 9), new("bolts", 10), new("nuts", 10), new("textolite", 2), new("tech_docs", 1, true) },
        ["gym"]      = new() { new("cement", 4), new("rubber", 6), new("cloth", 5), new("nuts", 7) },
    };

    private static List<ShelterReq> UpgradeMaterials(ShelterState s, string id)
    {
        if (ModuleMaterials.TryGetValue(id, out var list) && list.Count > 0)
            return list;
        int lvl = ShelterLevel(s, id);
        int need = Math.Clamp(lvl + 1, 1, 10);
        return new List<ShelterReq> { new("scrap", need), new("cloth", need) };
    }

    [HttpGet("shelter/info")]
    public object GetShelterInfo()
    {
        var p = _state.Player;
        var s = p.Shelter;

        var modules = ShelterModules.Select(kv =>
        {
            int lvl = ShelterLevel(s, kv.Key);
            var reqs = UpgradeMaterials(s, kv.Key).Select(r =>
            {
                int have = r.FromRaid
                    ? InventoryHelper.CountByCodeFromRaid(p, r.Code)
                      + p.Shelter.Stash.Where(i => i.Code == r.Code && i.FoundInRaid).Sum(i => Math.Max(0, i.Count))
                    : InventoryHelper.CountByCode(p, r.Code)
                      + p.Shelter.Stash.Where(i => i.Code == r.Code).Sum(i => Math.Max(0, i.Count));
                return new
                {
                    code = r.Code,
                    name = _catalog.Get(r.Code)?.Name ?? r.Code,
                    need = r.Need,
                    have,
                    fromRaid = r.FromRaid,
                };
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

        foreach (var req in materials)
        {
            int haveNow = req.FromRaid
                ? InventoryHelper.CountByCodeFromRaid(p, req.Code)
                  + p.Shelter.Stash.Where(i => i.Code == req.Code && i.FoundInRaid).Sum(i => Math.Max(0, i.Count))
                : InventoryHelper.CountByCode(p, req.Code)
                  + p.Shelter.Stash.Where(i => i.Code == req.Code).Sum(i => Math.Max(0, i.Count));
            if (haveNow < req.Need)
            {
                string suffix = req.FromRaid ? " (из рейда)" : "";
                return BadRequest($"Не хватает: {_catalog.Get(req.Code)?.Name ?? req.Code}{suffix} ×{req.Need}");
            }
        }
        if (p.Money < moneyCost) return BadRequest($"Нужно {moneyCost} руб.");

        p.Money -= moneyCost; p.Stats.MoneySpent += moneyCost;
        foreach (var req in materials)
        {
            int remaining = req.Need;
            if (req.FromRaid)
            {
                remaining -= InventoryHelper.RemoveAllByCodeFromRaid(p, req.Code, remaining);
                if (remaining > 0)
                {
                    foreach (var i in p.Shelter.Stash.Where(i => i.Code == req.Code && i.FoundInRaid).ToList())
                    {
                        if (remaining <= 0) break;
                        int take = Math.Min(Math.Max(1, i.Count), remaining);
                        i.Count -= take;
                        remaining -= take;
                        if (i.Count <= 0) p.Shelter.Stash.Remove(i);
                    }
                }
            }
            else
            {
                remaining -= InventoryHelper.RemoveAllByCode(p, req.Code, remaining);
                if (remaining > 0)
                {
                    foreach (var i in p.Shelter.Stash.Where(i => i.Code == req.Code).ToList())
                    {
                        if (remaining <= 0) break;
                        int take = Math.Min(Math.Max(1, i.Count), remaining);
                        i.Count -= take;
                        remaining -= take;
                        if (i.Count <= 0) p.Shelter.Stash.Remove(i);
                    }
                }
            }
        }

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
        if (_state.Raid is { Finished: false })
            return BadRequest("Нельзя переносить вещи в схрон во время рейда");
        var p = _state.Player;
        var item = p.Inventory.FirstOrDefault(i => i.Id == itemId);
        if (item == null) return NotFound();

        if (InventoryHelper.IsStackable(item))
        {
            var existing = p.Shelter.Stash.FirstOrDefault(i => i.Code == item.Code);
            if (existing != null)
            {
                existing.Count += item.Count;
                p.Inventory.Remove(item);
                InventoryHelper.RegisterCassette(p, item);
                return Ok(p);
            }
        }

        if (p.Shelter.Stash.Count >= p.Shelter.StashSlots) return BadRequest("Схрон полон");
        p.Inventory.Remove(item);
        p.Shelter.Stash.Add(item);
        InventoryHelper.RegisterCassette(p, item);
        p.Stats.StashesUsed += 1;
        return Ok(p);
    }

    [HttpPost("stash/take/{itemId}")]
    public IActionResult TakeStash(string itemId)
    {
        if (_state.Raid is { Finished: false })
            return BadRequest("Нельзя использовать схрон убежища во время рейда");
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

    // --- Навыки / характеристики (прокачка) ---
    private static readonly (string Id, string Name, string Icon, string Desc)[] SkillDefs = new[]
    {
        ("luck", "Удача", "🍀", "Влияет на находки и качество лута."),
        ("melee", "Ближний бой", "🔪", "Урон и владение оружием ближнего боя."),
        ("ranged", "Дальний бой", "🔫", "Урон и владение огнестрельным оружием."),
        ("accuracy", "Меткость", "🎯", "Снижает шанс промаха."),
        ("agility", "Ловкость", "🏃", "Проворство, бонус к AP и подвижности."),
        ("dodge", "Уклонение", "💨", "Шанс увернуться от атаки."),
        ("parry", "Парирование", "🛡", "Шанс частично заблокировать удар."),
        ("handling", "Обращение с оружием", "🔧", "Контроль оружия, меньше промахов."),
    };

    private static int GetSkill(Player p, string id) => id switch
    {
        "luck" => p.Luck,
        "melee" => p.MeleeSkill,
        "ranged" => p.RangedSkill,
        "accuracy" => p.Accuracy,
        "agility" => p.Agility,
        "dodge" => p.Dodge,
        "parry" => p.Parry,
        "handling" => p.WeaponHandling,
        _ => 0,
    };

    private static void SetSkill(Player p, string id, int value)
    {
        switch (id)
        {
            case "luck": p.Luck = value; break;
            case "melee": p.MeleeSkill = value; break;
            case "ranged": p.RangedSkill = value; break;
            case "accuracy": p.Accuracy = value; break;
            case "agility": p.Agility = value; break;
            case "dodge": p.Dodge = value; break;
            case "parry": p.Parry = value; break;
            case "handling": p.WeaponHandling = value; break;
        }
    }

    [HttpGet("skills")]
    public object GetSkills()
    {
        var p = _state.Player;
        return new
        {
            points = p.SkillPoints,
            definitions = SkillDefs.Select(s => new { id = s.Id, name = s.Name, icon = s.Icon, description = s.Desc, maxLevel = 20 }).ToList(),
            levels = SkillDefs.ToDictionary(s => s.Id, s => GetSkill(p, s.Id)),
        };
    }

    [HttpPost("skills/{id}/upgrade")]
    public IActionResult UpgradeSkill(string id)
    {
        var p = _state.Player;
        if (!SkillDefs.Any(s => s.Id == id)) return NotFound("Неизвестный навык");
        if (p.SkillPoints < 1) return BadRequest("Нет очков навыков");
        int lvl = GetSkill(p, id);
        if (lvl >= 20) return BadRequest("Навык уже максимального уровня");

        p.SkillPoints -= 1;
        SetSkill(p, id, lvl + 1);
        _state.Persist();
        return Ok(new
        {
            player = p,
            points = p.SkillPoints,
            definitions = SkillDefs.Select(s => new { id = s.Id, name = s.Name, icon = s.Icon, description = s.Desc, maxLevel = 20 }).ToList(),
            levels = SkillDefs.ToDictionary(s => s.Id, s => GetSkill(p, s.Id)),
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

    private List<Quest> VisibleTraderQuests(string traderId)
    {
        _state.RefreshDailies();
        var open = _state.Quests
            .Where(q => q.TraderId == traderId)
            .Where(q => !q.IsCompleted || q.IsRepeatable)
            .Where(q => !q.IsDaily || q.AvailableAt == null || q.AvailableAt <= DateTime.Now)
            .Where(IsChainReady)
            .OrderBy(q => q.Order)
            .ToList();
        // В цепочках показываем только первое открытое задание (принятое/доступное).
        var active = open.FirstOrDefault(q => q.IsAccepted && !q.IsCompleted);
        if (active != null) return new List<Quest> { active };
        var first = open.FirstOrDefault(q => !q.IsCompleted);
        if (first != null) return new List<Quest> { first };
        return open.Where(q => q.IsRepeatable).Take(1).ToList();
    }

    [HttpGet("quests")]
    public object GetQuests()
    {
        _state.RefreshDailies();
        var list = _state.Quests
            .Where(q => q.IsRepeatable || !q.IsCompleted)
            .Where(q => !q.IsDaily || q.AvailableAt == null || q.AvailableAt <= DateTime.Now)
            .Where(IsChainReady)
            .OrderBy(q => q.Order)
            .ToList();
        return new { quests = list };
    }

    [HttpGet("quests/trader/{traderId}")]
    public object GetQuestsByTrader(string traderId) => new { quests = VisibleTraderQuests(traderId) };

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

        var current = _questSvc.GetCurrentStory(_state.StoryQuests);
        // Показываем только первое открытое (текущее) задание главы.
        var chapterQuests = current != null ? new List<Quest> { current } : new List<Quest>();

        var completedStory = _state.StoryQuests.Where(q => q.IsCompleted).OrderBy(q => q.Chapter).ThenBy(q => q.Order).ToList();
        var completedTrader = _state.Quests.Where(q => q.IsCompleted).OrderBy(q => q.TraderId).ThenBy(q => q.Order).ToList();

        return new
        {
            current,
            chapterQuests,
            completed = completedStory,
            completedAll = completedStory.Concat(completedTrader).ToList(),
            allCount = _state.StoryQuests.Count,
            chapters,
            gameCompleted = _state.Player.GameCompleted,
            freeplay = _state.Player.Freeplay,
            endingChoice = _state.Player.EndingChoice,
            bombHidden = _state.Player.BombHidden,
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

        if (q.IsUrgent && q.IsFailed) return BadRequest("Срок срочного задания истёк");
        q.IsAccepted = true;
        q.IsFailed = false;
        if (q.IsUrgent && q.UrgentDurationMinutes > 0)
            q.DeadlineGameMinutes = _state.Player.GameTimeMinutes + q.UrgentDurationMinutes;
        return Ok(new { accepted = true, deadline = q.DeadlineGameMinutes, time = _state.GetGameTimeInfo() });
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
        _state.ExpireUrgentQuests();
        var q = FindQuest(id);
        var p = _state.Player;
        if (q == null || !q.IsAccepted || q.IsCompleted) return BadRequest("Недоступно");
        if (q.IsFailed) return BadRequest("Срок срочного задания истёк");

        if (q.Id == "s4_07")
            return Ok(new { player = p, requiresChoice = true, choices = new[] { "detonate", "hide" } });

        if (q.IsStory && q.Category == QuestCategory.Story)
        {
            var current = _questSvc.GetCurrentStory(_state.StoryQuests);
            if (current == null || current.Id != q.Id)
                return BadRequest("Сначала завершите текущее сюжетное задание");
        }
        else if (!IsChainReady(q))
        {
            return BadRequest("Сначала завершите предыдущее задание");
        }

        if (q.Type == QuestType.Fetch)
        {
            if (string.IsNullOrEmpty(q.TargetItemCode)) return BadRequest("Ошибка квеста");
            int have = InventoryHelper.CountByCodeFromRaid(p, q.TargetItemCode)
                      + p.Shelter.Stash.Where(i => i.Code == q.TargetItemCode && i.FoundInRaid).Sum(i => Math.Max(0, i.Count));
            if (have < q.TargetCount) return BadRequest("Нужны предметы, найденные в рейде");
        }
        else if (q.Type == QuestType.Talk)
        {
            q.Progress = Math.Max(q.Progress, q.TargetCount <= 0 ? 1 : q.TargetCount);
        }
        else
        {
            if (q.TargetCount > 0 && q.Progress < q.TargetCount) return BadRequest("Не выполнено");
        }

        var backup = CloneInventory(p);
        if (q.Type == QuestType.Fetch)
        {
            // Сдаём сначала из инвентаря, затем из схрона убежища (только предметы из рейда).
            int remaining = q.TargetCount;
            foreach (var i in p.Inventory.Where(i => i.Code == q.TargetItemCode && i.FoundInRaid).ToList())
            {
                if (remaining <= 0) break;
                int take = Math.Min(Math.Max(1, i.Count), remaining);
                i.Count -= take;
                remaining -= take;
                if (i.Count <= 0) p.Inventory.Remove(i);
            }
            foreach (var i in p.Shelter.Stash.Where(i => i.Code == q.TargetItemCode && i.FoundInRaid).ToList())
            {
                if (remaining <= 0) break;
                int take = Math.Min(Math.Max(1, i.Count), remaining);
                i.Count -= take;
                remaining -= take;
                if (i.Count <= 0) p.Shelter.Stash.Remove(i);
            }
        }

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
        if (!string.IsNullOrEmpty(q.Reward.UnlockTrader)
            && !p.UnlockedTraders.Contains(q.Reward.UnlockTrader))
            p.UnlockedTraders.Add(q.Reward.UnlockTrader);

        if (!string.IsNullOrEmpty(q.TraderId))
            TraderLoyalty.Add(p, q.TraderId, q.IsDaily ? 15 : 50);

        if (q.IsDaily)
            q.AvailableAt = DateTime.Now.Date.AddDays(1);

        if (q.IsStory)
        {
            var next = _questSvc.GetCurrentStory(_state.StoryQuests);
            if (next != null && !next.IsCompleted) next.IsAccepted = true;
        }
        // Финальный выбор: s4_07 завершается через /story/ending-choice, здесь только сигналим UI.
        if (q.Id == "story_9_10" || q.Id.EndsWith("_10_10"))
        {
            p.GameCompleted = true;
            p.Freeplay = true;
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
            IsGrenade = i.IsGrenade, Caliber = i.Caliber, FoundInRaid = i.FoundInRaid,
        }).ToList();

    private Quest? FindQuest(string id) => _state.AllQuests().FirstOrDefault(q => q.Id == id);

    // --- Игровое время ---
    [HttpGet("gametime")]
    public object GetGameTime() => _state.GetGameTimeInfo();

    [HttpPost("gametime/advance")]
    public object AdvanceTime([FromBody] TimeAdvanceDto dto)
    {
        _state.AdvanceGameTime(Math.Clamp(dto.Minutes, 0, 1440));
        return new { time = _state.GetGameTimeInfo() };
    }
    public record TimeAdvanceDto(int Minutes);

    // --- Доска объявлений (побочные задания) ---
    [HttpGet("quests/board")]
    public object GetBoard()
    {
        _state.ExpireUrgentQuests();
        var p = _state.Player;
        var unlocked = p.UnlockedLocations;
        var unlockedTraders = p.UnlockedTraders;
        var traderNames = TraderDisplayNames();
        var list = _state.SideQuests
            .Where(q => !q.IsCompleted || q.IsRepeatable)
            .Where(q => string.IsNullOrEmpty(q.TargetLocationId) || unlocked.Contains(q.TargetLocationId))
            .Where(q => string.IsNullOrEmpty(q.TraderId) || unlockedTraders.Contains(q.TraderId))
            .OrderByDescending(q => q.IsUrgent)
            .ThenBy(q => q.Order)
            .Select(q => {
                if (!string.IsNullOrEmpty(q.TraderId) && traderNames.TryGetValue(q.TraderId, out var tn)) q.TraderName = tn;
                else if (!string.IsNullOrEmpty(q.TraderId)) q.TraderName = _state.Traders.FirstOrDefault(t => t.Id == q.TraderId)?.Name ?? q.TraderId;
                else q.TraderName = null;
                return q;
            })
            .ToList();
        return new { quests = list, time = _state.GetGameTimeInfo() };
    }

    private static Dictionary<string, string> TraderDisplayNames() => new(StringComparer.OrdinalIgnoreCase)
    {
        ["prapor"] = "Шматко",
        ["therapist"] = "Медик",
        ["ragman"] = "Сидорович",
        ["blacksmith"] = "Гриша",
        ["buyer"] = "Скупщик",
        ["bartender"] = "Бармен",
    };

    // --- Диалог торговца ---
    [HttpGet("traders/{traderId}/dialogue")]
    public IActionResult GetDialogue(string traderId)
    {
        var t = _state.Traders.FirstOrDefault(x => x.Id == traderId);
        if (t == null) return NotFound();
        var p = _state.Player;
        var unlocked = p.UnlockedTraders.Contains(traderId);
        var traderQuests = VisibleTraderQuests(traderId);
        return Ok(new { trader = new { t.Id, t.Name, t.Description, t.Image, isUnlocked = unlocked }, dialogue = t.Dialogue, quests = traderQuests, time = _state.GetGameTimeInfo() });
    }

    [HttpPost("traders/{traderId}/talk")]
    public IActionResult TalkTrader(string traderId)
    {
        var t = _state.Traders.FirstOrDefault(x => x.Id == traderId);
        if (t == null) return NotFound();
        var p = _state.Player;
        if (!p.UnlockedTraders.Contains(traderId)) return BadRequest("Торговец недоступен");
        foreach (var q in _state.AllQuests().Where(q => q.IsAccepted && !q.IsCompleted && !q.IsFailed && q.Type == QuestType.Talk
                     && string.Equals(q.TraderId, traderId, StringComparison.OrdinalIgnoreCase)))
            q.Progress = Math.Max(q.Progress, q.TargetCount <= 0 ? 1 : q.TargetCount);
        return Ok(new { ok = true, player = p });
    }

    public record EndingChoiceDto(string Choice);
    [HttpPost("story/ending-choice")]
    public IActionResult EndingChoice([FromBody] EndingChoiceDto dto)
    {
        var p = _state.Player;
        var s4_06 = FindQuest("s4_06");
        if (s4_06 == null || !s4_06.IsCompleted) return BadRequest("Сначала завершите «Точку невозврата» (поговорите с Барменом).");
        var s4_07 = FindQuest("s4_07");
        if (s4_07 == null) return BadRequest("Финальное задание не найдено.");
        if (p.EndingChoice != null) return Ok(new { player = p, message = "Выбор уже сделан.", gameCompleted = p.GameCompleted });

        string choice = (dto.Choice ?? "").Trim().ToLowerInvariant();
        if (choice is "detonate" or "bomb" or "взорвать")
        {
            p.EndingChoice = "detonate";
            p.BombHidden = false;
            p.GameCompleted = true;
            p.Freeplay = false; // DLC-заглушка: взрыв завершает текущую ветку
            p.JournalThoughts.Add("Я активировал заряд на Градирнях. Небо вспыхнуло белым. Объект уничтожен. Связь с прошлым оборвалась вместе с ударной волной. Конец? Или только начало другой истории...");
            s4_07.IsAccepted = true; s4_07.IsCompleted = true; s4_07.Progress = Math.Max(1, s4_07.TargetCount);
            _state.Persist();
            return Ok(new { player = p, gameCompleted = true, freeplay = false, choice = "detonate",
                message = "Игра закончена. Вы взорвали ядерный заряд на Градирнях.\n\nУдарная волна стерла объект, наёмников и все следы секретной разработки. Тайна груза похоронена под бетоном и пеплом. ГГ выжил — но цена оказалась слишком высокой. (Заглушка: в будущем здесь откроется DLC-ветка последствий.)" });
        }
        if (choice is "hide" or "freeplay" or "спрятать")
        {
            p.EndingChoice = "hide";
            p.BombHidden = true;
            p.GameCompleted = true;
            p.Freeplay = true;
            // дать бомбу в инвентарь/убежище как напоминание (если есть место)
            if (!p.Inventory.Any(i => i.Code == "atomic_bomb") && !p.Shelter.Stash.Any(i => i.Code == "atomic_bomb"))
            {
                var bomb = _catalog.Create("atomic_bomb"); bomb.FoundInRaid = true;
                if (!InventoryHelper.AddToInventory(p, bomb)) p.Shelter.Stash.Add(bomb);
            }
            p.JournalThoughts.Add("Я спрятал заряд. Не сегодня. Зона ещё не сказала своего последнего слова, и, возможно, эта штука ещё пригодится. А пока — свободная игра.");
            s4_07.IsAccepted = true; s4_07.IsCompleted = true; s4_07.Progress = Math.Max(1, s4_07.TargetCount);
            _state.Persist();
            return Ok(new { player = p, gameCompleted = true, freeplay = true, choice = "hide",
                message = "Вы спрятали ядерный заряд. Финал отложен — начинается свободная игра.\n\nГГ уносит устройство с Градирней и прячет его. Если передумаете, заряд остаётся с вами (спрайт бомбы — в вашем снаряжении/убежище). Зона открыта." });
        }
        return BadRequest("Выберите: detonate (взорвать) или hide (спрятать).");
    }

    // --- Личный дневник / справочник ---
    [HttpGet("diary")]
    public object GetDiary()
    {
        var locs = _locations.All.Select(l => new { l.Id, l.Name, l.Tier, l.Description, l.Icon, enemies = l.Enemies.Select(e => new { e.Name, e.Icon, e.BaseHp, e.BaseDamage, e.Ranged }).ToList() }).ToList();
        var items = _catalog.All.Where(i => i.Lootable || i.Type == ItemType.Misc || i.Type == ItemType.Material || i.Type == ItemType.Medicine || i.Type == ItemType.Food || i.Type == ItemType.Drink)
            .Select(i => new { i.Code, i.Name, i.Description, i.Icon, type = i.Type.ToString(), i.Price }).Take(400).ToList();
        return new { thoughts = _state.Player.JournalThoughts, notes = _state.Player.JournalNotes, referenceItems = items, referenceLocations = locs, time = _state.GetGameTimeInfo() };
    }

    [HttpPost("diary/thought")]
    public IActionResult AddThought([FromBody] ThoughtDto dto)
    {
        // Мысли записывает ГГ (системно); эндпоинт оставлен для совместимости.
        if (!string.IsNullOrWhiteSpace(dto.Text) && !_state.Player.JournalThoughts.Contains(dto.Text))
            _state.Player.JournalThoughts.Add(dto.Text);
        return Ok(new { thoughts = _state.Player.JournalThoughts });
    }

    [HttpPost("diary/note")]
    public IActionResult AddNote([FromBody] ThoughtDto dto)
    {
        if (!string.IsNullOrWhiteSpace(dto.Text) && !_state.Player.JournalNotes.Contains(dto.Text))
            _state.Player.JournalNotes.Add(dto.Text.Trim());
        return Ok(new { notes = _state.Player.JournalNotes });
    }
    public record ThoughtDto(string Text);

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
