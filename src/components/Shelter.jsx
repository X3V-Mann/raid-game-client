import { useEffect, useState, useRef } from "react";
import { api } from "../api";
import { ItemIcon, rarityClass } from "./ItemVisual";
import { CASSETTES } from "../cassettes";
import { audio, useAudioState } from "../audio";

const MATERIAL_CODES = ["scrap", "cloth", "wire", "battery", "electronics", "cement", "rubber", "glass"];
const MATERIAL_META = {
  scrap: { icon: "🔩", name: "Металлолом" },
  cloth: { icon: "🧵", name: "Ткань" },
  wire: { icon: "🔌", name: "Провода" },
  battery: { icon: "🔋", name: "Аккумулятор" },
  electronics: { icon: "💾", name: "Микросхемы" },
  cement: { icon: "🧱", name: "Цемент" },
  rubber: { icon: "🛞", name: "Резина" },
  glass: { icon: "🍾", name: "Стекло" },
};

const StashContextMenu = ({ x, y, onTake, onClose }) => {
  const ref = useRef(null);
  useEffect(() => {
    const handleClick = (e) => {
      if (ref.current && !ref.current.contains(e.target)) onClose();
    };
    const handleEsc = (e) => e.key === "Escape" && onClose();
    document.addEventListener("mousedown", handleClick);
    document.addEventListener("keydown", handleEsc);
    return () => {
      document.removeEventListener("mousedown", handleClick);
      document.removeEventListener("keydown", handleEsc);
    };
  }, [onClose]);

  return (
    <div ref={ref} className="ctx-menu" style={{ top: y, left: x }} onClick={(e) => e.stopPropagation()}>
      <button onClick={() => { onTake(); onClose(); }}>⬆ Забрать в инвентарь</button>
    </div>
  );
};

