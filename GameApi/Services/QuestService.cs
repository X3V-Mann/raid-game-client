using GameApi.Models;

namespace GameApi.Services;

/// <summary>
/// v7.5: сюжет + цепочки торговцев + доска объявлений (побочные/срочные).
/// Генерирует структурированный набор заданий (основа под 100 сюжетных включая торговцев и 80+ побочных).
/// </summary>
public class QuestService
{
    public const string
        IcoTalk = "/task/Поговорить_С_Торговцем.png",
        IcoGo = "/task/Отправиться_В_Место.png",
        IcoScout = "/task/Разведать_Местность.png",
        IcoItem = "/task/Принести_Вещь.png",
        IcoDocs = "/task/Принести_Документы.png",
        IcoFlash = "/task/Пренести_Флешку.png",
        IcoNotes = "/task/Принести_Записи.png",
        IcoTools = "/task/Принести_Инструменты.png",
        IcoMats = "/task/Принести_Материалы.png",
        IcoArt = "/task/Принести_Артефакт.png",
        IcoKillHuman = "/task/Убить_Цель(Человек).png",
        IcoKillMut = "/task/Убить_Цель(Мутант).png";

    private static readonly (string Id, string Name, int Tier)[] Locs = {
        ("city","Город",1),("kordon","Кордон",1),("factory","Завод",2),("mall","Торговый центр",3),
        ("military","Военная база",4),("predbannik","Предбанник",5),("forest","Лес",6),
        ("outskirts","Окраины города",7),("suburb","Пригород",8),("gradirni","Градирни",9)
    };

    private static Quest Q(string id, string title, string desc, string icon, QuestType type, QuestCategory cat,
        int chapter = 0, int order = 0, string? trader = null, string? req = null,
        string? itemCode = null, string? itemName = null, int count = 1,
        string? loc = null, string? locName = null, string? bkind = null,
        bool boss = false, QuestTimeOfDay tod = QuestTimeOfDay.Any,
        bool urgent = false, int urgentMins = 0,
        int money = 0, int xp = 0, string? unlockLoc = null, string? unlockTrader = null,
        bool story = false, string? note = null, bool repeatable = false,
        string? npc = null, string? enemy = null) => new()
    {
        Id = id, Title = title, Description = desc, Icon = icon, Type = type, Category = cat,
        Chapter = chapter, Order = order, TraderId = trader, RequiresQuestId = req,
        TargetItemCode = itemCode, TargetItemName = itemName, TargetCount = count,
        TargetLocationId = loc, TargetLocationName = locName, TargetBuildingKind = bkind,
        TargetEnemyName = enemy, TargetNpcName = npc,
        TargetBoss = boss, TimeOfDay = tod, IsUrgent = urgent, UrgentDurationMinutes = urgentMins,
        Reward = new QuestReward { Money = money, Xp = xp, UnlockLocation = unlockLoc, UnlockTrader = unlockTrader },
        IsStory = story, IsRepeatable = repeatable, JournalNote = note,
    };

