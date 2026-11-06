namespace GameApi.Models;

public class Location
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public int Tier { get; set; } = 1;
    public string Description { get; set; } = "";
    public string Image { get; set; } = "";
    public string Icon { get; set; } = "";
    public double BossChance { get; set; } = 0.1;
    public Dictionary<Rarity, double> RarityWeights { get; set; } = new();
    public List<EnemyTemplate> Enemies { get; set; } = new();

    /// <summary>Квест, после сдачи которого открывается локация. null — открыта сразу.</summary>
    public string? UnlockQuestId { get; set; }
    public string? UnlockHint { get; set; }

    public List<string> ScenePool { get; set; } = new();
    public List<string> ThoughtPool { get; set; } = new();
}
