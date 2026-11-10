using GameApi.Models;

namespace GameApi.Services;

public class ItemCatalog
{
    public const string MissingIcon = "/error_or_missing.png";

    private readonly List<Item> _templates = new()
    {
        // === Ближний бой (только ручные — из JSON оружие ближнего боя есть, но эти оставляем как «стартовые») ===
        T("knife", "Нож", ItemType.WeaponMelee, Rarity.Common, 444, dmg: 8, icon: "/items/weapon/knife.png",
            desc: "Простой армейский нож. Тихо, дешево и всегда с собой."),
        T("machete", "Мачете", ItemType.WeaponMelee, Rarity.Uncommon, 568, dmg: 14, icon: "/items/weapon/machete.png",
            desc: "Тяжёлый клинок для рубки кустов и не только."),
        T("sledge", "Кувалда", ItemType.WeaponMelee, Rarity.Uncommon, 650, dmg: 18, icon: MissingIcon,
            desc: "Инструмент, который случайно оказался оружием."),

        // === Шлемы ===
        T("cap", "Кепка", ItemType.Helmet, Rarity.Common, 40, def: 1, icon: "/items/clothing/cap.png", desc: "От солнца помогает. От пули — не очень.", slowdown: 0, blastProtection: 2, bulletProtection: 4, physicalProtection: 2),
        T("helmet_ssh", "Каска СШ-68", ItemType.Helmet, Rarity.Uncommon, 250, def: 4, icon: "/items/clothing/helmet_ssh.png", desc: "Старая добрая сталь. Лучше, чем ничего.", slowdown: 0, blastProtection: 8, bulletProtection: 16, physicalProtection: 8),
        T("helmet_tactical", "Тактический шлем", ItemType.Helmet, Rarity.Uncommon, 700, def: 6, icon: MissingIcon, desc: "Лёгкий, с креплениями под ПНВ и гарнитуру.", slowdown: 0, blastProtection: 12, bulletProtection: 24, physicalProtection: 12),
        T("helmet_altyn", "Шлем «Алтын»", ItemType.Helmet, Rarity.Rare, 1200, def: 8, icon: "/items/clothing/helmet_altyn.png", desc: "Тяжёлый штурмовой шлем.", slowdown: 3, blastProtection: 24, bulletProtection: 42, physicalProtection: 16),
        T("helmet_exo", "Шлем экзоскелета", ItemType.Helmet, Rarity.Epic, 2500, def: 12, icon: MissingIcon, desc: "Кто додумался оторвать шлем от экзоскелета?", slowdown: 3, blastProtection: 32, bulletProtection: 58, physicalProtection: 24),

        // === Броня ===
        T("jacket", "Кожаная куртка", ItemType.Armor, Rarity.Common, 80, def: 2, icon: "/items/clothing/jacket.png", desc: "Держит холод и мелкие неприятности.", isSuit: true, slowdown: 0, blastProtection: 4, bulletProtection: 4, physicalProtection: 6),
        T("cloak", "Плащ сталкера", ItemType.Armor, Rarity.Common, 150, def: 3, icon: MissingIcon, desc: "Пропитанный пылью плащ. Лёгкая защита от непогоды и царапин.", isSuit: true, slowdown: 0, blastProtection: 5, bulletProtection: 4, physicalProtection: 10),
        T("vest", "Бронежилет", ItemType.Armor, Rarity.Uncommon, 700, def: 7, icon: "/items/clothing/vest.png", desc: "Базовый бронежилет. Универсальная защита.", slowdown: 1, blastProtection: 14, bulletProtection: 29, physicalProtection: 17),
        T("combat_suit", "Штурмовой комбез", ItemType.Armor, Rarity.Rare, 900, def: 9, icon: MissingIcon, desc: "Комбинезон с бронепластинами.", isSuit: true, slowdown: 1, blastProtection: 18, bulletProtection: 18, physicalProtection: 27),
        T("vest_heavy", "Тяжёлый бронежилет", ItemType.Armor, Rarity.Epic, 3200, def: 14, icon: "/items/clothing/vest_heavy.png", desc: "Много керамики и ещё больше веса.", slowdown: 3, blastProtection: 36, bulletProtection: 60, physicalProtection: 31),
        T("sphere", "Броня «Сфера»", ItemType.Armor, Rarity.Epic, 2600, def: 16, icon: MissingIcon, desc: "Экспериментальная разработка.", isSuit: true, slowdown: 4, blastProtection: 40, bulletProtection: 66, physicalProtection: 35),
        T("exo", "Экзоскелет", ItemType.Armor, Rarity.Legendary, 6000, def: 24, icon: MissingIcon, desc: "Рама, сервоприводы и броня.", slowdown: 4, blastProtection: 56, bulletProtection: 90, physicalProtection: 51),

        // === Рюкзаки ===
        T("pouch", "Наплечная сумка", ItemType.Backpack, Rarity.Common, 100, slots: 9, icon: "/items/clothing/pouch.png", desc: "Небольшая сумка."),
        T("bag_small", "Рюкзак «Турист»", ItemType.Backpack, Rarity.Common, 150, slots: 25, icon: MissingIcon, desc: "Простой рюкзак."),
        T("backpack", "Рюкзак", ItemType.Backpack, Rarity.Uncommon, 420, slots: 30, icon: "/items/clothing/backpack.png", desc: "Проверенный временем рюкзак. Удобен для вылазок."),
        T("raidpack", "Штурмовой рюкзак", ItemType.Backpack, Rarity.Rare, 1100, slots: 35, icon: "/items/clothing/raidpack.png", desc: "Усиленный каркас."),
        T("bag_large", "Большой рюкзак", ItemType.Backpack, Rarity.Rare, 900, slots: 40, icon: MissingIcon, desc: "Можно унести половину тайника."),
        T("bag_expedition", "Экспедиционный рюкзак", ItemType.Backpack, Rarity.Epic, 2000, slots: 50, icon: MissingIcon, desc: "Для долгих выходов."),

        // === Новая одежда, броня и рюкзаки (Starovshchik) ===

        // --- Шлемы и головные уборы ---
        T("helmet_beanie", "Шапка с помпоном", ItemType.Helmet, Rarity.Common, 30, def: 1, icon: "/items/clothing/shapka.png", desc: "Тёплая шапка с помпоном.", slowdown: 0, blastProtection: 2, bulletProtection: 4, physicalProtection: 2),
        T("helmet_balaclava", "Балаклава", ItemType.Helmet, Rarity.Uncommon, 120, def: 2, icon: "/items/clothing/balaklava.png", desc: "Маска на всё лицо, закрывает голову и шею.", slowdown: 0, blastProtection: 4, bulletProtection: 8, physicalProtection: 4),
        T("helmet_bandana", "Бандана", ItemType.Helmet, Rarity.Common, 40, def: 1, icon: "/items/clothing/bandana.png", desc: "Простая бандана на голову.", slowdown: 0, blastProtection: 2, bulletProtection: 4, physicalProtection: 2),
        T("helmet_cap_mil", "Военная кепка", ItemType.Helmet, Rarity.Uncommon, 90, def: 1, icon: "/items/clothing/cap_mil.png", desc: "Кепка с эмблемой части.", slowdown: 0, blastProtection: 2, bulletProtection: 4, physicalProtection: 2),
        T("helmet_ushanka", "Ушанка", ItemType.Helmet, Rarity.Rare, 300, def: 3, icon: "/items/clothing/ushanka.png", desc: "Тёплая ушанка с ушками и завязками.", slowdown: 0, blastProtection: 6, bulletProtection: 12, physicalProtection: 6),
        T("helmet_stalker", "Пуленепробиваемый шлем", ItemType.Helmet, Rarity.Rare, 800, def: 7, icon: "/items/clothing/stalker_helm.png", desc: "Тяжёлый пуленепробиваемый шлем с защитой лица.", slowdown: 0, blastProtection: 14, bulletProtection: 28, physicalProtection: 14),
        T("helmet_bulletproof", "Бронешлем", ItemType.Helmet, Rarity.Epic, 1500, def: 10, icon: "/items/clothing/ulach_Brone_shlem.png", desc: "Улучшенный баллистический шлем.", slowdown: 1, blastProtection: 20, bulletProtection: 40, physicalProtection: 20),
        T("helmet_gasmask", "Противогаз", ItemType.Helmet, Rarity.Rare, 520, def: 3, icon: "/items/clothing/gasmask.png", desc: "Гражданский противогаз с фильтром. Слабая баллистика, но защищает лицо.", slowdown: 1, blastProtection: 10, bulletProtection: 8, physicalProtection: 12),
        T("helmet_hardhat", "Строительная каска", ItemType.Helmet, Rarity.Common, 60, def: 3, icon: "/items/clothing/hardhat.png", desc: "Простая пластиковая каска.", slowdown: 0, blastProtection: 6, bulletProtection: 12, physicalProtection: 6),
        T("helmet_riot", "Шлем ОМОН", ItemType.Helmet, Rarity.Epic, 2000, def: 12, icon: "/items/clothing/riot-helmet.png", desc: "Шлем ОМОН с застёжкой и визором.", slowdown: 3, blastProtection: 32, bulletProtection: 58, physicalProtection: 24),

        // --- Костюмы и одежда (занимают слот костюма, под них можно надеть бронежилет) ---
        T("suit_leather_coat", "Комбинезон \"Монолит\"", ItemType.Armor, Rarity.Rare, 900, def: 8, icon: MissingIcon, desc: "Тяжёлый комбинезон группировки. Хорошая защита и узнаваемый силуэт.", isSuit: true, slowdown: 1, blastProtection: 16, bulletProtection: 16, physicalProtection: 22),
        T("suit_sweater", "ССП-99 «Эколог»", ItemType.Armor, Rarity.Epic, 2400, def: 11, icon: MissingIcon, desc: "Научный комбинезон экологов. Отличная защита и мобильность.", isSuit: true, slowdown: 1, blastProtection: 24, bulletProtection: 22, physicalProtection: 30),
        T("suit_shirt", "ВКПО НАТО", ItemType.Armor, Rarity.Uncommon, 220, def: 3, icon: MissingIcon, desc: "Полевой комплект в расцветке НАТО.", isSuit: true, slowdown: 0, blastProtection: 5, bulletProtection: 4, physicalProtection: 8),
        T("suit_denim", "Куртка Новичка", ItemType.Armor, Rarity.Uncommon, 170, def: 2, icon: MissingIcon, desc: "Простая куртка сталкера-новичка. Немного защищает от царапин.", isSuit: true, slowdown: 0, blastProtection: 4, bulletProtection: 3, physicalProtection: 8),
        T("suit_puffer", "Ватник", ItemType.Armor, Rarity.Rare, 400, def: 5, icon: "/items/clothing/ватник.png", desc: "Тёплый ватник.", isSuit: true, slowdown: 1, blastProtection: 10, bulletProtection: 10, physicalProtection: 15),
        T("suit_camo", "ВКПО ВС РФ", ItemType.Armor, Rarity.Rare, 600, def: 6, icon: MissingIcon, desc: "Всесезонный комплект полевого обмундирования ВС РФ.", isSuit: true, slowdown: 1, blastProtection: 12, bulletProtection: 12, physicalProtection: 18),
        T("suit_gilly", "Маскхалат", ItemType.Armor, Rarity.Epic, 1800, def: 8, icon: MissingIcon, desc: "Маскировочный халат для скрытного передвижения.", isSuit: true, slowdown: 2, blastProtection: 16, bulletProtection: 16, physicalProtection: 24),
        T("suit_seva", "Комбинезон СЕВА", ItemType.Armor, Rarity.Legendary, 5000, def: 15, icon: MissingIcon, desc: "Спецкомбинезон СЕВА с усиленной защитой.", isSuit: true, slowdown: 2, blastProtection: 30, bulletProtection: 30, physicalProtection: 45),
        T("suit_tactical", "Тактический комбинезон", ItemType.Armor, Rarity.Epic, 2500, def: 11, icon: MissingIcon, desc: "Тактический комбинезон с карманами.", isSuit: true, slowdown: 1, blastProtection: 22, bulletProtection: 22, physicalProtection: 33),
        T("suit_winter", "Зимний комбинезон", ItemType.Armor, Rarity.Rare, 700, def: 7, icon: MissingIcon, desc: "Утеплённый зимний комбинезон.", isSuit: true, slowdown: 1, blastProtection: 14, bulletProtection: 14, physicalProtection: 21),
        T("cloak_long", "Длинный плащ", ItemType.Armor, Rarity.Uncommon, 220, def: 4, icon: MissingIcon, desc: "Длинный плащ из плотной ткани. Лучше прикрывает тело, но чуть мешает.", isSuit: true, slowdown: 1, blastProtection: 7, bulletProtection: 5, physicalProtection: 12),

        T("suit_hoodie", "Зелёный свитер", ItemType.Armor, Rarity.Common, 80, def: 1, icon: MissingIcon, desc: "Простой зелёный свитер. Стартовая одежда выжившего.", isSuit: true, slowdown: 0, blastProtection: 2, bulletProtection: 2, physicalProtection: 3),
        T("suit_flannel", "Комбинезон наёмника", ItemType.Armor, Rarity.Rare, 850, def: 7, icon: MissingIcon, desc: "Практичный комбинезон наёмника с усиленными вставками.", isSuit: true, slowdown: 1, blastProtection: 14, bulletProtection: 14, physicalProtection: 20),
        T("suit_raincoat", "Дождевик", ItemType.Armor, Rarity.Uncommon, 150, def: 2, icon: MissingIcon, desc: "Водонепроницаемый дождевик.", isSuit: true, slowdown: 0, blastProtection: 4, bulletProtection: 4, physicalProtection: 6),
        T("suit_vest", "Армейский бронекостюм", ItemType.Armor, Rarity.Rare, 1100, def: 8, icon: MissingIcon, desc: "Армейский костюм с бронеэлементами. Надёжная базовая защита.", isSuit: true, slowdown: 1, blastProtection: 16, bulletProtection: 16, physicalProtection: 22),
        T("suit_armor", "Комбинезон Заря", ItemType.Armor, Rarity.Epic, 2000, def: 10, icon: MissingIcon, desc: "Штурмовой комбинезон «Заря» с композитными вставками.", isSuit: true, slowdown: 2, blastProtection: 20, bulletProtection: 20, physicalProtection: 30),

        // --- Бронежилеты/разгрузки (занимают слот брони, несовместимы со сферой/экзо в шлеме) ---
        T("vest_kevlar", "Кевларовый бронежилет", ItemType.Armor, Rarity.Rare, 1200, def: 8, icon: MissingIcon, desc: "Бронежилет из кевлара.", slowdown: 1, blastProtection: 16, bulletProtection: 32, physicalProtection: 19),
        T("vest_ceramic", "Лёгкий бронежилет", ItemType.Armor, Rarity.Epic, 2200, def: 12, icon: MissingIcon, desc: "Лёгкий бронежилет с защитными пластинами.", slowdown: 2, blastProtection: 24, bulletProtection: 44, physicalProtection: 27),
        T("vest_tactical", "Бронежилет прессы", ItemType.Armor, Rarity.Rare, 1500, def: 9, icon: MissingIcon, desc: "Бронежилет с маркировкой «PRESS».", slowdown: 1, blastProtection: 18, bulletProtection: 35, physicalProtection: 21),

        T("vest_light", "Разгрузочный жилет \"Wassermann\"", ItemType.Armor, Rarity.Uncommon, 650, def: 2, slots: 10, icon: MissingIcon, desc: "Разгрузочный жилет. Добавляет +10 слотов инвентаря.", slowdown: 0, blastProtection: 4, bulletProtection: 8, physicalProtection: 6),
        T("vest_military", "6Б43", ItemType.Armor, Rarity.Epic, 3000, def: 13, icon: "/items/clothing/bronezhilet-6b43-1.png", desc: "Армейский бронежилет 6Б43 усиленной защиты.", slowdown: 2, blastProtection: 26, bulletProtection: 47, physicalProtection: 29),
        T("vest_stalker", "Охотничий жилет", ItemType.Armor, Rarity.Rare, 720, def: 3, slots: 5, icon: MissingIcon, desc: "Охотничий жилет с карманами. Добавляет +5 слотов инвентаря.", slowdown: 0, blastProtection: 6, bulletProtection: 10, physicalProtection: 12),
        T("vest_police", "Полицейский бронежилет", ItemType.Armor, Rarity.Rare, 1050, def: 6, icon: MissingIcon, desc: "Полицейский бронежилет класса 2. Легче армейского, слабее от осколков.", slowdown: 1, blastProtection: 10, bulletProtection: 31, physicalProtection: 14),
        T("vest_bulletproof", "Бронежилет скрытого ношения", ItemType.Armor, Rarity.Legendary, 4500, def: 16, icon: MissingIcon, desc: "Скрытый бронежилет с высокой защитой.", slowdown: 1, blastProtection: 24, bulletProtection: 52, physicalProtection: 28),
        T("vest_steel", "Бронежилет 6Б2", ItemType.Armor, Rarity.Epic, 2600, def: 15, icon: MissingIcon, desc: "Тяжёлый армейский бронежилет 6Б2.", slowdown: 3, blastProtection: 38, bulletProtection: 63, physicalProtection: 33),
        T("vest_6b13", "Бронежилет штурмовой 6Б13", ItemType.Armor, Rarity.Epic, 3400, def: 14, icon: MissingIcon, desc: "Штурмовой бронежилет 6Б13 с усиленной защитой.", slowdown: 2, blastProtection: 28, bulletProtection: 50, physicalProtection: 30),
        T("vest_korund", "Бронежилет НПП КлАСС \"Корунд-ВМ\"", ItemType.Armor, Rarity.Legendary, 5200, def: 17, icon: MissingIcon, desc: "Тяжёлый штурмовой бронежилет «Корунд-ВМ».", slowdown: 3, blastProtection: 34, bulletProtection: 60, physicalProtection: 36),
        T("rig_commando", "Разгрузочная система Commando", ItemType.Armor, Rarity.Uncommon, 480, def: 1, slots: 8, icon: MissingIcon, desc: "Разгрузочная система. Добавляет +8 слотов инвентаря.", slowdown: 0, blastProtection: 2, bulletProtection: 4, physicalProtection: 4),

        // --- Рюкзаки ---
        T("bag_briefcase", "Школьный рюкзак", ItemType.Backpack, Rarity.Common, 200, slots: 15, icon: MissingIcon, desc: "Простой школьный рюкзак."),
        T("bag_sack", "Армейский вещмешок ВКБО", ItemType.Backpack, Rarity.Common, 120, slots: 20, icon: MissingIcon, desc: "Армейский вещмешок. Дёшево и сердито."),
        T("bag_daypack", "Городской рюкзак", ItemType.Backpack, Rarity.Uncommon, 320, slots: 28, icon: MissingIcon, desc: "Удобный городской рюкзак на каждый день."),
        T("bag_tactpack", "Тактический рюкзак", ItemType.Backpack, Rarity.Rare, 1400, slots: 38, icon: MissingIcon, desc: "Тактический рюкзак с МОЛЛЕ."),
        T("bag_milpack", "Армейский рюкзак", ItemType.Backpack, Rarity.Rare, 1600, slots: 42, icon: MissingIcon, desc: "Армейский рюкзак большой вместимости."),
        T("bag_huge", "Рюкзак штурмовой Mystery Ranch \"SATL Bridger Assault Pack\"", ItemType.Backpack, Rarity.Epic, 3200, slots: 55, icon: MissingIcon, desc: "Большой штурмовой рюкзак для длительных рейдов."),
        T("bag_duffel", "Спортивная сумка", ItemType.Backpack, Rarity.Uncommon, 360, slots: 32, icon: MissingIcon, desc: "Спортивная сумка. Вместительная, но менее удобная на спине."),
        T("bag_field", "Походный рюкзак", ItemType.Backpack, Rarity.Rare, 1800, slots: 45, icon: MissingIcon, desc: "Большой походный рюкзак для долгих вылазок."),
        T("bag_med", "Медицинская сумка", ItemType.Backpack, Rarity.Uncommon, 380, slots: 26, icon: MissingIcon, desc: "Компактная сумка для медикаментов и инструментов. Меньше мест, но легче."),
        T("bag_assault", "Штурмовой рюкзак", ItemType.Backpack, Rarity.Rare, 1300, slots: 36, icon: MissingIcon, desc: "Штурмовой рюкзак для вылазок."),
        T("bag_cargo", "Рюкзак рейдовый 6Ш118", ItemType.Backpack, Rarity.Epic, 3000, slots: 55, icon: MissingIcon, desc: "Армейский рейдовый рюкзак 6Ш118. Огромная вместимость."),

        // === Еда ===
        T("bread", "Хлеб", ItemType.Food, Rarity.Common, 25, restore: 20, restores: "hunger", icon: "/items/food/bread.png", desc: "Чёрствый, но сытный."),
        T("canned", "Тушёнка", ItemType.Food, Rarity.Uncommon, 60, restore: 40, restores: "hunger", icon: "/items/food/canned.png", desc: "Праздник в консервной банке."),
        T("stew", "Рагу", ItemType.Food, Rarity.Uncommon, 90, restore: 55, restores: "hunger", icon: "/items/food/stew.png", desc: "Домашнее рагу в вакууме."),
        T("sausage", "Колбаса", ItemType.Food, Rarity.Common, 45, restore: 30, restores: "hunger", icon: "/items/food/kolbasa.png", desc: "Ореховый сервелат, вкусно и сытно."),
        T("cheese", "Сыр", ItemType.Food, Rarity.Common, 40, restore: 25, restores: "hunger", icon: "/items/food/chesee.png", desc: "Сыр янтарный, производился где-то в городе."),
        T("crackers", "Галеты", ItemType.Food, Rarity.Common, 20, restore: 15, restores: "hunger", icon: "/items/food/crackers.png", desc: "Сухие, долго не портятся."),
        T("noodles", "Лапша быстрого приготовления", ItemType.Food, Rarity.Common, 30, restore: 25, restores: "hunger", icon: "/items/food/doshirak.png", desc: "Залить водой, 5 минут, и готово."),
        T("canned_fish", "Консервированная рыба", ItemType.Food, Rarity.Uncommon, 55, restore: 35, restores: "hunger", icon: "/items/food/fish_konserva.png", desc: "Сытно и дёшево."),
        T("honey", "Мёд", ItemType.Food, Rarity.Uncommon, 70, restore: 45, restores: "hunger", icon: "/items/food/honey.png", desc: "Не засахарился, удивительно. После употребления даёт +5 ХП.", restoreHpBonus: 5),
        T("mre", "Сухпаёк", ItemType.Food, Rarity.Rare, 150, restore: 70, restores: "hunger", icon: "/items/food/MRE.png", desc: "Годен до 2033, есть можно."),
        T("sandwich", "Бутерброд", ItemType.Food, Rarity.Common, 35, restore: 25, restores: "hunger", icon: "/items/food/buterbrod.png", desc: "Вкусно, с чаем хорошо пойдёт."),
        T("mint_candy", "Мятные конфеты", ItemType.Food, Rarity.Common, 20, restore: 5, restores: "hunger", icon: "/items/food/konfeti.png", desc: "Те самые конфеты с плюсиком. После употребления дают +1 ХП.", restoreHpBonus: 1),

        // === Вода / напитки ===
        T("water", "Вода", ItemType.Drink, Rarity.Common, 25, restore: 20, restores: "thirst", icon: "/items/food/water.png", desc: "Питьевая вода."),
        T("mineral_water", "Минеральная вода", ItemType.Drink, Rarity.Common, 35, restore: 30, restores: "thirst", icon: "/items/food/Mineral_Water.png", desc: "Минеральная вода в бутылке."),
        T("cola", "Газировка", ItemType.Drink, Rarity.Uncommon, 55, restore: 40, restores: "thirst", icon: "/items/food/cola.png", desc: "Тёплая, но всё ещё вкусная."),
        T("energy", "Энергетик", ItemType.Drink, Rarity.Uncommon, 80, restore: 35, restores: "thirst", icon: "/items/food/energy.png", desc: "Химия и кофеин."),
        T("juice", "Сок", ItemType.Drink, Rarity.Common, 40, restore: 30, restores: "thirst", icon: "/items/food/juce.png", desc: "Вроде виноградный, а на упаковке яблоко."),
        T("vodka", "Водка", ItemType.Drink, Rarity.Uncommon, 90, restore: 30, restores: "thirst", icon: "/items/food/vodka.png", desc: "Лучше водки — хуже нет."),
        T("tea_termos", "Чай", ItemType.Drink, Rarity.Uncommon, 65, restore: 30, restores: "thirst", icon: "/items/food/Tea.png", desc: "Простой чай. Производит и продаёт Бармен."),

        // === Медицина ===
        T("bandage", "Бинт", ItemType.Medicine, Rarity.Common, 40, restore: 25, restores: "hp", icon: "/items/medicine/bandage.png", desc: "Останавливает кровь."),
        T("bandage_sterile", "Стерильный бинт", ItemType.Medicine, Rarity.Uncommon, 70, restore: 35, restores: "hp", icon: "/items/medicine/aid_bandage.png", desc: "Чистый бинт. Меньше шансов на заражение."),
        T("painkiller", "Обезболивающее", ItemType.Medicine, Rarity.Uncommon, 100, restore: 20, restores: "hp", icon: "/items/medicine/painkiller.png", desc: "Заглушает боль."),
        T("painkiller_strong", "Бальзам «Золотая звезда»", ItemType.Medicine, Rarity.Rare, 180, restore: 35, restores: "hp", icon: "/items/medicine/balsam_GoldenStar.png", desc: "Жгучий бальзам. Снимает боль и разгоняет кровь."),
        T("medkit", "Аптечка", ItemType.Medicine, Rarity.Rare, 220, restore: 60, restores: "hp", icon: "/items/medicine/medkit.png", desc: "Полный набор."),
        T("surgical_kit", "Хирургический набор", ItemType.Medicine, Rarity.Epic, 500, restore: 90, restores: "hp", icon: "/items/medicine/Surgical_kit.png", desc: "Почти операционная на дому."),
        T("antirad", "Антирад", ItemType.Medicine, Rarity.Rare, 150, restore: 30, restores: "hp", icon: "/items/medicine/antirad.png", desc: "Горькая таблетка."),
        T("iodine", "Йод", ItemType.Medicine, Rarity.Common, 35, restore: 15, restores: "hp", icon: "/items/medicine/yod.png", desc: "Жжёт, но дезинфицирует."),
        T("tourniquet", "Жгут", ItemType.Medicine, Rarity.Uncommon, 80, restore: 25, restores: "hp", icon: "/items/medicine/esmarch.png", desc: "Для тяжёлых кровотечений."),
        T("splint", "Шина", ItemType.Medicine, Rarity.Uncommon, 90, restore: 20, restores: "hp", icon: "/items/medicine/shina.png", desc: "Фиксирует переломы."),
        T("stimulant", "Стимулятор", ItemType.Medicine, Rarity.Rare, 200, restore: 40, restores: "hp", icon: "/items/medicine/stimulator.png", desc: "Бодрит и лечит. Потом будет хуже."),
        T("antidote", "Антидот", ItemType.Medicine, Rarity.Rare, 220, restore: 25, restores: "hp", icon: "/items/medicine/antidote.png", desc: "Против ядов и токсинов."),
        T("blood_bag", "Пакет крови", ItemType.Medicine, Rarity.Epic, 450, restore: 80, restores: "hp", icon: "/items/medicine/bag_of_blood.png", desc: "Восстанавливает много крови быстро."),
        T("medkit_army", "Армейская аптечка", ItemType.Medicine, Rarity.Rare, 320, restore: 70, restores: "hp", icon: "/items/medicine/army_medkit.png", desc: "Усиленный армейский набор для полевых ран."),
        T("medkit_car", "Автомобильная аптечка", ItemType.Medicine, Rarity.Uncommon, 140, restore: 45, restores: "hp", icon: "/items/medicine/automedkit.png", desc: "Базовый набор из автоаптечки."),
        T("morphine", "Морфин", ItemType.Medicine, Rarity.Rare, 260, restore: 45, restores: "hp", icon: "/items/medicine/morphine.png", desc: "Сильное обезболивающее. Использовать осторожно."),
        T("hemostatic", "Гемостатик", ItemType.Medicine, Rarity.Uncommon, 110, restore: 30, restores: "hp", icon: "/items/medicine/gemostatic.png", desc: "Порошок для быстрой остановки кровотечения."),
        T("vaseline", "Вазелин", ItemType.Medicine, Rarity.Common, 30, restore: 10, restores: "hp", icon: "/items/medicine/vaseline.png", desc: "Защищает кожу и мелкие ссадины."),
        T("vitamins", "Витамины", ItemType.Medicine, Rarity.Common, 55, restore: 15, restores: "hp", icon: "/items/medicine/vitamins.png", desc: "Поддерживают тонус. Не заменяют лечение."),
        T("saline", "Физраствор", ItemType.Medicine, Rarity.Uncommon, 95, restore: 25, restores: "hp", icon: "/items/medicine/fizrastvor.png", desc: "Промывание и восполнение жидкости."),
        T("bandage_pack", "Набор бинтов", ItemType.Medicine, Rarity.Uncommon, 120, restore: 50, restores: "hp", icon: "/items/medicine/pack_of_bandage.png", desc: "Несколько бинтов в одной упаковке."),
        T("hydrogen_peroxide", "Перекись водорода", ItemType.Medicine, Rarity.Common, 45, restore: 20, restores: "hp", icon: MissingIcon, desc: "Антисептик. Обрабатывает раны и помогает остановить заражение."),
        T("syringe_sealed", "Шприц (новый, запечатанный)", ItemType.Medicine, Rarity.Uncommon, 60, restore: 0, restores: "", icon: MissingIcon, desc: "Новый запечатанный шприц. Сам по себе не лечит."),
        T("meds_kit_pack", "Набор медикаментов", ItemType.Misc, Rarity.Uncommon, 110, icon: MissingIcon, desc: "Набор расходных медикаментов. Использовать напрямую нельзя."),

        // === Материалы ===
        T("scrap", "Металлолом", ItemType.Material, Rarity.Common, 30, icon: "/items/resources/scrap.png", desc: "Ржавые куски металла."),
        T("cloth", "Ткань", ItemType.Material, Rarity.Common, 20, icon: "/items/resources/cloth.png", desc: "Отрез ткани."),
        T("glass", "Стекло", ItemType.Material, Rarity.Common, 25, icon: MissingIcon, desc: "Осколки и бутылки."),
        T("cement", "Цемент", ItemType.Material, Rarity.Common, 35, icon: MissingIcon, desc: "Мешок цемента."),
        T("wire", "Провода", ItemType.Material, Rarity.Uncommon, 45, icon: "/items/resources/wire.png", desc: "Моток медных проводов."),
        T("rubber", "Резина", ItemType.Material, Rarity.Uncommon, 40, icon: MissingIcon, desc: "Кусок резины."),
        T("battery", "Автомобильный аккумулятор", ItemType.Material, Rarity.Rare, 150, icon: "/items/resources/battery.png", desc: "Тяжёлый автомобильный аккумулятор. Ещё держит заряд."),
        T("battery_aa", "Батарейка", ItemType.Material, Rarity.Common, 20, icon: MissingIcon, desc: "Обычная пальчиковая батарейка."),
        T("fuel", "Топливо", ItemType.Material, Rarity.Rare, 90, icon: MissingIcon, desc: "Канистра с горючим."),
        T("fuel_can_5", "Канистра с бензином (5 л)", ItemType.Material, Rarity.Rare, 140, icon: MissingIcon, desc: "Канистра бензина на 5 литров."),
        T("fuel_can_10", "Канистра с бензином (10 л)", ItemType.Material, Rarity.Rare, 260, icon: MissingIcon, desc: "Канистра бензина на 10 литров."),
        T("electronics", "Микросхемы", ItemType.Material, Rarity.Rare, 120, icon: MissingIcon, desc: "Платы из старой техники."),
        T("toolkit", "Набор инструментов", ItemType.Material, Rarity.Rare, 300, icon: MissingIcon, desc: "Для сложных работ в убежище."),
        T("screwdriver_set", "Набор отвёрток", ItemType.Material, Rarity.Uncommon, 80, icon: MissingIcon, desc: "Набор отвёрток разных размеров."),
        T("awl", "Шило", ItemType.Material, Rarity.Common, 25, icon: MissingIcon, desc: "Острое шило. Пригодится в хозяйстве."),
        T("nails", "Гвозди", ItemType.Material, Rarity.Common, 15, icon: MissingIcon, desc: "Горсть ржавых гвоздей."),
        T("bolts", "Болты", ItemType.Material, Rarity.Common, 18, icon: MissingIcon, desc: "Горсть болтов с резьбой."),
        T("nuts", "Гайки", ItemType.Material, Rarity.Common, 15, icon: MissingIcon, desc: "Металлические гайки."),
        T("duct_tape", "Скотч", ItemType.Material, Rarity.Common, 25, icon: MissingIcon, desc: "Серый скотч. Универсально."),
        T("zipties", "Стяжки", ItemType.Material, Rarity.Common, 20, icon: MissingIcon, desc: "Пластиковые стяжки."),
        T("paracord", "Паракорд", ItemType.Material, Rarity.Uncommon, 45, icon: MissingIcon, desc: "Прочный паракорд. Много способов применения."),
        T("kerosene", "Керосин", ItemType.Material, Rarity.Uncommon, 60, icon: MissingIcon, desc: "Канистра керосина."),
        T("propane", "Пропан", ItemType.Material, Rarity.Uncommon, 70, icon: MissingIcon, desc: "Баллон пропана."),
        T("sparkplug", "Свечи зажигания", ItemType.Material, Rarity.Uncommon, 50, icon: MissingIcon, desc: "Комплект свечей."),
        T("salt", "Соль", ItemType.Material, Rarity.Common, 12, icon: MissingIcon, desc: "Пачка соли."),
        T("soda_pack", "Пачка соды", ItemType.Material, Rarity.Common, 15, icon: MissingIcon, desc: "Пищевая сода. Пригодится и в быту, и в химии."),
        T("gunpowder", "Порох", ItemType.Material, Rarity.Rare, 160, icon: MissingIcon, desc: "Небольшая ёмкость с порохом. Опасно."),
        T("matches", "Спички", ItemType.Material, Rarity.Common, 10, icon: "/items/resources/спички.png", desc: "Я 11 лет считаю спички у вас в коробках - их то 59, то 60, а иногда и 58. Вы там сумасшедшие что ли все?"),
        T("lighter", "Зажигалка", ItemType.Material, Rarity.Common, 25, icon: MissingIcon, desc: "Пластиковая зажигалка."),
        T("soap", "Кусок мыла", ItemType.Material, Rarity.Common, 12, icon: MissingIcon, desc: "Обычное хозяйственное мыло."),
        T("aramid_fabric", "Арамидная ткань", ItemType.Material, Rarity.Rare, 220, icon: MissingIcon, desc: "Прочная арамидная ткань для бронезащиты."),
        T("ripstop_fabric", "Ткань рипстоп", ItemType.Material, Rarity.Uncommon, 70, icon: MissingIcon, desc: "Износостойкая ткань рипстоп."),
        T("fleece_fabric", "Флисовая ткань", ItemType.Material, Rarity.Uncommon, 55, icon: MissingIcon, desc: "Мягкая тёплая флисовая ткань."),
        T("cordura_fabric", "Кордура ткань", ItemType.Material, Rarity.Rare, 180, icon: MissingIcon, desc: "Плотная ткань кордура."),
        T("relay", "Реле", ItemType.Material, Rarity.Uncommon, 65, icon: MissingIcon, desc: "Электромеханическое реле."),
        T("ram", "Оперативная память", ItemType.Material, Rarity.Rare, 140, icon: MissingIcon, desc: "Планка оперативной памяти."),
        T("psu", "Блок питания", ItemType.Material, Rarity.Rare, 170, icon: MissingIcon, desc: "Компьютерный блок питания."),
        T("pliers", "Плоскогубцы", ItemType.Material, Rarity.Uncommon, 55, icon: MissingIcon, desc: "Плоскогубцы для работы с металлом и проводами."),
        T("round_pliers", "Круглогубцы", ItemType.Material, Rarity.Uncommon, 55, icon: MissingIcon, desc: "Круглогубцы для точных работ."),
        T("drill", "Дрель", ItemType.Material, Rarity.Rare, 240, icon: MissingIcon, desc: "Электрическая дрель."),
        T("hand_drill", "Ручная дрель", ItemType.Material, Rarity.Uncommon, 90, icon: MissingIcon, desc: "Ручная механическая дрель."),
        T("multitool", "Мультитул", ItemType.Material, Rarity.Uncommon, 120, icon: MissingIcon, desc: "Складной мультитул с набором инструментов."),
        T("rosin", "Канифоль", ItemType.Material, Rarity.Uncommon, 35, icon: "/items/resources/Kanifol.png", desc: "Канифоль для пайки."),
        T("capacitors", "Конденсаторы", ItemType.Material, Rarity.Uncommon, 60, icon: "/items/resources/Конденсаторы.png", desc: "Набор электронных конденсаторов."),
        T("resistors", "Резисторы", ItemType.Material, Rarity.Uncommon, 45, icon: "/items/resources/Резисторы.png", desc: "Набор резисторов."),
        T("textolite", "Текстолит", ItemType.Material, Rarity.Rare, 110, icon: "/items/resources/Текстолит.png", desc: "Лист текстолита для плат."),

        // === Ценности / бартер ===
        T("watch", "Настольные часы", ItemType.Misc, Rarity.Rare, 300, icon: "/items/other/watch.png", desc: "часы в форме двуглавого орла"),
        T("amber", "Янтарь", ItemType.Misc, Rarity.Rare, 500, icon: MissingIcon, desc: "Кусок янтаря."),
        T("goldchain", "Золотая цепь", ItemType.Misc, Rarity.Epic, 900, icon: "/items/other/goldchain.png", desc: "Тяжёлая цепь."),
        T("icon", "Икона", ItemType.Misc, Rarity.Epic, 700, icon: MissingIcon, desc: "Старая икона в окладе."),
        T("artifact", "Артефакт «Капля»", ItemType.Misc, Rarity.Legendary, 5000, icon: "/items/Artifacts/Капля.png", desc: "Светится в темноте."),
        T("documents", "Документы", ItemType.Misc, Rarity.Rare, 1200, icon: MissingIcon, desc: "Папка с бумагами."),
        T("old_documents", "Старые документы", ItemType.Misc, Rarity.Uncommon, 180, icon: MissingIcon, desc: "Пожелтевшие документы. Кое-что ещё можно разобрать."),
        T("intel_folder", "Папка с разведданными", ItemType.Misc, Rarity.Epic, 1600, icon: MissingIcon, desc: "Папка с пометкой о разведданных."),
        T("tech_docs", "Техническая документация", ItemType.Misc, Rarity.Rare, 450, icon: MissingIcon, desc: "Схемы и техническая документация."),
        T("paper_pack", "Пачка бумаги", ItemType.Misc, Rarity.Common, 25, icon: MissingIcon, desc: "Пачка офисной бумаги."),
        T("old_money", "Старые деньги", ItemType.Misc, Rarity.Uncommon, 90, icon: MissingIcon, desc: "Стопка старых купюр."),
        T("coffee", "Кофе", ItemType.Misc, Rarity.Uncommon, 120, icon: MissingIcon, desc: "Банка растворимого кофе."),
        T("tea_box", "Коробка с чаем", ItemType.Misc, Rarity.Common, 45, icon: MissingIcon, desc: "Коробка чайных пакетиков."),
        T("cigarettes", "Сигареты", ItemType.Misc, Rarity.Common, 80, icon: MissingIcon, desc: "Пачка сигарет. Валюта не хуже денег."),
        T("chocolate", "Шоколад", ItemType.Misc, Rarity.Uncommon, 100, icon: MissingIcon, desc: "Плитка шоколада."),
        T("dogtags", "Жетоны", ItemType.Misc, Rarity.Rare, 250, icon: MissingIcon, desc: "Армейские жетоны. Часто встречаются у военных противников."),
        T("medal", "Медаль", ItemType.Misc, Rarity.Epic, 600, icon: MissingIcon, desc: "Боевая награда."),
        T("cards", "Игральные карты", ItemType.Misc, Rarity.Common, 60, icon: MissingIcon, desc: "Потрёпанная колода."),
        T("harmonica", "Губная гармошка", ItemType.Misc, Rarity.Uncommon, 150, icon: MissingIcon, desc: "Играет, если дунуть."),
        T("guitar", "Гитара", ItemType.Misc, Rarity.Uncommon, 350, icon: MissingIcon, desc: "Старая акустическая гитара."),
        T("compass", "Компас", ItemType.Misc, Rarity.Rare, 350, icon: MissingIcon, desc: "Работает, хоть и врёт."),
        T("binoculars", "Бинокль", ItemType.Misc, Rarity.Rare, 400, icon: MissingIcon, desc: "Видно далеко."),
        T("camera", "Фотоаппарат", ItemType.Misc, Rarity.Uncommon, 200, icon: MissingIcon, desc: "Плёнка ещё жива."),
        T("radio_parts", "Радиодетали", ItemType.Misc, Rarity.Rare, 300, icon: MissingIcon, desc: "Лампы, конденсаторы, платы."),
        T("geiger_counter", "Счётчик Гейгера", ItemType.Misc, Rarity.Epic, 800, icon: MissingIcon, desc: "Трещит там, где не надо."),
        T("keys", "Ключи", ItemType.Misc, Rarity.Common, 40, icon: MissingIcon, desc: "Связка старых ключей."),
        T("car_key", "Автомобильный ключ", ItemType.Misc, Rarity.Uncommon, 120, icon: MissingIcon, desc: "Ключ от автомобиля."),
        T("earrings", "Серьги", ItemType.Misc, Rarity.Uncommon, 220, icon: MissingIcon, desc: "Пара серёг."),
        T("ring", "Кольцо", ItemType.Misc, Rarity.Uncommon, 180, icon: MissingIcon, desc: "Простое кольцо."),
        T("necklace", "Ожерелье", ItemType.Misc, Rarity.Rare, 420, icon: MissingIcon, desc: "Ожерелье с подвеской."),
        T("candlestick", "Подсвечник", ItemType.Misc, Rarity.Rare, 380, icon: MissingIcon, desc: "Старый металлический подсвечник."),
        T("gold_bar", "Золотой слиток", ItemType.Misc, Rarity.Legendary, 12000, icon: MissingIcon, desc: "Тяжёлый золотой слиток. Очень редкая находка."),
        T("faberge_replica", "Яйцо Фаберже (реплика)", ItemType.Misc, Rarity.Epic, 2200, icon: MissingIcon, desc: "Красивая подделка знаменитого яйца."),
        T("bear_statuette", "Статуэтка Медведя", ItemType.Misc, Rarity.Rare, 520, icon: MissingIcon, desc: "Небольшая статуэтка медведя."),
        T("eagle_statuette", "Статуэтка орла", ItemType.Misc, Rarity.Rare, 520, icon: MissingIcon, desc: "Небольшая статуэтка орла."),
        T("skull_ring", "Золотое кольцо с черепом", ItemType.Misc, Rarity.Epic, 1400, icon: MissingIcon, desc: "Массивное золотое кольцо с черепом."),
        T("hdd", "Жёсткий диск", ItemType.Misc, Rarity.Rare, 360, icon: MissingIcon, desc: "Жёсткий диск. Данные могут быть ценными."),
        T("cd_disk", "CD-диск", ItemType.Misc, Rarity.Common, 35, icon: MissingIcon, desc: "Компакт-диск без подписи."),
        T("book", "Книга", ItemType.Misc, Rarity.Common, 50, icon: MissingIcon, desc: "Потрёпанная книга."),
        T("powerbank", "Повербанк", ItemType.Misc, Rarity.Uncommon, 160, icon: MissingIcon, desc: "Портативный аккумулятор."),
        T("phone", "Телефон", ItemType.Misc, Rarity.Uncommon, 220, icon: MissingIcon, desc: "Смартфон. Экран цел."),
        T("broken_phone", "Разбитый телефон", ItemType.Misc, Rarity.Common, 40, icon: MissingIcon, desc: "Телефон с разбитым экраном."),

        // === Части мутантов (добыча с мутантов) ===
        T("dog_tail", "Собачий хвост", ItemType.Material, Rarity.Common, 45, icon: "/items/animal_parts/Хвост собаки.png", desc: "Хвост мутировавшей собаки. Бармен требует части (кроме хвостов)."),
        T("flesh_eye", "Глаз плоти", ItemType.Material, Rarity.Uncommon, 70, icon: "/items/animal_parts/Глаз плоти.png", desc: "Мутный глаз плоти."),
        T("flesh_fat", "Сало плоти", ItemType.Material, Rarity.Common, 55, icon: "/items/animal_parts/Сало плоти.png", desc: "Жирный кусок сала плоти."),
        T("boar_meat", "Мясо кабана", ItemType.Material, Rarity.Common, 60, icon: "/items/animal_parts/мясо кабана.png", desc: "Кусок мяса дикого кабана."),
        T("boar_hoof", "Копыто кабана", ItemType.Material, Rarity.Uncommon, 50, icon: "/items/animal_parts/копыто кабана.png", desc: "Твёрдое копыто кабана."),

        // === Квестовые предметы / скриптовые ===
        T("quest_lost_cargo", "Утерянный груз", ItemType.Misc, Rarity.Rare, 0, icon: "/items/script_items/Стальной_Ящик.png", desc: "Ценный груз, который нужно было доставить клиенту.", lootable: false),
        T("quest_factory_docs", "Документы с завода", ItemType.Misc, Rarity.Rare, 0, icon: "/items/script_items/Документация.png", desc: "Правительственные документы о секретной разработке.", lootable: false),
        T("quest_merc_flash", "Флешка наёмников", ItemType.Misc, Rarity.Rare, 0, icon: "/task/Пренести_Флешку.png", desc: "Данные группы наёмников.", lootable: false),
        T("quest_bunker_key", "Ключ от бункера", ItemType.Misc, Rarity.Epic, 0, icon: "/items/other/ключ.png", desc: "Ключ к объекту на Градирнях.", lootable: false),
        T("quest_notes", "Записи выжившего", ItemType.Misc, Rarity.Uncommon, 0, icon: "/items/script_items/записка.png", desc: "Обрывки записей о том, что произошло.", lootable: false),
        T("q_closed_case", "Закрытый кейс", ItemType.Misc, Rarity.Rare, 0, icon: "/items/script_items/Закрытый_Кейс.png", desc: "Запертый кейс. Содержимое неизвестно.", lootable: false),
        T("q_steel_box", "Стальной ящик", ItemType.Misc, Rarity.Rare, 0, icon: "/items/script_items/Стальной_Ящик.png", desc: "Тяжёлый стальной ящик с грузом.", lootable: false),
        T("q_artifact_case", "Кейс с артефактами", ItemType.Misc, Rarity.Epic, 0, icon: "/items/script_items/Кейс_с_артефактами.png", desc: "Защитный контейнер с артефактами.", lootable: false),
        T("q_docs_folder", "Папка с документами", ItemType.Misc, Rarity.Rare, 0, icon: "/items/script_items/Документы.png", desc: "Папка с важными документами.", lootable: false),
        T("q_tech_docs", "Техническая документация (секретная)", ItemType.Misc, Rarity.Rare, 0, icon: "/items/script_items/Техническая_Документация.png", desc: "Секретные технические схемы.", lootable: false),
        T("q_intel_folder", "Разведданные", ItemType.Misc, Rarity.Rare, 0, icon: "/items/script_items/папка_с_разведданными.png", desc: "Папка с разведывательными данными.", lootable: false),
        T("q_old_docs", "Старые бумаги", ItemType.Misc, Rarity.Uncommon, 0, icon: "/items/script_items/старые_документы.png", desc: "Старые, но важные бумаги.", lootable: false),
        T("q_note", "Записка", ItemType.Misc, Rarity.Uncommon, 0, icon: "/items/script_items/записка.png", desc: "Короткая записка.", lootable: false),
        T("art_kaplya", "Артефакт «Капля» (контейнер)", ItemType.Misc, Rarity.Legendary, 0, icon: "/items/Artifacts/Капля.png", desc: "Редкий артефакт в защитной упаковке.", lootable: false),
        T("atomic_bomb", "Ядерный заряд (объект)", ItemType.Misc, Rarity.Legendary, 0, icon: "/blocks/atomic_bomb_for_final.png", desc: "Финальное устройство. Используется в сюжете.", lootable: false),

        // === Добыча с боссов (сюжет) ===
        T("boss_pda_city", "КПК Шрама", ItemType.Misc, Rarity.Rare, 0, icon: "/items/script_items/PDA.png", desc: "КПК главаря дворов Шрама. На нём — записи о поставках и контактах.", lootable: false),
        T("boss_flash_kordon", "Флешка Валета", ItemType.Misc, Rarity.Rare, 0, icon: "/items/script_items/флешка1.png", desc: "Зашифрованная флешка смотрящего Кордона. Кто-то очень хочет её получить.", lootable: false),
        T("boss_pda_factory", "КПК Цеховика", ItemType.Misc, Rarity.Rare, 0, icon: "/items/script_items/PDA.png", desc: "КПК бригадира наёмников Цеховика. Данные о грузе и маршрутах.", lootable: false),
        T("boss_flash_mall", "Флешка Кассира", ItemType.Misc, Rarity.Rare, 0, icon: "/items/script_items/флешка2.png", desc: "Флешка лидера мародёров. Списки тайников и схема связей.", lootable: false),
        T("boss_pda_military", "КПК полковника Громова", ItemType.Misc, Rarity.Epic, 0, icon: "/items/script_items/PDA.png", desc: "Защищённый КПК полковника. Военные коды и отметки об объекте.", lootable: false),
        T("boss_flash_predbannik", "Флешка Привратника", ItemType.Misc, Rarity.Rare, 0, icon: "/items/script_items/флешка3.png", desc: "Флешка Привратника с пропусками и журналом доступа.", lootable: false),
        T("boss_pda_forest", "КПК Лешего", ItemType.Misc, Rarity.Rare, 0, icon: "/items/script_items/PDA.png", desc: "Странный КПК, найденный у альфа-мутанта. Кто-то потерял его в лесу...", lootable: false),
        T("boss_flash_outskirts", "Флешка Ворона", ItemType.Misc, Rarity.Epic, 0, icon: "/items/script_items/флешка1.png", desc: "Флешка командира патруля. На ней — отчёты об охране периметра.", lootable: false),
        T("boss_pda_suburb", "КПК Палача", ItemType.Misc, Rarity.Epic, 0, icon: "/items/script_items/PDA.png", desc: "КПК командира ЧВК. Приказы, координаты и зашифрованные сообщения.", lootable: false),
        T("boss_flash_gradirni", "Флешка Куратора", ItemType.Misc, Rarity.Legendary, 0, icon: "/items/script_items/флешка2.png", desc: "Флешка главы охраны Градирней. Ключ к последним тайнам объекта.", lootable: false),

        // === Кассеты ===
        T("cassette_1", "Кассета «Good Ending»", ItemType.Cassette, Rarity.Rare, 300, icon: "/items/other/cassette.gif", desc: "Плёнка с тёплой мелодией.", lootable: false),
        T("cassette_2", "Кассета «Dirge for the Planet»", ItemType.Cassette, Rarity.Rare, 300, icon: "/items/other/cassette.gif", desc: "Тяжёлая, тянущаяся запись.", lootable: false),
        T("cassette_3", "Кассета «Desolated»", ItemType.Cassette, Rarity.Rare, 300, icon: "/items/other/cassette.gif", desc: "Гул в тишине.", lootable: false),

        // === Граната ===
        T("grenade", "Граната РГД-5", ItemType.Misc, Rarity.Common, 150, dmg: 40, icon: "/items/weapon/РГД-5.png", desc: "Осколочная граната РГД-5.", lootable: false, isGrenade: true),

        // === Тайник (редкий объект в зданиях) ===
        T("stash_box", "Тайник", ItemType.Stash, Rarity.Rare, 0, icon: "/blocks/stash.png", desc: "Скрытный тайник. Можно обыскать, чтобы найти лут.", isStash: true, lootable: false),
    };

