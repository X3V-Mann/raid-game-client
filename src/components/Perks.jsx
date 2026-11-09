import { useEffect, useState } from "react";
import { api } from "../api";

export default function Perks({ act, onToast, player }) {
  const [perks, setPerks] = useState(null);
  const [skills, setSkills] = useState(null);
  const [busy, setBusy] = useState(false);

  const loadPerks = () => api.getPerks().then(setPerks).catch(console.error);
  const loadSkills = () => api.getSkills().then(setSkills).catch(() => setSkills(null));

  useEffect(() => {
    loadPerks();
    loadSkills();
  }, []);

  const buyPerk = async (id) => {
    if (busy) return;
    setBusy(true);
    try {
      await api.buyPerk(id);
      await act(() => Promise.resolve());
      await loadPerks();
      onToast?.("Перк улучшен");
    } catch (e) {
      onToast?.(e.message || "Ошибка");
    } finally {
      setBusy(false);
    }
  };

  const upgradeSkill = async (id) => {
    if (busy) return;
    setBusy(true);
    try {
      await api.upgradeSkill(id);
      await act(() => Promise.resolve());
      await loadSkills();
      onToast?.("Характеристика улучшена");
    } catch (e) {
      onToast?.(e.message || "Ошибка");
    } finally {
      setBusy(false);
    }
  };

  if (!perks && !skills) {
    return <div className="panel"><h3>🧬 Навыки</h3><p className="desc">Загрузка...</p></div>;
  }

  const skillDefs = skills?.definitions || [
    { id: "luck", name: "Удача", icon: "🍀", description: "Влияет на находки и качество лута.", maxLevel: 20 },
    { id: "melee", name: "Ближний бой", icon: "🔪", description: "Урон и владение оружием ближнего боя.", maxLevel: 20 },
    { id: "ranged", name: "Дальний бой", icon: "🔫", description: "Урон и владение огнестрельным оружием.", maxLevel: 20 },
    { id: "accuracy", name: "Меткость", icon: "🎯", description: "Снижает шанс промаха.", maxLevel: 20 },
    { id: "agility", name: "Ловкость", icon: "🏃", description: "Проворство, бонус к AP и подвижности.", maxLevel: 20 },
    { id: "dodge", name: "Уклонение", icon: "💨", description: "Шанс увернуться от атаки.", maxLevel: 20 },
    { id: "parry", name: "Парирование", icon: "🛡", description: "Шанс частично заблокировать удар.", maxLevel: 20 },
    { id: "handling", name: "Обращение с оружием", icon: "🔧", description: "Контроль оружия, меньше промахов.", maxLevel: 20 },
  ];
  const skillMap = { luck: "luck", melee: "meleeSkill", ranged: "rangedSkill", accuracy: "accuracy", agility: "agility", dodge: "dodge", parry: "parry", handling: "weaponHandling" };
  const skillLevel = (id) => skills?.levels?.[id] ?? player?.[skillMap[id]] ?? player?.[id] ?? 1;
  const skillPoints = skills?.points ?? player?.skillPoints ?? 0;

  return (
    <div className="panel">
      <h3>🧬 Навыки</h3>
      <p className="desc">
        За каждый новый уровень даются очки характеристик и очки перков. Распределяйте их ниже.
      </p>

      <div className="stats-card" style={{ marginBottom: 12 }}>
        <h4>🎯 Характеристики</h4>
        <p className="desc">
          Очки характеристик: <b>{skillPoints}</b>. Влияют на бой, меткость, уклонение, добычу и требования оружия.
        </p>
        <div className="skills-list">
          {skillDefs.map((sk) => {
            const lvl = skillLevel(sk.id);
            const max = sk.maxLevel || 20;
            const can = skillPoints > 0 && lvl < max && !busy;
            return (
              <div key={sk.id} className="stat-row" style={{ alignItems: "center", gap: 8 }}>
                <span className="stat-label" style={{ flex: 1 }} title={sk.description || ""}>{sk.icon} {sk.name}</span>
                <span className="stat-value">{lvl}/{max}</span>
                <button
                  disabled={!can}
                  onClick={() => upgradeSkill(sk.id)}
                  style={{ padding: "2px 10px", fontSize: 12 }}
                  title={sk.description || "Улучшить"}
                >
                  +
                </button>
              </div>
            );
          })}
        </div>
      </div>

      <div className="stats-card">
        <h4>✨ Перки</h4>
        <p className="desc">Очки перков: <b>{perks?.points ?? player?.perkPoints ?? 0}</b>. Пассивные умения выжившего.</p>

        {perks ? (
          <div className="perk-grid">
            {perks.definitions.map((p) => {
              const level = perks.levels[p.id] || 0;
              const isMax = level >= p.maxLevel;
              return (
                <div key={p.id} className={`perk-card ${isMax ? "maxed" : ""}`}>
                  <div className="perk-head">
                    <span className="perk-icon">{p.icon}</span>
                    <b>{p.name}</b>
                    <span className="perk-level">{level}/{p.maxLevel}</span>
                  </div>
                  <p>{p.description}</p>
                  <div className="perk-pips">
                    {Array.from({ length: p.maxLevel }).map((_, i) => (
                      <span key={i} className={i < level ? "on" : ""} />
                    ))}
                  </div>
                  <button disabled={isMax || perks.points < 1 || busy} onClick={() => buyPerk(p.id)}>
                    {isMax ? "Максимум" : perks.points < 1 ? "Нет очков" : "Улучшить (1 очко)"}
                  </button>
                </div>
              );
            })}
          </div>
        ) : (
          <p className="desc">Не удалось загрузить перки.</p>
        )}
      </div>
    </div>
  );
}
