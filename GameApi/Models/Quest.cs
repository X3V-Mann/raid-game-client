namespace GameApi.Models;

public enum QuestType { Fetch, Kill, Survive }

public class QuestReward
{
    public int Money { get; set; }
    public int Xp { get; set; }
    public List<Item> Items { get; set; } = new();
    public string? UnlockLocation { get; set; }
}

public class Quest
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public string? TraderId { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public QuestType Type { get; set; }
    public string? TargetItemCode { get; set; }
    public string? TargetItemName { get; set; }
    public int TargetCount { get; set; }
    public int Progress { get; set; }
    public QuestReward Reward { get; set; } = new();
    public bool IsStory { get; set; }

    /// <summary>Глава сюжета (0 — задания торговцев).</summary>
    public int Chapter { get; set; }
    public int Order { get; set; } = 0;

    public bool IsAccepted { get; set; }
    public bool IsCompleted { get; set; }

    /// <summary>Повторяемое задание (дейлики торговцев).</summary>
    public bool IsRepeatable { get; set; } = false;
    public bool IsDaily { get; set; } = false;

    /// <summary>Для дейлика: раньше этого времени задание не выдаётся.</summary>
    public DateTime? AvailableAt { get; set; }

    /// <summary>Предыдущее задание цепочки, которое нужно сдать.</summary>
    public string? RequiresQuestId { get; set; }

    /// <summary>Задание засчитывается только за убийство босса.</summary>
    public bool TargetBoss { get; set; }
}
