import { useEffect, useState } from "react";
import { api } from "../api";
import RubleIcon from "./RubleIcon";

export default function StatsTab({ player, act, onToast }) {
  const s = player.stats || {};
  const [achievements, setAchievements] = useState(null);
  const [skills, setSkills] = useState(null);
  const [busy, setBusy] = useState(false);

  const loadSkills = () => api.getSkills().then(setSkills).catch(() => setSkills(null));

  useEffect(() => {
    api
      .getAchievements()
      .then(setAchievements)
      .catch(() => setAchievements(null));
    loadSkills();
  }, []);

  const upgrade = async (id) => {
    if (busy) return;
    setBusy(true);
    try {
      await api.upgradeSkill(id);
      await act?.(() => Promise.resolve());
      await loadSkills();
      onToast?.("Навык улучшен");
    } catch (e) {
      onToast?.(e.message || "Ошибка");
    } finally {
      setBusy(false);
    }
  };

  const StatRow = ({ label, value, icon }) => (
    <div className="stat-row">
      <span className="stat-label">{icon} {label}</span>
      <span className="stat-value">{value}</span>
    </div>
  );

  const skillDefs = skills?.definitions || [
    { id: "luck", name: "Удача", icon: "🍀" },
    { id: "melee", name: "Ближний бой", icon: "🔪" },
    { id: "ranged", name: "Дальний бой", icon: "🔫" },
    { id: "accuracy", name: "Меткость", icon: "🎯" },
    { id: "agility", name: "Ловкость", icon: "🏃" },
    { id: "dodge", name: "Уклонение", icon: "💨" },
    { id: "parry", name: "Парирование", icon: "🛡" },
    { id: "handling", name: "Обращение с оружием", icon: "🔧" },
  ];
  const levelOf = (id) => skills?.levels?.[id] ?? player[id] ?? player[{
    luck: "luck", melee: "meleeSkill", ranged: "rangedSkill", accuracy: "accuracy",
    agility: "agility", dodge: "dodge", parry: "parry", handling: "weaponHandling",
  }[id]] ?? 1;

  return (
    <div className="panel">
      <h3>📊 Статистика выжившего</h3>
      <p className="desc">Твои достижения в Зоне.</p>

      <div className="stats-grid">
        <div className="stats-card">
          <h4>Выживание</h4>
          <StatRow icon="🚪" label="Рейдов пережито" value={s.raidsSurvived || 0} />
          <StatRow icon="💀" label="Рейдов провалено" value={s.raidsFailed || 0} />
          <StatRow icon="🚶" label="Клеток пройдено" value={s.cellsPassed || 0} />
          <StatRow icon="☠️" label="Врагов убито" value={s.enemiesKilled || 0} />
        </div>
        <div className="stats-card">
          <h4>Экономика</h4>
          <StatRow icon="📈" label="Заработано" value={<>{s.moneyEarned || 0} <RubleIcon size={13} /></>} />
          <StatRow icon="📉" label="Потрачено" value={<>{s.moneySpent || 0} <RubleIcon size={13} /></>} />
        </div>
        <div className="stats-card">
          <h4>Выживаемость</h4>
          <StatRow icon="❤️" label="HP восстановлено" value={s.hpHealed || 0} />
          <StatRow icon="🩸" label="HP потеряно" value={s.hpDamaged || 0} />
          <StatRow icon="🍖" label="Еды съедено (шт)" value={s.foodEaten || 0} />
        </div>
      </div>

      <div className="stats-card">
        <h4>🎯 Характеристики и навыки</h4>
        <p className="desc">
          Очки навыков: <b>{skills?.points ?? player.skillPoints ?? 0}</b>. Очки даются за новый уровень.
        </p>
        <div className="skills-list">
          {skillDefs.map((sk) => {
            const lvl = levelOf(sk.id);
            const can = (skills?.points ?? player.skillPoints ?? 0) > 0 && lvl < (sk.maxLevel || 20);
            return (
              <div key={sk.id} className="stat-row" style={{ alignItems: "center", gap: 8 }}>
                <span className="stat-label" style={{ flex: 1 }}>{sk.icon} {sk.name}</span>
                <span className="stat-value">{lvl}</span>
                <button
                  disabled={!can || busy}
                  onClick={() => upgrade(sk.id)}
                  style={{ padding: "2px 8px", fontSize: 12 }}
                  title={sk.description || ""}
                >
                  +
                </button>
              </div>
            );
          })}
        </div>
      </div>

      <div className="stats-card">
        <h4>🏆 Достижения</h4>
        {achievements ? (
          <>
            <p className="desc">
              Найдено {achievements.unlocked ?? 0} из {achievements.total ?? 0}.
            </p>
            <div className="achievements-list">
              {(achievements.definitions || []).map((a) => (
                <div key={a.id} className="stat-row">
                  <span className="stat-label">
                    {a.icon} {a.name}
                  </span>
                  <span className="stat-value">{a.unlocked ? "✅" : "🔒"}</span>
                  <span className="desc">{a.description}</span>
                </div>
              ))}
            </div>
          </>
        ) : (
          <p className="desc">Загрузка...</p>
        )}
      </div>
    </div>
  );
}
