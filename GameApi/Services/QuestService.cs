using GameApi.Models;

namespace GameApi.Services;

public class QuestService
{
    private readonly ItemCatalog _catalog;

    public QuestService(ItemCatalog catalog) => _catalog = catalog;

    private static readonly Dictionary<int, string> ChapterArcs = new()
    {
        [1] = "После погони вы заперлись в старом бункере на окраине. Нужно выжить и обустроиться.",
        [2] = "Рация ловит обрывки чужих переговоров. В них всё чаще звучит завод.",
        [3] = "Завод — единственное место с электричеством. Но там уже есть хозяева.",
        [4] = "Военная база хранит ответы. И очень хорошо охраняется.",
        [5] = "В лесу видели то, чего не должно быть. Пора узнать, кто здесь на самом деле главный.",
        [6] = "Окраины перекрыты патрулями. Кто-то очень не хочет пускать вас дальше.",
        [7] = "Пригород живёт по своим законам. Здесь заказывают музыку и оплачивают войну.",
        [8] = "В архиве остались документы. В них — имена, счета и настоящая причина конфликта.",
        [9] = "Правда оказалась хуже слухов. Осталось решить, что с ней делать.",
        [10] = "Градирни — конечная точка. Здесь заканчивается всё.",
    };

    private static readonly Dictionary<int, string[]> ChapterTitles = new()
    {
        [1] = new[] { "Первый выход", "Бетон и железо", "Чистый двор", "Вода и хлеб", "Ночной шорох",
                      "Провода", "Крыша", "Список покупок", "Тишина в эфире", "Дом" },
        [2] = new[] { "Связь", "Обрывки", "Заводской сигнал", "Караван", "Соглядатаи",
                      "Ночная смена", "Дорога на завод", "Пропавшая группа", "Частоты", "Крысы в подвале" },
        [3] = new[] { "Питание", "Генератор", "Этаж за этажом", "Смена", "Масло и кровь",
                      "Цех", "Трофей", "Огнетушитель", "Котёл", "Хозяин завода" },
        [4] = new[] { "Каска", "Караул", "Под обстрелом", "Арсенал", "Плац",
                      "Блокпост", "Рация", "Сапёры", "Знамя", "Комендант базы" },
        [5] = new[] { "Капля", "Тихая охота", "Тропы", "Лесник", "Ветки",
                      "Туман", "Гнездо", "Следы", "Засада", "Сердце леса" },
        [6] = new[] { "Время", "Патрули", "Дорога", "Бетонный забор", "Прожектор",
                      "Склад", "Перехват", "Проверка", "Ночной рейд", "Окраина" },
        [7] = new[] { "Золото", "Кварталы", "Осада", "Пустой двор", "Детский рисунок",
                      "Флаг", "Качели", "Комендантский час", "Штурм", "Пригород" },
        [8] = new[] { "Архив", "Свидетели", "Тишина", "Папка", "Печати",
                      "Сейф", "Охрана архива", "Черновики", "Подпись", "Правда" },
        [9] = new[] { "Бумаги", "Закрытые рты", "Последний рубеж", "Счета", "Имена",
                      "Две страны", "Курьер", "Приказ", "Ложь", "Градирни" },
        [10] = new[] { "Периметр", "Комендант", "Обратный отсчёт", "Бетон", "Градирня",
                       "Охрана", "Красная лампа", "Бункер", "Кнопка", "Конец" },
    };

    private static readonly Dictionary<int, string[]> FetchPools = new()
    {
        [1] = new[] { "scrap", "cloth", "wire" },
        [2] = new[] { "wire", "scrap", "cement" },
        [3] = new[] { "battery", "wire", "electronics" },
        [4] = new[] { "helmet_ssh", "cloth", "battery" },
        [5] = new[] { "artifact", "watch", "amber" },
        [6] = new[] { "watch", "goldchain", "icon" },
        [7] = new[] { "goldchain", "icon", "amber" },
        [8] = new[] { "documents", "watch", "goldchain" },
        [9] = new[] { "documents", "goldchain", "icon" },
        [10] = new[] { "documents", "documents", "documents" },
    };