    private static readonly Dictionary<string, string> IconOverrides = new()
    {
        ["sledge"] = "/items/weapon/sledge.png", ["helmet_tactical"] = "/items/clothing/helmet_tactical.png",
        ["helmet_exo"] = "/items/clothing/helmet_exo.png", ["cloak"] = "/items/clothing/cloak.png",
        ["combat_suit"] = "/items/clothing/combat_suit.png", ["sphere"] = "/items/clothing/sphere.png",
        ["exo"] = "/items/clothing/exo.png", ["bag_small"] = "/items/clothing/bag_small.png",
        ["bag_large"] = "/items/clothing/bag_large.png", ["bag_expedition"] = "/items/clothing/bag_expedition.png",
        ["energy"] = "/items/food/energy.png", ["stew"] = "/items/food/stew.png",
        ["painkiller"] = "/items/medicine/painkiller.png", ["antirad"] = "/items/medicine/antirad.png",
        ["cement"] = "/items/resources/cement.png", ["glass"] = "/items/resources/Glass.png",
        ["rubber"] = "/items/resources/Rubber.png", ["fuel"] = "/items/resources/топливо.png",
        ["kerosene"] = "/items/resources/Керосин.png", ["propane"] = "/items/resources/Пропан.png",
        ["sparkplug"] = "/items/resources/свеча_зажигания.png", ["nails"] = "/items/resources/гвозди.png",
        ["zipties"] = "/items/resources/Стяжки.png", ["electronics"] = "/items/resources/микросхема.png",
        ["toolkit"] = "/items/resources/набор инструментов.png", ["duct_tape"] = "/items/resources/duct_tape.png",
        ["amber"] = "/items/other/янтарь.png", ["icon"] = "/items/other/Икона.png",
        ["documents"] = "/items/other/Документы.png", ["coffee"] = "/items/other/кофе.png",
        ["cigarettes"] = "/items/other/sigarettes.png", ["chocolate"] = "/items/other/шоколад.png",
        ["dogtags"] = "/items/other/dogtag.png", ["medal"] = "/items/other/Medal.png",
        ["cards"] = "/items/other/cards.png", ["harmonica"] = "/items/other/harmonica.png",
        ["compass"] = "/items/other/compass.png", ["binoculars"] = "/items/other/binoculars.png",
        ["camera"] = "/items/other/photoapparat.png", ["radio_parts"] = "/items/other/радиодетали.png",
        ["geiger_counter"] = "/items/other/geiger.png",

        // Костюмы
        ["suit_leather_coat"] = "/items/clothing/Монолит.png",
        ["suit_sweater"] = "/items/clothing/СПП-99.png",
        ["suit_shirt"] = "/items/clothing/ВКПО_НАТО.png",
        ["suit_denim"] = "/items/clothing/Куртка_Новичка.png",
        ["suit_camo"] = "/items/clothing/ВКПО_ВС_РФ.png",
        ["suit_gilly"] = "/items/clothing/Маскхалат.png",
        ["suit_seva"] = "/items/clothing/СЕВА.png",
        ["suit_tactical"] = "/items/clothing/Тактический_комбинезон.png",
        ["suit_winter"] = "/items/clothing/Зимний_комбинезон.png",
        ["cloak_long"] = "/items/clothing/Длинный плащ.png",
        ["suit_hoodie"] = "/items/clothing/Злёный свитер.png",
        ["suit_flannel"] = "/items/clothing/Комбинезон_наёмника.png",
        ["suit_raincoat"] = "/items/clothing/Дождевик.png",
        ["suit_vest"] = "/items/clothing/Армейский_бронекостюм.png",
        ["suit_armor"] = "/items/clothing/Заря.png",

        // Бронежилеты / разгрузки
        ["vest_kevlar"] = "/items/clothing/кевларовый_бронежилет.png",
        ["vest_ceramic"] = "/items/clothing/лёгкий бронежилет.png",
        ["vest_tactical"] = "/items/clothing/Бронежилет_прессы.png",
        ["vest_light"] = "/items/clothing/Разгрузочный жилет Wassermann.png",
        ["vest_stalker"] = "/items/clothing/Охотничий_Жилет.png",
        ["vest_police"] = "/items/clothing/полицейский_бронежилет.png",
        ["vest_bulletproof"] = "/items/clothing/Бронежилет скрытого ношения.png",
        ["vest_steel"] = "/items/clothing/Бронежилет_6Б2.png",
        ["vest_6b13"] = "/items/clothing/Бронежилет_штурмовой_6Б13.png",
        ["vest_korund"] = "/items/clothing/Корунд-ВМ.png",
        ["rig_commando"] = "/items/clothing/Commando.png",

        // Рюкзаки
        ["bag_briefcase"] = "/items/clothing/Школьный рюкзак.png",
        ["bag_sack"] = "/items/clothing/VKBO.png",
        ["bag_daypack"] = "/items/clothing/городской_рюкзак.png",
        ["bag_tactpack"] = "/items/clothing/Тактический рюкзак.png",
        ["bag_milpack"] = "/items/clothing/армейский рюкзак.png",
        ["bag_huge"] = "/items/clothing/SATL Bridger Assault Pack.png",
        ["bag_duffel"] = "/items/clothing/Спортивная_сумка.png",
        ["bag_field"] = "/items/clothing/Походный рюкзак.png",
        ["bag_med"] = "/items/clothing/медицинская_сумка.png",
        ["bag_assault"] = "/items/clothing/штурмовой рюкзак.png",
        ["bag_cargo"] = "/items/clothing/6Ш118.png",

        // Медицина
        ["hydrogen_peroxide"] = "/items/resources/Перикись_водорода.png",
        ["syringe_sealed"] = "/items/resources/Шприц в упаковке.png",

        // Материалы
        ["glass"] = "/items/resources/Glass.png",
        ["cement"] = "/items/resources/cement.png",
        ["rubber"] = "/items/resources/Rubber.png",
        ["fuel"] = "/items/resources/топливо.png",
        ["electronics"] = "/items/resources/микросхема.png",
        ["toolkit"] = "/items/resources/набор инструментов.png",
        ["nails"] = "/items/resources/гвозди.png",
        ["duct_tape"] = "/items/resources/duct_tape.png",
        ["zipties"] = "/items/resources/Стяжки.png",
        ["kerosene"] = "/items/resources/Керосин.png",
        ["propane"] = "/items/resources/Пропан.png",
        ["sparkplug"] = "/items/resources/свеча_зажигания.png",
        ["rosin"] = "/items/resources/Kanifol.png",
        ["capacitors"] = "/items/resources/Конденсаторы.png",
        ["resistors"] = "/items/resources/Резисторы.png",
        ["textolite"] = "/items/resources/Текстолит.png",
        ["battery_aa"] = "/items/resources/батарейка.png",
        ["fuel_can_5"] = "/items/resources/канистра5л.png",
        ["fuel_can_10"] = "/items/resources/канистра10л.png",
        ["screwdriver_set"] = "/items/resources/Набор_отвёрток.png",
        ["awl"] = "/items/resources/шило.png",
        ["bolts"] = "/items/resources/болт.png",
        ["nuts"] = "/items/resources/гайка.png",
        ["paracord"] = "/items/resources/Паракорд.png",
        ["salt"] = "/items/resources/соль.png",
        ["soda_pack"] = "/items/resources/пачка соды.png",
        ["gunpowder"] = "/items/resources/порох.png",
        ["lighter"] = "/items/resources/зажигалка.png",
        ["soap"] = "/items/resources/мыло.png",
        ["aramid_fabric"] = "/items/resources/арамидная.png",
        ["ripstop_fabric"] = "/items/resources/рипстоп.png",
        ["fleece_fabric"] = "/items/resources/флис.png",
        ["cordura_fabric"] = "/items/resources/Кордура.png",
        ["relay"] = "/items/resources/реле.png",
        ["ram"] = "/items/resources/оперативная_память.png",
        ["psu"] = "/items/resources/блок_питания.png",
        ["pliers"] = "/items/resources/плоскогубцы.png",
        ["round_pliers"] = "/items/resources/круглогубцы.png",
        ["drill"] = "/items/resources/дрель.png",
        ["hand_drill"] = "/items/resources/ручная_дрель.png",
        ["multitool"] = "/items/resources/мультитул.png",

        // Разное / ценности
        ["documents"] = "/items/other/Документы.png",
        ["meds_kit_pack"] = "/items/other/Набор_медикаментов.png",
        ["old_documents"] = "/items/other/старые_документы.png",
        ["intel_folder"] = "/items/other/папка_с_разведданными.png",
        ["tech_docs"] = "/items/other/Техническая_Документация.png",
        ["paper_pack"] = "/items/other/пачка_бумаги.png",
        ["old_money"] = "/items/other/старые_деньги.png",
        ["tea_box"] = "/items/other/чай.png",
        ["guitar"] = "/items/other/гитара.png",
        ["keys"] = "/items/other/ключ.png",
        ["car_key"] = "/items/other/Авто_ключ.png",
        ["earrings"] = "/items/other/серьги.png",
        ["ring"] = "/items/other/кольцо.png",
        ["necklace"] = "/items/other/ожерелье.png",
        ["candlestick"] = "/items/other/подсвечник.png",
        ["gold_bar"] = "/items/other/золотой слиток.png",
        ["faberge_replica"] = "/items/other/яйцо фаберже.png",
        ["bear_statuette"] = "/items/other/статуэтка медведя.png",
        ["eagle_statuette"] = "/items/other/статуэтка орла.png",
        ["skull_ring"] = "/items/other/кольцо с черепом.png",
        ["hdd"] = "/items/other/жёсткий диск.png",
        ["cd_disk"] = "/items/other/cd_диск.png",
        ["book"] = "/items/other/потрёпанная книга.png",
        ["powerbank"] = "/items/other/повербанк.png",
        ["phone"] = "/items/other/телефон.png",
        ["broken_phone"] = "/items/other/разбитый_телефон.png",
    };

