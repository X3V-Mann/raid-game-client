namespace GameApi.Models;

public class PlayerStats
{
    public int RaidsSurvived { get; set; } = 0;
    public int RaidsFailed { get; set; } = 0;
    public int CellsPassed { get; set; } = 0;
    public int EnemiesKilled { get; set; } = 0;
    public int BossesKilled { get; set; } = 0;
    public int QuestsCompleted { get; set; } = 0;
    public int MoneyEarned { get; set; } = 0;
    public int MoneySpent { get; set; } = 0;
    public int HpHealed { get; set; } = 0;
    public int HpDamaged { get; set; } = 0; // сколько HP потеряно
    public int FoodEaten { get; set; } = 0; // съедено штук
    public int Flees { get; set; } = 0; // успешные побеги с боя
    public int SalesMade { get; set; } = 0; // продано предметов
    public int StashesUsed { get; set; } = 0; // использовано хранилища
}
