namespace GameApi.Models;

public class Enemy
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public string Name { get; set; } = "";
    public string Icon { get; set; } = "";
    public int Hp { get; set; }
    public int MaxHp { get; set; }
    public int Damage { get; set; }
    public int Defense { get; set; }
    public bool Ranged { get; set; }
    public int Ap { get; set; } = 5;          // очки действия
    public int ApMax { get; set; } = 5;
    public int Range { get; set; } = 1;        // дальность атаки
    public int X { get; set; }                 // позиция на тактической сетке
    public int Y { get; set; }
    public bool IsBoss { get; set; } = false;

    /// <summary>Добыча, которую можно забрать с трупа.</summary>
    public List<Item> Loot { get; set; } = new();
    public int Money { get; set; }
    public bool Looted { get; set; }
}