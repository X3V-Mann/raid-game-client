using GameApi.Models;

namespace GameApi.Services;

public class ItemCatalog
{
    public const string MissingIcon = "/error_or_missing.png";

    private readonly List<Item> _templates = new()
    {
        // === Ближний бой (только ручные — из JSON оружие ближнего боя есть, но эти оставляем как «стартовые») ===
        T("knife", "Нож", ItemType.WeaponMelee, Rarity.Common, 50, dmg: 8, icon: "/items/knife.png",
            desc: "Простой армейский нож. Тихо, дешево и всегда с собой."),
        T("machete", "Мачете", ItemType.WeaponMelee, Rarity.Uncommon, 180, dmg: 14, icon: "/items/machete.png",
            desc: "Тяжёлый клинок для рубки кустов и не только."),
        T("axe", "Пожарный топор", ItemType.WeaponMelee, Rarity.Rare, 150, dmg: 22, icon: "/items/axe.png",
            desc: "Пробивает двери и рёбра. Незаменим в тесном бою."),
        T("sledge", "Кувалда", ItemType.WeaponMelee, Rarity.Uncommon, 800, dmg: 18, icon: MissingIcon,
            desc: "Инструмент, который случайно оказался оружием."),

        // === Шлемы ===
        T("cap", "Кепка", ItemType.Helmet, Rarity.Common, 40, def: 1, icon: "/items/cap.png", desc: "От солнца помогает. От пули — не очень."),
        T("helmet_ssh", "Каска СШ-68", ItemType.Helmet, Rarity.Uncommon, 250, def: 4, icon: "/items/helmet_ssh.png", desc: "Старая добрая сталь. Лучше, чем ничего."),
        T("helmet_tactical", "Тактический шлем", ItemType.Helmet, Rarity.Uncommon, 700, def: 6, icon: MissingIcon, desc: "Лёгкий, с креплениями под ПНВ и гарнитуру."),
        T("helmet_altyn", "Шлем «Алтын»", ItemType.Helmet, Rarity.Rare, 1200, def: 8, icon: "/items/helmet_altyn.png", desc: "Тяжёлый штурмовой шлем."),
        T("helmet_exo", "Шлем экзоскелета", ItemType.Helmet, Rarity.Epic, 2500, def: 12, icon: MissingIcon, desc: "Герметичный купол с забралом."),

        // === Броня ===
        T("jacket", "Кожаная куртка", ItemType.Armor, Rarity.Common, 80, def: 2, icon: "/items/jacket.png", desc: "Держит холод и мелкие неприятности."),
        T("cloak", "Плащ сталкера", ItemType.Armor, Rarity.Common, 150, def: 3, icon: MissingIcon, desc: "Пропитанный пылью плащ."),
        T("vest", "Бронежилет", ItemType.Armor, Rarity.Uncommon, 700, def: 7, icon: "/items/vest.png", desc: "Базовый бронежилет."),
        T("combat_suit", "Штурмовой комбез", ItemType.Armor, Rarity.Rare, 900, def: 9, icon: MissingIcon, desc: "Комбинезон с бронепластинами."),
        T("vest_heavy", "Тяжёлый бронежилет", ItemType.Armor, Rarity.Epic, 3200, def: 14, icon: "/items/vest_heavy.png", desc: "Много керамики и ещё больше веса."),
        T("sphere", "Броня «Сфера»", ItemType.Armor, Rarity.Epic, 2600, def: 16, icon: MissingIcon, desc: "Экспериментальная разработка."),
        T("exo", "Экзоскелет", ItemType.Armor, Rarity.Legendary, 6000, def: 24, icon: MissingIcon, desc: "Рама, сервоприводы и броня."),

        // === Рюкзаки ===
        T("pouch", "Наплечная сумка", ItemType.Backpack, Rarity.Common, 100, slots: 9, icon: "/items/pouch.png", desc: "Небольшая сумка."),
        T("bag_small", "Рюкзак «Турист»", ItemType.Backpack, Rarity.Common, 150, slots: 25, icon: MissingIcon, desc: "Простой рюкзак."),
        T("backpack", "Рюкзак", ItemType.Backpack, Rarity.Uncommon, 400, slots: 30, icon: "/items/backpack.png", desc: "Проверенный временем рюкзак."),
        T("raidpack", "Штурмовой рюкзак", ItemType.Backpack, Rarity.Rare, 1100, slots: 35, icon: "/items/raidpack.png", desc: "Усиленный каркас."),
        T("bag_large", "Большой рюкзак", ItemType.Backpack, Rarity.Rare, 900, slots: 40, icon: MissingIcon, desc: "Можно унести половину тайника."),
        T("bag_expedition", "Экспедиционный рюкзак", ItemType.Backpack, Rarity.Epic, 2000, slots: 50, icon: MissingIcon, desc: "Для долгих выходов."),

        // === Еда ===
        T("bread", "Хлеб", ItemType.Food, Rarity.Common, 25, restore: 20, restores: "hunger", icon: "/items/bread.png", desc: "Чёрствый, но сытный."),
        T("canned", "Тушёнка", ItemType.Food, Rarity.Uncommon, 60, restore: 40, restores: "hunger", icon: "/items/canned.png", desc: "Праздник в консервной банке."),
        T("stew", "Рагу", ItemType.Food, Rarity.Uncommon, 90, restore: 55, restores: "hunger", icon: MissingIcon, desc: "Домашнее рагу в вакууме."),
        T("sausage", "Колбаса", ItemType.Food, Rarity.Common, 45, restore: 30, restores: "hunger", icon: MissingIcon, desc: "Копчёная, дешёвая, жирная."),
        T("cheese", "Сыр", ItemType.Food, Rarity.Common, 40, restore: 25, restores: "hunger", icon: MissingIcon, desc: "Пластиковый сыр в вакууме."),
        T("crackers", "Галеты", ItemType.Food, Rarity.Common, 20, restore: 15, restores: "hunger", icon: MissingIcon, desc: "Сухие, но не портятся."),
        T("noodles", "Лапша быстрого приготовления", ItemType.Food, Rarity.Common, 30, restore: 25, restores: "hunger", icon: MissingIcon, desc: "Залей кипятком — и живи."),
        T("canned_fish", "Консервированная рыба", ItemType.Food, Rarity.Uncommon, 55, restore: 35, restores: "hunger", icon: MissingIcon, desc: "Пахнет морем. Или чем-то похожим."),
        T("honey", "Мёд", ItemType.Food, Rarity.Uncommon, 70, restore: 45, restores: "hunger", icon: MissingIcon, desc: "Натуральный, густой, дорогой."),
        T("mre", "Сухпаёк", ItemType.Food, Rarity.Rare, 150, restore: 70, restores: "hunger", icon: MissingIcon, desc: "Полный армейский сухпаёк. На день."),

        // === Вода ===
        T("water", "Вода", ItemType.Drink, Rarity.Common, 25, restore: 20, restores: "thirst", icon: "/items/water.png", desc: "Питьевая вода."),
        T("cola", "Газировка", ItemType.Drink, Rarity.Uncommon, 55, restore: 40, restores: "thirst", icon: "/items/cola.png", desc: "Тёплая, но всё ещё вкусная."),
        T("energy", "Энергетик", ItemType.Drink, Rarity.Uncommon, 80, restore: 35, restores: "thirst", icon: MissingIcon, desc: "Химия и кофеин."),
        T("juice", "Сок", ItemType.Drink, Rarity.Common, 40, restore: 30, restores: "thirst", icon: MissingIcon, desc: "Фруктовый сок в тетрапаке."),
        T("vodka", "Водка", ItemType.Drink, Rarity.Uncommon, 90, restore: 30, restores: "thirst", icon: MissingIcon, desc: "Не вода, но согревает."),

        // === Медицина ===
        T("bandage", "Бинт", ItemType.Medicine, Rarity.Common, 40, restore: 25, restores: "hp", icon: "/items/bandage.png", desc: "Останавливает кровь."),
        T("bandage_sterile", "Стерильный бинт", ItemType.Medicine, Rarity.Uncommon, 70, restore: 35, restores: "hp", icon: MissingIcon, desc: "Чистый бинт. Меньше шансов на заражение."),
        T("painkiller", "Обезболивающее", ItemType.Medicine, Rarity.Uncommon, 100, restore: 20, restores: "hp", icon: MissingIcon, desc: "Заглушает боль."),
        T("painkiller_strong", "Мощное обезболивающее", ItemType.Medicine, Rarity.Rare, 180, restore: 35, restores: "hp", icon: MissingIcon, desc: "Для тех, кому очень больно."),
        T("medkit", "Аптечка", ItemType.Medicine, Rarity.Rare, 220, restore: 60, restores: "hp", icon: "/items/medkit.png", desc: "Полный набор."),
        T("surgical_kit", "Хирургический набор", ItemType.Medicine, Rarity.Epic, 500, restore: 90, restores: "hp", icon: MissingIcon, desc: "Почти операционная на дому."),
        T("antirad", "Антирад", ItemType.Medicine, Rarity.Rare, 150, restore: 30, restores: "hp", icon: MissingIcon, desc: "Горькая таблетка."),
        T("iodine", "Йод", ItemType.Medicine, Rarity.Common, 35, restore: 15, restores: "hp", icon: MissingIcon, desc: "Жжёт, но дезинфицирует."),
        T("tourniquet", "Жгут", ItemType.Medicine, Rarity.Uncommon, 80, restore: 25, restores: "hp", icon: MissingIcon, desc: "Для тяжёлых кровотечений."),
        T("splint", "Шина", ItemType.Medicine, Rarity.Uncommon, 90, restore: 20, restores: "hp", icon: MissingIcon, desc: "Фиксирует переломы."),
        T("stimulant", "Стимулятор", ItemType.Medicine, Rarity.Rare, 200, restore: 40, restores: "hp", icon: MissingIcon, desc: "Бодрит и лечит. Потом будет хуже."),
        T("antidote", "Антидот", ItemType.Medicine, Rarity.Rare, 220, restore: 25, restores: "hp", icon: MissingIcon, desc: "Против ядов и токсинов."),
        T("blood_bag", "Пакет крови", ItemType.Medicine, Rarity.Epic, 450, restore: 80, restores: "hp", icon: MissingIcon, desc: "Восстанавливает много крови быстро."),

        // === Материалы ===
        T("scrap", "Металлолом", ItemType.Material, Rarity.Common, 30, icon: "/items/scrap.png", desc: "Ржавые куски металла."),
        T("cloth", "Ткань", ItemType.Material, Rarity.Common, 20, icon: "/items/cloth.png", desc: "Отрез ткани."),
        T("glass", "Стекло", ItemType.Material, Rarity.Common, 25, icon: MissingIcon, desc: "Осколки и бутылки."),
        T("cement", "Цемент", ItemType.Material, Rarity.Common, 35, icon: MissingIcon, desc: "Мешок цемента."),
        T("wire", "Провода", ItemType.Material, Rarity.Uncommon, 45, icon: "/items/wire.png", desc: "Моток медных проводов."),
        T("rubber", "Резина", ItemType.Material, Rarity.Uncommon, 40, icon: MissingIcon, desc: "Кусок резины."),
        T("battery", "Аккумулятор", ItemType.Material, Rarity.Rare, 150, icon: "/items/battery.png", desc: "Ещё живой аккумулятор."),
        T("fuel", "Топливо", ItemType.Material, Rarity.Rare, 90, icon: MissingIcon, desc: "Канистра с горючим."),
        T("electronics", "Микросхемы", ItemType.Material, Rarity.Rare, 120, icon: MissingIcon, desc: "Платы из старой техники."),
        T("toolkit", "Набор инструментов", ItemType.Material, Rarity.Rare, 300, icon: MissingIcon, desc: "Для сложных работ в убежище."),
        T("nails", "Гвозди", ItemType.Material, Rarity.Common, 15, icon: MissingIcon, desc: "Горсть ржавых гвоздей."),
        T("duct_tape", "Скотч", ItemType.Material, Rarity.Common, 25, icon: MissingIcon, desc: "Серый скотч. Универсально."),
        T("zipties", "Стяжки", ItemType.Material, Rarity.Common, 20, icon: MissingIcon, desc: "Пластиковые стяжки."),
        T("kerosene", "Керосин", ItemType.Material, Rarity.Uncommon, 60, icon: MissingIcon, desc: "Канистра керосина."),
        T("propane", "Пропан", ItemType.Material, Rarity.Uncommon, 70, icon: MissingIcon, desc: "Баллон пропана."),
        T("sparkplug", "Свечи зажигания", ItemType.Material, Rarity.Uncommon, 50, icon: MissingIcon, desc: "Комплект свечей."),

        // === Ценности / бартер ===
        T("watch", "Часы", ItemType.Misc, Rarity.Rare, 300, icon: "/items/watch.png", desc: "Механические часы."),
        T("amber", "Янтарь", ItemType.Misc, Rarity.Rare, 500, icon: MissingIcon, desc: "Кусок янтаря."),
        T("goldchain", "Золотая цепь", ItemType.Misc, Rarity.Epic, 900, icon: "/items/goldchain.png", desc: "Тяжёлая цепь."),
        T("icon", "Икона", ItemType.Misc, Rarity.Epic, 700, icon: MissingIcon, desc: "Старая икона в окладе."),
        T("artifact", "Артефакт «Капля»", ItemType.Misc, Rarity.Legendary, 5000, icon: "/items/artifact.png", desc: "Светится в темноте."),
        T("documents", "Документы", ItemType.Misc, Rarity.Rare, 1200, icon: MissingIcon, desc: "Папка с бумагами."),
        T("coffee", "Кофе", ItemType.Misc, Rarity.Uncommon, 120, icon: MissingIcon, desc: "Банка растворимого кофе."),
        T("cigarettes", "Сигареты", ItemType.Misc, Rarity.Common, 80, icon: MissingIcon, desc: "Пачка сигарет. Валюта не хуже денег."),
        T("chocolate", "Шоколад", ItemType.Misc, Rarity.Uncommon, 100, icon: MissingIcon, desc: "Плитка шоколада."),
        T("dogtags", "Жетоны", ItemType.Misc, Rarity.Rare, 250, icon: MissingIcon, desc: "Солдатские жетоны. Чьи-то имена."),
        T("medal", "Медаль", ItemType.Misc, Rarity.Epic, 600, icon: MissingIcon, desc: "Боевая награда."),
        T("cards", "Игральные карты", ItemType.Misc, Rarity.Common, 60, icon: MissingIcon, desc: "Потрёпанная колода."),
        T("harmonica", "Губная гармошка", ItemType.Misc, Rarity.Uncommon, 150, icon: MissingIcon, desc: "Играет, если дунуть."),
        T("compass", "Компас", ItemType.Misc, Rarity.Rare, 350, icon: MissingIcon, desc: "Работает, хоть и врёт."),
        T("binoculars", "Бинокль", ItemType.Misc, Rarity.Rare, 400, icon: MissingIcon, desc: "Видно далеко."),
        T("camera", "Фотоаппарат", ItemType.Misc, Rarity.Uncommon, 200, icon: MissingIcon, desc: "Плёнка ещё жива."),
        T("radio_parts", "Радиодетали", ItemType.Misc, Rarity.Rare, 300, icon: MissingIcon, desc: "Лампы, конденсаторы, платы."),
        T("geiger_counter", "Счётчик Гейгера", ItemType.Misc, Rarity.Epic, 800, icon: MissingIcon, desc: "Трещит там, где не надо."),

        // === Кассеты ===
        T("cassette_1", "Кассета «Good Ending»", ItemType.Cassette, Rarity.Rare, 300, icon: "/items/cassette.gif", desc: "Плёнка с тёплой мелодией.", lootable: false),
        T("cassette_2", "Кассета «Dirge for the Planet»", ItemType.Cassette, Rarity.Rare, 300, icon: "/items/cassette.gif", desc: "Тяжёлая, тянущаяся запись.", lootable: false),
        T("cassette_3", "Кассета «Desolated»", ItemType.Cassette, Rarity.Rare, 300, icon: "/items/cassette.gif", desc: "Гул в тишине.", lootable: false),

        // === Граната ===
        T("grenade", "FDE-6", ItemType.Misc, Rarity.Common, 150, dmg: 40, icon: MissingIcon, desc: "Обычная осколочная граната.", lootable: true, isGrenade: true),
    };

