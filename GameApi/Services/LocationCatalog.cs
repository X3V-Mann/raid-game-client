using GameApi.Models;

namespace GameApi.Services;

public class LocationCatalog
{
    public List<Location> All { get; } = new()
    {
        new Location
        {
            Id = "city", Name = "🏙 Город", Tier = 1,
            Description = "Разрушенные улицы. Много мусора, но и лут бывает вкусный.",
            Image = "/locations/city.jpg", Icon = "🏙", BossChance = 0.05,
            BossName = "Шрам (Главарь дворов)", BossIcon = "/bosses/Шрам.jpg",
            RarityWeights = new() { [Rarity.Common] = 60, [Rarity.Uncommon] = 25, [Rarity.Rare] = 10, [Rarity.Epic] = 4, [Rarity.Legendary] = 1 },
            Enemies = new()
            {
                new() { Name = "Дикий", Icon = "/enemies/dikiy.png", BaseHp = 18, BaseDamage = 8, Weight = 3 },
                new() { Name = "Мародёр", Icon = "/enemies/maroder.png", BaseHp = 22, BaseDamage = 9, Weight = 3 },
                new() { Name = "Бомж", Icon = "/enemies/bomzh.png", BaseHp = 15, BaseDamage = 7, Weight = 2 },
                new() { Name = "Пёс", Icon = "/enemies/pes.png", BaseHp = 12, BaseDamage = 10, Weight = 2 },
                new() { Name = "Кабан", Icon = "/enemies/boar.png", BaseHp = 28, BaseDamage = 11, Weight = 1 },
                new() { Name = "Одиночка", Icon = "/enemies/odinochka.png", BaseHp = 25, BaseDamage = 9, Weight = 1 },
            },
            ScenePool = new()
            {
                "Заброшенный дом|В окне мелькнула тень, но внутри пусто|🏚️",
                "Стая ворон|Птицы взлетают с трубы, провожая тебя взглядом|🐦",
                "Плакат|Старый плакат: «Выживи любой ценой»|📰",
                "Костёр|Кто-то недавно сидел здесь. Угли ещё тёплые|🔥",
            },
            ThoughtPool = new() { "Пахнет гарью и мокрым бетоном.", "Раньше тут кипела жизнь. Теперь — тишина.", "Надо держаться подальше от открытых улиц.", "Где-то далеко упало здание. Или это гром?" },
        },

        new Location
        {
            Id = "kordon", Name = "🌾 Кордон", Tier = 1,
            Description = "Первый рубеж. Деревня, блокпост и дорога в никуда.",
            Image = "/locations/kordon.png", Icon = "🌾", BossChance = 0.08,
            BossName = "Валет (Смотрящий Кордона)", BossIcon = "/bosses/Валет.jpg",
            RarityWeights = new() { [Rarity.Common] = 55, [Rarity.Uncommon] = 28, [Rarity.Rare] = 12, [Rarity.Epic] = 4, [Rarity.Legendary] = 1 },
            Enemies = new()
            {
                new() { Name = "Дикий", Icon = "/enemies/dikiy.png", BaseHp = 20, BaseDamage = 9, Weight = 3 },
                new() { Name = "Мародёр", Icon = "/enemies/maroder.png", BaseHp = 24, BaseDamage = 10, Weight = 3 },
                new() { Name = "Бомж", Icon = "/enemies/bomzh.png", BaseHp = 16, BaseDamage = 7, Weight = 2 },
                new() { Name = "Пёс", Icon = "/enemies/pes.png", BaseHp = 14, BaseDamage = 11, Weight = 2 },
                new() { Name = "Плоть", Icon = "/enemies/flesh.png", BaseHp = 38, BaseDamage = 10, BaseDefense = 1, Weight = 1 },
                new() { Name = "Кабан", Icon = "/enemies/boar.png", BaseHp = 30, BaseDamage = 13, Weight = 1 },
                new() { Name = "Одиночка", Icon = "/enemies/odinochka.png", BaseHp = 26, BaseDamage = 10, Weight = 2 },
                new() { Name = "Бандит", Icon = "/enemies/bandit.png", BaseHp = 28, BaseDamage = 11, Weight = 1 },
            },
            ScenePool = new()
            {
                "Блокпост|Мешки с песком, брошенный УАЗ, тишина|🚧",
                "Колодец|Вода ещё есть. Ведро на месте|🪣",
                "Деревенский дом|Дверь приоткрыта, внутри темно|🏠",
                "Поле|Рожь выросла выше роста. Кто-то в ней шевелится|🌾",
            },
            ThoughtPool = new() { "Кордон — это ещё не Зона. Или уже?", "Тут пахнет дымом и дизелем.", "Дорога одна. Назад или вперёд.", "Птицы молчат. Плохо." },
        },

        new Location
        {
            Id = "factory", Name = "🏭 Завод", Tier = 2,
            Description = "Ржавые цеха. Больше врагов, но и лут ценнее.",
            Image = "/locations/factory.jpg", Icon = "🏭", BossChance = 0.12,
            BossName = "Цеховик (Бригадир наёмников)", BossIcon = "/bosses/Цеховик.png",
            UnlockQuestId = "prapor_4", UnlockHint = "Откроется после цепочки Прапора",
            RarityWeights = new() { [Rarity.Common] = 40, [Rarity.Uncommon] = 30, [Rarity.Rare] = 18, [Rarity.Epic] = 9, [Rarity.Legendary] = 3 },
            Enemies = new()
            {
                new() { Name = "Мародёр", Icon = "/enemies/maroder.png", BaseHp = 28, BaseDamage = 11, Weight = 2 },
                new() { Name = "Бандит", Icon = "/enemies/bandit.png", BaseHp = 32, BaseDamage = 13, Weight = 3 },
                new() { Name = "Наёмник", Icon = "/enemies/naemnik.png", BaseHp = 35, BaseDamage = 14, BaseDefense = 2, Weight = 2 },
                new() { Name = "Рейдер", Icon = "/enemies/raider.png", BaseHp = 30, BaseDamage = 16, Ranged = true, Weight = 1 },
                new() { Name = "Отморозок", Icon = "/enemies/otmorozok.png", BaseHp = 40, BaseDamage = 14, Weight = 1 },
                new() { Name = "Дикий", Icon = "/enemies/dikiy.png", BaseHp = 22, BaseDamage = 10, Weight = 1 },
            },
            ScenePool = new()
            {
                "Скрип механизмов|Где-то в глубине цехов ещё крутятся станки|⚙️",
                "Радио-шум|Из разбитого динамика слышно обрывок передачи|📻",
                "Следы крови|Свежие следы ведут в сторону заката|🩸",
                "Дым из трубы|Кто-то развёл огонь на верхнем этаже|🏭",
            },
            ThoughtPool = new() { "Пахнет машинным маслом и ржавчиной.", "Гул станков не прекращался даже после конца.", "Надо следить за трубами — оттуда удобно стрелять.", "Где-то капает вода. Или это что-то другое?" },
        },

        new Location
        {
            Id = "mall", Name = "🏬 Торговый центр", Tier = 3,
            Description = "Стекло, бетон и пустые полки. Кто-то всё ещё охраняет это место.",
            Image = "/locations/mall.jpg", Icon = "🏬", BossChance = 0.18,
            BossName = "Кассир (Лидер мародёров)", BossIcon = "/bosses/Кассир.png",
            RarityWeights = new() { [Rarity.Common] = 30, [Rarity.Uncommon] = 30, [Rarity.Rare] = 22, [Rarity.Epic] = 13, [Rarity.Legendary] = 5 },
            Enemies = new()
            {
                new() { Name = "Бандит", Icon = "/enemies/bandit.png", BaseHp = 34, BaseDamage = 13, Weight = 3 },
                new() { Name = "Наёмник", Icon = "/enemies/naemnik.png", BaseHp = 38, BaseDamage = 16, BaseDefense = 2, Weight = 3 },
                new() { Name = "Рейдер", Icon = "/enemies/raider.png", BaseHp = 32, BaseDamage = 18, Ranged = true, Weight = 2 },
                new() { Name = "Отморозок", Icon = "/enemies/otmorozok.png", BaseHp = 42, BaseDamage = 16, Weight = 2 },
                new() { Name = "Мародёр", Icon = "/enemies/maroder.png", BaseHp = 30, BaseDamage = 13, Weight = 1 },
                new() { Name = "Одиночка", Icon = "/enemies/odinochka.png", BaseHp = 32, BaseDamage = 13, Weight = 1 },
            },
            ScenePool = new()
            {
                "Разбитая витрина|За стеклом пусто, но кто-то оставил записку|🪟",
                "Эскалатор|Ступени стоят. Свет мигает|🪜",
                "Фонтан|Вода давно высохла, на дне — гильзы|⛲",
                "Вывеска|«ВСЁ ДЛЯ ДОМА». Теперь — для тех, кто выжил|🪧",
            },
            ThoughtPool = new() { "Пахнет пластиком и пылью.", "Тут когда-то было людно. Теперь — тихо.", "Слишком чисто для заброшенного места.", "Кто-то ходит по второму этажу." },
        },

        new Location
        {
            Id = "military", Name = "🪖 Военная база", Tier = 4,
            Description = "Оцепление, бетон, армейский лут. Смертельно.",
            Image = "/locations/military.jpg", Icon = "🪖", BossChance = 0.25,
            BossName = "Полковник Громов", BossIcon = "/bosses/полковник%20громов.png",
            UnlockQuestId = "ragman_4", UnlockHint = "Откроется после цепочки Старьёвщика",
            RarityWeights = new() { [Rarity.Common] = 20, [Rarity.Uncommon] = 30, [Rarity.Rare] = 25, [Rarity.Epic] = 18, [Rarity.Legendary] = 7 },
            Enemies = new()
            {
                new() { Name = "Военный", Icon = "/enemies/voenny.png", BaseHp = 45, BaseDamage = 18, BaseDefense = 2, Weight = 3 },
                new() { Name = "Спецназ", Icon = "/enemies/specnaz.png", BaseHp = 50, BaseDamage = 21, BaseDefense = 3, Weight = 2 },
                new() { Name = "Наёмник", Icon = "/enemies/naemnik.png", BaseHp = 40, BaseDamage = 16, Weight = 2 },
                new() { Name = "Снайпер", Icon = "/enemies/snaiper.png", BaseHp = 35, BaseDamage = 24, Ranged = true, Weight = 1 },
                new() { Name = "Сержант", Icon = "/enemies/serzhant.png", BaseHp = 55, BaseDamage = 19, BaseDefense = 4, Weight = 1 },
                new() { Name = "Рейдер", Icon = "/enemies/raider.png", BaseHp = 38, BaseDamage = 19, Ranged = true, Weight = 1 },
            },
            ScenePool = new()
            {
                "Колючая проволока|Зацепился за старую растяжку, но она пустая|🪤",
                "Брошенный БТР|Люк открыт, внутри никого|🛻",
                "Сигнальная ракета|Вспышка на горизонте — кто-то подал знак|🚨",
                "Радиопомехи|В наушнике трещит чей-то голос|📡",
            },
            ThoughtPool = new() { "Воздух густой, как перед грозой.", "На бетоне свежие следы. Не мои.", "Если тут есть снайпер — я уже покойник.", "Слышу чей-то шёпот в рации. Не отвечай." },
        },

        new Location
        {
            Id = "predbannik", Name = "🚪 Предбанник", Tier = 5,
            Description = "Последний рубеж перед чем-то большим. Смешанные группы, тяжёлый лут.",
            Image = "/locations/predbannik.jpg", Icon = "🚪", BossChance = 0.30,
            BossName = "Привратник", BossIcon = "/bosses/Привратник.png",
            RarityWeights = new() { [Rarity.Common] = 20, [Rarity.Uncommon] = 28, [Rarity.Rare] = 26, [Rarity.Epic] = 18, [Rarity.Legendary] = 8 },
            Enemies = new()
            {
                new() { Name = "Военный", Icon = "/enemies/voenny.png", BaseHp = 50, BaseDamage = 19, BaseDefense = 2, Weight = 2 },
                new() { Name = "Спецназ", Icon = "/enemies/specnaz.png", BaseHp = 55, BaseDamage = 22, BaseDefense = 3, Weight = 2 },
                new() { Name = "Наёмник", Icon = "/enemies/naemnik.png", BaseHp = 48, BaseDamage = 19, BaseDefense = 2, Weight = 2 },
                new() { Name = "Снайпер", Icon = "/enemies/snaiper.png", BaseHp = 40, BaseDamage = 27, Ranged = true, Weight = 1 },
                new() { Name = "Ветеран", Icon = "/enemies/stalker_07.jpg", BaseHp = 60, BaseDamage = 22, BaseDefense = 3, Weight = 2 },
                new() { Name = "Рейдер", Icon = "/enemies/raider.png", BaseHp = 42, BaseDamage = 21, Ranged = true, Weight = 2 },
                new() { Name = "Отморозок", Icon = "/enemies/otmorozok.png", BaseHp = 52, BaseDamage = 21, Weight = 1 },
            },
            ScenePool = new()
            {
                "Бетонный коридор|Лампы горят через одну. Впереди — гермодверь|🚪",
                "Камера хранения|Ячейки открыты, но не все|🗄️",
                "Вентиляция|Где-то в шахте слышен скрежет|💨",
                "Стол с бумагами|Чьи-то записи, половина сгорела|📄",
            },
            ThoughtPool = new() { "Пахнет сыростью и железом.", "Кто-то очень не хотел, чтобы я сюда попал.", "Тишина здесь плотная, как вата.", "Одна дверь. Один шанс." },
        },

        new Location
        {
            Id = "forest", Name = "🌲 Лес", Tier = 6,
            Description = "Тихий лес, который только кажется пустым.",
            Image = "/locations/forest.jpg", Icon = "🌲", BossChance = 0.15,
            BossName = "Леший (Альфа-мутант)", BossIcon = "/bosses/Леший.png",
            UnlockQuestId = "blacksmith_4", UnlockHint = "Откроется после цепочки Кузнеца",
            RarityWeights = new() { [Rarity.Common] = 35, [Rarity.Uncommon] = 30, [Rarity.Rare] = 20, [Rarity.Epic] = 11, [Rarity.Legendary] = 4 },
            Enemies = new()
            {
                new() { Name = "Мутант", Icon = "/enemies/stalker_01.jpg", BaseHp = 32, BaseDamage = 14, Weight = 2 },
                new() { Name = "Плоть", Icon = "/enemies/flesh.png", BaseHp = 55, BaseDamage = 16, BaseDefense = 2, Weight = 2 },
                new() { Name = "Кабан", Icon = "/enemies/boar.png", BaseHp = 45, BaseDamage = 21, Weight = 2 },
                new() { Name = "Браконьер", Icon = "/enemies/stalker_02.jpg", BaseHp = 36, BaseDamage = 16, Ranged = true, Weight = 2 },
                new() { Name = "Одиночка", Icon = "/enemies/stalker_03.jpg", BaseHp = 40, BaseDamage = 14, BaseDefense = 2, Weight = 1 },
                new() { Name = "Следопыт", Icon = "/enemies/stalker_04.jpg", BaseHp = 42, BaseDamage = 18, Weight = 1 },
                new() { Name = "Пёс", Icon = "/enemies/pes.png", BaseHp = 24, BaseDamage = 13, Weight = 1 },
            },
            ScenePool = new()
            {
                "Тишина|Птицы замолчали задолго до твоего появления|🌲",
                "Сломанные ветки|Кто-то прошёл здесь совсем недавно|👣",
                "Старая вышка|Наверху кто-то оставил рюкзак|🗼",
                "Туман|Лес растворяется в молоке на расстоянии десяти шагов|🌫️",
            },
            ThoughtPool = new() { "Лес не любит громких.", "Следы слишком ровные для зверя.", "Здесь пахнет хвоей и железом.", "Если прислушаться, слышно чьё-то дыхание." },
        },

        new Location
        {
            Id = "outskirts", Name = "🏚 Окраины города", Tier = 7,
            Description = "Промзона на границе сектора. Патрули и забытые склады.",
            Image = "/locations/outskirts.jpg", Icon = "🏚", BossChance = 0.2,
            BossName = "Ворон (Командир патруля)", BossIcon = "/bosses/Ворон.png",
            UnlockQuestId = "therapist_4", UnlockHint = "Откроется после цепочки Медика",
            RarityWeights = new() { [Rarity.Common] = 25, [Rarity.Uncommon] = 30, [Rarity.Rare] = 24, [Rarity.Epic] = 15, [Rarity.Legendary] = 6 },
            Enemies = new()
            {
                new() { Name = "Патруль", Icon = "/enemies/stalker_05.jpg", BaseHp = 48, BaseDamage = 19, BaseDefense = 2, Weight = 3 },
                new() { Name = "Снайпер-наёмник", Icon = "/enemies/stalker_06.jpg", BaseHp = 40, BaseDamage = 26, Ranged = true, Weight = 2 },
                new() { Name = "Мародёр", Icon = "/enemies/maroder.png", BaseHp = 38, BaseDamage = 16, Weight = 2 },
                new() { Name = "Ветеран", Icon = "/enemies/stalker_07.jpg", BaseHp = 55, BaseDamage = 21, BaseDefense = 3, Weight = 1 },
                new() { Name = "Наёмник", Icon = "/enemies/naemnik.png", BaseHp = 44, BaseDamage = 19, Weight = 1 },
            },
            ScenePool = new()
            {
                "Бетонный забор|На стене свежая надпись: «Не верь картам»|🧱",
                "Склад|Двери заперты изнутри|🏢",
                "Прожектор|Луч света медленно прошёлся по дороге|🔦",
                "Радиопереговоры|Сквозь помехи слышно цифры|📡",
            },
            ThoughtPool = new() { "Слишком много патрулей для забытой окраины.", "Кто-то платит этим людям. И хорошо платит.", "Мне не нравится, как они смотрят на карту.", "Город остался позади. Проблемы — нет." },
        },

        new Location
        {
            Id = "suburb", Name = "🏘 Пригород", Tier = 8,
            Description = "Кварталы, где жили те, кто сейчас охотится на тебя.",
            Image = "/locations/suburb.jpg", Icon = "🏘", BossChance = 0.25,
            BossName = "Палач (Командир ЧВК)", BossIcon = "/bosses/Палач.png",
            UnlockQuestId = "buyer_4", UnlockHint = "Откроется после цепочки Скупщика",
            RarityWeights = new() { [Rarity.Common] = 18, [Rarity.Uncommon] = 28, [Rarity.Rare] = 27, [Rarity.Epic] = 19, [Rarity.Legendary] = 8 },
            Enemies = new()
            {
                new() { Name = "Боец ЧВК", Icon = "/enemies/stalker_08.jpg", BaseHp = 58, BaseDamage = 22, BaseDefense = 3, Weight = 3 },
                new() { Name = "Тяжёлый", Icon = "/enemies/stalker_09.jpg", BaseHp = 68, BaseDamage = 24, BaseDefense = 4, Weight = 2 },
                new() { Name = "Штурмовик", Icon = "/enemies/stalker_10.jpg", BaseHp = 52, BaseDamage = 26, Ranged = true, Weight = 2 },
                new() { Name = "Офицер", Icon = "/enemies/stalker_11.jpg", BaseHp = 60, BaseDamage = 27, BaseDefense = 3, Weight = 1 },
                new() { Name = "Спецназ", Icon = "/enemies/specnaz.png", BaseHp = 58, BaseDamage = 24, BaseDefense = 3, Weight = 1 },
            },
            ScenePool = new()
            {
                "Пустой двор|Качели скрипят от ветра|🛝",
                "Брошенная машина|Двигатель ещё тёплый|🚗",
                "Детский рисунок|На стене мелом нарисованы солдаты|🖍️",
                "Флаг|Над крышей висит чужой флаг|🚩",
            },
            ThoughtPool = new() { "Здесь жили люди. Теперь здесь живут приказы.", "Каждый дом простреливается.", "Они не охраняют город. Они охраняют что-то в нём.", "Слишком чисто для зоны боевых действий." },
        },

        new Location
        {
            Id = "gradirni", Name = "☢ Градирни", Tier = 9,
            Description = "Остывшие градирни. Здесь заканчивается всё.",
            Image = "/locations/gradirni.jpg", Icon = "☢", BossChance = 0.45,
            BossName = "Куратор (Глава охраны объекта)", BossIcon = "/bosses/Куратор.png",
            UnlockQuestId = "story_9_10", UnlockHint = "Откроется после 9-й главы сюжета",
            RarityWeights = new() { [Rarity.Common] = 10, [Rarity.Uncommon] = 20, [Rarity.Rare] = 30, [Rarity.Epic] = 28, [Rarity.Legendary] = 12 },
            Enemies = new()
            {
                new() { Name = "Охрана объекта", Icon = "/enemies/stalker_12.jpg", BaseHp = 72, BaseDamage = 27, BaseDefense = 5, Weight = 3 },
                new() { Name = "Инженер", Icon = "/enemies/stalker_13.jpg", BaseHp = 48, BaseDamage = 19, Weight = 2 },
                new() { Name = "Офицер охраны", Icon = "/enemies/stalker_14.jpg", BaseHp = 85, BaseDamage = 30, BaseDefense = 6, Weight = 2 },
                new() { Name = "Снайпер", Icon = "/enemies/stalker_15.jpg", BaseHp = 55, BaseDamage = 35, Ranged = true, Weight = 1 },
                new() { Name = "Тяжёлый", Icon = "/enemies/stalker_09.jpg", BaseHp = 80, BaseDamage = 29, BaseDefense = 5, Weight = 1 },
            },
            ScenePool = new()
            {
                "Гудок|Низкий гул идёт из-под земли|📢",
                "Туман|Белый пар стелется по бетону|🌫️",
                "Красные лампы|Аварийное освещение мигает в такт сердцу|🚨",
                "Бункер|Гермодверь приоткрыта. Внутри свет|🚪",
            },
            ThoughtPool = new() { "Здесь пахнет озоном и концом.", "Они построили это не для войны.", "Если я ошибаюсь, обратной дороги не будет.", "Красная кнопка не прощает ошибок." },
        },
    };

    public Location? Get(string id) => All.FirstOrDefault(l => l.Id == id);

    public bool IsUnlocked(Player player, Location loc) =>
        string.IsNullOrEmpty(loc.UnlockQuestId) || player.UnlockedLocations.Contains(loc.Id);
}