namespace GameApi.Models;

/// <summary>Полный снимок прогресса: игрок + задания + активный рейд.</summary>
public class SaveFile
{
    public Player Player { get; set; } = new();
    public List<Quest> Quests { get; set; } = new();
    public List<Quest> StoryQuests { get; set; } = new();
    public List<Quest> SideQuests { get; set; } = new();
    public RaidState? Raid { get; set; }
    public string Version { get; set; } = "7.5";
}
