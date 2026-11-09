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

    /// <summary>Открывает локации, привязанные к уже полученным/доступным заданиям (напр. Завод при наличии задания «побывать на Заводе»).</summary>
    public void EnsureQuestBasedLocationUnlocks()
    {
        var p = Player;
        if (p == null) return;
        bool changed = false;

        // Завод: задание уже есть (цепочка Бармена дошла до bartender_6 или он принят; сюжет s1_10 текущий/принят).
        if (!p.UnlockedLocations.Contains("factory"))
        {
            var b5 = AllQuests().FirstOrDefault(q => q.Id == "bartender_5");
            var b6 = AllQuests().FirstOrDefault(q => q.Id == "bartender_6");
            bool bartenderGate = b6 != null && !b6.IsCompleted && ((b5?.IsCompleted ?? false) || b6.IsAccepted);

            var currentStory = _quests.GetCurrentStory(StoryQuests);
            bool storyGate = (currentStory?.Id == "s1_10" && !currentStory.IsCompleted)
                             || AllQuests().Any(q => q.Id == "s1_10" && q.IsAccepted && !q.IsCompleted);

            if (bartenderGate || storyGate)
            {
                p.UnlockedLocations.Add("factory");
                changed = true;
            }
        }

        if (changed) Persist();
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

        // Регистрируем оружие из Data/weapon_list.json (weapons + traderWeapons), включая гранаты.
        var seenCodes = new HashSet<string>();
        foreach (var w in WeaponCatalog.All.Concat(WeaponCatalog.TraderWeapons))
        {
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

        var grenadeStock = Stock("grenade", "wpn_f1_grenade", "wpn_smoke_grenade");

        var traders = new List<Trader>
        {
            new()
            {
                Id = "prapor", Name = "Прапор", Description = "Оружие на любой вкус.",
                Image = "/traders/prapor.png",
                Dialogue = new(){ new(){Speaker="Прапор",Text="Оружие есть. Вопросы — после дела."} },
                DialogueTree = new()
                {
                    new() { Id="root", Speaker="Прапор", Text="Подходи, не мямли. Стволы, патроны, гранаты — всё по назначению. Что тебе?", Choices = {
                        new() { Text="Нужна работа.", NextId="work" },
                        new() { Text="Что за обстановка на Заводе?", NextId="factory" },
                        new() { Text="Какое оружие посоветуешь новичку?", NextId="advice" },
                        new() { Text="Ничего, осматриваюсь.", Action="close" },
                    }},
                    new() { Id="work", Speaker="Прапор", Text="Дела есть. Иногда грязные, иногда просто опасные. Оплата честная — если вернёшься.", Choices = {
                        new() { Text="Я берусь. Давай задачу.", Action="accept_current_quest", NextId="work_ok" },
                        new() { Text="Назад.", NextId="root" },
                    }},
                    new() { Id="work_ok", Speaker="Прапор", Text="Так-то лучше. Проверю исполнение — и разговор будет другой.", Choices = {
                        new() { Text="Сделаю.", Action="talk", NextId="root" },
                    }},
                    new() { Id="factory", Speaker="Прапор", Text="Завод — мясорубка. Бандиты, странные люди с хорошей экипировкой и обломки старых цехов. Без плана и патронов туда лучше не лезть.", Choices = {
                        new() { Text="Учту.", NextId="root" },
                    }},
                    new() { Id="advice", Speaker="Прапор", Text="Новичку? Дробовик или простой автомат и башкой думать. Самый крутой ствол не спасёт, если полез в открытую на троих.", Choices = {
                        new() { Text="Справедливо.", NextId="root" },
                    }},
                },
                // Прапор — оружейный магазин: его стволы должны стоять в ценах melee 300–1000 / огнестрел 1100+,
                // поэтому базовую стоимость оружия у него смещаем ниже (снаряжение у «Старьёвщика» дороже — ×1.6 в ItemCatalog).
                Buys = new() { ItemType.WeaponMelee, ItemType.WeaponRanged, ItemType.Ammo },
                BuyRate = 0.5,
                PriceMultiplier = 0.8,
                Stock = Stock("knife", "machete", "sledge")
                    .Concat(weaponStock)
                    .Concat(ammoStock)
                    .Concat(grenadeStock)
                    .ToList(),
                Barter = new()
                {
                    new() { Id = "prapor_knife", GivesCode = "knife", Cost = { Need("scrap", 2), Need("cloth", 1) } },
                    new() { Id = "prapor_machete", GivesCode = "machete", Cost = { Need("scrap", 4), Need("duct_tape", 2) } },
                    new() { Id = "prapor_ammo9", GivesCode = "ammo_9x19mm", GivesCount = 30, Cost = { Need("scrap", 2), Need("wire", 1) } },
                    new() { Id = "prapor_ammo545", GivesCode = "ammo_545x39mm", GivesCount = 30, Cost = { Need("scrap", 3), Need("wire", 2) } },
                    new() { Id = "prapor_ammo762", GivesCode = "ammo_762x39mm", GivesCount = 30, Cost = { Need("scrap", 4), Need("gunpowder", 1) } },
                    new() { Id = "prapor_ammo12", GivesCode = "ammo_12gauge", GivesCount = 20, Cost = { Need("scrap", 3), Need("gunpowder", 1) } },
                    new() { Id = "prapor_grenade", GivesCode = "grenade", GivesCount = 1, Cost = { Need("scrap", 5), Need("gunpowder", 2), Need("wire", 1) } },
                    new() { Id = "prapor_f1_grenade", GivesCode = "wpn_f1_grenade", GivesCount = 1, Cost = { Need("scrap", 6), Need("gunpowder", 3), Need("wire", 1) } },
                    new() { Id = "prapor_smoke_grenade", GivesCode = "wpn_smoke_grenade", GivesCount = 1, Cost = { Need("cloth", 2), Need("chemicals", 1), Need("wire", 1) } },
                },
            },
            new()
            {
                Id = "therapist", Name = "Сергей", Description = "Только медицина: перевязка, лекарства и аптечки.",
                Image = "/traders/therapist.png",
                Dialogue = new(){ new(){Speaker="Сергей",Text="Покажи раны. И без героизма."} },
                DialogueTree = new()
                {
                    new() { Id="root", Speaker="Сергей", Text="Живой? Уже неплохо. Показывай, что там у тебя — раны сами не зарастут, особенно в Зоне.", Choices = {
                        new() { Text="Нужна какая-нибудь помощь/работа?", NextId="work" },
                        new() { Text="Какие опасности в Зоне кроме пуль?", NextId="lore" },
                        new() { Text="Чем мутанты опасны?", NextId="mutants" },
                        new() { Text="Я в порядке.", Action="close" },
                    }},
                    new() { Id="work", Speaker="Сергей", Text="Помощь нужна всегда: бинты, лекарства, образцы. Если принесёшь нужное — и тебе выгода, и другим выжившим легче.", Choices = {
                        new() { Text="Хорошо, что от меня требуется?", Action="accept_current_quest", NextId="work_ok" },
                        new() { Text="Назад.", NextId="root" },
                    }},
                    new() { Id="work_ok", Speaker="Сергей", Text="Договорились. Только аккуратнее — я врач, но чудеса делаю редко.", Choices = {
                        new() { Text="Постараюсь вернуться целым.", Action="talk", NextId="root" },
                    }},
                    new() { Id="lore", Speaker="Сергей", Text="Кровотечение, инфекции, голод, усталость. Иногда самая глупая царапина после рейда становится настоящей проблемой. Не пренебрегай аптечкой.", Choices = {
                        new() { Text="Буду иметь в виду.", NextId="root" },
                    }},
                    new() { Id="mutants", Speaker="Сергей", Text="Они непредсказуемы. Одни бьют в лоб, другие заражают или калечат. Но их части иногда полезны — для исследований и обмена. Только брать их лучше осторожно.", Choices = {
                        new() { Text="Понятно.", NextId="root" },
                    }},
                },
                Buys = new() { ItemType.Medicine }, BuyRate = 3.375, PriceMultiplier = 6.75,
                Stock = Stock("bandage", "bandage_sterile", "bandage_pack", "iodine", "vaseline",
                              "painkiller", "painkiller_strong", "morphine", "hemostatic", "saline",
                              "tourniquet", "splint", "vitamins",
                              "medkit", "medkit_army", "medkit_car",
                              "surgical_kit", "antirad", "stimulant", "antidote", "blood_bag"),
                Barter = new()
                {
                    new() { Id = "therapist_medkit", GivesCode = "medkit", Cost = { Need("bandage", 2), Need("cloth", 1) } },
                    new() { Id = "therapist_antirad", GivesCode = "antirad", Cost = { Need("bandage", 1), Need("battery", 1) } },
                    new() { Id = "therapist_blood", GivesCode = "blood_bag", Cost = { Need("tourniquet", 1), Need("iodine", 1), Need("bandage_sterile", 1) } },
                    new() { Id = "therapist_army", GivesCode = "medkit_army", Cost = { Need("medkit", 1), Need("hemostatic", 1), Need("saline", 1) } },
                    new() { Id = "therapist_surgical", GivesCode = "surgical_kit", Cost = { Need("medkit_army", 1), Need("morphine", 1), Need("bandage_pack", 1) } },
                    new() { Id = "therapist_stim", GivesCode = "stimulant", GivesCount = 2, Cost = { Need("painkiller", 2), Need("saline", 1) } },
                    new() { Id = "therapist_bandagepack", GivesCode = "bandage_pack", GivesCount = 2, Cost = { Need("cloth", 3), Need("iodine", 1) } },
                },
            },
            new()
            {
                Id = "bartender", Name = "Бармен", Description = "Еда, напитки и тёплый уголок.",
                Image = "/traders/barman.png",
                Dialogue = new(){ new(){Speaker="Бармен",Text="Снова ты. Садись, рассказывай."} },
                DialogueTree = new()
                {
                    new() { Id="root", Speaker="Бармен", Text="Снова ты. Садись, наливаю. В Зоне живых гостей мало — и все с историями.", Choices = {
                        new() { Text="Есть работа для меня?", NextId="work" },
                        new() { Text="Что слышно в Зоне?", NextId="lore" },
                        new() { Text="Расскажи про тот груз...", NextId="cargo" },
                        new() { Text="Поговорим позже.", Action="close" },
                    }},
                    new() { Id="work", Speaker="Бармен", Text="Работа всегда найдётся. То припасы притащить, то тварей отогнать. Главное — вернуться живым, остальное приложится.", Choices = {
                        new() { Text="Берусь. Что нужно сделать?", Action="accept_current_quest", NextId="work_ok" },
                        new() { Text="Назад.", NextId="root" },
                    }},
                    new() { Id="work_ok", Speaker="Бармен", Text="Вот и славно. Детали — в задании. И не геройствуй зря: мёртвые заказов не закрывают.", Choices = {
                        new() { Text="Понял.", Action="talk", NextId="root" },
                    }},
                    new() { Id="lore", Speaker="Бармен", Text="Слышно разное. На Кордоне сталкеры нервничают, на Заводе кто-то снова роет цеха. А по ночам в Городе лучше не светиться — там теперь не только бандиты.", Choices = {
                        new() { Text="Что за люди на Заводе?", NextId="factory" },
                        new() { Text="Назад.", NextId="root" },
                    }},
                    new() { Id="factory", Speaker="Бармен", Text="Наёмники, говорят. Аккуратные, вооружённые, без лишних разговоров. Такие просто так по Зоне не гуляют — значит, кто-то хорошо платит.", Choices = {
                        new() { Text="Понятно.", NextId="root" },
                    }},
                    new() { Id="cargo", Speaker="Бармен", Text="Ты спрашиваешь опасные вещи. Груз был непростой, и за ним шли серьёзные люди. Помоги мне с парой дел — и я расскажу больше.", Choices = {
                        new() { Text="Есть работа для меня?", NextId="work" },
                        new() { Text="Вернёмся к этому позже.", NextId="root" },
                    }},
                },
                Buys = new() { ItemType.Food, ItemType.Drink }, BuyRate = 3.5, PriceMultiplier = 7.0,
                Stock = Stock("bread", "canned", "stew", "sausage", "cheese", "crackers", "noodles",
                              "canned_fish", "honey", "mre", "sandwich", "mint_candy",
                              "water", "mineral_water", "cola", "energy", "juice", "vodka", "tea_termos"),
                Barter = new()
                {
                    new() { Id = "bartender_mre", GivesCode = "mre", Cost = { Need("canned", 2), Need("water", 1) } },
                    new() { Id = "bartender_stew", GivesCode = "stew", Cost = { Need("bread", 2), Need("sausage", 1) } },
                    new() { Id = "bartender_energy", GivesCode = "energy", GivesCount = 2, Cost = { Need("cola", 2), Need("coffee", 1) } },
                    new() { Id = "bartender_tea", GivesCode = "tea_termos", GivesCount = 1, Cost = { Need("water", 1), Need("honey", 1) } },
                    new() { Id = "bartender_honey", GivesCode = "honey", GivesCount = 2, Cost = { Need("water", 2), Need("mint_candy", 2) } },
                    new() { Id = "bartender_vodka", GivesCode = "vodka", Cost = { Need("water", 2), Need("bread", 1) } },
                    new() { Id = "bartender_supply", GivesCode = "canned", GivesCount = 3, Cost = { Need("scrap", 2), Need("cloth", 1) } },
                    new() { Id = "bartender_mineral", GivesCode = "mineral_water", GivesCount = 3, Cost = { Need("water", 3), Need("salt", 1) } },
                },
            },
            new()
            {
                Id = "ragman", Name = "Сидорович", Description = "Экипировка и рюкзаки.",
                Image = "/traders/ragman.png",
                Dialogue = new(){ new(){Speaker="Сидорович",Text="Одежда, броня, рюкзаки — выбирай, пока жив."} },
                DialogueTree = new()
                {
                    new() { Id="root", Speaker="Сидорович", Text="Подходи, выбирай. Броня, шлем, рюкзак — всё, что между тобой и могилой. Дёшево не обещаю, но вещь честная.", Choices = {
                        new() { Text="Есть для меня работа?", NextId="work" },
                        new() { Text="Что лучше взять в рейд?", NextId="advice" },
                        new() { Text="Как дела на Барахолке?", NextId="lore" },
                        new() { Text="Пока просто смотрю.", Action="close" },
                    }},
                    new() { Id="work", Speaker="Сидорович", Text="Работа найдётся. То ткань нужна, то кто-то бронежилет не донёс из рейда. Сделаешь — будем договариваться.", Choices = {
                        new() { Text="Что нужно?", Action="accept_current_quest", NextId="work_ok" },
                        new() { Text="Назад.", NextId="root" },
                    }},
                    new() { Id="work_ok", Speaker="Сидорович", Text="Вот и договорились. Задание смотри в списке. Вернёшься с добычей — поговорим по-другому.", Choices = {
                        new() { Text="Иду.", Action="talk", NextId="root" },
                    }},
                    new() { Id="advice", Speaker="Сидорович", Text="Смотри по задаче. Лёгкий выход — взял рюкзак побольше и тихо. Зачистка — броня и шлем решают. А главное — не бери того, что не унесёшь.", Choices = {
                        new() { Text="Учту.", NextId="root" },
                    }},
                    new() { Id="lore", Speaker="Сидорович", Text="Дела как в Зоне: сегодня есть товар, завтра стрельба на подходах. Народ всё равно тащит хабар — жить-то надо. Так что Барахолка стоит, пока стоим мы.", Choices = {
                        new() { Text="Бывай.", NextId="root" },
                    }},
                },
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
                    new() { Id = "ragman_vest", GivesCode = "vest", Cost = { Need("cloth", 3), Need("rubber", 2) } },
                    new() { Id = "ragman_combat", GivesCode = "combat_suit", Cost = { Need("cloth", 5), Need("cement", 2) } },
                    new() { Id = "ragman_raidpack", GivesCode = "raidpack", Cost = { Need("cloth", 4), Need("zipties", 3), Need("duct_tape", 2) } },
                    new() { Id = "ragman_helmet", GivesCode = "helmet_ssh", Cost = { Need("scrap", 4), Need("cloth", 2) } },
                    new() { Id = "ragman_backpack", GivesCode = "backpack", Cost = { Need("cloth", 4), Need("paracord", 2) } },
                    new() { Id = "ragman_jacket", GivesCode = "jacket", Cost = { Need("cloth", 2), Need("duct_tape", 1) } },
                    new() { Id = "ragman_rig", GivesCode = "vest_light", Cost = { Need("cloth", 3), Need("zipties", 4) } },
                },
            },
            new()
            {
                Id = "blacksmith", Name = "Кузнец", Description = "Броня и материалы.",
                Image = "/traders/blacksmith.png",
                Dialogue = new(){ new(){Speaker="Кузнец",Text="Принеси металл — сделаю, что смогу."} },
                DialogueTree = new()
                {
                    new() { Id="root", Speaker="Кузнец", Text="Заходи. Если железо, провода, инструменты — найдём общий язык. Без дела в мастерской не стою.", Choices = {
                        new() { Text="Есть работа?", NextId="work" },
                        new() { Text="Что тебе сейчас нужнее всего?", NextId="need" },
                        new() { Text="Совет по снаряге?", NextId="advice" },
                        new() { Text="Заглянул просто так.", Action="close" },
                    }},
                    new() { Id="work", Speaker="Кузнец", Text="Работы хватает. Материалов вечно не хватает. Принесёшь нужное — отблагодарю, а иногда и полезную вещь подгоню.", Choices = {
                        new() { Text="Что принести?", Action="accept_current_quest", NextId="work_ok" },
                        new() { Text="Назад.", NextId="root" },
                    }},
                    new() { Id="work_ok", Speaker="Кузнец", Text="Вот это разговор. Детали в задании. Главное — не тащи совсем хлам, мне толк нужен.", Choices = {
                        new() { Text="Принесу.", Action="talk", NextId="root" },
                    }},
                    new() { Id="need", Speaker="Кузнец", Text="Металлолом, провода, электроника — основа всего. С этим и броню подлатать можно, и убежище держать на плаву.", Choices = {
                        new() { Text="Понял.", NextId="root" },
                    }},
                    new() { Id="advice", Speaker="Кузнец", Text="Снаряга должна работать, а не красиво блестеть. Починил вовремя — уже спасся один раз. И вес считай: перегруз в рейде убивает не хуже пули.", Choices = {
                        new() { Text="Дельный совет.", NextId="root" },
                    }},
                },
                Buys = new() { ItemType.Material }, BuyRate = 3.625, PriceMultiplier = 7.25,
                Stock = Stock("scrap", "cloth", "wire", "battery", "cement", "glass", "rubber", "electronics",
                              "fuel", "toolkit", "nails", "duct_tape", "zipties", "kerosene", "propane", "sparkplug"),
                Barter = new()
                {
                    new() { Id = "blacksmith_helmet", GivesCode = "helmet_tactical", Cost = { Need("scrap", 4), Need("cement", 1) } },
                    new() { Id = "blacksmith_toolkit", GivesCode = "toolkit", Cost = { Need("scrap", 3), Need("electronics", 1) } },
                    new() { Id = "blacksmith_sparkplug", GivesCode = "sparkplug", GivesCount = 3, Cost = { Need("scrap", 1), Need("wire", 1) } },
                    new() { Id = "blacksmith_wire", GivesCode = "wire", GivesCount = 5, Cost = { Need("scrap", 3), Need("duct_tape", 1) } },
                    new() { Id = "blacksmith_electronics", GivesCode = "electronics", GivesCount = 2, Cost = { Need("capacitors", 2), Need("resistors", 3), Need("wire", 2) } },
                    new() { Id = "blacksmith_nails", GivesCode = "nails", GivesCount = 10, Cost = { Need("scrap", 2) } },
                    new() { Id = "blacksmith_fuel", GivesCode = "fuel", Cost = { Need("rubber", 2), Need("scrap", 2) } },
                },
            },
            new()
            {
                Id = "buyer", Name = "Скупщик", Description = "Скупает всё подряд. Дорого продаёт хлам.",
                Image = "/traders/buyer.png",
                BuysEverything = true, BuyRate = 2.5, PriceMultiplier = 10.0,
                Dialogue = new(){ new(){Speaker="Скупщик",Text="Ты живой. Уже хорошо. В этой зоне это редкость."} },
                DialogueTree = new()
                {
                    new() { Id="root", Speaker="Скупщик", Text="О, знакомое лицо... или незнакомое. В Зоне это быстро меняется. Что принёс — интересное или просто мусор?", Choices = {
                        new() { Text="Есть дело/работа?", NextId="work" },
                        new() { Text="Что ценится в Зоне?", NextId="value" },
                        new() { Text="Ты много знаешь. Что насчёт прошлого груза?", NextId="cargo" },
                        new() { Text="Просто смотрю товар.", Action="close" },
                    }},
                    new() { Id="work", Speaker="Скупщик", Text="Дела есть. Я люблю, когда кто-то делает грязную работу, а я получаю полезный результат. Взаимовыгодно, если ты понимаешь намёки.", Choices = {
                        new() { Text="Говори, что нужно.", Action="accept_current_quest", NextId="work_ok" },
                        new() { Text="Назад.", NextId="root" },
                    }},
                    new() { Id="work_ok", Speaker="Скупщик", Text="Умница. Принесёшь — обсудим цену твоей удачи. Задание уже ждёт.", Choices = {
                        new() { Text="Договорились.", Action="talk", NextId="root" },
                    }},
                    new() { Id="value", Speaker="Скупщик", Text="Ценится то, что редко и работает: документы, электроника, украшения, артефакты. Но и обычный хлам куплю — дёшево, конечно. У меня тут не благотворительность.", Choices = {
                        new() { Text="Ясно.", NextId="root" },
                    }},
                    new() { Id="cargo", Speaker="Скупщик", Text="Про груз слухи ходят разные. Кто-то говорит — военные, кто-то — наёмники, кто-то — совсем чужие деньги. Следы есть, но за ответы обычно платят. Или работают.", Choices = {
                        new() { Text="Есть дело/работа?", NextId="work" },
                        new() { Text="Вернёмся к этому.", NextId="root" },
                    }},
                },
                Stock = Stock("watch", "amber", "goldchain", "icon", "artifact", "documents",
                              "coffee", "cigarettes", "chocolate", "dogtags", "medal", "cards",
                              "harmonica", "compass", "binoculars", "camera", "radio_parts", "geiger_counter",
                              "glass", "rubber", "scrap", "fuel"),
                Barter = new()
                {
                    new() { Id = "buyer_artifact", GivesCode = "artifact", Cost = { Need("goldchain", 1), Need("icon", 1), Need("watch", 1) } },
                    new() { Id = "buyer_electronics", GivesCode = "electronics", GivesCount = 2, Cost = { Need("glass", 3), Need("rubber", 2) } },
                    new() { Id = "buyer_geiger", GivesCode = "geiger_counter", Cost = { Need("radio_parts", 1), Need("electronics", 2), Need("battery", 1) } },
                    new() { Id = "buyer_coffee", GivesCode = "coffee", Cost = { Need("cigarettes", 2), Need("cards", 1) } },
                    new() { Id = "buyer_radio", GivesCode = "radio_parts", GivesCount = 2, Cost = { Need("wire", 3), Need("capacitors", 2) } },
                    new() { Id = "buyer_binoculars", GivesCode = "binoculars", Cost = { Need("glass", 4), Need("scrap", 2) } },
                    new() { Id = "buyer_docs", GivesCode = "documents", Cost = { Need("paper_pack", 3), Need("old_documents", 2) } },
                    new() { Id = "buyer_gold", GivesCode = "goldchain", Cost = { Need("old_money", 4), Need("watch", 1) } },
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
        Player.JournalNotes.Clear();
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

    /// <summary>Смерть игрока: теряется всё, кроме оружия ближнего боя и квестовых/сюжетных предметов.</summary>
    public Player KillPlayer()
    {
        var p = Player;
        var keepQuestItems = p.Inventory
            .Where(i => i.Code.StartsWith("quest_", StringComparison.OrdinalIgnoreCase)
                     || i.Code.StartsWith("q_", StringComparison.OrdinalIgnoreCase)
                     || i.Code.StartsWith("boss_", StringComparison.OrdinalIgnoreCase)
                     || i.Code.StartsWith("art_", StringComparison.OrdinalIgnoreCase))
            .Select(i => i)
            .ToList();
        foreach (var qi in keepQuestItems) qi.FoundInRaid = true;
        p.WeaponRanged = null;
        p.Helmet = null; p.Armor = null; p.Suit = null; p.Backpack = null;
        p.Inventory.Clear();
        foreach (var qi in keepQuestItems) p.Inventory.Add(qi);
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