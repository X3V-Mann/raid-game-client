import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { api } from "../../api";
import Combat from "./Combat";
import RaidFeed from "./RaidFeed";
import { AVATARS } from "../../avatars";
import RubleIcon from "../RubleIcon";

const TILE_ICON = {
  tree: "🌲",
  exit: "🚪",
};

const avatarSrcFor = (player) => {
  const id = player?.avatar || "messer";
  const def = AVATARS.find((a) => a.id === id);
  const ext = def?.ext || "png";
  return `/avatars/${id}.${ext}`;
};

const feedKindFor = (line) => {
  if (/погиб|смерт|истощ/i.test(line)) return "death";
  if (/бой|враг|победа|атак|урон|hp/i.test(line)) return "combat";
  if (/добыч|наход|₽|деньг/i.test(line)) return "loot";
  if (/обыск|труп/i.test(line)) return "loot";
  return "info";
};

export default function Raid({ player, location, onExit, onToast, onPlayerChange }) {
  const [raid, setRaid] = useState(null);
  const [combat, setCombat] = useState(null);
  const [selected, setSelected] = useState(null);
  const [busy, setBusy] = useState(false);
  const [finished, setFinished] = useState(false);
  const [loading, setLoading] = useState(true);
  const busyRef = useRef(false);

  const apply = useCallback(
    (res) => {
      if (!res) return;
      if (res.raid) setRaid(res.raid);
      if (res.player) onPlayerChange?.(res.player);
      if (res.state !== undefined) setCombat(res.state);
      if (res.message) onToast?.(res.message);
      if (res.raidFinished) {
        setFinished(true);
        setTimeout(() => onExit?.(res.player), 350);
      }
    },
    [onExit, onPlayerChange, onToast],
  );

  const load = useCallback(async () => {
    try {
      const st = await api.getRaidState();
      if (st.active && st.raid) {
        setRaid(st.raid);
        setCombat(st.raid.combat || null);
        if (st.player) onPlayerChange?.(st.player);
      } else {
        const res = await api.startRaid(location?.tier ?? 1, location?.id ?? "city");
        apply(res);
      }
    } catch (e) {
      onToast?.(e.message || "Не удалось начать рейд");
      onExit?.();
    } finally {
      setLoading(false);
    }
  }, [apply, location, onExit, onPlayerChange, onToast]);

  useEffect(() => {
    load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const playerX = raid?.playerX ?? 0;
  const playerY = raid?.playerY ?? 0;

  const selectedBuilding = useMemo(
    () => raid?.buildings?.find((b) => b.id === selected) || null,
    [raid, selected],
  );

  const buildingAt = useCallback(
    (x, y) => raid?.buildings?.find(
      (b) => x >= b.x && x < b.x + b.width && y >= b.y && y < b.y + b.height,
    ),
    [raid],
  );

  const adjacentTo = (b) =>
    !!b &&
    playerX >= b.x - 1 &&
    playerX <= b.x + b.width &&
    playerY >= b.y - 1 &&
    playerY <= b.y + b.height &&
    !(playerX >= b.x && playerX < b.x + b.width &&
      playerY >= b.y && playerY < b.y + b.height);

  const mutantAt = useCallback(
    (x, y) => raid?.mutants?.find((m) => !m.defeated && m.x === x && m.y === y) || null,
    [raid],
  );

  const carAt = useCallback(
    (x, y) => raid?.cars?.find((c) => c.x === x && c.y === y) || null,
    [raid],
  );

  const run = async (fn) => {
    if (busyRef.current) return;
    busyRef.current = true;
    setBusy(true);
    try {
      const res = await fn();
      apply(res);
      return res;
    } catch (e) {
      onToast?.(e.message || "Ошибка действия");
    } finally {
      busyRef.current = false;
      setBusy(false);
    }
  };

  const tileMap = useMemo(() => {
    const map = new Map();
    (raid?.tiles || []).forEach((t) => map.set(`${t.x}:${t.y}`, t));
    return map;
  }, [raid]);

  const raidBlocked = (x, y) => {
    if (!raid || x < 0 || y < 0 || x >= raid.width || y >= raid.height) return true;
    const tile = tileMap.get(`${x}:${y}`);
    return !tile || (tile.type !== "ground" && tile.type !== "exit" && tile.type !== "transition");
  };

  // Кратчайший путь по 8 направлениям вокруг деревьев и зданий.
  const findRaidPath = (sx, sy, tx, ty) => {
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

        if (raidBlocked(nx, ny)) continue;
        cameFrom.set(k, cur);
        queue.push({ x: nx, y: ny });
      }
    }
    return null;
  };

  const walkTo = async (x, y) => {
    if (busyRef.current || finished) return;
    const path = findRaidPath(playerX, playerY, x, y);
    if (path == null) {
      onToast?.("Путь к этой клетке заблокирован.");
      return;
    }
    if (path.length === 0) return;

    busyRef.current = true;
    setBusy(true);
    try {
      for (const step of path) {
        const res = await api.moveRaid(step.x, step.y);
        apply(res);
        if (res?.raid?.combat) break;
      }
    } catch (e) {
      onToast?.(e.message || "Не удалось переместиться");
    } finally {
      busyRef.current = false;
      setBusy(false);
    }
  };

  const moveTo = (x, y) => {
    const dist = Math.max(Math.abs(x - playerX), Math.abs(y - playerY));
    if (dist === 1) run(() => api.moveRaid(x, y));
    else if (dist > 1) walkTo(x, y);
  };

  const enterBuilding = async (b) => {
    setSelected(b.id);
    if (!adjacentTo(b)) {
      onToast?.("Подойдите к зданию ближе.");
      return;
    }
    await run(() => api.enterBuilding(b.id));
  };

  const lootBuildingStash = async (b) => {
    if (!adjacentTo(b)) {
      onToast?.("Подойдите к зданию ближе.");
      return;
    }
    await run(() => api.lootBuildingStash(b.id));
  };

  const onTileClick = (tile) => {
    if (busy || finished) return;
    if (tile.type === "building") {
      const b = buildingAt(tile.x, tile.y);
      if (b) enterBuilding(b);
      return;
    }
    if (tile.type === "stash") {
      const dist = Math.max(Math.abs(tile.x - playerX), Math.abs(tile.y - playerY));
      if (dist > 1 || dist === 0) {
        onToast?.("Подойдите к тайнику ближе.");
        return;
      }
      run(() => api.lootStash(tile.x, tile.y));
      return;
    }
    if (tile.type === "car") {
      const dist = Math.max(Math.abs(tile.x - playerX), Math.abs(tile.y - playerY));
      if (dist > 1 || dist === 0) {
        onToast?.("Подойдите к автомобилю ближе.");
        return;
      }
      run(() => api.lootCar(tile.x, tile.y));
      return;
    }
    if (tile.type === "tree") {
      onToast?.("Здесь непроходимая чаща.");
      return;
    }
    if (tile.type === "ground" || tile.type === "exit" || tile.type === "transition") moveTo(tile.x, tile.y);
  };

  // Стрелки — шаг по карте рейда (в бою их обрабатывает Combat).
  const keyHandlerRef = useRef(null);
  useEffect(() => {
    keyHandlerRef.current = (e) => {
      if (!raid || busy || finished || combat) return;
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

      const nx = playerX + dir[0];
      const ny = playerY + dir[1];
      if (raidBlocked(nx, ny)) return;
      run(() => api.moveRaid(nx, ny));
    };
  });

  useEffect(() => {
    const onKeyDown = (e) => keyHandlerRef.current?.(e);
    window.addEventListener("keydown", onKeyDown);
    return () => window.removeEventListener("keydown", onKeyDown);
  }, []);

  const leaveRaid = () =>
    run(async () => {
      const res = await api.exitRaid();
      setFinished(true);
      onToast?.("Ты вернулся в убежище.");
      setTimeout(() => onExit?.(res?.player), 500);
      return res;
    });

  const abandon = () => {
    if (!window.confirm("Покинуть рейд? Всё, что не вынесено, останется здесь.")) return;
    run(async () => {
      const res = await api.abandonRaid();
      setFinished(true);
      setTimeout(() => onExit?.(res?.player), 500);
      return res;
    });
  };

  const onCombatClose = async (alreadyRes) => {
    try {
      let res = alreadyRes;
      if (!(res && res.raid !== undefined && res.state === undefined)) {
        res = await api.closeCombat();
      }
      apply(res);
      setCombat(null);
      const st = await api.getRaidState();
      if (st.active) {
        setRaid(st.raid);
        if (st.player) onPlayerChange?.(st.player);
      }
      if (res?.message) onToast?.(res.message);
    } catch (e) {
      const msg = e?.message || "Не удалось завершить бой";
      if (msg.includes("Бой не активен")) {
        setCombat(null);
        try {
          const st = await api.getRaidState();
          if (st.active) setRaid(st.raid);
          if (st?.player) onPlayerChange?.(st.player);
        } catch {}
        return;
      }
      onToast?.(msg);
    }
  };

  const onCombatDeath = (deadPlayer) => {
    setCombat(null);
    setFinished(true);
    if (deadPlayer) onPlayerChange?.(deadPlayer);
    setTimeout(() => onExit?.({ ...(deadPlayer || player), died: true }), 900);
  };

  if (loading) return <div className="loading">Готовим рейд...</div>;
  if (!raid) return <div className="loading">Рейд недоступен...</div>;

  const tiles = raid.tiles || [];
  const rows = Array.from({ length: raid.height }, (_, y) =>
    Array.from({ length: raid.width }, (_, x) =>
      tiles.find((t) => t.x === x && t.y === y) || { x, y, type: "ground" },
    ),
  );

  const feed = (raid.log || []).slice(-10).reverse().map((line, i) => ({
    kind: feedKindFor(line),
    icon: line.slice(0, 2).trim() || "·",
    title: i === 0 ? "Последнее" : "",
    text: line.replace(/^[^\p{L}\p{N}]+/u, ""),
  }));

  return (
    <div className="raid-screen">
      <div
        className="raid-banner"
        style={location?.image ? { backgroundImage: `url(${location.image})` } : undefined}
      >
        <div className="raid-banner-overlay" />
        <div className="raid-banner-content">
          <h3>{raid.locationName || location?.name}</h3>
          <div className="raid-banner-stats">
            <span>❤️ {player?.hp}/{player?.maxHp}</span>
            <span>⚡ {player?.stamina}/{player?.maxStamina}</span>
            <span>🍖 {player?.hunger}</span>
            <span>💧 {player?.thirst}</span>
            <span>⚖ {player?.currentWeight ?? 0}/{player?.maxWeight ?? 50}</span>
            <span><RubleIcon size={14} /> {player?.money}</span>
          </div>
        </div>
      </div>

      <div className="raid-body">
        <div className="raid-main">
          <div className="raid-map-header">
            <h3>🗺 {raid.locationName}{raid.maxSectors > 1 ? ` · сектор ${raid.sector}/${raid.maxSectors}` : ""}</h3>
            <div className="raid-stats">
              <span>👣 {raid.cellsVisited}</span>
              <span>☠️ {raid.enemiesKilled}</span>
              <span>🏢 {raid.buildings.length}</span>
              <span>🚗 {(raid.cars || []).filter((c) => !c.looted).length}</span>
              <span>🐺 {(raid.mutants || []).filter((m) => !m.defeated).length}</span>
              <span>⚔️ {raid.combat ? "Бой" : "Свободно"}</span>
            </div>
          </div>

          <div className="raid-grid-scroll">
            <div
              className="raid-map raid-grid"
              style={{ gridTemplateColumns: `repeat(${raid.width}, 34px)` }}
            >
              {rows.flat().map((tile) => {
                const b = tile.type === "building" ? buildingAt(tile.x, tile.y) : null;
                const car = tile.type === "car" ? carAt(tile.x, tile.y) : null;
                const mutant = mutantAt(tile.x, tile.y);
                const isYou = tile.x === playerX && tile.y === playerY;
                const bAnchor = b && b.x === tile.x && b.y === tile.y;
                const classes = [
                  "raid-tile",
                  (tile.x + tile.y) % 2 === 0 ? "tile-light" : "tile-dark",
                  tile.type === "tree" ? "tile-tree" : "",
                  tile.type === "exit" ? "exit" : "",
                  tile.type === "transition" ? "transition" : "",
                  tile.type === "car" ? "tile-car" : "",
                  car?.looted ? "cleared" : "",
                  b ? "building" : "",
                  b?.cleared ? "cleared" : "",
                  b && selected === b.id ? "selected-building" : "",
                  mutant ? "tile-mutant" : "",
                  isYou ? "you" : "",
                ]
                  .filter(Boolean)
                  .join(" ");

                return (
                  <div
                    key={`${tile.x}-${tile.y}`}
                    className={classes}
                    onClick={() => onTileClick(tile)}
                    title={
                      mutant
                        ? `🐺 ${mutant.name} · видит на ${mutant.visionRadius ?? 3} клетки · здания и деревья закрывают обзор`
                        : b
                        ? `${b.name} · ${b.width}×${b.height}${
                            b.looted ? " · обыскано" : b.hasEnemies && !b.cleared ? " · враги" : " · можно войти"
                          }${b.hasStash && !b.stashLooted ? " · есть тайник" : ""}${
                            (b.furniture || []).some((f) => !f.looted) ? " · есть мебель" : ""
                          }`
                        : car
                          ? `${car.name}${car.looted ? " · обыскан" : " — подойдите и нажмите, чтобы обыскать"}`
                          : tile.type === "exit"
                          ? "Выход из рейда"
                          : tile.type === "tree"
                            ? "Дерево"
                            : tile.type === "stash"
                              ? "Тайник — подойдите и нажмите, чтобы обыскать"
                              : tile.type === "transition"
                                ? "Переход дальше по локации"
                                : ""
                    }
                    style={{ width: 34, height: 34 }}
                  >
                    {isYou ? <img src={avatarSrcFor(player)} className="tile-avatar" alt="ГГ" /> : mutant ? <img src={mutant.icon} className="tile-enemy-img tile-mutant-img" alt={mutant.name} /> : bAnchor ? b.icon : tile.type === "car" ? "🚗" : tile.type === "tree" ? <img src="/blocks/tree.png" className="tile-img" alt="tree" /> : tile.type === "stash" ? <img src="/blocks/stash.png" className="tile-img" alt="stash" /> : tile.type === "transition" ? "➜" : TILE_ICON[tile.type] || ""}
                  </div>
                );
              })}
            </div>
          </div>

          <div className="raid-hint">
            Клик по клетке — идти туда. Стрелки — шаг. Клик по зданию — войти и обыскать.
            Мутанты видят на 3 клетки, но здания и деревья закрывают им обзор — их можно обойти.
          </div>

          {selectedBuilding && (
            <div className={`raid-building-card ${adjacentTo(selectedBuilding) ? "" : "far"}`}>
              <div className="raid-building-head">
                <span className="raid-building-icon">{selectedBuilding.icon}</span>
                <div>
                  <b>{selectedBuilding.name}</b>
                  <small>
                    {selectedBuilding.width}×{selectedBuilding.height} ·{" "}
                    {selectedBuilding.cleared
                      ? "зачищено"
                      : selectedBuilding.hasEnemies
                        ? "враги внутри"
                        : "тихо, можно войти"}
                  </small>
                </div>
                <span className={`raid-building-status ${selectedBuilding.looted ? "done" : ""}`}>
                  {selectedBuilding.looted ? "✅ обыскано" : "📦 не обыскано"}
                </span>
              </div>
              <div className="raid-building-desc">{selectedBuilding.description}</div>
              <div className="raid-building-actions">
                <button
                  className="btn-next"
                  disabled={busy || !adjacentTo(selectedBuilding)}
                  onClick={() => enterBuilding(selectedBuilding)}
                >
                  {selectedBuilding.hasEnemies && !selectedBuilding.cleared
                    ? "⚔️ Войти в бой"
                    : selectedBuilding.looted
                      ? "🚪 Войти"
                      : "🚪 Войти и обыскать"}
                </button>
                {selectedBuilding.hasStash && !selectedBuilding.stashLooted && !(selectedBuilding.hasEnemies && !selectedBuilding.cleared) && (
                  <button
                    className="btn-next"
                    disabled={busy || !adjacentTo(selectedBuilding)}
                    onClick={() => lootBuildingStash(selectedBuilding)}
                    title="Редкий тайник в здании"
                  >
                    <img src="/blocks/stash.png" alt="" style={{ width: 16, height: 16, verticalAlign: "middle", marginRight: 4 }} onError={(e) => { e.currentTarget.style.display = "none"; }} />
                    🗄 Тайник
                  </button>
                )}
                {!adjacentTo(selectedBuilding) && (
                  <span className="raid-hint">Подойдите к зданию ближе.</span>
                )}
              </div>
              {(selectedBuilding.furniture || []).length > 0 && (
                <div className="raid-building-furniture" style={{ marginTop: 8, fontSize: 12, opacity: 0.85 }}>
                  Мебель внутри: {(selectedBuilding.furniture || []).filter((f) => !f.looted).length} / {(selectedBuilding.furniture || []).length}. Зайдите в здание, чтобы обыскать.
                </div>
              )}
            </div>
          )}

          <div className="raid-linear-buttons">
            {raid.atExit && (
              <button className="btn-exit-raid" disabled={busy} onClick={leaveRaid}>
                🚪 Покинуть рейд
              </button>
            )}
            {!raid.atExit && (
              <span className="raid-hint">Выход на востоке карты: 🚪</span>
            )}
            <button className="btn-abandon" disabled={busy} onClick={abandon}>
              🏃 Бросить рейд
            </button>
          </div>
        </div>

        <RaidFeed events={feed} />
      </div>

      {combat && (
        <Combat
          initial={combat}
          player={player}
          onUpdate={apply}
          onClose={onCombatClose}
          onDeath={onCombatDeath}
          onToast={onToast}
        />
      )}
    </div>
  );
}
