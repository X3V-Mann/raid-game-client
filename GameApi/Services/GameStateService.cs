using GameApi.Models;

namespace GameApi.Services;

public class GameStateService
{
    private readonly ItemCatalog _catalog;
    private readonly QuestService _quests;

    public Enemy? PendingEnemy { get; set; }
    public CombatStateDto? CurrentCombat { get; set; }
    public Player Player { get; }
    public List<Trader> Traders { get; }
    public List<Quest> Quests { get; }
    public List<Quest> StoryQuests { get; }

    public GameStateService(ItemCatalog catalog, QuestService quests, WeaponCatalog weapons)
    {
        _catalog = catalog;
        _quests = quests;

        Player = new Player
        {
            Backpack = null,
            Inventory = new() { catalog.Create("knife") },
        };

        // Регистрируем оружие из Data/weapon_list.json (только секция "weapons").
        var seenCodes = new HashSet<string>();
        foreach (var w in WeaponCatalog.All)
        {
            if (string.Equals(w.WeaponClass, "Grenade", StringComparison.OrdinalIgnoreCase)) continue;
            var item = WeaponCatalog.CreateItem(w);
            if (seenCodes.Add(item.Code)) _catalog.Add(item);
        }

        // Один шаблон патрона на каждый уникальный калибр.
        foreach (var caliber in WeaponCatalog.All
                     .Where(w => !string.IsNullOrWhiteSpace(w.Caliber)
                                 && !string.Equals(w.WeaponClass, "Grenade", StringComparison.OrdinalIgnoreCase))
                     .Select(w => w.Caliber!)
                     .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            _catalog.CreateAmmo(caliber, 0.5, 60, $"Патроны {caliber}.");
        }
        _catalog.CreateAmmo("bolt", 0.4, 70, "Болты для арбалета.");

        Traders = CreateTraders();

        Quests = new List<Quest>();
        StoryQuests = new List<Quest>();
        ResetQuests();
    }

