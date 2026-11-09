using GameApi.Models;

namespace GameApi.Services;

public record PerkDefinition(string Id, string Name, string Icon, string Description, int MaxLevel);

public static class PerkCatalog
{
    public static readonly List<PerkDefinition> All = new()
    {
        // «Ловкость» и «Уклонение» перенесены в характеристики (дубли удалены из перков).
        new("tough",   "Крепкий орешек", "❤️", "+10 к максимальному запасу здоровья за уровень", 5),
        new("endurance","Выносливость",  "⚡", "+10 к максимальной выносливости за уровень", 5),
        new("marksman","Стрелок",        "🎯", "+2 к урону в бою за уровень", 5),
        new("armor",   "Толстая кожа",   "🛡", "+1 к защите за уровень", 5),
        new("cover",   "Укрытие",        "🧱", "+5% к снижению урона в укрытии за уровень", 5),
        new("medic",   "Фельдшер",       "💉", "+25% к эффективности медикаментов", 3),
        new("loot",    "Мародёр",        "📦", "+1 предмет в добыче за каждые 2 уровня", 4),
        new("barter",  "Торгаш",         "🤝", "−7% к цене покупки у торговцев за уровень", 3),
        new("sales",   "Перекупщик",     "💰", "+10% к цене продажи за уровень", 3),
        new("regen",   "Ремонтник",      "🩹", "+1 HP к пассивному восстановлению в убежище", 5),
        new("adrenaline", "Ярость",      "💢", "+2 к урону в бою, пока здоровье ниже 50%", 3),
    };

    public static PerkDefinition? Get(string id) => All.FirstOrDefault(p => p.Id == id);
}

public static class PerkEffects
{
    public static int DamageBonus(Player p) =>
        (p.PerkLevel("marksman") * 2 + p.Shelter.ArmoryLevel / 2) + AdrenalineDamage(p);

    public static int DefenseBonus(Player p) => p.PerkLevel("armor");

    public static int MaxHpBonus(Player p) => p.PerkLevel("tough") * 10;

    public static int MaxStaminaBonus(Player p) =>
        p.PerkLevel("endurance") * 10 + p.Shelter.GymLevel * 5;

    /// <summary>Бонус AP от перков (Ловкость-перк удалён; AP даёт характеристика Ловкость).</summary>
    public static int ApBonus(Player p) => 0;

    /// <summary>Дополнительное снижение урона от перка «Укрытие» (0..0.25).</summary>
    public static double CoverReduction(Player p) => Math.Min(0.25, p.PerkLevel("cover") * 0.05);

    /// <summary>+2 урона за уровень перка, пока HP ниже половины максимума.</summary>
    public static int AdrenalineDamage(Player p) =>
        p.Hp * 2 < p.MaxHp ? p.PerkLevel("adrenaline") * 2 : 0;

    /// <summary>Бонус уклонения от перков (Уклонение-перк удалён; уклонение даёт характеристика).</summary>
    public static double DodgeChance(Player p) => 0.0;

    public static double BuyMultiplier(Player p) =>
        Math.Max(0.55, 1.0 - p.PerkLevel("barter") * 0.07 - p.Shelter.WorkshopLevel * 0.02);

    public static double SellMultiplier(Player p) =>
        1.0 + p.PerkLevel("sales") * 0.10 + p.Shelter.WorkshopLevel * 0.03;

    public static int ExtraLootRolls(Player p) => p.PerkLevel("loot") / 2;

    public static double HealMultiplier(Player p) => 1.0 + p.PerkLevel("medic") * 0.25;

    public static int RegenPerTick(Player p) =>
        5 + p.Shelter.MedBlockLevel * 5 + p.PerkLevel("regen");
}