    private static readonly Dictionary<string, string> IconOverrides = new()
    {
        ["sledge"] = "/items/sledge.png", ["helmet_tactical"] = "/items/helmet_tactical.png",
        ["helmet_exo"] = "/items/helmet_exo.png", ["cloak"] = "/items/cloak.png",
        ["combat_suit"] = "/items/combat_suit.png", ["sphere"] = "/items/sphere.png",
        ["exo"] = "/items/exo.png", ["bag_small"] = "/items/bag_small.png",
        ["bag_large"] = "/items/bag_large.png", ["bag_expedition"] = "/items/bag_expedition.png",
        ["energy"] = "/items/energy.png", ["stew"] = "/items/stew.png",
        ["painkiller"] = "/items/painkiller.png", ["antirad"] = "/items/antirad.png",
        ["cement"] = "/items/cement.png",
    };

    private static readonly Dictionary<string, int> LootTierOverrides = new()
    {
        ["scrap"] = 1, ["cloth"] = 1, ["wire"] = 1, ["glass"] = 1, ["cement"] = 1, ["rubber"] = 1,
        ["nails"] = 1, ["duct_tape"] = 1, ["zipties"] = 1,
        ["battery"] = 2, ["electronics"] = 2, ["fuel"] = 2, ["toolkit"] = 2,
        ["kerosene"] = 2, ["propane"] = 2, ["sparkplug"] = 2,
        ["documents"] = 4,
        ["exo"] = 6,
        ["cassette_1"] = 1, ["cassette_2"] = 1, ["cassette_3"] = 1,
    };

