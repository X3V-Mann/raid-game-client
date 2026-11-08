namespace GameApi.Models;

public class LootResult
{
    public List<Item> Items { get; set; } = new();
    public int Money { get; set; }
    public int Xp { get; set; }
}
