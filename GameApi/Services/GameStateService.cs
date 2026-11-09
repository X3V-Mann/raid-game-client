using System.Text.Json;
using GameApi.Models;

namespace GameApi.Services;

public class GameStateService
{
    private static readonly JsonSerializerOptions PersistJson = new() { WriteIndented = true };

    private readonly ItemCatalog _catalog;
    private readonly QuestService _quests;

    public Enemy? PendingEnemy { get; set; }
    public CombatStateDto? CurrentCombat { get; set; }

    /// <summary>Активный рейд. Живёт на сервере, поэтому переживает закрытие вкладки.</summary>
    public RaidState? Raid { get; set; }

    /// <summary>Каталог автосохранений (заполняется в Program.cs).</summary>
    public string SaveDirectory { get; set; } = Path.Combine(AppContext.BaseDirectory, "saves");

    public Player Player { get; }
    public List<Trader> Traders { get; }
    public List<Quest> Quests { get; }
    public List<Quest> StoryQuests { get; }
    public List<Quest> SideQuests { get; } = new();

    /// <summary>Сколько игровых минут добавляется за один пассивный тик (5 сек реального времени).</summary>
    public const int GameMinutesPerTick = 5;

    public static bool IsNight(long totalMinutes)
    {
        int m = (int)(totalMinutes % 1440);
        return m < 360 || m >= 1260; // 00:00-05:59, 21:00-23:59
    }

    public object GetGameTimeInfo()
    {
        long t = Player.GameTimeMinutes;
        int day = (int)(t / 1440) + 1;
        int mins = (int)(t % 1440);
        int h = mins / 60, m = mins % 60;
        bool night = IsNight(t);
        return new { day, hour = h, minute = m, totalMinutes = t, isNight = night, text = $"День {day}, {h:00}:{m:00}" };
    }

    public void AdvanceGameTime(int minutes)
    {
        if (minutes <= 0) return;
        Player.GameTimeMinutes += minutes;
        ExpireUrgentQuests();
    }

    public void ExpireUrgentQuests()
    {
        foreach (var q in AllQuests().Where(q => q.IsUrgent && q.IsAccepted && !q.IsCompleted && !q.IsFailed && q.DeadlineGameMinutes > 0))
        {
            if (Player.GameTimeMinutes > q.DeadlineGameMinutes)
            {
                q.IsFailed = true;
                q.IsAccepted = false;
                q.Progress = 0;
            }
        }
    }

    public IEnumerable<Quest> AllQuests() => Quests.Concat(StoryQuests).Concat(SideQuests);