    // ============================================================
    // СЮЖЕТ: 10 ГЛАВ ПО 10 ЗАДАНИЙ
    // ============================================================
    public List<Quest> CreateStory()
    {
        var result = new List<Quest>();

        for (int chapter = 1; chapter <= 10; chapter++)
        {
            var titles = ChapterTitles[chapter];
            var objectives = BuildObjectives(chapter);

            for (int i = 0; i < objectives.Count; i++)
            {
                var o = objectives[i];
                int order = i + 1;

                int money = 250 + chapter * 320 + i * 55;
                int xp = 40 + chapter * 22 + i * 8;

                string[] rewardItems = Array.Empty<string>();
                if (chapter == 5 && order == 10) rewardItems = new[] { "wpn_vss" };
                if (chapter == 8 && order == 10) rewardItems = new[] { "wpn_scarh" };
                if (chapter == 10 && order == 10) rewardItems = new[] { "wpn_barrettm82" };
                if (order == 4) rewardItems = new[] { "bandage" };
                if (order == 8) rewardItems = new[] { chapter >= 5 ? "medkit" : "bandage" };

                string itemName = o.ItemCode == null ? "" : _catalog.Get(o.ItemCode)?.Name ?? o.ItemCode;
                string objective = o.Type switch
                {
                    QuestType.Fetch => $"Нужно принести: {itemName} ×{o.Target}.",
                    QuestType.Survive => $"Вернитесь живым из рейдов ({o.Target} раз).",
                    _ when o.Boss => "Устраните босса локации.",
                    _ => $"Устраните противников: {o.Target}.",
                };

                result.Add(Story(
                    chapter, order, o.Type, titles[i],
                    $"{ChapterArcs[chapter]} {objective}",
                    o.Target, money, xp, o.ItemCode, o.Unlock, o.Boss, rewardItems));
            }
        }

        return result;
    }

    private List<(QuestType Type, int Target, string? ItemCode, bool Boss, string? Unlock)> BuildObjectives(int chapter)
    {
        if (chapter == 10)
        {
            return new()
            {
                (QuestType.Kill, 10, null, false, null),
                (QuestType.Kill, 12, null, false, null),
                (QuestType.Survive, 3, null, false, null),
                (QuestType.Kill, 14, null, false, null),
                (QuestType.Survive, 3, null, false, null),
                (QuestType.Kill, 16, null, false, null),
                (QuestType.Survive, 4, null, false, null),
                (QuestType.Kill, 18, null, false, null),
                (QuestType.Kill, 1, null, true, null),
                (QuestType.Survive, 6, null, false, null),
            };
        }

        var pool = FetchPools[chapter];
        int n = chapter;

        int FetchCount(string code, int baseCount) => code switch
        {
            "artifact" or "documents" or "goldchain" or "icon" or "watch" or "helmet_ssh" => 1,
            "battery" or "electronics" or "toolkit" => 2,
            _ => Math.Clamp(baseCount, 1, 6),
        };

        return new()
        {
            (QuestType.Kill, 4 + n * 2, null, false, null),
            (QuestType.Fetch, FetchCount(pool[0], 3 + n / 2), pool[0], false, null),
            (QuestType.Survive, 2 + n / 4, null, false, null),
            (QuestType.Kill, 6 + n * 3, null, false, null),
            (QuestType.Fetch, FetchCount(pool[1], 2 + n / 3), pool[1], false, null),
            (QuestType.Survive, 2, null, false, null),
            (QuestType.Kill, 8 + n * 3, null, false, null),
            (QuestType.Fetch, FetchCount(pool[2], 1 + n / 4), pool[2], false, null),
            (QuestType.Survive, 2 + n / 4, null, false, null),
            (QuestType.Kill, 10 + n * 4, null, false, chapter == 9 ? "gradirni" : null),
        };
    }