    public List<Quest> CreateStory()
    {
        var list = new List<Quest>();
        int o;

        // Глава 1: Пробуждение (открытие торговцев, поиск груза, завод)
        o = 1;
        list.Add(Q("s1_01","Пробуждение","Очнуться в подвале-убежище. Осмотреться и прийти в себя.",IcoScout,QuestType.Survive,QuestCategory.Story,1,o++,story:true,count:1,money:50,xp:20,note:"Голова раскалывается. Подвал... моё убежище? Память пустая."));
        list.Add(Q("s1_02","Знакомый бармен","Поговорить с Барменом — он один из тех, кого я помню.",IcoTalk,QuestType.Talk,QuestCategory.Story,1,o++,req:"s1_01",story:true,count:1,money:80,xp:30,note:"Бармен узнаёт меня. Говорит, что я вляпался во что-то серьёзное.",trader:"bartender"));
        list.Add(Q("s1_03","Разведка обстановки","Сходить в Город и разведать обстановку. Выйти живым.",IcoGo,QuestType.ExtractAlive,QuestCategory.Story,1,o++,req:"s1_02",story:true,count:1,loc:"city",locName:"Город",money:120,xp:50,note:"Город всё такой же мёртвый. Но кто-то явно искал меня."));
        list.Add(Q("s1_04","Скупщик знает больше","Поговорить со Скупщиком о пропаже снаряги и груза.",IcoTalk,QuestType.Talk,QuestCategory.Story,1,o++,req:"s1_03",story:true,count:1,money:80,xp:30,trader:"buyer"));
        list.Add(Q("s1_05","След на Кордоне","Найти утерянный груз или его следы на Кордоне.",IcoItem,QuestType.Fetch,QuestCategory.Story,1,o++,req:"s1_04",story:true,count:1,itemCode:"quest_notes",itemName:"Записи выжившего",loc:"kordon",locName:"Кордон",money:200,xp:80,note:"В записях — обрывки маршрута. Груз уводили куда-то дальше."));
        list.Add(Q("s1_06","Доверие Скупщика","Выполнить поручение Скупщика: принести ценные находки.",IcoMats,QuestType.Fetch,QuestCategory.Story,1,o++,req:"s1_05",story:true,count:3,itemCode:"scrap",itemName:"Металлолом",money:150,xp:60,unlockTrader:"prapor"));
        list.Add(Q("s1_07","Новый контакт: Прапор","Поговорить с Прапором — Скупщик дал наводку.",IcoTalk,QuestType.Talk,QuestCategory.Story,1,o++,req:"s1_06",story:true,count:1,trader:"prapor",money:100,xp:40));
        list.Add(Q("s1_08","Слухи о Заводе","Побывать на Кордоне и разведать путь к Заводу.",IcoScout,QuestType.VisitLocation,QuestCategory.Story,1,o++,req:"s1_07",story:true,count:1,loc:"kordon",locName:"Кордон",money:120,xp:50));
        list.Add(Q("s1_09","Путь на Завод","Выжить в рейде на Кордоне — Бармен обещал открыть путь.",IcoGo,QuestType.ExtractAlive,QuestCategory.Story,1,o++,req:"s1_08",story:true,count:1,loc:"kordon",locName:"Кордон",money:180,xp:70));
        list.Add(Q("s1_10","Дорога к истине","Завершить первую главу: добраться до Завода.",IcoGo,QuestType.VisitLocation,QuestCategory.Story,1,o++,req:"s1_09",story:true,count:1,loc:"factory",locName:"Завод",unlockLoc:"factory",money:300,xp:120,note:"Завод. Здесь кто-то знает, что стало с грузом."));

        // Глава 2: Завод и секреты
        o = 1;
        list.Add(Q("s2_01","Обследование цехов","Зайти в здание на Заводе и осмотреться.",IcoScout,QuestType.EnterBuilding,QuestCategory.Story,2,o++,req:"s1_10",story:true,count:1,loc:"factory",locName:"Завод",money:150,xp:60));
        list.Add(Q("s2_02","Тени на Заводе","Убить 5 врагов на Заводе.",IcoKillHuman,QuestType.Kill,QuestCategory.Story,2,o++,req:"s2_01",story:true,count:5,loc:"factory",locName:"Завод",money:250,xp:100));
        list.Add(Q("s2_03","Правительственный след","Найти документы с завода.",IcoDocs,QuestType.Fetch,QuestCategory.Story,2,o++,req:"s2_02",story:true,count:1,itemCode:"quest_factory_docs",itemName:"Документы с завода",loc:"factory",locName:"Завод",money:350,xp:140,note:"Правительственный груз. Секретная разработка. Это уже не просто пропажа."));
        list.Add(Q("s2_04","Доложить Бармену","Поговорить с Барменом о найденных документах.",IcoTalk,QuestType.Talk,QuestCategory.Story,2,o++,req:"s2_03",story:true,count:1,trader:"bartender",money:120,xp:60));
        list.Add(Q("s2_05","Цепочка Прапора","Выполнить задание Прапора, чтобы получить доступ дальше.",IcoItem,QuestType.Fetch,QuestCategory.Story,2,o++,req:"s2_04",story:true,count:2,itemCode:"wire",itemName:"Провода",trader:"prapor",money:180,xp:70,unlockTrader:"blacksmith"));
        list.Add(Q("s2_06","Кузнец подключается","Поговорить с Кузнецом.",IcoTalk,QuestType.Talk,QuestCategory.Story,2,o++,req:"s2_05",story:true,count:1,trader:"blacksmith",money:100,xp:50,unlockTrader:"therapist"));
        list.Add(Q("s2_07","Медик и Старьёвщик","Поговорить с Медиком — Кузнец дал рекомендацию.",IcoTalk,QuestType.Talk,QuestCategory.Story,2,o++,req:"s2_06",story:true,count:1,trader:"therapist",money:100,xp:50,unlockTrader:"ragman"));
        list.Add(Q("s2_08","Экипировка Старьёвщика","Поговорить со Старьёвщиком и получить поддержку.",IcoTalk,QuestType.Talk,QuestCategory.Story,2,o++,req:"s2_07",story:true,count:1,trader:"ragman",money:100,xp:50));
        list.Add(Q("s2_09","Ночной рейд","Убить 3 врагов на Заводе ночью.",IcoKillHuman,QuestType.Kill,QuestCategory.Story,2,o++,req:"s2_08",story:true,count:3,loc:"factory",locName:"Завод",tod:QuestTimeOfDay.Night,money:300,xp:120));
        list.Add(Q("s2_10","Охотник на босса: Завод","Бармен просит убрать Цеховика — бригадира наёмников на Заводе. Убить босса.",IcoKillHuman,QuestType.KillBoss,QuestCategory.Story,2,o++,req:"s2_09",story:true,count:1,loc:"factory",locName:"Завод",boss:true,unlockLoc:"mall",money:500,xp:200,note:"Кто-то явно охранял эти документы. Наёмники?"));
        list.Add(Q("s2_10_loot","КПК Цеховика","Обыскать Цеховика и забрать его КПК с данными о грузе. Принести Бармену.",IcoFlash,QuestType.Fetch,QuestCategory.Story,2,o++,req:"s2_10",story:true,count:1,itemCode:"boss_pda_factory",itemName:"КПК Цеховика",loc:"factory",locName:"Завод",money:350,xp:150,note:"В КПК — маршруты и имена. След ведёт дальше."));

        // Глава 3: Наёмники
        o = 1;
        list.Add(Q("s3_01","След наёмников","Найти флешку наёмников на Военной базе или в Торговом центре.",IcoFlash,QuestType.Fetch,QuestCategory.Story,3,o++,req:"s2_10_loot",story:true,count:1,itemCode:"quest_merc_flash",itemName:"Флешка наёмников",loc:"mall",locName:"Торговый центр",money:300,xp:130));
        list.Add(Q("s3_02","Засада","Выжить в рейде на Военную базу.",IcoGo,QuestType.ExtractAlive,QuestCategory.Story,3,o++,req:"s3_01",story:true,count:1,loc:"military",locName:"Военная база",unlockLoc:"military",money:350,xp:150));
        list.Add(Q("s3_03","Чистка периметра","Убить 8 врагов на Военной базе.",IcoKillHuman,QuestType.Kill,QuestCategory.Story,3,o++,req:"s3_02",story:true,count:8,loc:"military",locName:"Военная база",money:400,xp:170));
        list.Add(Q("s3_04","Босс: Военная база","Прапор просит убрать полковника Громова на Военной базе и забрать его документы/данные. Убить босса.",IcoKillHuman,QuestType.KillBoss,QuestCategory.Story,3,o++,req:"s3_03",story:true,count:1,loc:"military",locName:"Военная база",boss:true,unlockLoc:"predbannik",money:600,xp:240));
        list.Add(Q("s3_04_loot","КПК Громова","Забрать КПК полковника Громова — в нём могут быть военные коды к объекту.",IcoFlash,QuestType.Fetch,QuestCategory.Story,3,o++,req:"s3_04",story:true,count:1,itemCode:"boss_pda_military",itemName:"КПК полковника Громова",loc:"military",locName:"Военная база",money:400,xp:170));
        list.Add(Q("s3_05","Предбанник","Побывать на локации Предбанник.",IcoGo,QuestType.VisitLocation,QuestCategory.Story,3,o++,req:"s3_04_loot",story:true,count:1,loc:"predbannik",locName:"Предбанник",money:250,xp:120));
        list.Add(Q("s3_06","Босс: Предбанник","Скупщик просит разобраться с Привратником и забрать его флешку с пропусками. Убить босса.",IcoKillHuman,QuestType.KillBoss,QuestCategory.Story,3,o++,req:"s3_05",story:true,count:1,loc:"predbannik",locName:"Предбанник",boss:true,unlockLoc:"forest",money:650,xp:260));
        list.Add(Q("s3_06_loot","Флешка Привратника","Забрать флешку Привратника с журналом доступа.",IcoFlash,QuestType.Fetch,QuestCategory.Story,3,o++,req:"s3_06",story:true,count:1,itemCode:"boss_flash_predbannik",itemName:"Флешка Привратника",loc:"predbannik",locName:"Предбанник",money:420,xp:180));
        list.Add(Q("s3_07","Лесные тропы","Разведать Лес и найти следы груза.",IcoScout,QuestType.EnterBuilding,QuestCategory.Story,3,o++,req:"s3_06_loot",story:true,count:1,loc:"forest",locName:"Лес",money:300,xp:140));
        list.Add(Q("s3_08","Босс: Лес","Бармен просит разобраться с Лешим в Лесу — и забрать найденный у него КПК. Убить босса.",IcoKillMut,QuestType.KillBoss,QuestCategory.Story,3,o++,req:"s3_07",story:true,count:1,loc:"forest",locName:"Лес",boss:true,unlockLoc:"outskirts",money:650,xp:260));
        list.Add(Q("s3_08_loot","КПК Лешего","Забрать КПК, найденный у Лешего. Возможно, он принадлежал пропавшему курьеру.",IcoFlash,QuestType.Fetch,QuestCategory.Story,3,o++,req:"s3_08",story:true,count:1,itemCode:"boss_pda_forest",itemName:"КПК Лешего",loc:"forest",locName:"Лес",money:420,xp:180));
        list.Add(Q("s3_09","Окраины","Выжить в рейде на Окраины города.",IcoGo,QuestType.ExtractAlive,QuestCategory.Story,3,o++,req:"s3_08_loot",story:true,count:1,loc:"outskirts",locName:"Окраины города",money:400,xp:180));
        list.Add(Q("s3_10","Босс: Окраины","Прапор просит устранить Ворона — командира патруля на Окраинах — и забрать его флешку с отчётами.",IcoKillHuman,QuestType.KillBoss,QuestCategory.Story,3,o++,req:"s3_09",story:true,count:1,loc:"outskirts",locName:"Окраины города",boss:true,unlockLoc:"suburb",money:700,xp:280));
        list.Add(Q("s3_10_loot","Флешка Ворона","Забрать флешку Ворона с отчётами об охране периметра.",IcoFlash,QuestType.Fetch,QuestCategory.Story,3,o++,req:"s3_10",story:true,count:1,itemCode:"boss_flash_outskirts",itemName:"Флешка Ворона",loc:"outskirts",locName:"Окраины города",money:450,xp:190));

        // Глава 4: Финал
        o = 1;
        list.Add(Q("s4_01","Пригород","Побывать в Пригороде — там следы охраны объекта.",IcoGo,QuestType.VisitLocation,QuestCategory.Story,4,o++,req:"s3_10_loot",story:true,count:1,loc:"suburb",locName:"Пригород",money:400,xp:180));
        list.Add(Q("s4_02","Босс: Пригород","Бармен просит убрать Палача — командира ЧВК в Пригороде — и забрать его КПК с приказами.",IcoKillHuman,QuestType.KillBoss,QuestCategory.Story,4,o++,req:"s4_01",story:true,count:1,loc:"suburb",locName:"Пригород",boss:true,unlockLoc:"gradirni",money:800,xp:320));
        list.Add(Q("s4_02_loot","КПК Палача","Забрать КПК Палача с приказами и координатами объекта.",IcoFlash,QuestType.Fetch,QuestCategory.Story,4,o++,req:"s4_02",story:true,count:1,itemCode:"boss_pda_suburb",itemName:"КПК Палача",loc:"suburb",locName:"Пригород",money:500,xp:220));
        list.Add(Q("s4_03","Ключ от бункера","Найти ключ от бункера на Градирнях/в Пригороде.",IcoItem,QuestType.Fetch,QuestCategory.Story,4,o++,req:"s4_02_loot",story:true,count:1,itemCode:"quest_bunker_key",itemName:"Ключ от бункера",money:500,xp:220,note:"Ключ есть. Осталось решить, что делать дальше."));
        list.Add(Q("s4_04","Градирни","Выйти живым с локации Градирни.",IcoGo,QuestType.ExtractAlive,QuestCategory.Story,4,o++,req:"s4_03",story:true,count:1,loc:"gradirni",locName:"Градирни",money:600,xp:260));
        list.Add(Q("s4_05","Босс: Градирни","Бармен просит устранить Куратора — главу охраны Градирней — и забрать его флешку. Это последний ключ.",IcoKillHuman,QuestType.KillBoss,QuestCategory.Story,4,o++,req:"s4_04",story:true,count:1,loc:"gradirni",locName:"Градирни",boss:true,money:1000,xp:400));
        list.Add(Q("s4_05_loot","Флешка Куратора","Забрать флешку Куратора. На ней — последние тайны объекта и протоколы.",IcoFlash,QuestType.Fetch,QuestCategory.Story,4,o++,req:"s4_05",story:true,count:1,itemCode:"boss_flash_gradirni",itemName:"Флешка Куратора",loc:"gradirni",locName:"Градирни",money:700,xp:280));
        list.Add(Q("s4_06","Точка невозврата","Поговорить с Барменом перед финалом.",IcoTalk,QuestType.Talk,QuestCategory.Story,4,o++,req:"s4_05_loot",story:true,count:1,trader:"bartender",money:200,xp:100,note:"Красная кнопка. Две дороги."));
        list.Add(Q("s4_07","Выбор: Градирни","Принять решение на Градирнях. Взорвать ядерный заряд (концовка, заглушка DLC) или спрятать бомбу и уйти в свободную игру. Спрайт заряда: /blocks/atomic_bomb_for_final.png",IcoScout,QuestType.Talk,QuestCategory.Story,4,o++,req:"s4_06",story:true,count:1,loc:"gradirni",locName:"Градирни",money:1500,xp:600,note:"Красная кнопка или тайник. Выбор за мной."));
        list.Add(Q("story_9_10","Эпилог","Свободная игра: зона всё ещё жива.",IcoGo,QuestType.Survive,QuestCategory.Story,4,o++,req:"s4_07",story:true,count:999999,money:0,xp:0));

        // Дополнительные сюжетные задания (чтобы база была богаче; расширяется до 100 вместе с торговыми цепочками)
        var fillers = new (string id,string t,string d,string icon,QuestType qt,int ch,string req,int cnt,string? code,string? iname,string? loc,string? ln)[]
        {
            ("s1_f1","Запасы в убежище","Принести ткань для обустройства.",IcoMats,QuestType.Fetch,1,"s1_02",4,"cloth","Ткань",null,null),
            ("s1_f2","Проверка слуха","Убить 3 мутантов в Городе.",IcoKillMut,QuestType.Kill,1,"s1_03",3,null,null,"city","Город"),
            ("s1_f3","Первые находки","Принести болты с Кордона.",IcoMats,QuestType.Fetch,1,"s1_05",5,"bolts","Болты","kordon","Кордон"),
            ("s2_f1","Инструменты Прапора","Принести набор отвёрток.",IcoTools,QuestType.Fetch,2,"s2_05",1,"screwdriver_set","Набор отвёрток",null,null),
            ("s2_f2","Зачистка склада","Зайти в здание на Заводе.",IcoScout,QuestType.EnterBuilding,2,"s2_01",1,null,null,"factory","Завод"),
            ("s3_f1","Разведка ТЦ","Побывать в Торговом центре.",IcoGo,QuestType.VisitLocation,3,"s2_10",1,null,null,"mall","Торговый центр"),
            ("s3_f2","Трофеи наёмников","Принести провода из рейда.",IcoMats,QuestType.Fetch,3,"s3_01",6,"wire","Провода",null,null),
            ("s4_f1","Финальная подготовка","Принести конденсаторы.",IcoMats,QuestType.Fetch,4,"s4_02",4,"capacitors","Конденсаторы",null,null),
        };
        int fo = 50;
        foreach (var f in fillers)
            list.Add(Q(f.id,f.t,f.d,f.icon,f.qt,QuestCategory.Story,f.ch,fo++,req:f.req,story:true,count:f.cnt,itemCode:f.code,itemName:f.iname,loc:f.loc,locName:f.ln,money:100+f.ch*40,xp:40+f.ch*20));

        // Сюжетные встречи с NPC (скриптовые персонажи на локациях) — вдохновлено архетипами STALKER/Tarkov (оригинальные задания)
        var npcs = new (string id,string t,string d,string icon,QuestType qt,int ch,string req,int cnt,string? loc,string? ln,string? npc,string? code,string? iname,string? note)[]
        {
            ("npc_talk_boryan","Курьер в городе","Найти Боряна в Городе или на Кордоне и поговорить. Он может знать о пропаже груза.",IcoTalk,QuestType.Talk,1,"s1_03",1,"city","Город","Борян",null,null,"Борян видел подозрительных людей у Кордона."),
            ("npc_fetch_boryan","Записка Боряна","Получить записку от Боряна (поговорите с ним в рейде).",IcoNotes,QuestType.Fetch,1,"npc_talk_boryan",1,"city","Город",null,"q_note","Записка",null),
            ("npc_talk_varshava","Радиомолчание","Поговорить с Варшавой на Кордоне/Заводе — у него есть разведданные.",IcoTalk,QuestType.Talk,2,"s2_01",1,"kordon","Кордон","Варшава",null,null,null),
            ("npc_talk_doctor","Медицинский след","Найти Доктора в Городе или ТЦ. Ему передавали странные документы.",IcoTalk,QuestType.Talk,1,"s1_04",1,"mall","Торговый центр","Доктор",null,null,null),
            ("npc_talk_klav","Ключ к данным","Поговорить с Клавишником на Заводе/Военной базе о документации.",IcoTalk,QuestType.Talk,2,"s2_02",1,"factory","Завод","Клавишник",null,null,null),
            ("npc_talk_marshin","Майор и кейс","Найти Майора Маршина на Военной базе/Окраинах и получить закрытый кейс.",IcoTalk,QuestType.Talk,3,"s3_03",1,"military","Военная база","Майор",null,null,"Маршин намекнул, что наёмники — лишь исполнители."),
            ("npc_fetch_marshin","Закрытый кейс","Принести закрытый кейс от Майора Маршина.",IcoItem,QuestType.Fetch,3,"npc_talk_marshin",1,"military","Военная база",null,"q_closed_case","Закрытый кейс",null),
            ("npc_talk_nemets","Стальной груз","Поговорить с Немцем в ТЦ/на Окраинах. Он охранял часть груза.",IcoTalk,QuestType.Talk,3,"s3_01",1,"mall","Торговый центр","Немец",null,null,null),
            ("npc_talk_ozzi","Курьер на Градирни","Найти Оззи в Пригороде/на Градирнях. Он доставит кейс с артефактами.",IcoTalk,QuestType.Talk,4,"s4_01",1,"suburb","Пригород","Оззи",null,null,null),
            ("npc_talk_ucheniy","Слова Учёного","Поговорить с Учёным в Лесу/Предбаннике об артефактах и объекте.",IcoTalk,QuestType.Talk,3,"s3_06",1,"forest","Лес","Учёный",null,null,"«Капля» — не просто камень. Это ключ."),
            ("s2_boss_city","Ликвидация: Шрам","Скупщик просит убрать Шрама в Городе и забрать его КПК. Убить босса.",IcoKillHuman,QuestType.KillBoss,2,"s2_02",1,"city","Город",null,null,null,"Архетип: целеуказание (elimination/target)."),
            ("s2_boss_city_loot","КПК Шрама","Забрать КПК Шрама — в нём записи о поставках и контактах.",IcoFlash,QuestType.Fetch,2,"s2_boss_city",1,"city","Город",null,"boss_pda_city","КПК Шрама",null),
            ("s3_boss_mall","Ликвидация: Кассир","Бармен просит убрать Кассира в Торговом центре и забрать его флешку со списками тайников.",IcoKillHuman,QuestType.KillBoss,3,"s2_10_loot",1,"mall","Торговый центр",null,null,null,null),
            ("s3_boss_mall_loot","Флешка Кассира","Забрать флешку Кассира — там схемы связей мародёров.",IcoFlash,QuestType.Fetch,3,"s3_boss_mall",1,"mall","Торговый центр",null,"boss_flash_mall","Флешка Кассира",null),
            ("s1_night","Ночная смена","Выйти живым из Города ночью — проверка нервов.",IcoGo,QuestType.ExtractAlive,1,"s1_03",1,"city","Город",null,null,null,"Архетип: ночной рейд/выживание."),
            ("s3_urgent_docs","Срочно: пакет данных","СРОЧНО: найти флешку наёмников в ТЦ и успеть до истечения игрового времени.",IcoFlash,QuestType.Fetch,3,"s2_10_loot",1,"mall","Торговый центр",null,"quest_merc_flash","Флешка наёмников","Архетип: timed handover."),
        };
        int no = 70;
        foreach (var n in npcs)
        {
            var q = Q(n.id,n.t,n.d,n.icon,n.qt,QuestCategory.Story,n.ch,no++,req:n.req,story:true,count:n.cnt,loc:n.loc,locName:n.ln,npc:n.npc,itemCode:n.code,itemName:n.iname,note:n.note,money:120+n.ch*45,xp:45+n.ch*18);
            if (n.id=="s3_urgent_docs") { q.IsUrgent=true; q.UrgentDurationMinutes=900; }
            if (n.id=="s1_night") q.TimeOfDay = QuestTimeOfDay.Night;
            list.Add(q);
        }

        return list;
    }