    public GameStateService(ItemCatalog catalog, QuestService quests, WeaponCatalog weapons)
    {
        _catalog = catalog;
        _quests = quests;

        Player = new Player
        {
            Backpack = null,
            Suit = catalog.Create("suit_hoodie"),
            Inventory = new() { catalog.Create("knife") },
        };

        // Регистрируем оружие из Data/weapon_list.json (weapons + traderWeapons).
        var seenCodes = new HashSet<string>();
        foreach (var w in WeaponCatalog.All.Concat(WeaponCatalog.TraderWeapons))
        {
            if (string.Equals(w.WeaponClass, "Grenade", StringComparison.OrdinalIgnoreCase)) continue;
            var item = WeaponCatalog.CreateItem(w);
            if (seenCodes.Add(item.Code)) _catalog.Add(item);
        }

        // Один шаблон патрона на каждый уникальный калибр.
        foreach (var caliber in WeaponCatalog.All.Concat(WeaponCatalog.TraderWeapons)
                     .Where(w => !string.IsNullOrWhiteSpace(w.Caliber)
                                 && !string.Equals(w.WeaponClass, "Grenade", StringComparison.OrdinalIgnoreCase))
                     .Select(w => w.Caliber!)
                     .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            _catalog.CreateAmmo(caliber, 0.05, 60, $"Патроны {caliber}.");
        }
        _catalog.CreateAmmo("bolt", 0.05, 70, "Болты для арбалета.");

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

        // Все стволы из JSON (кроме гранат), включая traderWeapons, отсортированные по требуемой лояльности.
        var weaponStock = WeaponCatalog.All
            .Concat(WeaponCatalog.TraderWeapons)
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
                Dialogue = new(){ new(){Speaker="Прапор",Text="Оружие есть. Вопросы — после дела."}, new(){Speaker="Прапор",Text="На Заводе засели серьёзные люди. Без подготовки не суйся."}, new(){Speaker="Прапор",Text="Принеси то, что нужно, — и получишь доступ к лучшему стволу."} },
                // Прапор — оружейный магазин: его стволы должны стоять в ценах melee 300–1000 / огнестрел 1100+,
                // поэтому базовую стоимость оружия у него смещаем ниже (снаряжение у «Старьёвщика» дороже — ×1.6 в ItemCatalog).
                Buys = new() { ItemType.WeaponMelee, ItemType.WeaponRanged, ItemType.Ammo },
                BuyRate = 0.5,
                PriceMultiplier = 0.8,
                Stock = Stock("knife", "machete", "sledge")
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
                Id = "therapist", Name = "Сергей", Description = "Только медицина: перевязка, лекарства и аптечки.",
                Image = "/traders/therapist.png",
                Dialogue = new(){ new(){Speaker="Сергей",Text="Покажи раны. И без героизма."}, new(){Speaker="Сергей",Text="После рейда — сначала ко мне, потом к Бармену."}, new(){Speaker="Сергей",Text="Части мутантов опасны, но в умелых руках — источник данных и товара."} },
                Buys = new() { ItemType.Medicine }, BuyRate = 0.5,
                Stock = Stock("bandage", "bandage_sterile", "bandage_pack", "iodine", "vaseline",
                              "painkiller", "painkiller_strong", "morphine", "hemostatic", "saline",
                              "tourniquet", "splint", "vitamins",
                              "medkit", "medkit_army", "medkit_car",
                              "surgical_kit", "antirad", "stimulant", "antidote", "blood_bag"),
                Barter = new()
                {
                    new() { Id = "therapist_medkit", GivesCode = "medkit", Cost = { Need("bandage", 2), Need("cloth", 1) } },
                    new() { Id = "therapist_antirad", GivesCode = "antirad", Cost = { Need("bandage", 1), Need("battery", 1) }, Money = 100 },
                    new() { Id = "therapist_blood", GivesCode = "blood_bag", Cost = { Need("tourniquet", 1), Need("iodine", 1), Need("bandage_sterile", 1) }, Money = 200 },
                    new() { Id = "therapist_army", GivesCode = "medkit_army", Cost = { Need("medkit", 1), Need("hemostatic", 1), Need("saline", 1) }, Money = 150 },
                },
            },
            new()
            {
                Id = "bartender", Name = "Бармен", Description = "Еда, напитки и тёплый уголок.",
                Image = "/traders/barman.png",
                Dialogue = new(){ new(){Speaker="Бармен",Text="Снова ты. Садись, рассказывай."}, new(){Speaker="Бармен",Text="Я помню тебя — и помню, что за тобой шли. Груз был непростой."}, new(){Speaker="Бармен",Text="Хочешь правду? Сначала помоги Зоне помочь тебе. Части мутантов, слухи, документы — всё в дело."} },
                Buys = new() { ItemType.Food, ItemType.Drink }, BuyRate = 0.5,
                Stock = Stock("bread", "canned", "stew", "sausage", "cheese", "crackers", "noodles",
                              "canned_fish", "honey", "mre", "sandwich", "mint_candy",
                              "water", "cola", "energy", "juice", "vodka", "tea_termos"),
                Barter = new()
                {
                    new() { Id = "bartender_mre", GivesCode = "mre", Cost = { Need("canned", 2), Need("water", 1) }, Money = 50 },
                    new() { Id = "bartender_stew", GivesCode = "stew", Cost = { Need("bread", 2), Need("sausage", 1) } },
                    new() { Id = "bartender_energy", GivesCode = "energy", GivesCount = 2, Cost = { Need("cola", 2), Need("coffee", 1) } },
                    new() { Id = "bartender_tea", GivesCode = "tea_termos", GivesCount = 1, Cost = { Need("water", 1), Need("honey", 1) }, Money = 20 },
                },
            },
            new()
            {
                Id = "ragman", Name = "Сидорович", Description = "Экипировка и рюкзаки.",
                Image = "/traders/ragman.png",
                Dialogue = new(){ new(){Speaker="Сидорович",Text="Одежда, броня, рюкзаки — выбирай, пока жив."}, new(){Speaker="Сидорович",Text="Хороший костюм не спасёт от глупости, но от пули — иногда да."} },
                Buys = new() { ItemType.Helmet, ItemType.Armor, ItemType.Backpack }, BuyRate = 0.5,
                Stock = Stock("cap", "helmet_ssh", "helmet_tactical", "helmet_altyn", "helmet_exo",
                              "jacket", "cloak", "vest", "combat_suit", "vest_heavy", "sphere", "exo",
                              "pouch", "bag_small", "backpack", "raidpack", "bag_large", "bag_expedition",
                              // Новые шлемы и головные уборы
                              "helmet_beanie", "helmet_balaclava", "helmet_bandana", "helmet_cap_mil", "helmet_ushanka",
                              "helmet_stalker", "helmet_bulletproof", "helmet_gasmask", "helmet_hardhat", "helmet_riot",
                              // Новые костюмы и одежда (слот костюма)
                              "suit_leather_coat", "suit_sweater", "suit_shirt", "suit_denim", "suit_puffer",
                              "suit_camo", "suit_gilly", "suit_seva", "suit_tactical", "suit_winter",
                              "cloak_long", "suit_hoodie", "suit_flannel", "suit_raincoat",
                              "suit_vest", "suit_armor",
                              // Новые бронежилеты/разгрузки (слот брони)
                              "vest_kevlar", "vest_ceramic", "vest_tactical", "vest_light",
                              "vest_military", "vest_stalker", "vest_police", "vest_bulletproof", "vest_steel",
                              "vest_6b13", "vest_korund", "rig_commando",
                              // Новые рюкзаки
                              "bag_briefcase", "bag_sack", "bag_daypack", "bag_tactpack", "bag_milpack",
                              "bag_huge", "bag_duffel", "bag_field", "bag_med", "bag_assault", "bag_cargo"),
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
                Dialogue = new(){ new(){Speaker="Кузнец",Text="Принеси металл — сделаю, что смогу."}, new(){Speaker="Кузнец",Text="Электроника, пайка, инструмент — без этого убежище не живёт."} },
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
                Image = "/traders/buyer.png",
                BuysEverything = true, BuyRate = 0.25, PriceMultiplier = 1.6,
                Dialogue = new(){ new(){Speaker="Скупщик",Text="Ты живой. Уже хорошо. В этой зоне это редкость."}, new(){Speaker="Скупщик",Text="Память дырявая? Бывает. Но долг и груз — вещи материальные. Ищи следы."}, new(){Speaker="Скупщик",Text="Принеси что-нибудь ценное — и я открою тебе нужные двери."} },
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

    public void ResetQuests()
    {
        Quests.Clear();
        Quests.AddRange(_quests.CreateTraderQuests());

        StoryQuests.Clear();
        StoryQuests.AddRange(_quests.CreateStory());

        SideQuests.Clear();
        SideQuests.AddRange(_quests.CreateSideQuests());
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
        // В рейде автоматическая регенерация (убежище) не работает, но игровое время идёт.
        AdvanceGameTime(GameMinutesPerTick);
        if (Raid is { Finished: false }) return;

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
        Player.SkillPoints = 0;
        Player.Luck = 1;
        Player.MeleeSkill = 1;
        Player.RangedSkill = 1;
        Player.Accuracy = 1;
        Player.Agility = 1;
        Player.Dodge = 1;
        Player.Parry = 1;
        Player.WeaponHandling = 1;
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
        Player.UnlockedTraders = new() { "buyer", "bartender" };
        Player.GameTimeMinutes = 480;
        Player.JournalThoughts.Clear();
        Player.EndingChoice = null;
        Player.BombHidden = false;
        Player.Perks.Clear();
        Player.TraderRep.Clear();
        Player.Cassettes.Clear();
        Player.Achievements.Clear(); // фикс: достижения тоже сбрасываем

        Player.WeaponMelee = null;
        Player.WeaponRanged = null;
        Player.Helmet = null;
        Player.Armor = null;
        Player.Suit = _catalog.Create("suit_hoodie");
        Player.Backpack = null;

        Player.Inventory.Clear();
        Player.Inventory.Add(_catalog.Create("knife"));

        Player.Shelter = new ShelterState();
        Player.Stats = new PlayerStats();

        CurrentCombat = null;
        PendingEnemy = null;
        Raid = null;

        ResetQuests();
    }

    /// <summary>Смерть игрока: теряется всё, кроме оружия ближнего боя.</summary>
    public Player KillPlayer()
    {
        var p = Player;
        p.WeaponRanged = null;
        p.Helmet = null; p.Armor = null; p.Suit = null; p.Backpack = null;
        p.Inventory.Clear();
        if (p.WeaponMelee != null) p.Inventory.Add(p.WeaponMelee);
        p.WeaponMelee = null;
        p.Suit = _catalog.Create("suit_hoodie");
        p.Hp = Math.Max(1, p.MaxHp / 10);
        p.Stamina = Math.Max(10, p.MaxStamina / 3);
        p.Hunger = Math.Max(0, p.Hunger - 20);
        p.Thirst = Math.Max(0, p.Thirst - 20);
        p.Stats.RaidsFailed += 1;

        CurrentCombat = null;
        PendingEnemy = null;
        Raid = null;
        AchievementCatalog.Evaluate(this);
        Persist();
        return p;
    }

    /// <summary>Записывает автосохранение (игрок + задания + активный рейд).</summary>
    public void Persist()
    {
        try
        {
            Directory.CreateDirectory(SaveDirectory);
            var snapshot = new SaveFile
            {
                Player = Player,
                Quests = Quests,
                StoryQuests = StoryQuests,
                SideQuests = SideQuests,
                Raid = Raid,
                Version = "7.5",
            };
            File.WriteAllText(
                Path.Combine(SaveDirectory, "autosave.json"),
                JsonSerializer.Serialize(snapshot, PersistJson));
        }
        catch (Exception e)
        {
            Console.WriteLine("Ошибка автосохранения: " + e.Message);
        }
    }
}