    // ============================================================
    // ЦЕПОЧКИ ТОРГОВЦЕВ (открывают локации)
    // ============================================================
    public List<Quest> CreateTraderQuests() => new()
    {
        Chain("prapor", 1, QuestType.Kill, 3, "Проверка",
            "Прапор смотрит на вас как на новобранца. Уберите трёх — и он поверит, что вы умеете держать оружие.",
            400, 60),
        Chain("prapor", 2, QuestType.Kill, 8, "Зачистка",
            "В городе расплодилось отребье. Прапор просит навести порядок.",
            900, 100, requires: "prapor_1", rewards: "pm"),
        Chain("prapor", 3, QuestType.Fetch, 1, "Трофей",
            "Прапор хочет вернуть пистолет, который потерял в рейде. Принесите ПМ.",
            1200, 120, itemCode: "pm", requires: "prapor_2", rewards: "shotgun"),
        Chain("prapor", 4, QuestType.Kill, 15, "Война",
            "Прапор больше не шутит. Пятнадцать целей — и он даст вам кое-что серьёзное, а заодно подскажет дорогу на завод.",
            2500, 250, requires: "prapor_3", rewards: "akm", unlock: "factory"),

        Chain("therapist", 1, QuestType.Survive, 1, "Первый пациент",
            "Медик просит просто вернуться живым. Ему нужны живые люди, а не герои.",
            400, 60),
        Chain("therapist", 2, QuestType.Fetch, 3, "Перевязка",
            "Бинтов не хватает. Принесите три штуки — и он перевяжет вас бесплатно.",
            500, 80, itemCode: "bandage", requires: "therapist_1"),
        Chain("therapist", 3, QuestType.Fetch, 2, "Аптечка",
            "Для тяжёлых нужны настоящие аптечки. Две штуки — и он будет вам должен.",
            900, 120, itemCode: "medkit", requires: "therapist_2", rewards: "antirad"),
        Chain("therapist", 4, QuestType.Survive, 3, "Карантин",
            "В городе вспышка. Медик просит пережить три рейда — и покажет безопасную дорогу на окраины.",
            1500, 220, requires: "therapist_3", rewards: "medkit", unlock: "outskirts"),

        Chain("ragman", 1, QuestType.Fetch, 3, "Материал для шитья",
            "Старьёвщик чинит снаряжение и просит ткань. Три куска — и он возьмётся за ваш рюкзак.",
            350, 60, itemCode: "cloth"),
        Chain("ragman", 2, QuestType.Fetch, 2, "Провода для ремонта",
            "Без проводов не работает ни одна застёжка. Принесите два мотка.",
            550, 90, itemCode: "wire", requires: "ragman_1", rewards: "jacket"),
        Chain("ragman", 3, QuestType.Fetch, 1, "Рюкзак",
            "У одного из клиентов пропал рюкзак. Найдите замену.",
            900, 140, itemCode: "backpack", requires: "ragman_2", rewards: "vest"),
        Chain("ragman", 4, QuestType.Survive, 2, "Клиент с характером",
            "Старьёвщик хочет убедиться, что вы доживёте до следующего заказа. За это он сдаст военную базу.",
            800, 180, requires: "ragman_3", rewards: "helmet_tactical", unlock: "military"),

        Chain("blacksmith", 1, QuestType.Fetch, 5, "Первая партия",
            "Кузнецу нужен металл. Пять кусков — и он покажет, на что способен.",
            400, 60, itemCode: "scrap"),
        Chain("blacksmith", 2, QuestType.Fetch, 2, "Аккумуляторы",
            "Из аккумуляторов получаются лучшие сплавы. Два — и заказ принят.",
            800, 100, itemCode: "battery", requires: "blacksmith_1", rewards: "helmet_ssh"),
        Chain("blacksmith", 3, QuestType.Fetch, 2, "Микросхемы",
            "Кузнец просит электронику. Говорит, для умных вещей.",
            1000, 140, itemCode: "electronics", requires: "blacksmith_2"),
        Chain("blacksmith", 4, QuestType.Fetch, 1, "Инструмент",
            "Набор инструментов нужен ему больше, чем вам. За него кузнец расскажет о лесной дороге.",
            1500, 220, itemCode: "toolkit", requires: "blacksmith_3", rewards: "vest_heavy", unlock: "forest"),

        Chain("buyer", 1, QuestType.Fetch, 5, "Стекло",
            "Скупщик берёт всё, что блестит. Пять осколков — и он в деле.",
            300, 50, itemCode: "glass"),
        Chain("buyer", 2, QuestType.Fetch, 1, "Часы",
            "Часы — это всегда валюта. Он найдёт покупателя.",
            500, 80, itemCode: "watch", requires: "buyer_1"),
        Chain("buyer", 3, QuestType.Fetch, 1, "Золото",
            "Золотая цепь в его руках превращается в деньги быстрее, чем вы успеете передумать.",
            800, 120, itemCode: "goldchain", requires: "buyer_2"),
        Chain("buyer", 4, QuestType.Fetch, 1, "Икона",
            "Икона без оклада, но с историей. За неё скупщик сдаст тропу в пригород.",
            1200, 180, itemCode: "icon", requires: "buyer_3", unlock: "suburb"),

        Chain("prapor", 2, QuestType.Kill, 8, "Зачистка",
            "В городе расплодилось отребье. Прапор просит навести порядок.",
            900, 100, requires: "prapor_1", rewards: "wpn_glock19"),
        Chain("prapor", 3, QuestType.Fetch, 1, "Трофей",
            "Прапор хочет вернуть пистолет, который потерял в рейде. Принесите Glock 19.",
            1200, 120, itemCode: "wpn_glock19", requires: "prapor_2", rewards: "wpn_mp133"),
        Chain("prapor", 4, QuestType.Kill, 15, "Война",
            "Прапор больше не шутит. Пятнадцать целей — и он даст вам кое-что серьёзное, а заодно подскажет дорогу на завод.",
            2500, 250, requires: "prapor_3", rewards: "wpn_ak74n", unlock: "factory"),
        Chain("prapor", 5, QuestType.Kill, 25, "Арсенал",
            "Прапор хочет видеть, как вы работаете. Двадцать пять целей — и он отдаст АКМ.",
            4000, 400, requires: "prapor_4", rewards: "wpn_akm"),

        Chain("ragman", 5, QuestType.Survive, 3, "Экипировка",
            "Старьёвщик проверит, как вы держите удар. Три рейда — и он отдаст MP5A3.",
            2000, 300, requires: "ragman_4", rewards: "wpn_mp5a3"),

        Chain("blacksmith", 5, QuestType.Fetch, 3, "Мастер",
            "Кузнец собирает заказ. Три микросхемы — и он отдаст M4A1.",
            2500, 350, itemCode: "electronics", requires: "blacksmith_4", rewards: "wpn_m4a1"),

        Chain("therapist", 5, QuestType.Fetch, 3, "Долг жизни",
            "Медик просит три аптечки. За это отдаст UMP45.",
            2200, 320, itemCode: "medkit", requires: "therapist_4", rewards: "wpn_ump45"),

        Chain("buyer", 5, QuestType.Fetch, 1, "Коллекционер",
            "Скупщик хочет артефакт. За него отдаст СВД.",
            3000, 400, itemCode: "artifact", requires: "buyer_4", rewards: "wpn_svd"),
    };