    public List<Quest> CreateTraderQuests()
    {
        // Сюжетные/цепочки заданий торговцев (включаются в общий объём сюжетных), выдаются через Диалог.
        var list = new List<Quest>();
        void Chain(string trader, (string id,string t,string d,string icon,QuestType qt,int cnt,string? code,string? iname,string? loc,string? ln,bool boss,int money,int xp,string? ul,string? ut,string? note)[] steps)
        {
            string? prev = null;
            for (int i = 0; i < steps.Length; i++)
            {
                var s = steps[i];
                list.Add(Q(s.id, s.t, s.d, s.icon, s.qt, QuestCategory.Trader, 0, i+1, trader, prev,
                    s.code, s.iname, s.cnt, s.loc, s.ln, boss:s.boss,
                    money:s.money, xp:s.xp, unlockLoc:s.ul, unlockTrader:s.ut, story:true, note:s.note));
                prev = s.id;
            }
        }

        Chain("buyer", new (string id,string t,string d,string icon,QuestType qt,int cnt,string? code,string? iname,string? loc,string? ln,bool boss,int money,int xp,string? ul,string? ut,string? note)[]{
            ("buyer_1","Честный обмен","Принеси Скупщику лом — проверим, что ты живой.",IcoMats,QuestType.Fetch,3,"scrap","Металлолом",null,null,false,80,30,null,null,null),
            ("buyer_2","Глаза и уши","Побывай на Кордоне и вернись.",IcoGo,QuestType.ExtractAlive,1,null,null,"kordon","Кордон",false,120,50,null,null,null),
            ("buyer_3","Редкая безделушка","Принеси часы (из рейда).",IcoItem,QuestType.Fetch,1,"watch","Настольные часы",null,null,false,200,70,null,null,null),
            ("buyer_4","Наводка","Принеси записи выжившего — Скупщик кое-что вспомнит.",IcoNotes,QuestType.Fetch,1,"quest_notes","Записи выжившего",null,null,false,250,90,null,"prapor","Скупщик кивает: «Прапор тебя ждёт»."),
            ("buyer_5","Деловые связи","Убей 5 врагов в Городе.",IcoKillHuman,QuestType.Kill,5,null,null,"city","Город",false,220,80,null,null,null),
            ("buyer_6","Ценный груз","Принеси документы.",IcoDocs,QuestType.Fetch,1,"documents","Документы",null,null,false,300,100,null,null,null),
            ("buyer_7","Слухи об Окраинах","Побывай на Военной базе.",IcoGo,QuestType.VisitLocation,1,null,null,"military","Военная база",false,280,100,null,null,null),
            ("buyer_8","Последняя услуга","Принеси золотую цепь (из рейда).",IcoArt,QuestType.Fetch,1,"goldchain","Золотая цепь",null,null,false,500,150,null,null,null),
        });

        Chain("bartender", new (string id,string t,string d,string icon,QuestType qt,int cnt,string? code,string? iname,string? loc,string? ln,bool boss,int money,int xp,string? ul,string? ut,string? note)[]{
            ("bartender_1","Для своих","Принеси воду ×2 (из рейда).",IcoItem,QuestType.Fetch,2,"water","Вода",null,null,false,70,25,null,null,null),
            ("bartender_2","Разговор по душам","Выйди живым из Города.",IcoGo,QuestType.ExtractAlive,1,null,null,"city","Город",false,100,40,null,null,"Бармен: «Вспоминай осторожно. Тут за память убивают»."),
            ("bartender_3","Охота на мутантов","Принеси части мутантов (сало плоти).",IcoMats,QuestType.Fetch,2,"flesh_fat","Сало плоти",null,null,false,160,60,null,null,null),
            ("bartender_4","Чистка Кордона","Убей 4 мутантов на Кордоне.",IcoKillMut,QuestType.Kill,4,null,null,"kordon","Кордон",false,180,70,null,null,null),
            ("bartender_5","Важные бумаги","Принеси записи выжившего.",IcoNotes,QuestType.Fetch,1,"quest_notes","Записи выжившего",null,null,false,220,80,null,null,null),
            ("bartender_6","Дорога на Завод","Побывай на Заводе.",IcoGo,QuestType.VisitLocation,1,null,null,"factory","Завод",false,240,90,"factory",null,"Бармен дал наводку — Завод открыт."),
            ("bartender_7","Секреты цехов","Принеси документы с завода.",IcoDocs,QuestType.Fetch,1,"quest_factory_docs","Документы с завода",null,null,false,350,130,null,null,null),
            ("bartender_8","Ночной разговор","Убей 2 врагов ночью в Городе.",IcoKillHuman,QuestType.Kill,2,null,null,"city","Город",false,260,90,null,null,null),
            ("bartender_9","Перед бурей","Принеси тушёнку (из рейда) — запасы.",IcoItem,QuestType.Fetch,2,"canned","Тушёнка",null,null,false,200,80,null,null,null),
        });

        Chain("prapor", new (string id,string t,string d,string icon,QuestType qt,int cnt,string? code,string? iname,string? loc,string? ln,bool boss,int money,int xp,string? ul,string? ut,string? note)[]{
            ("prapor_1","Проверка бойца","Убей 3 врагов в Городе.",IcoKillHuman,QuestType.Kill,3,null,null,"city","Город",false,120,50,null,null,null),
            ("prapor_2","Металл для дела","Принеси металлолом ×4 (из рейда).",IcoMats,QuestType.Fetch,4,"scrap","Металлолом",null,null,false,150,60,null,null,null),
            ("prapor_3","Разведка Кордона","Выйди живым с Кордона.",IcoGo,QuestType.ExtractAlive,1,null,null,"kordon","Кордон",false,180,70,null,null,null),
            ("prapor_4","Оружейник","Принеси инструменты (плоскогубцы).",IcoTools,QuestType.Fetch,1,"pliers","Плоскогубцы",null,null,false,220,80,null,"blacksmith",null),
            ("prapor_5","Зачистка","Убей 6 врагов на Заводе.",IcoKillHuman,QuestType.Kill,6,null,null,"factory","Завод",false,320,120,null,null,null),
            ("prapor_6","Босс Завода","Убей босса на Заводе.",IcoKillHuman,QuestType.KillBoss,1,null,null,"factory","Завод",true,500,200,null,null,null),
            ("prapor_7","Тяжёлый металл","Принеси болты ×8 (из рейда).",IcoMats,QuestType.Fetch,8,"bolts","Болты",null,null,false,240,90,null,null,null),
            ("prapor_8","Контроль ТЦ","Побывай в Торговом центре.",IcoGo,QuestType.VisitLocation,1,null,null,"mall","Торговый центр",false,260,100,null,null,null),
        });

        Chain("blacksmith", new (string id,string t,string d,string icon,QuestType qt,int cnt,string? code,string? iname,string? loc,string? ln,bool boss,int money,int xp,string? ul,string? ut,string? note)[]{
            ("blacksmith_1","Сырьё","Принеси металлолом.",IcoMats,QuestType.Fetch,5,"scrap","Металлолом",null,null,false,100,40,null,null,null),
            ("blacksmith_2","Электрика","Принеси провода ×3 (из рейда).",IcoMats,QuestType.Fetch,3,"wire","Провода",null,null,false,140,55,null,null,null),
            ("blacksmith_3","Пайка","Принеси канифоль ×2 (из рейда).",IcoMats,QuestType.Fetch,2,"rosin","Канифоль",null,null,false,200,75,null,"therapist",null),
            ("blacksmith_4","Лесные ресурсы","Выйди живым из Леса.",IcoGo,QuestType.ExtractAlive,1,null,null,"forest","Лес",false,260,100,null,null,null),
            ("blacksmith_5","Платы","Принеси текстолит ×2 (из рейда).",IcoMats,QuestType.Fetch,2,"textolite","Текстолит",null,null,false,300,110,null,"ragman",null),
            ("blacksmith_6","Крепёж","Принеси гайки ×6 (из рейда).",IcoMats,QuestType.Fetch,6,"nuts","Гайки",null,null,false,220,85,null,null,null),
            ("blacksmith_7","Ночной заказ","Принеси электронику ночью (любой рейд ночью) — убей 1 врага ночью.",IcoKillHuman,QuestType.Kill,1,null,null,null,null,false,280,100,null,null,null),
            ("blacksmith_8","Мастер","Принеси набор инструментов.",IcoTools,QuestType.Fetch,1,"toolkit","Набор инструментов",null,null,false,400,140,null,null,null),
        });

        Chain("therapist", new (string id,string t,string d,string icon,QuestType qt,int cnt,string? code,string? iname,string? loc,string? ln,bool boss,int money,int xp,string? ul,string? ut,string? note)[]{
            ("therapist_1","Перевязка","Принеси бинты.",IcoItem,QuestType.Fetch,3,"bandage","Бинт",null,null,false,90,35,null,null,null),
            ("therapist_2","Травы? Нет, припасы","Принеси обезболивающее.",IcoItem,QuestType.Fetch,2,"painkiller","Обезболивающее",null,null,false,150,55,null,null,null),
            ("therapist_3","Выживший","Выйди живым из рейда на Кордоне.",IcoGo,QuestType.ExtractAlive,1,null,null,"kordon","Кордон",false,160,60,null,null,null),
            ("therapist_4","Полевой госпиталь","Принеси армейскую аптечку (из рейда).",IcoItem,QuestType.Fetch,1,"medkit_army","Армейская аптечка",null,null,false,300,110,null,null,null),
            ("therapist_5","Мутантная угроза","Принеси глаз плоти.",IcoMats,QuestType.Fetch,2,"flesh_eye","Глаз плоти",null,null,false,240,90,null,null,null),
            ("therapist_6","Окраины","Побывай на Окраинах города.",IcoGo,QuestType.VisitLocation,1,null,null,"outskirts","Окраины города",false,260,100,null,null,null),
            ("therapist_7","Срочная помощь","Принеси физраствор.",IcoItem,QuestType.Fetch,2,"saline","Физраствор",null,null,false,220,85,null,null,null),
            ("therapist_8","Долг врача","Убей 5 врагов на Военной базе.",IcoKillHuman,QuestType.Kill,5,null,null,"military","Военная база",false,380,140,null,null,null),
        });

        Chain("ragman", new (string id,string t,string d,string icon,QuestType qt,int cnt,string? code,string? iname,string? loc,string? ln,bool boss,int money,int xp,string? ul,string? ut,string? note)[]{
            ("ragman_1","Тряпьё","Принеси ткань.",IcoMats,QuestType.Fetch,5,"cloth","Ткань",null,null,false,90,35,null,null,null),
            ("ragman_2","Пошив","Принеси флисовую ткань.",IcoMats,QuestType.Fetch,3,"fleece_fabric","Флисовая ткань",null,null,false,180,65,null,null,null),
            ("ragman_3","Выход в город","Выйди живым из Города.",IcoGo,QuestType.ExtractAlive,1,null,null,"city","Город",false,150,55,null,null,null),
            ("ragman_4","Броня","Принеси резину ×4 (из рейда).",IcoMats,QuestType.Fetch,4,"rubber","Резина",null,null,false,200,75,null,null,null),
            ("ragman_5","Склад в ТЦ","Зайди в здание в Торговом центре.",IcoScout,QuestType.EnterBuilding,1,null,null,"mall","Торговый центр",false,240,90,null,null,null),
            ("ragman_6","Трофеи","Убей 6 врагов в Торговом центре.",IcoKillHuman,QuestType.Kill,6,null,null,"mall","Торговый центр",false,340,125,null,null,null),
            ("ragman_7","Походный набор","Принеси паракорд.",IcoMats,QuestType.Fetch,3,"paracord","Паракорд",null,null,false,220,85,null,null,null),
            ("ragman_8","Стиль выжившего","Побывай в Пригороде.",IcoGo,QuestType.VisitLocation,1,null,null,"suburb","Пригород",false,300,110,null,null,null),
        });

        return list;
    }

