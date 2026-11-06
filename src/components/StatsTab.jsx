import { useEffect, useState } from "react";
import { api } from "../api";

export default function StatsTab({ player }) {
  const s = player.stats || {};
  const [achievements, setAchievements] = useState(null);

  useEffect(() => {
    api
      .getAchievements()
      .then(setAchievements)
      .catch(() => setAchievements(null));
  }, []);

  const StatRow = ({ label, value, icon }) => (
    <div className="stat-row">
      <span className="stat-label">{icon} {label}</span>
      <span className="stat-value">{value}</span>
    </div>
  );

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
          <StatRow icon="📈" label="Заработано" value={`${s.moneyEarned || 0} ₽`} />
          <StatRow icon="📉" label="Потрачено" value={`${s.moneySpent || 0} ₽`} />
        </div>
        <div className="stats-card">
          <h4>Выживаемость</h4>
          <StatRow icon="❤️" label="HP восстановлено" value={s.hpHealed || 0} />
          <StatRow icon="🩸" label="HP потеряно" value={s.hpDamaged || 0} />
          <StatRow icon="🍖" label="Еды съедено (шт)" value={s.foodEaten || 0} />
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
