import { useEffect, useLayoutEffect, useMemo, useRef, useState } from "react";
import { api } from "../../api";
import { ItemIcon } from "../ItemVisual";
import { AVATARS } from "../../avatars";
import RubleIcon from "../RubleIcon";

const sleep = (ms) => new Promise((resolve) => setTimeout(resolve, ms));
const cheb = (x0, y0, x1, y1) => Math.max(Math.abs(x1 - x0), Math.abs(y1 - y0));

const TILE_ICON = {
  wall: "🧱",
  sandbag: "🛡",
};
const TILE_IMG = {
  wall: "/blocks/brick_wall.jpg",
  sandbag: "/blocks/sandbag.png",
  tree: "/blocks/tree.png",
};

const avatarSrcFor = (player) => {
  const id = player?.avatar || "messer";
  const def = AVATARS.find((a) => a.id === id);
  const ext = def?.ext || "png";
  return `/avatars/${id}.${ext}`;
};

const ACTION_COST = {
  attack: 2,
  aim: 3,
  burst: 4,
  defend: 1,
  item: 2,
  grenade: 3,
  flee: 3,
  move: 1,
  loot: 1,
  end: 0,
};

export default function Combat({ initial, player, onUpdate, onClose, onDeath, onToast }) {
  const [state, setState] = useState(initial);
  const [busy, setBusy] = useState(false);
  const [mode, setMode] = useState("main"); // main | item | skills
  const [target, setTarget] = useState(null);
  const [corpseId, setCorpseId] = useState(null);
  const [grenade, setGrenade] = useState(null);
  const [tileSize, setTileSize] = useState(40);
  const [tracer, setTracer] = useState(null);
  const busyRef = useRef(false);
  const gridRef = useRef(null);
  const tracerTimerRef = useRef(null);

  const cols = state?.width || 8;
  const gridRows = state?.height || 8;

  // Подгоняем клетки под доступное место, чтобы поле 8×8 всегда было видно целиком.
  useLayoutEffect(() => {
    const el = gridRef.current;
    if (!el) return;

    const measure = () => {
      const rect = el.getBoundingClientRect();
      const avail = Math.min(rect.width - 8, rect.height - 8);
      const size = Math.max(20, Math.min(56, Math.floor(avail / Math.max(cols, gridRows))));
      setTileSize((prev) => (prev === size ? prev : size));
    };

    measure();
    const ro = new ResizeObserver(measure);
    ro.observe(el);
    window.addEventListener("resize", measure);
    return () => {
      ro.disconnect();
      window.removeEventListener("resize", measure);
    };
  }, [cols, gridRows]);

  const interior = state?.mode === "interior";
  const objects = state?.objects || [];
  const enemies = state?.enemies || [];
  const alive = enemies.filter((e) => e.hp > 0);
  const corpses = enemies.filter((e) => e.hp <= 0 && !e.looted);
  const targetEnemy = alive.find((e) => e.id === target) || alive[0] || null;
  const corpse = enemies.find((e) => e.id === corpseId) || null;

  const usableItems = (player?.inventory || []).filter(
    (i) => i.isGrenade || ["hp", "hunger", "thirst"].includes(i.restores),
  );

  const itemEffectLabel = (item) => {
    if (item.isGrenade) return `💣 урон ${item.damage || 40}`;
    if (item.restores === "hp") return `+${item.restore} HP`;
    if (item.restores === "hunger") return `+${item.restore} сытости`;
    if (item.restores === "thirst") return `+${item.restore} воды`;
    return "";
  };

  const itemIsUseless = (item) => {
    if (item.isGrenade) return false;
    if (item.restores === "hp") return state ? state.playerHp >= state.playerMaxHp : false;
    if (item.restores === "hunger") return state ? state.playerHunger >= 100 : false;
    if (item.restores === "thirst") return state ? state.playerThirst >= 100 : false;
    return true;
  };

  const hpPct = state ? Math.max(0, (state.playerHp / Math.max(1, state.playerMaxHp)) * 100) : 0;
  const apPct = state ? Math.max(0, (state.playerAp / Math.max(1, state.playerApMax)) * 100) : 0;
  const dead = !!state?.finished && state.playerHp <= 0;
  const won = !!state?.finished && state.playerWon;

  const runtime = useMemo(() => {
    if (!state) return null;
    const map = new Map();
    (state.tiles || []).forEach((t) => map.set(`${t.x}:${t.y}`, t));
    return map;
  }, [state]);

  const blocked = (x, y) => {
    if (!state || x < 0 || y < 0 || x >= state.width || y >= state.height) return true;
    const tile = runtime?.get(`${x}:${y}`);
    if (!tile) return true;
    if (tile.type === "wall" || tile.type === "sandbag") return true;
    if (enemies.some((e) => e.hp > 0 && e.x === x && e.y === y)) return true;
    return objects.some((o) => o.blocksMovement && o.x === x && o.y === y);
  };

  // Поиск кратчайшего пути по 8 направлениям (без врагов и препятствий).
  const findPath = (sx, sy, tx, ty) => {
    if (sx === tx && sy === ty) return [];
    const key = (x, y) => `${x}:${y}`;
    const dirs = [
      [-1, -1], [0, -1], [1, -1],
      [-1, 0], [1, 0],
      [-1, 1], [0, 1], [1, 1],
    ];
    const queue = [{ x: sx, y: sy }];
    const cameFrom = new Map([[key(sx, sy), null]]);

    while (queue.length > 0) {
      const cur = queue.shift();
      for (const [dx, dy] of dirs) {
        const nx = cur.x + dx;
        const ny = cur.y + dy;
        const k = key(nx, ny);
        if (cameFrom.has(k)) continue;

        if (nx === tx && ny === ty) {
          cameFrom.set(k, cur);
          const path = [];
          let node = { x: nx, y: ny };
          while (node && !(node.x === sx && node.y === sy)) {
            path.push(node);
            node = cameFrom.get(key(node.x, node.y));
          }
          return path.reverse();
        }

        if (blocked(nx, ny)) continue;
        cameFrom.set(k, cur);
        queue.push({ x: nx, y: ny });
      }
    }
    return null;
  };

  const push = (res) => {
    if (!res) return;
    if (res.state !== undefined) setState(res.state);
    onUpdate?.(res);
  };

  const showTracer = (from, to, kind = "shot") => {
    if (!from || !to) return;
    if (tracerTimerRef.current) clearTimeout(tracerTimerRef.current);
    const shot = { from, to, kind, key: Date.now() + Math.random() };
    setTracer(shot);
    tracerTimerRef.current = setTimeout(() => setTracer((t) => (t?.key === shot.key ? null : t)), 220);
  };

  const runEnemyTurns = async (current) => {
    if (current?.mode === "interior") return;
    let cur = current;
    let prevHp = current?.playerHp;
    let guard = 0;
    while (
      cur &&
      !cur.finished &&
      !cur.isPlayerTurn &&
      (cur.pendingEnemyIds?.length || 0) > 0 &&
      guard++ < 24
    ) {
      await sleep(520);
      const res = await api.combatEnemy();
      const next = res.state;
      if (next) {
        const evt = next.lastEvent || "";
        const damaged = typeof prevHp === "number" && next.playerHp < prevHp;
        if (damaged || /промах|уклон/i.test(evt)) {
          const shooter = (next.enemies || []).find(
            (e) => e.ranged && e.hp > 0 && evt.startsWith((e.name || "") + ":"),
          ) || (next.enemies || []).find((e) => e.ranged && e.hp > 0);
          if (shooter) {
            showTracer({ x: shooter.x, y: shooter.y }, { x: next.playerX, y: next.playerY }, "enemy");
          }
        }
        prevHp = next.playerHp;
        cur = next;
      }
      push(res);
    }
  };

  const act = async (action, opts = {}) => {
    if (busyRef.current || !state || state.finished) return;
    busyRef.current = true;
    setBusy(true);
    try {
      const prevState = state;
      const res = await api.combatAction(
        action,
        opts.enemyId,
        opts.itemId,
        opts.targetX,
        opts.targetY,
      );
      if ((action === "attack" || action === "aim" || action === "burst") && opts.enemyId) {
        const targetBefore = prevState?.enemies?.find((e) => e.id === opts.enemyId);
        if (targetBefore) {
          showTracer(
            { x: prevState.playerX, y: prevState.playerY },
            { x: targetBefore.x, y: targetBefore.y },
            "shot",
          );
        }
      }
      push(res);
      if (res?.message) onToast?.(res.message);
      if (prevState?.mode === "interior" && res && res.state === undefined && res.raid !== undefined) {
        // Интерьер закрыт (например, выход) — возвращаемся на карту рейда.
        onUpdate?.(res);
        onClose?.(res);
        return;
      }
      setMode("main");
      setGrenade(null);
      if (res.state && !res.state.finished && !res.state.isPlayerTurn && res.state.mode !== "interior") {
        await runEnemyTurns(res.state);
      }
    } catch (e) {
      onToast?.(e.message || "Действие не удалось");
    } finally {
      busyRef.current = false;
      setBusy(false);
    }
  };

  const takeLoot = async (enemyId, itemId) => {
    if (busyRef.current) return;
    busyRef.current = true;
    setBusy(true);
    try {
      const res = await api.lootCorpse(enemyId, itemId);
      push(res);
    } catch (e) {
      onToast?.(e.message || "Не удалось обыскать труп");
    } finally {
      busyRef.current = false;
      setBusy(false);
    }
  };

  // Идём к выбранной клетке шаг за шагом, пока хватает AP (в интерьере — свободно).
  const walkTo = async (targetX, targetY) => {
    if (busyRef.current || !state || state.finished) return;
    const path = findPath(state.playerX, state.playerY, targetX, targetY);
    if (path == null) {
      onToast?.("Путь к этой клетке заблокирован.");
      return;
    }
    if (path.length === 0) return;

    busyRef.current = true;
    setBusy(true);
    try {
      let cur = state;
      const isInterior = cur.mode === "interior";
      for (const step of path) {
        if (!cur || cur.finished || (!isInterior && !cur.isPlayerTurn)) break;
        if (!isInterior && cur.playerAp < ACTION_COST.move) break;

        const res = await api.combatAction("move", undefined, undefined, step.x, step.y);
        cur = res.state || cur;
        push(res);

        if (res?.message && isInterior && res.state === undefined && res.raid !== undefined) {
          onUpdate?.(res);
          onClose?.(res);
          return;
        }
        if (cur.finished) break;
        if (!isInterior && !cur.isPlayerTurn) {
          await runEnemyTurns(cur);
          break;
        }
      }
    } catch (e) {
      onToast?.(e.message || "Не удалось переместиться");
    } finally {
      busyRef.current = false;
      setBusy(false);
    }
  };

  // Стрелки — шаг в выбранную сторону.
  const keyHandlerRef = useRef(null);
  useEffect(() => {
    keyHandlerRef.current = (e) => {
      if (!state || state.finished || busy || mode !== "main" || grenade || corpseId) return;
      const tag = document.activeElement?.tagName;
      if (tag === "INPUT" || tag === "TEXTAREA" || tag === "SELECT") return;

      const dirs = {
        ArrowUp: [0, -1],
        ArrowDown: [0, 1],
        ArrowLeft: [-1, 0],
        ArrowRight: [1, 0],
      };
      const dir = dirs[e.key];
      if (!dir) return;
      e.preventDefault();

      const nx = state.playerX + dir[0];
      const ny = state.playerY + dir[1];
      if (blocked(nx, ny)) return;
      if (state.mode !== "interior" && state.playerAp < ACTION_COST.move) {
        onToast?.("Не хватает AP для шага.");
        return;
      }
      act("move", { targetX: nx, targetY: ny });
    };
  });

  useEffect(() => {
    const onKeyDown = (e) => keyHandlerRef.current?.(e);
    window.addEventListener("keydown", onKeyDown);
    return () => window.removeEventListener("keydown", onKeyDown);
  }, []);

  useEffect(() => () => { if (tracerTimerRef.current) clearTimeout(tracerTimerRef.current); }, []);

  if (!state) return <div className="loading">Готовим бой...</div>;

  const rows = Array.from({ length: state.height }, (_, y) =>
    Array.from({ length: state.width }, (_, x) => runtime.get(`${x}:${y}`) || { x, y, type: "empty" }),
  );

  const openCorpse = (enemy) => {
    if (!enemy || enemy.looted) return;
    if (!state.finished && cheb(state.playerX, state.playerY, enemy.x, enemy.y) > 1) {
      onToast?.("Подойдите к трупу ближе.");
      return;
    }
    setCorpseId(enemy.id);
  };

  const onTileClick = (tile) => {
    if (busy) return;

    const enemy = enemies.find((e) => e.x === tile.x && e.y === tile.y);
    if (enemy) {
      if (enemy.hp > 0 && !state.finished) setTarget(enemy.id);
      else if (enemy.hp <= 0) openCorpse(enemy);
      return;
    }

    const obj = objects.find((o) => o.x === tile.x && o.y === tile.y);
    if (state.mode === "interior" && obj) {
      const distObj = cheb(state.playerX, state.playerY, tile.x, tile.y);
      if (distObj <= 1) {
        act("interact", { targetX: tile.x, targetY: tile.y });
      } else if (obj.blocksMovement) {
        // Ищем соседнюю доступную клетку рядом с объектом.
        const dirs = [[-1,-1],[0,-1],[1,-1],[-1,0],[1,0],[-1,1],[0,1],[1,1]];
        let best = null;
        let bestD = Infinity;
        for (const [dx, dy] of dirs) {
          const nx = tile.x + dx, ny = tile.y + dy;
          if (blocked(nx, ny)) continue;
          const d = cheb(state.playerX, state.playerY, nx, ny);
          if (d < bestD) { bestD = d; best = { x: nx, y: ny }; }
        }
        if (best) {
          onToast?.("Подойдите ближе и нажмите ещё раз.");
          walkTo(best.x, best.y);
        } else {
          onToast?.("Не подобраться к объекту.");
        }
      } else {
        onToast?.("Подойдите ближе.");
        walkTo(tile.x, tile.y);
      }
      return;
    }

    if (state.finished) return;

    if (grenade) {
      act("grenade", { itemId: grenade.id, targetX: tile.x, targetY: tile.y });
      return;
    }

    if (tile.type === "wall" || tile.type === "sandbag") {
      onToast?.(tile.type === "wall" ? "Стена непроходима." : "Мешки с песком блокируют путь.");
      return;
    }

    const dist = cheb(state.playerX, state.playerY, tile.x, tile.y);
    if (dist === 1) act("move", { targetX: tile.x, targetY: tile.y });
    else walkTo(tile.x, tile.y);
  };

  const actionButton = (label, action, opts = {}, disabled = false) => (
    <button
      key={label}
      disabled={busy || state.finished || disabled || state.playerAp < (ACTION_COST[action] ?? 0)}
      onClick={() => act(action, opts)}
    >
      {label}
      <small> · {ACTION_COST[action]} AP</small>
    </button>
  );

  return (
    <div className="rpg-battle-bg">
      <div className="rpg-battle combat-tactical">
        <div className="combat-header">
          <h3>{interior ? "🏠" : "⚔️"} {state.title}</h3>
          <div className="ap-display">
            {interior ? (
              <span className="turn-you">Внутри здания · можно ходить и обыскивать</span>
            ) : (
              <>
                <span>
                  AP {state.playerAp}/{state.playerApMax}
                </span>
                <div className="ap-bar">
                  <div className="ap-bar-fill" style={{ width: `${apPct}%` }} />
                </div>
                {state.isPlayerTurn ? <span className="turn-you">Ваш ход</span> : <span className="turn-enemy">Ход врагов…</span>}
              </>
            )}
          </div>
        </div>

        <div className="combat-topline">
          <span className={state.playerHp <= state.playerMaxHp * 0.3 ? "danger" : ""}>
            ❤️ {state.playerHp}/{state.playerMaxHp}
          </span>
          <span>🛡 {state.playerDefense}</span>
          <span>⚔️ {state.playerDamage}</span>
          <span>🍖 {state.playerHunger}</span>
          <span>💧 {state.playerThirst}</span>
          <span>👣 {movesLeftLabel(state)}</span>
        </div>

        <div className="combat-hpbar">
          <div className="combat-hpbar-fill" style={{ width: `${hpPct}%` }} />
        </div>

        <div className="combat-grid-scroll" ref={gridRef}>
          <div
            className={`raid-map combat-map ${grenade ? "grenade-targeting" : ""}`}
            style={{
              gridTemplateColumns: `repeat(${state.width}, ${tileSize}px)`,
              gridAutoRows: `${tileSize}px`,
              position: "relative",
            }}
          >
            {rows.flat().map((tile) => {
              const enemy = enemies.find((e) => e.x === tile.x && e.y === tile.y);
              const obj = objects.find((o) => o.x === tile.x && o.y === tile.y);
              const isPlayer = tile.x === state.playerX && tile.y === state.playerY;
              const classes = [
                "raid-tile",
                (tile.x + tile.y) % 2 === 0 ? "tile-light" : "tile-dark",
                tile.type === "wall" ? "wall" : "",
                tile.type === "sandbag" ? "sandbag" : "",
                enemy && enemy.hp > 0 ? "enemy" : "",
                enemy && enemy.hp <= 0 ? "corpse" : "",
                enemy && targetEnemy?.id === enemy.id ? "selected" : "",
                obj ? `interior-obj interior-${obj.kind}` : "",
                obj?.looted ? "cleared" : "",
                isPlayer ? "you" : "",
              ]
                .filter(Boolean)
                .join(" ");

              const title = enemy
                ? `${enemy.name} · ${enemy.hp}/${enemy.maxHp} HP`
                : obj
                  ? `${obj.name}${obj.looted ? " (обыскано)" : " — подойдите и нажмите"}`
                  : tile.type === "wall"
                    ? "Стена"
                    : tile.type === "sandbag"
                      ? "Укрытие"
                      : "";

              return (
                <div
                  key={`${tile.x}-${tile.y}`}
                  className={classes}
                  onClick={() => onTileClick(tile)}
                  title={title}
                  style={{ width: tileSize, height: tileSize }}
                >
                  {isPlayer ? (
                    <img src={avatarSrcFor(player)} className="tile-avatar" alt="ГГ" />
                  ) : enemy && enemy.hp > 0 ? (
                    enemy.isBoss || enemy.icon?.startsWith?.("/") ? (
                      <img src={enemy.icon} className="tile-enemy-img" alt={enemy.name} />
                    ) : (
                      <span>{enemy.icon || "🧟"}</span>
                    )
                  ) : enemy ? (
                    <span>☠️</span>
                  ) : obj ? (
                    obj.sprite?.startsWith?.("/") ? (
                      <img src={obj.sprite} className="tile-img" alt={obj.name} onError={(e) => { e.currentTarget.style.display = "none"; }} />
                    ) : (
                      <span style={{ opacity: obj.looted ? 0.45 : 1 }}>{obj.icon || "📦"}</span>
                    )
                  ) : TILE_IMG[tile.type] ? (
                    <img src={TILE_IMG[tile.type]} className="tile-img" alt={tile.type} />
                  ) : (
                    <span>{TILE_ICON[tile.type] || ""}</span>
                  )}
                  {enemy && (
                    <div className={`combat-enemy-hp ${enemy.hp <= 0 ? "dead" : ""}`}>
                      <div
                        style={{ width: `${Math.max(0, (enemy.hp / Math.max(1, enemy.maxHp)) * 100)}%` }}
                      />
                    </div>
                  )}
                </div>
              );
            })}
            {tracer && (
              <svg
                key={tracer.key}
                className="combat-tracer"
                style={{
                  position: "absolute",
                  left: 0,
                  top: 0,
                  width: state.width * tileSize,
                  height: state.height * tileSize,
                  pointerEvents: "none",
                  overflow: "visible",
                }}
              >
                <line
                  x1={(tracer.from.x + 0.5) * tileSize}
                  y1={(tracer.from.y + 0.5) * tileSize}
                  x2={(tracer.to.x + 0.5) * tileSize}
                  y2={(tracer.to.y + 0.5) * tileSize}
                  stroke={tracer.kind === "enemy" ? "#ff5b4a" : "#ffe08a"}
                  strokeWidth="2"
                  strokeLinecap="round"
                  opacity="0.95"
                />
                <circle
                  cx={(tracer.to.x + 0.5) * tileSize}
                  cy={(tracer.to.y + 0.5) * tileSize}
                  r={Math.max(2, tileSize * 0.12)}
                  fill={tracer.kind === "enemy" ? "#ff8a70" : "#fff3b0"}
                  opacity="0.9"
                />
              </svg>
            )}
          </div>
        </div>

        <div className="combat-legend">
          {interior ? (
            <>
              <span>🏠 мебель/контейнеры — подойдите и нажмите</span>
              <span>🗄 тайник — редкая находка</span>
              <span>🚪 выход из здания</span>
              <span>⌨️ стрелки или клик — идти</span>
            </>
          ) : (
            <>
              <span>🧱 стена</span>
              <span>🛡 укрытие</span>
              <span>☠️ труп — клик, чтобы обыскать</span>
              <span>⌨️ стрелки или клик по клетке — идти</span>
            </>
          )}
        </div>

        <div className="combat-info">
          {state.log?.slice(-3).map((line, i) => (
            <div key={i} className={i === Math.min(2, state.log.length - 1) ? "combat-line current" : "combat-line"}>
              {line}
            </div>
          ))}
        </div>

        {corpse && (
          <div className="combat-modal">
            <div className="combat-modal-card loot-card">
              <div className="combat-modal-head">
                <b>☠️ {corpse.name}</b>
                <button className="close-x" onClick={() => setCorpseId(null)}>✕</button>
              </div>
              <div className="loot-money"><RubleIcon size={14} /> {corpse.money || 0}</div>
              <div className="loot-grid">
                {(corpse.loot || []).length === 0 && <div className="loot-empty">Труп пуст.</div>}
                {(corpse.loot || []).map((item) => (
                  <button
                    key={item.id}
                    className="loot-item"
                    disabled={busy}
                    onClick={() => takeLoot(corpse.id, item.id)}
                    title="Забрать предмет"
                  >
                    <span className="loot-item-icon"><ItemIcon item={item} /></span>
                    <span className="loot-item-name">{item.name}</span>
                    {item.count > 1 && <span className="loot-item-count">×{item.count}</span>}
                  </button>
                ))}
              </div>
              <div className="combat-modal-actions">
                <button
                  className="btn-next"
                  disabled={busy || ((corpse.loot || []).length === 0 && !corpse.money)}
                  onClick={() => takeLoot(corpse.id)}
                >
                  📦 Забрать всё
                </button>
                <button onClick={() => setCorpseId(null)}>Закрыть</button>
              </div>
            </div>
          </div>
        )}

        {mode === "skills" && (
          <div className="combat-modal">
            <div className="combat-modal-card">
              <div className="combat-modal-head">
                <b>🧬 Навыки</b>
                <button className="close-x" onClick={() => setMode("main")}>✕</button>
              </div>
              <div className="combat-modal-list">
                <button
                  disabled={busy || state.finished || !targetEnemy || state.playerAp < ACTION_COST.aim}
                  onClick={() => act("aim", { enemyId: targetEnemy?.id })}
                >
                  <span className="combat-use-name">🎯 Прицельный выстрел</span>
                  <span className="combat-use-effect">{ACTION_COST.aim} AP · точный</span>
                </button>
                <button
                  disabled={busy || state.finished || !targetEnemy || state.playerAp < ACTION_COST.burst}
                  onClick={() => act("burst", { enemyId: targetEnemy?.id })}
                >
                  <span className="combat-use-name">🔥 Стрельба очередью</span>
                  <span className="combat-use-effect">{ACTION_COST.burst} AP · 3 выстрела</span>
                </button>
                <button
                  disabled={busy || state.finished || state.playerAp < ACTION_COST.defend}
                  onClick={() => act("defend")}
                >
                  <span className="combat-use-name">🛡 Защита</span>
                  <span className="combat-use-effect">{ACTION_COST.defend} AP · −урон</span>
                </button>
              </div>
              <div className="combat-modal-actions">
                <button onClick={() => setMode("main")}>↩ Назад</button>
              </div>
            </div>
          </div>
        )}

        {mode === "item" && (
          <div className="combat-modal">
            <div className="combat-modal-card">
              <div className="combat-modal-head">
                <b>🎒 Инвентарь</b>
                <button className="close-x" onClick={() => setMode("main")}>✕</button>
              </div>
              <div className="combat-modal-list">
                {usableItems.length === 0 && (
                  <div className="loot-empty">Нет предметов для использования.</div>
                )}
                {usableItems.map((item) => {
                  const cost = item.isGrenade ? ACTION_COST.grenade : ACTION_COST.item;
                  return (
                    <button
                      key={item.id}
                      disabled={busy || itemIsUseless(item) || state.playerAp < cost}
                      onClick={() => {
                        if (item.isGrenade) {
                          setGrenade(item);
                          setMode("main");
                        } else {
                          act("item", { itemId: item.id });
                        }
                      }}
                    >
                      <span className="loot-item-icon"><ItemIcon item={item} /></span>
                      <span className="combat-use-name">
                        {item.name}
                        {item.count > 1 ? ` ×${item.count}` : ""}
                      </span>
                      <span className="combat-use-effect">
                        {itemEffectLabel(item)} · {cost} AP
                      </span>
                    </button>
                  );
                })}
              </div>
              <div className="combat-modal-actions">
                <button onClick={() => setMode("main")}>↩ Назад</button>
              </div>
            </div>
          </div>
        )}

        {grenade && (
          <div className="combat-targeting-hint">
            💣 {grenade.name}: выберите клетку броска (до 4 клеток) ·{" "}
            <button onClick={() => setGrenade(null)}>отмена</button>
          </div>
        )}

        <div className="rpg-bottom">
          <div className="rpg-status">
            <div className="rpg-status-name">
              {player?.name || "Выживший"} <span>ур. {player?.level}</span>
            </div>
            <div className="rpg-mini-stats">
              {interior ? (
                <>
                  <span>📦 Объектов: {objects.filter((o) => !o.looted && o.kind !== "exit").length}</span>
                  <span>🗄 Тайник: {objects.some((o) => o.kind === "stash" && !o.looted) ? "есть" : "нет"}</span>
                  <span>⌨️ Ходьба свободная</span>
                </>
              ) : (
                <>
                  <span>🎯 Цель: {targetEnemy ? targetEnemy.name : "нет"}</span>
                  <span>💥 AP: {state.playerAp}</span>
                  <span>🧟 Живых: {alive.length}</span>
                  <span>☠️ Трупов: {corpses.length}</span>
                </>
              )}
            </div>
          </div>

          <div className="rpg-commands">
            {dead ? (
              <div className="combat-result-block">
                <div className="combat-result">☠️ Ты погиб</div>
                <button className="btn-abandon" onClick={() => onDeath?.(player)}>
                  Вернуться в убежище
                </button>
              </div>
            ) : state.finished ? (
              <div className="combat-result-block">
                <div className="combat-result">
                  {interior ? "🏠 Осмотр завершён" : won ? "🏆 Победа!" : "🏃 Отступление"}
                </div>
                {!interior && (
                  <button
                    className="btn-next"
                    disabled={busy || corpses.length === 0}
                    onClick={() => openCorpse(corpses[0])}
                  >
                    ☠️ Обыскать трупы ({corpses.length})
                  </button>
                )}
                <button className="btn-exit-raid" onClick={onClose} disabled={busy}>
                  {state.mutantId ? "🚪 Продолжить рейд" : "🚪 Покинуть здание"}
                </button>
              </div>
            ) : interior ? (
              <>
                <div className="combat-target">Подойдите к мебели/тайнику и нажмите, чтобы обыскать.</div>
                <button disabled={busy || usableItems.length === 0} onClick={() => setMode("item")}>
                  🎒 Инвентарь<small> · предметы</small>
                </button>
                <button className="btn-exit-raid" disabled={busy} onClick={onClose}>
                  🚪 Выйти из здания
                </button>
              </>
            ) : (
              <>
                <div className="combat-target">
                  {alive.length > 0
                    ? `Цель: ${targetEnemy?.name || "—"} (${targetEnemy?.hp ?? 0}/${targetEnemy?.maxHp ?? 0})`
                    : "Противников нет"}
                </div>
                {actionButton("⚔️ Атака", "attack", { enemyId: targetEnemy?.id }, !targetEnemy)}
                <button
                  disabled={busy || state.finished || state.playerAp < ACTION_COST.defend}
                  onClick={() => setMode("skills")}
                >
                  🧬 Навыки<small> · 1–4 AP</small>
                </button>
                <button
                  disabled={busy || usableItems.length === 0 || state.playerAp < ACTION_COST.item}
                  onClick={() => setMode("item")}
                >
                  🎒 Инвентарь<small> · предметы</small>
                </button>
                {actionButton("🏃 Побег", "flee")}
                <button disabled={busy} onClick={() => act("end")}>
                  ⏭ Завершить ход
                </button>
                {corpses.length > 0 && (
                  <button disabled={busy} onClick={() => openCorpse(corpses[0])}>
                    ☠️ Трупы ({corpses.length})<small> · 1 AP</small>
                  </button>
                )}
              </>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}

function movesLeftLabel(state) {
  if (!state) return "";
  // Перемещение стоит 1 AP, поэтому это просто число доступных шагов.
  return `${state.playerAp} шаг(ов)`;
}