    private static readonly Dictionary<string, string> AmmoIconOverrides = new()
    {
        [".338 Lapua Magnum"] = "/ammo/.338_Lapua_Magnum.png",
        [".45 ACP"] = "/ammo/.45acp.png",
        [".50 AE"] = "/ammo/.50ae.png",
        [".50 BMG"] = "/ammo/.50bmg.png",
        ["12 Gauge"] = "/ammo/12gauge.png",
        ["12.7x55mm"] = "/ammo/12,7x55.png",
        ["4.6x30mm"] = "/ammo/4.6x30.png",
        ["5.45x39mm"] = "/ammo/5.45x39.png",
        ["5.8x42mm"] = "/ammo/58x42mm-dvp88.png",
        ["5.56x45mm NATO"] = "/ammo/5.56x45.png",
        ["5.7x28mm"] = "/ammo/5.7x28.png",
        ["7.62x25mm"] = "/ammo/7.62x25.png",
        ["7.62x39mm"] = "/ammo/7.62x39.png",
        ["7.62x51mm NATO"] = "/ammo/7.62x51.png",
        ["7.62x54mmR"] = "/ammo/7.62x54.png",
        ["7.65mm"] = "/ammo/7.65.png",
        ["7.92x57mm Mauser"] = "/ammo/7.92x57.png",
        ["9x18mm"] = "/ammo/9x18.png",
        ["9x19mm"] = "/ammo/9x19.png",
        ["9x39mm"] = "/ammo/9x39.png",
        ["bolt"] = "/ammo/crossbow_bolt.png",
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
        ["knife"] = 0.4, ["machete"] = 1.0, ["sledge"] = 4.0,
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
        string icon = "", string desc = "", bool lootable = true, bool isGrenade = false, string caliber = "",
        bool isSuit = false, int slowdown = 0, int blastProtection = 0, int bulletProtection = 0,
        int physicalProtection = 0, int accuracy = 0, int rangeEffective = 0, int rangeMax = 0,
        int rpm = 0, int requiredSkill = 0, bool isStash = false, int restoreHpBonus = 0)
    {
        if (icon == MissingIcon && IconOverrides.TryGetValue(code, out var mapped)) icon = mapped;

        int lootTier = LootTierOverrides.TryGetValue(code, out var tier) ? tier : Math.Clamp((int)r + 1, 1, 7);
        double weight = WeightOverrides.TryGetValue(code, out var w) ? w : DefaultWeight(type);

        // Снаряжение стоит заметно дороже расходников и лута.
        int finalPrice = type is ItemType.Helmet or ItemType.Armor or ItemType.Backpack
            ? (int)Math.Round(price * 1.6)
            : price;

        // Ближний бой не должен превышать 35 урона.
        int finalDamage = type == ItemType.WeaponMelee ? Math.Clamp(dmg, 1, 35) : dmg;

        return new Item
        {
            Code = code, Name = name, Description = desc, Icon = icon, Type = type, Rarity = r, Price = finalPrice,
            Damage = finalDamage, Defense = def, BackpackSlots = slots, Restore = restore, Restores = restores,
            RestoreHpBonus = restoreHpBonus,
            Lootable = lootable, LootTier = lootTier, Weight = weight, IsGrenade = isGrenade, Caliber = caliber,
            IsSuit = isSuit,
            Slowdown = slowdown, BlastProtection = blastProtection, BulletProtection = bulletProtection,
            PhysicalProtection = physicalProtection,
            Accuracy = accuracy, RangeEffective = rangeEffective, RangeMax = rangeMax, Rpm = rpm,
            RequiredSkill = requiredSkill,
            IsStash = isStash,
        };
    }