    // ============================================================
    // ЕЖЕДНЕВНЫЕ ЗАДАНИЯ
    // ============================================================
    public List<Quest> CreateDailies() => new()
    {
        Daily("prapor", 1, QuestType.Kill, 4, null, "Вечерняя зачистка",
            "Прапор платит за каждый вечер без стрельбы. Четыре цели — и он доволен.", 350, 50),
        Daily("prapor", 2, QuestType.Kill, 6, null, "Ночной патруль",
            "Ночью город принадлежит не вам. Докажите обратное.", 500, 70),
        Daily("prapor", 3, QuestType.Survive, 1, null, "Живой свидетель",
            "Просто вернитесь живым. Прапор хочет видеть вас завтра.", 300, 40),

        Daily("therapist", 1, QuestType.Fetch, 3, "bandage", "Бинты",
            "Медик снова без бинтов. Принесите три штуки.", 250, 40),
        Daily("therapist", 2, QuestType.Fetch, 2, "painkiller", "Обезболивающее",
            "Раненые не спят от боли. Две упаковки — и ночь пройдёт тихо.", 350, 50),
        Daily("therapist", 3, QuestType.Survive, 1, null, "Осмотр",
            "Медик просит вернуться целым. Ему надоело штопать вас.", 300, 40),

        Daily("ragman", 1, QuestType.Fetch, 4, "cloth", "Ткань на продажу",
            "Ткани много не бывает. Четыре куска — и он даст скидку.", 300, 40),
        Daily("ragman", 2, QuestType.Fetch, 2, "wire", "Проводка",
            "Провода нужны для ремонта снаряжения. Два мотка.", 350, 50),
        Daily("ragman", 3, QuestType.Fetch, 1, "rubber", "Резина",
            "Резина — расходник. Один кусок, и он ваш должник.", 300, 40),

        Daily("blacksmith", 1, QuestType.Fetch, 6, "scrap", "Металлолом",
            "Кузнец всегда голоден до металла. Шесть кусков.", 400, 50),
        Daily("blacksmith", 2, QuestType.Fetch, 1, "battery", "Аккумулятор",
            "Один аккумулятор — и он сделает вам скидку.", 450, 60),
        Daily("blacksmith", 3, QuestType.Fetch, 2, "cement", "Цемент",
            "Кузнец расширяет мастерскую. Два мешка цемента.", 350, 40),

        Daily("buyer", 1, QuestType.Fetch, 6, "glass", "Стекло",
            "Скупщик скупает стекло ведрами. Шесть осколков.", 300, 40),
        Daily("buyer", 2, QuestType.Fetch, 1, "rubber", "Резина",
            "Резина уходит на переработку. Один кусок.", 250, 30),
        Daily("buyer", 3, QuestType.Fetch, 1, "amber", "Янтарь",
            "Куски янтаря он продаёт втридорога. Найдите один.", 500, 60),
    };