    private static readonly (string c, string n)[] SidePartFetch = { ("flesh_eye","Глаз плоти"),("flesh_fat","Сало плоти"),("boar_meat","Мясо кабана"),("boar_hoof","Копыто кабана") };
    private static readonly (string c, string n, string icon)[] SideMatFetch = {
        ("scrap","Металлолом",IcoMats),("wire","Провода",IcoMats),("cloth","Ткань",IcoMats),("bolts","Болты",IcoMats),
        ("capacitors","Конденсаторы",IcoMats),("resistors","Резисторы",IcoMats),("bandage","Бинт",IcoItem),("canned","Тушёнка",IcoItem),
        ("water","Вода",IcoItem),("mineral_water","Минеральная вода",IcoItem),("electronics","Микросхемы",IcoMats),("toolkit","Набор инструментов",IcoTools)
    };
    private static readonly string[] SideBuildingKinds = { "house","warehouse","shop","garage","military","hospital" };

    public Quest CreateRandomSideQuest(string traderId, IEnumerable<Quest>? existing = null)
    {
        var rng = Random.Shared;
        var ids = existing != null ? new HashSet<string>(existing.Select(q => q.Id), StringComparer.OrdinalIgnoreCase) : new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        string id;
        do { id = "side_r_" + Guid.NewGuid().ToString("N")[..10]; } while (ids.Contains(id));

        bool urgent = rng.NextDouble() < 0.22;
        int urgentMins = urgent ? (rng.Next(4, 10) * 60) : 0; // 4-9 игровых часов
        int kind = rng.Next(0, 6);
        var loc = Locs[rng.Next(Locs.Length)];
        Quest q;
        switch (kind)
        {
            case 0:
            {
                var pf = SidePartFetch[rng.Next(SidePartFetch.Length)];
                int cnt = rng.Next(1, 4);
                q = Q(id, "Поставка частей", $"Принести {pf.n} ×{cnt} (из рейда).", IcoMats, QuestType.Fetch, QuestCategory.Side, 0, rng.Next(1, 9999), traderId,
                    itemCode: pf.c, itemName: pf.n, count: cnt, urgent: urgent, urgentMins: urgentMins, money: 60 + cnt * 25, xp: 25 + cnt * 12);
                break;
            }
            case 1:
            {
                int cnt = 2 + loc.Tier + rng.Next(0, 3);
                bool night = rng.NextDouble() < 0.35;
                q = Q(id, "Зачистка территории", $"Убить {cnt} врагов на локации {loc.Name}{(night ? " ночью" : "")}.", IcoKillHuman, QuestType.Kill, QuestCategory.Side, 0, rng.Next(1, 9999), traderId,
                    count: cnt, loc: loc.Id, locName: loc.Name, tod: night ? QuestTimeOfDay.Night : QuestTimeOfDay.Any, urgent: urgent, urgentMins: urgentMins, money: 80 + loc.Tier * 25, xp: 35 + loc.Tier * 12);
                break;
            }
            case 2:
                q = Q(id, "Разведка и выход", $"Побывать на локации {loc.Name} и выйти живым.", IcoGo, QuestType.ExtractAlive, QuestCategory.Side, 0, rng.Next(1, 9999), traderId,
                    count: 1, loc: loc.Id, locName: loc.Name, urgent: urgent, urgentMins: urgentMins, money: 100 + loc.Tier * 20, xp: 40 + loc.Tier * 10);
                break;
            case 3:
                q = Q(id, "Охота на босса", $"Убить босса на локации {loc.Name}.", IcoKillHuman, QuestType.KillBoss, QuestCategory.Side, 0, rng.Next(1, 9999), traderId,
                    count: 1, loc: loc.Id, locName: loc.Name, boss: true, urgent: urgent, urgentMins: urgentMins, money: 250 + loc.Tier * 60, xp: 120 + loc.Tier * 25);
                break;
            case 4:
            {
                var bk = SideBuildingKinds[rng.Next(SideBuildingKinds.Length)];
                int cnt = 1 + rng.Next(0, 2);
                q = Q(id, "Обыск помещений", $"Зайти в здание и обыскать ({bk}) — {cnt} раз(а).", IcoScout, QuestType.EnterBuilding, QuestCategory.Side, 0, rng.Next(1, 9999), traderId,
                    count: cnt, bkind: bk, urgent: urgent, urgentMins: urgentMins, money: 90 + cnt * 30, xp: 35 + cnt * 15);
                break;
            }
            default:
            {
                var mf = SideMatFetch[rng.Next(SideMatFetch.Length)];
                int cnt = 2 + rng.Next(0, 4);
                q = Q(id, $"Нужен {mf.n}", $"Принести {mf.n} ×{cnt} (из рейда).", mf.icon, QuestType.Fetch, QuestCategory.Side, 0, rng.Next(1, 9999), traderId,
                    itemCode: mf.c, itemName: mf.n, count: cnt, urgent: urgent, urgentMins: urgentMins, money: 70 + cnt * 20, xp: 30 + cnt * 10);
                break;
            }
        }
        q.IsRepeatable = false; // слот доски заменяется новым заданием после выполнения
        return q;
    }

