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
    public bool IsBoss { get; set; } = false;
}