    public List<Item> All => _templates;
    public Item? Get(string code) => _templates.FirstOrDefault(t => t.Code == code);

    public Item Add(Item item) { _templates.Add(item); return item; }

    /// <summary>Получить или создать боеприпас по калибру. Один шаблон на калибр; возвращает новый экземпляр.</summary>
    public Item CreateAmmo(string caliber, double weight, int price, string desc)
    {
        var existing = _templates.FirstOrDefault(t => t.Type == ItemType.Ammo && t.Caliber == caliber);
        if (existing != null)
        {
            if (AmmoIconOverrides.TryGetValue(caliber, out var existingIcon)) existing.Icon = existingIcon;
            return Create(existing.Code);
        }

        int damage = AmmoDamageHint(caliber);
        bool isBolt = string.Equals(caliber, "bolt", StringComparison.OrdinalIgnoreCase);
        var item = new Item
        {
            Code = "ammo_" + NormalizeCode(caliber),
            Name = isBolt ? "Болты для арбалета" : $"Патроны {caliber}",
            Description = isBolt ? "Болты для арбалета." : desc,
            Icon = AmmoIconOverrides.TryGetValue(caliber, out var ammoIcon) ? ammoIcon : MissingIcon,
            Type = ItemType.Ammo,
            Rarity = Rarity.Common,
            Price = WeaponCatalog.AmmoPriceFor(caliber),
            Weight = weight,
            Lootable = true,
            Caliber = caliber,
            LootTier = WeaponCatalog.LootTierForDamage(damage),
            FoundInRaid = false,
        };
        _templates.Add(item);
        return Create(item.Code);
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
        "bolt" => 55,
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
            BackpackSlots = t.BackpackSlots, Restore = t.Restore, Restores = t.Restores, RestoreHpBonus = t.RestoreHpBonus,
            Lootable = t.Lootable, LootTier = t.LootTier, RequiredLoyalty = t.RequiredLoyalty,
            Weight = t.Weight, Count = 1, IsGrenade = t.IsGrenade, Caliber = t.Caliber,
            IsSuit = t.IsSuit, Slowdown = t.Slowdown, BlastProtection = t.BlastProtection,
            BulletProtection = t.BulletProtection, PhysicalProtection = t.PhysicalProtection,
            Accuracy = t.Accuracy, RangeEffective = t.RangeEffective, RangeMax = t.RangeMax,
            Rpm = t.Rpm, RequiredSkill = t.RequiredSkill, IsStash = t.IsStash,
        };
    }
}