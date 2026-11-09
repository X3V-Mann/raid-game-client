namespace GameApi.Models;

public enum QuestType
{
    Fetch,          // принести предметы
    Kill,           // убить
    KillBoss,       // убить босса (опц. на локации)
    Survive,        // пережить вылазки/выйти живым
    VisitLocation,  // побывать на локации
    EnterBuilding,  // зайти в здание (по типу/любое)
    ExtractAlive,   // зайти на локацию и выйти живым
    Talk            // поговорить с торговцем
}

public enum QuestCategory { Story, Trader, Side }
public enum QuestTimeOfDay { Any, Day, Night }

public class QuestReward
{
    public int Money { get; set; }
    public int Xp { get; set; }
    public List<Item> Items { get; set; } = new();
    public string? UnlockLocation { get; set; }
    public string? UnlockTrader { get; set; }
}

public class Quest
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public string? TraderId { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Icon { get; set; } = ""; // /task/*.png
    public QuestType Type { get; set; }
    public QuestCategory Category { get; set; } = QuestCategory.Story;

    public string? TargetItemCode { get; set; }
    public string? TargetItemName { get; set; }
    public int TargetCount { get; set; }
    public int Progress { get; set; }

    public string? TargetLocationId { get; set; }
    public string? TargetLocationName { get; set; }
    public string? TargetBuildingKind { get; set; } // для EnterBuilding
    public string? TargetEnemyName { get; set; }
    public string? TargetNpcName { get; set; }
    public bool TargetBoss { get; set; }

    public QuestTimeOfDay TimeOfDay { get; set; } = QuestTimeOfDay.Any;
    public bool IsUrgent { get; set; }
    /// <summary>Дедлайн в игровых минутах (TotalMinutes на момент принятия + длительность). 0 = нет.</summary>
    public long DeadlineGameMinutes { get; set; }
    public int UrgentDurationMinutes { get; set; } // сколько игровых минут даётся

    public QuestReward Reward { get; set; } = new();
    public bool IsStory { get; set; }

    /// <summary>Глава сюжета (0 — задания торговцев/побочные).</summary>
    public int Chapter { get; set; }
    public int Order { get; set; } = 0;

    public bool IsAccepted { get; set; }
    public bool IsCompleted { get; set; }
    public bool IsFailed { get; set; }

    /// <summary>Повторяемое задание (дейлики/доска объявлений).</summary>
    public bool IsRepeatable { get; set; } = false;
    public bool IsDaily { get; set; } = false;

    /// <summary>Для дейлика: раньше этого времени задание не выдаётся.</summary>
    public DateTime? AvailableAt { get; set; }

    /// <summary>Предыдущее задание цепочки, которое нужно сдать.</summary>
    public string? RequiresQuestId { get; set; }

    /// <summary>Текст размышления ГГ (для дневника) при получении/сдаче.</summary>
    public string? JournalNote { get; set; }
}
