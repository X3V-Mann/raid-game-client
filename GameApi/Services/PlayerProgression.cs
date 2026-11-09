using GameApi.Models;

namespace GameApi.Services;

/// <summary>
/// Единая точка расчёта уровней и характеристик игрока.
/// Уровень выводится из опыта, поэтому клиенту нельзя «прислать» себе уровень.
/// </summary>
public static class PlayerProgression
{
    public const int BaseMaxHp = 100;
    public const int BaseMaxStamina = 100;
    public const int MaxLevel = 50;
    public const int HpPerLevel = 10;
    public const int StaminaPerLevel = 5;

    /// <summary>Сколько опыта нужно, чтобы перейти с уровня level на level + 1.</summary>
    public static int XpForNextLevel(int level) => 100 + (level - 1) * 75;

    /// <summary>Сколько всего опыта нужно, чтобы достичь уровня level.</summary>
    public static int XpToReachLevel(int level)
    {
        int total = 0;
        for (int l = 1; l < level; l++) total += XpForNextLevel(l);
        return total;
    }

    public static int LevelFromXp(int xp)
    {
        int level = 1;
        while (level < MaxLevel && xp >= XpToReachLevel(level + 1)) level++;
        return level;
    }

    /// <summary>
    /// Пересчитывает уровень/максимумы по опыту и перкам.
    /// Возвращает число полученных уровней. За каждый уровень даётся очко перков.
    /// </summary>
    public static int Apply(Player p, bool grantPerkPoints = true)
    {
        MigrateDuplicatePerks(p);

        int newLevel = LevelFromXp(Math.Max(0, p.Xp));
        int gained = newLevel - p.Level;

        p.Level = newLevel;
        p.MaxHp = BaseMaxHp + (newLevel - 1) * HpPerLevel + PerkEffects.MaxHpBonus(p);
        p.MaxStamina = BaseMaxStamina + (newLevel - 1) * StaminaPerLevel + PerkEffects.MaxStaminaBonus(p);

        if (gained > 0)
        {
            if (grantPerkPoints)
            {
                p.PerkPoints += gained;
                p.SkillPoints += gained;
            }
            p.Hp = p.MaxHp;
            p.Stamina = p.MaxStamina;
        }
        else
        {
            p.Hp = Math.Clamp(p.Hp, 0, p.MaxHp);
            p.Stamina = Math.Clamp(p.Stamina, 0, p.MaxStamina);
        }

        return gained;
    }

    /// <summary>Миграция: перки «Ловкость»/«Уклонение» удалены как дубли характеристик — переносим вложенные уровни в характеристики.</summary>
    private static void MigrateDuplicatePerks(Player p)
    {
        if (p.Perks == null) return;
        if (p.Perks.TryGetValue("agility", out var agiLvl) && agiLvl > 0)
        {
            p.Agility = Math.Min(20, Math.Max(1, p.Agility) + agiLvl * 2);
            p.Perks.Remove("agility");
        }
        if (p.Perks.TryGetValue("dodge", out var dodgeLvl) && dodgeLvl > 0)
        {
            p.Dodge = Math.Min(20, Math.Max(1, p.Dodge) + dodgeLvl * 2);
            p.Perks.Remove("dodge");
        }
    }
}