    public List<Quest> CreateSideQuests()
    {
        // 80+ побочных заданий для доски объявлений (могут повторяться по типу; часть срочные)
        var list = new List<Quest>();
        var rng = new Random(75);
        string[] traders = { "bartender", "buyer", "prapor", "therapist", "blacksmith", "ragman" };
        var partFetch = SidePartFetch;
        var matFetch = SideMatFetch;
        int n = 1;
        // Принести (части/материалы/предметы)
        foreach (var tr in traders)
        for (int i=0;i<4;i++)
        {
            var pf = partFetch[rng.Next(partFetch.Length)];
            bool urgent = (n % 5 == 0);
            list.Add(Q($"side_{n:D3}", urgent?"СРОЧНО: Части для дела":"Поставка частей", $"{(tr=="bartender"?"Бармен":"Доска")}: принести {pf.n} ×{1+i%3}.", IcoMats, QuestType.Fetch, QuestCategory.Side, 0, n, tr,
                itemCode:pf.c, itemName:pf.n, count:1+i%3, urgent:urgent, urgentMins: urgent ? 720 : 0, money:60+i*20, xp:25+i*10, repeatable:true));
            n++;
        }
        // Убить на локации / ночью/днём / босса
        foreach (var loc in Locs)
        {
            var customer = traders[n % traders.Length];
            bool night = rng.NextDouble()<0.4;
            bool urgent = rng.NextDouble()<0.25;
            list.Add(Q($"side_{n:D3}", urgent?"СРОЧНО: Зачистка":"Зачистка территории", $"Убить {2+loc.Tier} врагов на локации {loc.Name}{(night?" ночью":"")}.", IcoKillHuman, QuestType.Kill, QuestCategory.Side, 0, n, customer,
                count:2+loc.Tier, loc:loc.Id, locName:loc.Name, tod: night?QuestTimeOfDay.Night:QuestTimeOfDay.Any, urgent:urgent, urgentMins: urgent?540:0, money:80+loc.Tier*25, xp:35+loc.Tier*12, repeatable:true));
            n++;
            customer = traders[n % traders.Length];
            urgent = rng.NextDouble()<0.2;
            list.Add(Q($"side_{n:D3}", urgent?"СРОЧНО: Выживший":"Разведка и выход", $"Побывать на локации {loc.Name} и выйти живым.", IcoGo, QuestType.ExtractAlive, QuestCategory.Side, 0, n, customer,
                count:1, loc:loc.Id, locName:loc.Name, urgent:urgent, urgentMins: urgent?480:0, money:100+loc.Tier*20, xp:40+loc.Tier*10, repeatable:true));
            n++;
            customer = traders[n % traders.Length];
            list.Add(Q($"side_{n:D3}", "Охота на босса", $"Убить босса на локации {loc.Name}.", IcoKillHuman, QuestType.KillBoss, QuestCategory.Side, 0, n, customer,
                count:1, loc:loc.Id, locName:loc.Name, boss:true, money:250+loc.Tier*60, xp:120+loc.Tier*25, repeatable:true));
            n++;
        }
        // Зайти в здания / принести материалы
        string[] bks = { "house","warehouse","shop","garage","military","hospital" };
        foreach (var bk in bks)
        for (int i=0;i<2;i++)
        {
            bool urgent = (n%7==0);
            list.Add(Q($"side_{n:D3}", urgent?"СРОЧНО: Обыск":"Обыск помещений", $"Зайти в здания и обыскать ({bk}) — выполнить вход в здание.", IcoScout, QuestType.EnterBuilding, QuestCategory.Side, 0, n, traders[n % traders.Length],
                count:1+i, bkind:bk, urgent:urgent, urgentMins:urgent?600:0, money:90+i*30, xp:35+i*15, repeatable:true));
            n++;
        }
        foreach (var mf in matFetch)
        for (int i=0;i<2;i++)
        {
            bool urgent = (n%6==0);
            list.Add(Q($"side_{n:D3}", urgent?$"СРОЧНО: Нужен {mf.n}":$"Нужен {mf.n}", $"Принести {mf.n} ×{2+i*2} (из рейда).", mf.icon, QuestType.Fetch, QuestCategory.Side, 0, n, "bartender",
                itemCode:mf.c, itemName:mf.n, count:2+i*2, urgent:urgent, urgentMins:urgent?720:0, money:70+i*25, xp:30+i*12, repeatable:true));
            n++;
        }
        // доп. варианты по времени суток
        foreach (var loc in Locs.Take(6))
        {
            list.Add(Q($"side_{n:D3}", "Дневной патруль", $"Убить 3 врагов днём на локации {loc.Name}.", IcoKillHuman, QuestType.Kill, QuestCategory.Side, 0, n, traders[n % traders.Length],
                count:3, loc:loc.Id, locName:loc.Name, tod:QuestTimeOfDay.Day, money:120, xp:50, repeatable:true)); n++;
            list.Add(Q($"side_{n:D3}", "Ночная охота", $"Убить 2 мутантов ночью на локации {loc.Name}.", IcoKillMut, QuestType.Kill, QuestCategory.Side, 0, n, traders[n % traders.Length],
                count:2, loc:loc.Id, locName:loc.Name, tod:QuestTimeOfDay.Night, money:140, xp:55, repeatable:true)); n++;
        }

        return list;
    }

    public List<Quest> CreateDailies() => new();
    public List<Quest> CreateRepeatableQuests() => new();

    public Quest? GetCurrentStory(List<Quest> story) =>
        story.Where(q => q.IsStory && !q.IsCompleted && !q.IsFailed)
             .OrderBy(q => q.Chapter).ThenBy(q => q.Order).FirstOrDefault();
}