    private static readonly Dictionary<string, double> WeightOverrides = new()
    {
        ["knife"] = 0.4, ["machete"] = 1.0, ["axe"] = 2.2, ["sledge"] = 4.0,
        ["exo"] = 18.0, ["sphere"] = 12.0, ["vest_heavy"] = 10.0, ["combat_suit"] = 7.0,
    };

    private static double DefaultWeight(ItemType type) => type switch
    {
        ItemType.WeaponMelee => 2.0,
        ItemType.WeaponRanged => 3.5,
        ItemType.Helmet => 1.5,
        ItemType.Armor => 6.0,
        ItemType.Backpack => 1.2,
        ItemType.Food => 0.4,
        ItemType.Drink => 0.6,
        ItemType.Medicine => 0.2,
        ItemType.Material => 0.8,
        ItemType.Cassette => 0.15,
        ItemType.Ammo => 0.5,
        _ => 0.3,
    };

    private static Item T(string code, string name, ItemType type, Rarity r, int price,
        int dmg = 0, int def = 0, int slots = 0, int restore = 0, string restores = "",
        string icon = "", string desc = "", bool lootable = true, bool isGrenade = false, string caliber = "")
    {
        if (icon == MissingIcon && IconOverrides.TryGetValue(code, out var mapped)) icon = mapped;

        int lootTier = LootTierOverrides.TryGetValue(code, out var tier) ? tier : Math.Clamp((int)r + 1, 1, 7);
        double weight = WeightOverrides.TryGetValue(code, out var w) ? w : DefaultWeight(type);

        return new Item
        {
            Code = code, Name = name, Description = desc, Icon = icon, Type = type, Rarity = r, Price = price,
            Damage = dmg, Defense = def, BackpackSlots = slots, Restore = restore, Restores = restores,
            Lootable = lootable, LootTier = lootTier, Weight = weight, IsGrenade = isGrenade, Caliber = caliber,
        };
    }