    private List<Trader> CreateTraders()
    {
        List<Item> Stock(params string[] codes) => codes.Select(code =>
        {
            var item = _catalog.Create(code);
            if (item.RequiredLoyalty <= 0) item.RequiredLoyalty = TraderLoyalty.RequiredFor(code);
            return item;
        }).ToList();

        ItemStack Need(string code, int count) => new() { Code = code, Count = count };

        // Все стволы из JSON (кроме гранат), отсортированные по требуемой лояльности.
        var weaponStock = WeaponCatalog.All
            .Where(w => !string.Equals(w.WeaponClass, "Grenade", StringComparison.OrdinalIgnoreCase))
            .Select(WeaponCatalog.CreateItem)
            .OrderBy(i => i.RequiredLoyalty)
            .ThenBy(i => i.Damage)
            .ToList();

        var ammoStock = _catalog.All
            .Where(i => i.Type == ItemType.Ammo)
            .Select(i => _catalog.Create(i.Code))
            .ToList();

        var traders = new List<Trader>
        {
            new()
            {
                Id = "prapor", Name = "Прапор", Description = "Оружие на любой вкус.",
                Image = "/traders/prapor.png",
                Buys = new() { ItemType.WeaponMelee, ItemType.WeaponRanged, ItemType.Ammo },
                BuyRate = 0.5,
                Stock = Stock("knife", "machete", "axe", "sledge")
                    .Concat(weaponStock)
                    .Concat(ammoStock)
                    .ToList(),
                Barter = new()
                {
                    new() { Id = "prapor_knife", GivesCode = "knife", Cost = { Need("scrap", 2), Need("cloth", 1) }, Money = 200 },
                    new() { Id = "prapor_ammo9", GivesCode = "ammo_9x19mm", GivesCount = 30, Cost = { Need("scrap", 2), Need("wire", 1) }, Money = 150 },
                    new() { Id = "prapor_ammo545", GivesCode = "ammo_545x39mm", GivesCount = 30, Cost = { Need("scrap", 3), Need("wire", 2) }, Money = 300 },
                },
            },
            new()
            {
                Id = "therapist", Name = "Медик", Description = "Медицина и провизия.",
                Image = "/traders/therapist.jpg",
                Buys = new() { ItemType.Medicine, ItemType.Food, ItemType.Drink }, BuyRate = 0.5,
                Stock = Stock("bandage", "bandage_sterile", "painkiller", "painkiller_strong",
                              "medkit", "surgical_kit", "antirad", "iodine", "tourniquet", "splint",
                              "stimulant", "antidote", "blood_bag",
                              "bread", "canned", "stew", "sausage", "cheese", "crackers", "noodles",
                              "canned_fish", "honey", "mre",
                              "water", "cola", "energy", "juice", "vodka"),
                Barter = new()
                {
                    new() { Id = "therapist_medkit", GivesCode = "medkit", Cost = { Need("bandage", 2), Need("cloth", 1) } },
                    new() { Id = "therapist_antirad", GivesCode = "antirad", Cost = { Need("bandage", 1), Need("battery", 1) }, Money = 100 },
                    new() { Id = "therapist_blood", GivesCode = "blood_bag", Cost = { Need("tourniquet", 1), Need("iodine", 1), Need("bandage_sterile", 1) }, Money = 200 },
                },
            },
            new()
            {
                Id = "ragman", Name = "Старьёвщик", Description = "Экипировка и рюкзаки.",
                Image = "/traders/ragman.png",
                Buys = new() { ItemType.Helmet, ItemType.Armor, ItemType.Backpack }, BuyRate = 0.5,
                Stock = Stock("cap", "helmet_ssh", "helmet_tactical", "helmet_altyn", "helmet_exo",
                              "jacket", "cloak", "vest", "combat_suit", "vest_heavy", "sphere", "exo",
                              "pouch", "bag_small", "backpack", "raidpack", "bag_large", "bag_expedition"),
                Barter = new()
                {
                    new() { Id = "ragman_vest", GivesCode = "vest", Cost = { Need("cloth", 3), Need("rubber", 2) }, Money = 200 },
                    new() { Id = "ragman_combat", GivesCode = "combat_suit", Cost = { Need("cloth", 5), Need("cement", 2) }, Money = 500 },
                    new() { Id = "ragman_raidpack", GivesCode = "raidpack", Cost = { Need("cloth", 4), Need("zipties", 3), Need("duct_tape", 2) }, Money = 400 },
                },
            },
            new()
            {
                Id = "blacksmith", Name = "Кузнец", Description = "Броня и материалы.",
                Image = "/traders/blacksmith.png",
                Buys = new() { ItemType.Material }, BuyRate = 0.5,
                Stock = Stock("scrap", "cloth", "wire", "battery", "cement", "glass", "rubber", "electronics",
                              "fuel", "toolkit", "nails", "duct_tape", "zipties", "kerosene", "propane", "sparkplug"),
                Barter = new()
                {
                    new() { Id = "blacksmith_helmet", GivesCode = "helmet_tactical", Cost = { Need("scrap", 4), Need("cement", 1) } },
                    new() { Id = "blacksmith_toolkit", GivesCode = "toolkit", Cost = { Need("scrap", 3), Need("electronics", 1) } },
                    new() { Id = "blacksmith_sparkplug", GivesCode = "sparkplug", GivesCount = 3, Cost = { Need("scrap", 1), Need("wire", 1) } },
                },
            },
            new()
            {
                Id = "buyer", Name = "Скупщик", Description = "Скупает всё подряд. Дорого продаёт хлам.",
                Image = "/traders/buyer.jpg",
                BuysEverything = true, BuyRate = 0.25, PriceMultiplier = 1.6,
                Stock = Stock("watch", "amber", "goldchain", "icon", "artifact", "documents",
                              "coffee", "cigarettes", "chocolate", "dogtags", "medal", "cards",
                              "harmonica", "compass", "binoculars", "camera", "radio_parts", "geiger_counter",
                              "glass", "rubber", "scrap", "fuel"),
                Barter = new()
                {
                    new() { Id = "buyer_artifact", GivesCode = "artifact", Cost = { Need("goldchain", 1), Need("icon", 1), Need("watch", 1) }, Money = 2000 },
                    new() { Id = "buyer_electronics", GivesCode = "electronics", GivesCount = 2, Cost = { Need("glass", 3), Need("rubber", 2) } },
                    new() { Id = "buyer_geiger", GivesCode = "geiger_counter", Cost = { Need("radio_parts", 1), Need("electronics", 2), Need("battery", 1) }, Money = 300 },
                    new() { Id = "buyer_coffee", GivesCode = "coffee", Cost = { Need("cigarettes", 2), Need("cards", 1) } },
                },
            },
        };

        foreach (var trader in traders)
            foreach (var offer in trader.Barter)
                offer.RequiredLoyalty = TraderLoyalty.RequiredFor(offer.GivesCode);

        return traders;
    }

