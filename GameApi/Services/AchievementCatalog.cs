using GameApi.Models;

namespace GameApi.Services;

public record AchievementDefinition(
    string Id,
    string Name,
    string Icon,
    string Description,
    Func<Player, GameStateService, bool> Check);

public static class AchievementCatalog
{
    public static readonly List<AchievementDefinition> All = new()
    {
        new("first_blood", "Первая кровь", "🩸", "Убить первого противника.", (p, _) => p.Stats.EnemiesKilled >= 1),
        new("killer", "Чистильщик", "⚔️", "Убить 50 противников.", (p, _) => p.Stats.EnemiesKilled >= 50),
        new("butcher", "Мясник", "💀", "Убить 250 противников.", (p, _) => p.Stats.EnemiesKilled >= 250),

        new("escape", "Побег", "🏃", "Сбежать с боя.", (p, _) => p.Stats.Flees >= 1),
        new("steward", "Хранитель", "📦", "Закинуть предмет в схрон.", (p, _) => p.Stats.StashesUsed >= 1),
        new("sales", "Чёрный рынок", "💸", "Совершить первую сделку с торговцем.", (p, _) => p.Stats.SalesMade >= 1),

        new("first_raid", "Вернулся живым", "🚪", "Пережить первый рейд.", (p, _) => p.Stats.RaidsSurvived >= 1),
        new("stalker", "Сталкер", "🎒", "Пережить 10 рейдов.", (p, _) => p.Stats.RaidsSurvived >= 10),
        new("legend", "Легенда Зоны", "🏅", "Пережить 50 рейдов.", (p, _) => p.Stats.RaidsSurvived >= 50),
        new("hardcore", "Несгибаемый", "🪦", "Провалить 10 рейдов и не сдаться.", (p, _) => p.Stats.RaidsFailed >= 10),

        new("first_quest", "Первый заказ", "📋", "Выполнить первое задание.", (p, _) => p.Stats.QuestsCompleted >= 1),
        new("quest_master", "Исполнитель", "📑", "Выполнить 25 заданий.", (p, _) => p.Stats.QuestsCompleted >= 25),
        new("quest_legend", "Легенда контрактов", "🗂", "Выполнить 100 заданий.", (p, _) => p.Stats.QuestsCompleted >= 100),

        new("rich", "На подъёме", "💰", "Заработать 10 000 ₽.", (p, _) => p.Stats.MoneyEarned >= 10_000),
        new("tycoon", "Барон", "💎", "Заработать 100 000 ₽.", (p, _) => p.Stats.MoneyEarned >= 100_000),

        new("first_boss", "Охотник на боссов", "☠️", "Убить первого босса.", (p, _) => p.Stats.BossesKilled >= 1),
        new("boss_hunter", "Гроза боссов", "💥", "Убить 10 боссов.", (p, _) => p.Stats.BossesKilled >= 10),

        new("cassette", "Меломан", "📼", "Найти первую кассету.", (p, _) => p.Cassettes.Count >= 1),
        new("audiophile", "Аудиофил", "🎧", "Собрать все три кассеты.", (p, _) => p.Cassettes.Count >= 3),

        new("loyal", "Свой человек", "🤝", "Достичь 10-го уровня лояльности у торговца.",
            (p, _) => p.TraderRep.Values.Any(v => v >= 3800)),
        new("builder", "Хозяин убежища", "🏠", "Прокачать любой модуль убежища до 5 уровня.",
            (p, _) => new[]
            {
                p.Shelter.MedBlockLevel, p.Shelter.RestZoneLevel, p.Shelter.KitchenLevel,
                p.Shelter.WaterLevel, p.Shelter.StashLevel, p.Shelter.WorkshopLevel,
                p.Shelter.GeneratorLevel, p.Shelter.ArmoryLevel, p.Shelter.GymLevel,
            }.Any(l => l >= 5)),

        new("perk_master", "Универсал", "🧬", "Вложить 10 очков в навыки.",
            (p, _) => p.Perks.Values.Sum() >= 10),

        new("chapter_5", "Половина пути", "📖", "Завершить 50 сюжетных заданий.",
            (_, s) => s.StoryQuests.Count(q => q.IsCompleted) >= 50),
        new("finale", "Конец игры", "☢", "Дойти до финала в Градирнях.",
            (p, _) => p.GameCompleted),

        new("marathon", "Долгая дорога", "👣", "Пройти 500 клеток в рейдах.",
            (p, _) => p.Stats.CellsPassed >= 500),
    };

    /// <summary>Открывает все подходящие достижения. Возвращает список новых.</summary>
    public static List<AchievementDefinition> Evaluate(GameStateService state)
    {
        var p = state.Player;
        var newly = new List<AchievementDefinition>();

        foreach (var achievement in All)
        {
            if (p.Achievements.Contains(achievement.Id)) continue;

            bool unlocked;
            try
            {
                unlocked = achievement.Check(p, state);
            }
            catch
            {
                unlocked = false;
            }

            if (unlocked)
            {
                p.Achievements.Add(achievement.Id);
                newly.Add(achievement);
            }
        }

        return newly;
    }
}