    public List<Item> All => _templates;
    public Item? Get(string code) => _templates.FirstOrDefault(t => t.Code == code);

    public Item Add(Item item) { _templates.Add(item); return item; }

    /// <summary>Получить или создать боеприпас по калибру. Один шаблон на калибр.</summary>
    public Item CreateAmmo(string caliber, double weight, int price, string desc)
    {
        var existing = _templates.FirstOrDefault(t => t.Type == ItemType.Ammo && t.Caliber == caliber);
        if (existing != null) return existing;

        int damage = AmmoDamageHint(caliber);
        var item = new Item
        {
            Code = "ammo_" + NormalizeCode(caliber),
            Name = $"Патроны {caliber}",
            Description = desc,
            Icon = MissingIcon,
            Type = ItemType.Ammo,
            Rarity = Rarity.Common,
            Price = price,
            Weight = weight,
            Lootable = true,
            Caliber = caliber,
            LootTier = WeaponCatalog.LootTierForDamage(damage),
        };
        _templates.Add(item);
        return item;
    }

    /// <summary>Ориентировочный урон оружия под калибр — чтобы задать тир лута.</summary>
    private static int AmmoDamageHint(string caliber) => caliber switch
    {
        "9x18mm" => 20, "9x19mm" => 24, "7.62x25mm" => 28, "7.65mm" => 18,
        ".45 ACP" => 30, ".50 AE" => 45,
        "5.7x28mm" => 24, "4.6x30mm" => 22,
        "5.45x39mm" => 32, "5.56x45mm NATO" => 33, "7.62x39mm" => 36,
        "9x39mm" => 42, "7.62x51mm NATO" => 38, "7.62x54mmR" => 55,
        "5.8x42mm" => 34, "12.7x55mm" => 55,
        ".338 Lapua Magnum" => 85, ".50 BMG" => 95, "2mm EC" => 110,
        "7.92x57mm Mauser" => 60, "12 Gauge" => 11,
        _ => 25,
    };

    private static string NormalizeCode(string s)
    {
        var chars = new List<char>();
        foreach (var c in s)
            if (char.IsLetterOrDigit(c)) chars.Add(char.ToLowerInvariant(c));
        return new string(chars.ToArray());
    }

    public Item Create(string code)
    {
        var t = Get(code) ?? _templates[0];
        return new Item
        {
            Code = t.Code, Name = t.Name, Description = t.Description, Icon = t.Icon,
            Type = t.Type, Rarity = t.Rarity, Price = t.Price, Damage = t.Damage, Defense = t.Defense,
            BackpackSlots = t.BackpackSlots, Restore = t.Restore, Restores = t.Restores,
            Lootable = t.Lootable, LootTier = t.LootTier, RequiredLoyalty = t.RequiredLoyalty,
            Weight = t.Weight, Count = 1, IsGrenade = t.IsGrenade, Caliber = t.Caliber,
        };
    }
}