using GameApi.Models;

namespace GameApi.Services;

/// <summary>
/// Заглушки заданий (вариант B). Вся инфраструктура и UI сохранены,
/// но контент временно заменён на «Скоро» / «В разработке».
/// </summary>
public class QuestService
{
    private static readonly string[] TraderIds = { "prapor", "therapist", "bartender", "ragman", "blacksmith", "buyer" };

    public List<Quest> CreateStory() => new()
    {
        new Quest
        {
            Id = "story_soon",
            IsStory = true,
            Chapter = 1,
            Order = 1,
            Type = QuestType.Survive,
            Title = "В разработке",
            Description = "Сюжетная линия находится в разработке. Следите за обновлениями.",
            TargetCount = int.MaxValue,
            Reward = new QuestReward(),
        },
    };

    public List<Quest> CreateTraderQuests() => TraderIds.Select(trader => new Quest
    {
        Id = $"{trader}_soon",
        TraderId = trader,
        Order = 1,
        Type = QuestType.Survive,
        Title = "Скоро",
        Description = "Задания этого торговца находятся в разработке.",
        TargetCount = int.MaxValue,
        Reward = new QuestReward(),
    }).ToList();

    public List<Quest> CreateDailies() => new();

    public List<Quest> CreateRepeatableQuests() => new();

    public Quest? GetCurrentStory(List<Quest> story) =>
        story.Where(q => !q.IsCompleted).OrderBy(q => q.Chapter).ThenBy(q => q.Order).FirstOrDefault();
}
