import { useEffect, useState } from "react";
import { api } from "../../api";
import { ItemIcon } from "../ItemVisual";

export default function Combat({ player, locationId, onEnd, onPlayerChange }) {
  const [state, setState] = useState(null);
  const [busy, setBusy] = useState(false);
  const [mode, setMode] = useState("main"); // main | attack | skills | item
  const [selectedEnemy, setSelectedEnemy] = useState(null);

  useEffect(() => {
    api.combatStart(locationId || "city")
      .then(setState)
      .catch((e) => onEnd(player, { playerDied: false, error: e.message }));
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [locationId]);

  useEffect(() => {
    if (!state?.finished) return;
    const timer = setTimeout(() => {
      if (state.playerWon) {
        onEnd({ ...player, hp: state.playerHp }, {
          playerWon: true,
          droppedLoot: state._lastDrop?.droppedLoot || [],
          droppedMoney: state._lastDrop?.droppedMoney || 0,
          droppedXp: state._lastDrop?.droppedXp || 0,
        });
      } else if (state.playerFled) {
        onEnd({ ...player, hp: state.playerHp }, { playerFled: true });
      } else {
        onEnd({ ...player, hp: 0 }, { playerDied: true });
      }
    }, 1700);
    return () => clearTimeout(timer);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [state?.finished]);

  if (!state) return <div className="loading">Готовим бой...</div>;

  const enemies = state.enemies || [];
  const alive = enemies.filter((e) => e.hp > 0);
  const meds = (player.inventory || []).filter(
    (i) => i.restores === "hp" || i.restores === "hunger" || i.restores === "thirst" || i.isGrenade
  );

  const ranged = player.weaponRanged;
  const hasCaliber = !!(ranged && ranged.caliber);
  const ammoCount = hasCaliber
    ? (player.inventory || [])
        .filter((i) => i.type === "Ammo" && i.caliber === ranged.caliber)
        .reduce((s, i) => s + (i.count || 1), 0)
    : 0;
  const canShoot = hasCaliber && ammoCount > 0;

  const stamina = state.playerStamina ?? player.stamina ?? 0;
  const maxStamina = state.playerMaxStamina || player.maxStamina || 1;
  const staminaPct = Math.max(0, Math.min(100, (stamina / maxStamina) * 100));
  const hpPct = Math.max(0, (state.playerHp / state.playerMaxHp) * 100);
  const last = (state.log || []).slice(-4);

  const doAction = async (action, enemyId, itemId) => {
    if (busy || state.finished) return;
    setBusy(true);
    try {
      const res = await api.combatAction(action, enemyId, itemId);
      setState({ ...res.state, _lastDrop: res.state.finished ? res : undefined });
      setMode("main");
      onPlayerChange?.(res.player ?? { ...player, hp: res.state.playerHp });
    } catch (e) {
      console.error(e);
    } finally {
      setBusy(false);
    }
  };

  const targetId = selectedEnemy || alive[0]?.id;

  return (
    <div className="rpg-battle-bg">
      <div className="rpg-battle">
        <div className="rpg-enemies">
          {enemies.map((e) => {
            const dead = e.hp <= 0;
            const pct = Math.max(0, (e.hp / e.maxHp) * 100);
            const sel = targetId === e.id && !dead;
            return (
              <div
                key={e.id}
                className={`rpg-enemy ${dead ? "dead" : ""} ${sel ? "selected" : ""}`}
                onClick={() => !dead && setSelectedEnemy(e.id)}
              >
                <div className="rpg-enemy-name">
                  {e.isBoss ? "💀 " : ""}{e.name}{e.ranged ? " 🔫" : ""}
                </div>
                <div className="rpg-enemy-sprite">
                  {e.icon?.startsWith?.("/") ? <img src={e.icon} alt={e.name} /> : <span>{e.icon || "🧟"}</span>}
                </div>
                <div className="rpg-hpbar">
                  <div className="rpg-hpbar-fill" style={{ width: `${pct}%` }} />
                  <span>{e.hp}/{e.maxHp}</span>
                </div>
              </div>
            );
          })}
        </div>

        <div className="rpg-message">
          {state._lastDrop?.state?.finished || state.finished ? (
            <div className="rpg-result">
              {state.playerWon ? "🏆 Победа!" : state.playerFled ? "🏃 Отступление" : "☠️ Поражение"}
            </div>
          ) : (
            last.map((line, i) => (
              <div key={i} className={i === last.length - 1 ? "rpg-line current" : "rpg-line"}>
                {line}
              </div>
            ))
          )}
        </div>

        <div className="rpg-bottom">
          <div className="rpg-status">
            <div className="rpg-status-name">
              {player.name || "Выживший"} <span>ур. {player.level}</span>
            </div>
            <div className="rpg-bar-row">
              <span className="rpg-bar-label">HP</span>
              <div className="rpg-bar"><div className="rpg-bar-fill hp" style={{ width: `${hpPct}%` }} /></div>
              <b>{state.playerHp}/{state.playerMaxHp}</b>
            </div>
            <div className="rpg-bar-row">
              <span className="rpg-bar-label">SP</span>
              <div className="rpg-bar"><div className="rpg-bar-fill sp" style={{ width: `${staminaPct}%` }} /></div>
              <b>{stamina}/{maxStamina}</b>
            </div>
            <div className="rpg-mini-stats">
              <span className={state.playerHunger < 25 ? "danger" : ""}>🍖 {state.playerHunger}</span>
              <span className={state.playerThirst < 25 ? "danger" : ""}>💧 {state.playerThirst}</span>
              <span>⚔️ {state.playerDamage}</span>
              <span>🛡️ {state.playerDefense}</span>
              <span>⚖ {state.playerWeight?.toFixed?.(1) ?? 0}/{state.playerMaxWeight}</span>
              {ranged && <span>🔫 {ranged.name}{hasCaliber ? ` (${ammoCount})` : ""}</span>}
            </div>
          </div>

          <div className="rpg-commands">
            {mode === "main" && (
              <>
                <button onClick={() => setMode("attack")} disabled={busy || alive.length === 0}>
                  ⚔️ Атака
                </button>
                <button onClick={() => setMode("skills")} disabled={busy || alive.length === 0}>
                  ✨ Навыки
                </button>
                <button onClick={() => setMode("item")} disabled={busy || meds.length === 0}>
                  🎒 Предмет ({meds.length})
                </button>
                <button onClick={() => doAction("defend")} disabled={busy}>🛡 Защита</button>
                <button onClick={() => doAction("flee")} disabled={busy}>🏃 Побег</button>
              </>
            )}

            {mode === "attack" && (
              <>
                {canShoot ? (
                  <>
                    <button onClick={() => doAction("attack", targetId)} disabled={busy}>
                      🔫 Стрельба
                    </button>
                    <button onClick={() => doAction("aim", targetId)} disabled={busy || stamina < 18}>
                      🎯 Прицельный (18 SP)
                    </button>
                    <button onClick={() => doAction("burst", targetId)} disabled={busy || stamina < 25}>
                      🔥 Очередь (25 SP)
                    </button>
                    <button onClick={() => doAction("rage", targetId)} disabled={busy || stamina < 8}>
                      💢 Ярость
                    </button>
                  </>
                ) : ranged ? (
                  <div className="rpg-line current" style={{ gridColumn: "1 / -1" }}>
                    🔫 Нет патронов под {ranged.name} ({ranged.caliber || "без калибра"}) — стрельба недоступна.
                  </div>
                ) : null}

                {!canShoot && player.weaponMelee && (
                  <button onClick={() => doAction("rage", targetId)} disabled={busy || stamina < 8}>
                    💢 Ярость
                  </button>
                )}

                <button onClick={() => doAction("attack", targetId)} disabled={busy}>
                  ⚔️ Резать
                </button>
                <button onClick={() => doAction("attack", targetId)} disabled={busy}>
                  💪 Ударить
                </button>
                <button onClick={() => setMode("main")} disabled={busy}>↩ Назад</button>
              </>
            )}

            {mode === "skills" && (
              <>
                {canShoot && (
                  <>
                    <button onClick={() => doAction("aim", targetId)} disabled={busy || stamina < 18}>
                      🎯 Прицельный (18 SP)
                    </button>
                    <button onClick={() => doAction("burst", targetId)} disabled={busy || stamina < 25}>
                      🔥 Очередь (25 SP)
                    </button>
                  </>
                )}
                {(canShoot || player.weaponMelee) && (
                  <button onClick={() => doAction("rage", targetId)} disabled={busy || stamina < 8}>
                    💢 Ярость
                  </button>
                )}
                <button onClick={() => setMode("main")} disabled={busy}>↩ Назад</button>
              </>
            )}

            {mode === "item" && (
              <>
                {meds.length === 0 && (
                  <div className="rpg-line current" style={{ gridColumn: "1 / -1" }}>
                    Нет доступных предметов.
                  </div>
                )}
                {meds.map((m) => (
                  <button
                    key={m.id}
                    disabled={busy}
                    onClick={() => {
                      if (m.isGrenade) doAction("grenade", targetId, m.id);
                      else doAction("item", undefined, m.id);
                    }}
                  >
                    <span className="med-icon" style={{ marginRight: 6 }}><ItemIcon item={m} /></span>
                    {m.isGrenade ? "🎯 " : ""}{m.name}
                    {m.count > 1 ? ` ×${m.count}` : ""}
                    {m.isGrenade ? "" : m.restores === "hunger" ? ` +${m.restore}🍖`
                      : m.restores === "thirst" ? ` +${m.restore}💧` : ` +${m.restore}❤️`}
                  </button>
                ))}
                <button onClick={() => setMode("main")} disabled={busy}>↩ Назад</button>
              </>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}