export default function Shelter({ player, act }) {
  const [info, setInfo] = useState(null);
  const [busy, setBusy] = useState(false);
  const [ctx, setCtx] = useState(null);
  const [selectedStash, setSelectedStash] = useState(null);
  const audioState = useAudioState();

  const discoveredCassettes = player.cassettes || [];

  useEffect(() => {
    // Уходим из убежища — кассета замолкает, возвращается эмбиент.
    return () => audio.stopCassette();
  }, []);

  const loadInfo = async () => {
    const data = await api.getShelterInfo();
    setInfo(data);
  };

  useEffect(() => {
    loadInfo().catch(console.error);
  }, []);

  const s = player.shelter;

  const countOf = (code) =>
    player.inventory.filter((i) => i.code === code).reduce((sum, i) => sum + (i.count || 1), 0);

  const upgrade = async (id) => {
    if (busy) return;
    setBusy(true);
    try {
      await api.upgradeShelter(id);
      await act(() => Promise.resolve());
      await loadInfo();
    } catch (e) {
      alert(e.message || "Ошибка улучшения");
    } finally {
      setBusy(false);
    }
  };

  const handleStashCtx = (e, item) => {
    e.preventDefault();
    e.stopPropagation();
    if (!item) return;
    setCtx({ x: e.clientX, y: e.clientY, item });
  };

  return (
    <div className="panel">
      <h3>Убежище</h3>

      <div className="cassette-player">
        <div className="cassette-head">
          <b>📼 Кассетный проигрыватель</b>
          <span>встроен в убежище · найдено {discoveredCassettes.length}/{CASSETTES.length}</span>
        </div>

        <div className="cassette-body">
          <img
            src="/items/cassette.gif"
            alt="Кассета"
            className={`cassette-gif ${audioState.playing ? "playing" : ""}`}
          />

          <div className="cassette-info">
            <div className="cassette-now">
              {audioState.cassette ? `${audioState.playing ? "▶" : "⏸"} ${audioState.cassette.title}` : "Плёнка не выбрана"}
            </div>
            <div className="cassette-sub">
              {discoveredCassettes.length === 0
                ? "Кассеты находятся в рейдах. Найдите первую."
                : "Выберите кассету из коллекции."}
            </div>

            <div className="cassette-list">
              {CASSETTES.map((cassette) => {
                const owned = discoveredCassettes.includes(cassette.code);
                const active = audioState.cassette?.id === cassette.id;
                return (
                  <button
                    key={cassette.id}
                    disabled={!owned}
                    className={active ? "active" : ""}
                    onClick={() => audio.playCassette(cassette)}
                    title={owned ? cassette.title : "Кассета не найдена"}
                  >
                    <span>{owned ? (active && audioState.playing ? "⏸" : "▶") : "🔒"}</span>
                    {cassette.title}
                  </button>
                );
              })}
            </div>

            <div className="cassette-controls">
              <button onClick={() => audio.stopCassette()} disabled={!audioState.cassette}>
                ⏹ Стоп
              </button>
              <span>Громкость: {Math.round(audioState.settings.music * 100)}% (в настройках)</span>
            </div>
          </div>
        </div>
      </div>

      <div className="rest-summary">
        <b>🩹 Пассивное восстановление</b>
        {info ? (
          <span>
            каждые {info.regen.interval} сек: ❤️ +{info.regen.hp} &nbsp; ⚡ +{info.regen.stamina}
            {info.regen.hunger > 0 && <> &nbsp; 🍖 +{info.regen.hunger}</>}
            {info.regen.thirst > 0 && <> &nbsp; 💧 +{info.regen.thirst}</>}
          </span>
        ) : (
          <span>Загрузка...</span>
        )}
        <span className="hint">
          Улучшайте мед. блок, кухню и водоочистку, чтобы восстанавливаться быстрее.
        </span>
      </div>

      <div className="materials-panel">
        <div className="materials-title">🔧 Материалы в инвентаре:</div>
        <div className="materials-grid">
          {MATERIAL_CODES.map((code) => {
            const count = countOf(code);
            const meta = MATERIAL_META[code];
            return (
              <div key={code} className={`material-chip ${count > 0 ? "has" : "empty"}`}>
                <span className="material-icon">{meta.icon}</span>
                <span className="material-name">{meta.name}</span>
                <span className="material-count">×{count}</span>
              </div>
            );
          })}
        </div>
      </div>

      <p className="desc">
        Улучшения требуют деньги и материалы: по {`{уровень}`} шт. металлолома, ткани и проводов.
        С 3-го уровня нужен аккумулятор, с 5-го — микросхемы. Максимальный уровень — 5.
      </p>

      <div className="modules">
        {(info?.modules || []).map((m) => {
          const isMax = m.level >= m.maxLevel;
          const reqOk = m.requirements.every((r) => r.have >= r.need);
          const canUpgrade = !isMax && reqOk && player.money >= m.nextCost;

          return (
            <div key={m.id} className="module">
              <b>{m.icon} {m.name} — ур. {m.level}{isMax ? " (макс.)" : ""}</b>
              <span>{m.effect}</span>

              {!isMax && (
                <div className="module-reqs">
                  {m.requirements.map((r) => (
                    <span key={r.code} className={r.have >= r.need ? "ok" : "no"}>
                      {r.name} {r.have}/{r.need}
                    </span>
                  ))}
                </div>
              )}

              <button disabled={!canUpgrade || busy} onClick={() => upgrade(m.id)}>
                {isMax ? "Максимальный уровень" : `Улучшить (${m.nextCost} ₽)`}
              </button>
            </div>
          );
        })}
      </div>

      <h4>Схрон ({s.stash.length}/{s.stashSlots})</h4>
      <p className="hint">💡 ПКМ по предмету — забрать в инвентарь</p>
      <div className="inv-grid">
        {[
          ...s.stash,
          ...Array(Math.max(0, s.stashSlots - s.stash.length)).fill(null),
        ].map((item, i) => (
          <div
            key={item ? item.id : `stash-empty-${i}`}
            className={`inv-cell ${item ? rarityClass(item.rarity) : "empty"} ${
              selectedStash?.id === item?.id && item ? "selected" : ""
            }`}
            onClick={() => item && setSelectedStash(item)}
            onContextMenu={(e) => handleStashCtx(e, item)}
          >
            {item && (
              <>
                <div className="item-icon">
                  <ItemIcon item={item} />
                  {item.count > 1 && <span className="item-count-badge">×{item.count}</span>}
                </div>
                <div className="item-name">{item.name}</div>
              </>
            )}
          </div>
        ))}
      </div>

      {selectedStash && (
        <div className="stash-detail">
          <b>{selectedStash.name}</b>
          <p>{selectedStash.description || "Описание пока отсутствует."}</p>
          <button onClick={() => setSelectedStash(null)}>Закрыть</button>
        </div>
      )}

      {ctx && (
        <StashContextMenu
          x={ctx.x}
          y={ctx.y}
          onTake={() => act(() => api.stashTake(ctx.item.id))}
          onClose={() => setCtx(null)}
        />
      )}
    </div>
  );
}