    // ============================================================
    // ПОВТОРЯЕМЫЕ КВЕСТЫ (торговцы — можно выполнять и выполнять)
    // ============================================================
    public List<Quest> CreateRepeatableQuests() => new()
    {
        Repeatable("prapor", 1, QuestType.Kill, 3, "Ежедневная зачистка",
            "Прапор снова шлёт вас на засаду. Три цели — и он снова в хорошем настроении.",
            300, 40),
        Repeatable("therapist", 1, QuestType.Fetch, 2, "Бинты",
            "Медик без бинтов. Принесите два — и он снова перевяжет кого-нибудь.",
            200, 30, itemCode: "bandage"),
        Repeatable("ragman", 1, QuestType.Fetch, 2, "Ткань",
            "Старьёвщик шьёт новые карманы. Две куска ткани — и он снова ваш.",
            200, 30, itemCode: "cloth"),
        Repeatable("blacksmith", 1, QuestType.Fetch, 3, "Металл",
            "Кузнец голоден до металла. Три куска — и он снова за работу.",
            250, 40, itemCode: "scrap"),
        Repeatable("buyer", 1, QuestType.Fetch, 1, "Золото",
            "Скупщик крутится на золоте. Один кусок — и он снова за вас.",
            300, 50, itemCode: "goldchain"),
    };

    // ============================================================
    // ХЕЛПЕРЫ
    // ============================================================
    private Quest Story(int chapter, int order, QuestType type, string title, string desc,
        int target, int money, int xp, string? itemCode = null, string? unlock = null,
        bool boss = false, params string[] rewardItems)
    {
        var reward = new QuestReward { Money = money, Xp = xp, UnlockLocation = unlock };
        foreach (var code in rewardItems) reward.Items.Add(_catalog.Create(code));

        return new Quest
        {
            Id = $"story_{chapter}_{order}",
            Chapter = chapter,
            Order = order,
            IsStory = true,
            Type = type,
            Title = title,
            Description = desc,
            TargetCount = target,
            TargetItemCode = itemCode,
            TargetItemName = itemCode == null ? null : _catalog.Get(itemCode)?.Name,
            TargetBoss = boss,
            Reward = reward,
        };
    }

    private Quest Chain(string trader, int order, QuestType type, int target, string title,
        string desc, int money, int xp, string? itemCode = null, string? requires = null,
        string? unlock = null, params string[] rewards)
    {
        var reward = new QuestReward { Money = money, Xp = xp, UnlockLocation = unlock };
        foreach (var code in rewards) reward.Items.Add(_catalog.Create(code));

        return new Quest
        {
            Id = $"{trader}_{order}",
            TraderId = trader,
            Order = order,
            Type = type,
            Title = title,
            Description = desc,
            TargetCount = target,
            TargetItemCode = itemCode,
            TargetItemName = itemCode == null ? null : _catalog.Get(itemCode)?.Name,
            RequiresQuestId = requires,
            Reward = reward,
        };
    }

    private Quest Daily(string trader, int idx, QuestType type, int target, string? itemCode,
        string title, string desc, int money, int xp)
    {
        return new Quest
        {
            Id = $"{trader}_daily_{idx}",
            TraderId = trader,
            Type = type,
            Title = title,
            Description = desc,
            TargetCount = target,
            TargetItemCode = itemCode,
            TargetItemName = itemCode == null ? null : _catalog.Get(itemCode)?.Name,
            IsRepeatable = true,
            IsDaily = true,
            Reward = new QuestReward { Money = money, Xp = xp },
        };
    }

    private Quest Repeatable(string trader, int order, QuestType type, int target, string title,
        string desc, int money, int xp, string? itemCode = null)
    {
        return new Quest
        {
            Id = $"{trader}_repeat_{order}",
            TraderId = trader,
            Order = order,
            Type = type,
            Title = title,
            Description = desc,
            TargetCount = target,
            TargetItemCode = itemCode,
            TargetItemName = itemCode == null ? null : _catalog.Get(itemCode)?.Name,
            IsRepeatable = true,
            Reward = new QuestReward { Money = money, Xp = xp },
        };
    }

    public Quest? GetCurrentStory(List<Quest> story) =>
        story.Where(q => !q.IsCompleted).OrderBy(q => q.Chapter).ThenBy(q => q.Order).FirstOrDefault();
}
