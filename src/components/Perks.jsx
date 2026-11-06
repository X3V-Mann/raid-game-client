import { useEffect, useState } from "react";
import { api } from "../api";

export default function Perks({ act, onToast }) {
  const [data, setData] = useState(null);
  const [busy, setBusy] = useState(false);

  const load = () => api.getPerks().then(setData).catch(console.error);

  useEffect(() => {
    load();
  }, []);

  const buy = async (id) => {
    if (busy) return;
    setBusy(true);
    try {
      await api.buyPerk(id);
      await act(() => Promise.resolve());
      await load();
      onToast?.("Навык улучшен");
    } catch (e) {
      onToast?.(e.message || "Ошибка");
    } finally {
      setBusy(false);
    }
  };

  if (!data) return <div className="panel"><h3>Навыки</h3><p className="desc">Загрузка...</p></div>;

  return (
    <div className="panel">
      <h3>🧬 Навыки</h3>
      <p className="desc">
        За каждый новый уровень даётся 1 очко навыка. Очки можно вкладывать в умения ниже.
      </p>

      <div className="perk-points">
        Свободных очков: <b>{data.points}</b>
      </div>

      <div className="perk-grid">
        {data.definitions.map((p) => {
          const level = data.levels[p.id] || 0;
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
              <button disabled={isMax || data.points < 1 || busy} onClick={() => buy(p.id)}>
                {isMax ? "Максимум" : data.points < 1 ? "Нет очков" : "Улучшить (1 очко)"}
              </button>
            </div>
          );
        })}
      </div>
    </div>
  );
}
