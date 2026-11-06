namespace GameApi.Models;

public class EnemyTemplate
{
    public string Name { get; set; } = "";
    public string Icon { get; set; } = "";      // путь к картинке
    public int BaseHp { get; set; } = 20;
    public int BaseDamage { get; set; } = 5;
    public int BaseDefense { get; set; } = 0;
    public bool Ranged { get; set; } = false;
    public double Weight { get; set; } = 1;     // шанс выпадения в пуле
}

public class LocationEnemies
{
    public string LocationId { get; set; } = "";
    public List<EnemyTemplate> Templates { get; set; } = new();
}