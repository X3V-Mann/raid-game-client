using GameApi.Models;

namespace GameApi.Models;

/// <summary>Полный снимок прогресса: игрок + задания (сюжет и торговцы).</summary>
public class SaveFile
{
    public Player Player { get; set; } = new();
    public List<Quest> Quests { get; set; } = new();
    public List<Quest> StoryQuests { get; set; } = new();
    public string Version { get; set; } = "3";
}