    private void ResetQuests()
    {
        Quests.Clear();
        Quests.AddRange(_quests.CreateTraderQuests());
        Quests.AddRange(_quests.CreateDailies());
        Quests.AddRange(_quests.CreateRepeatableQuests());

        StoryQuests.Clear();
        StoryQuests.AddRange(_quests.CreateStory());
    }

    public void RefreshDailies()
    {
        var now = DateTime.Now;
        foreach (var q in Quests.Where(q => q.IsDaily && q.IsCompleted && q.AvailableAt.HasValue && q.AvailableAt <= now))
        {
            q.IsCompleted = false;
            q.IsAccepted = false;
            q.Progress = 0;
            q.AvailableAt = null;
        }
    }

    public void PassiveTick()
    {
        var p = Player;
        int hpPerTick = PerkEffects.RegenPerTick(p);

        if (p.Hp < p.MaxHp) p.Hp = Math.Min(p.MaxHp, p.Hp + hpPerTick);
        if (p.Stamina < p.MaxStamina) p.Stamina = Math.Min(p.MaxStamina, p.Stamina + 5 + p.Shelter.RestZoneLevel * 5);
        if (p.Thirst < 100) p.Thirst = Math.Min(100, p.Thirst + 1 + p.Shelter.WaterLevel * 5);
        if (p.Hunger < 100) p.Hunger = Math.Min(100, p.Hunger + 1 + p.Shelter.KitchenLevel * 5);
    }

    public object GetRegenInfo() => new
    {
        interval = 5,
        hp = PerkEffects.RegenPerTick(Player),
        stamina = 5 + Player.Shelter.RestZoneLevel * 5,
        hunger = 1 + Player.Shelter.KitchenLevel * 5,
        thirst = 1 + Player.Shelter.WaterLevel * 5,
        medblock = Player.Shelter.MedBlockLevel,
    };

    public void Reset()
    {
        Player.Level = 1;
        Player.Xp = 0;
        Player.PerkPoints = 0;
        Player.MaxHp = PlayerProgression.BaseMaxHp;
        Player.MaxStamina = PlayerProgression.BaseMaxStamina;
        Player.Money = 15;
        Player.Hp = Player.MaxHp;
        Player.Stamina = Player.MaxStamina;
        Player.Hunger = 100;
        Player.Thirst = 100;

        Player.Name = "";
        Player.Avatar = "messer";
        Player.Onboarded = false;
        Player.GameCompleted = false;
        Player.Freeplay = false;
        Player.UnlockedLocations = new() { "city" };
        Player.Perks.Clear();
        Player.TraderRep.Clear();
        Player.Cassettes.Clear();
        Player.Achievements.Clear(); // фикс: достижения тоже сбрасываем

        Player.WeaponMelee = null;
        Player.WeaponRanged = null;
        Player.Helmet = null;
        Player.Armor = null;
        Player.Backpack = null;

        Player.Inventory.Clear();
        Player.Inventory.Add(_catalog.Create("knife"));

        Player.Shelter = new ShelterState();
        Player.Stats = new PlayerStats();

        CurrentCombat = null;
        PendingEnemy = null;

        ResetQuests();
    }
}