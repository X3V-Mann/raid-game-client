import { useEffect, useState } from "react";
import { api } from "../../api";
import Combat from "./Combat";
import RaidFeed from "./RaidFeed";

function addItemsToInventory(inventory, items, capacity) {
  const inv = inventory.map((i) => ({ ...i }));
  // Ammo добавлен — патроны стакаются, как на бэкенде.
  const STACKABLE = ["Food", "Drink", "Medicine", "Material", "Misc", "Ammo"];
  let overflow = false;

  for (const item of items) {
    if (STACKABLE.includes(item.type)) {
      const existing = inv.find((i) => i.code === item.code);
      if (existing) {
        existing.count = (existing.count || 1) + (item.count || 1);
        continue;
      }
    }
    if (inv.length < capacity) {
      inv.push({ ...item, count: item.count || 1 });
    } else {
      overflow = true;
    }
  }
  return { inv, overflow };
}

export default function Raid({ player, location, onExit, onToast, onPlayerChange }) {
  const [encounters, setEncounters] = useState([]);
  const [currentIndex, setCurrentIndex] = useState(-1);
  const [combatOpen, setCombatOpen] = useState(false);
  const [feed, setFeed] = useState([]);
  const [finished, setFinished] = useState(false);

  // Отдых: разрешён один раз на каждой клетке.
  // -99 — «ещё не отдыхали ни на одной клетке».
  const [lastRestedAt, setLastRestedAt] = useState(-99);

  const addFeed = (item) => setFeed((f) => [...f, item].slice(-5));

  useEffect(() => {
    api
      .startRaid(location?.tier ?? 1, location?.id ?? "city")
      .then((enc) => {
        setEncounters(enc);
        addFeed({
          kind: "info",
          icon: "🌆",
          title: "Рейд начался",
          text: `${location?.name || "Локация"}. Впереди ${enc.length} клеток.`,
        });
      })
      .catch((e) => {
        onToast?.(e.message || "Ошибка старта рейда");
        onExit?.(player);
      });
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const nextStep = async () => {
    if (finished || combatOpen) return;
    const next = currentIndex + 1;

    if (next >= encounters.length) {
      finishRaid();
      return;
    }

    const enc = encounters[next];
    const isStart = next === 0;
    const isEnd = next === encounters.length - 1;

    let p = { ...player };
    if (!isStart && !isEnd) {
      const maxW = p.maxWeight || 50;
      const loadFactor = 1 + Math.max(0, (p.currentWeight || 0) - maxW * 0.5) / Math.max(1, maxW);
      const cost = Math.round((3 + Math.random() * 12) * loadFactor);

      p.stamina = Math.max(0, p.stamina - cost);
      p.hunger = Math.max(0, p.hunger - Math.ceil(cost / 2));
      p.thirst = Math.max(0, p.thirst - Math.ceil(cost / 2));

      const exhausted = p.stamina <= 0;
      const starving = p.hunger <= 0 || p.thirst <= 0;

      if (exhausted || starving) {
        const dmg = 1 + Math.ceil(cost / 3);
        p.hp = Math.max(1, p.hp - dmg);
        addFeed({
          kind: "trap",
          icon: "💀",
          title: starving ? "Истощение" : "Усталость",
          text: `−${cost} выносливости${starving ? `, −${Math.ceil(cost / 2)} голода/жажды` : ""} · −${dmg} HP`,
        });
      } else {
        addFeed({
          kind: "info",
          icon: "🎒",
          title: "В пути",
          text: `−${cost} выносливости, −${Math.ceil(cost / 2)} голода и жажды`,
        });
      }
    }

    // Небольшой лут может попасться и на пустой/сюжетной клетке.
    let foundText = "";
    const t = enc.type;
    if (t !== "Loot" && t !== 1 && (enc.lootItems?.length > 0 || enc.moneyReward > 0)) {
      const { inv, overflow } = addItemsToInventory(p.inventory, enc.lootItems || [], p.inventoryCapacity);
      const names = (enc.lootItems || [])
        .map((i) => (i.count > 1 ? `${i.name} ×${i.count}` : i.name))
        .join(", ");
      p = {
        ...p,
        inventory: inv,
        money: p.money + (enc.moneyReward || 0),
        xp: p.xp + (enc.xpReward || 0),
      };
      foundText = `${names ? `Найдено: ${names}. ` : ""}${enc.moneyReward ? `+${enc.moneyReward} ₽` : ""}${overflow ? " (часть не влезла)" : ""}`;
    }

    onPlayerChange?.(p);
    setCurrentIndex(next);
    // Новая клетка — отдых снова доступен.
    setLastRestedAt(-99);

    if (t === "Combat" || t === 2) {
      addFeed({ kind: "combat", icon: "⚔️", title: `Клетка ${next + 1}`, text: "Засада!" });
      // Перед боем отдаём серверу актуальные выносливость/голод/вес,
      // чтобы усталость в пути влияла на бой.
      try {
        await api.syncPlayer(p);
      } catch (e) {
        console.error(e);
      }
      setCombatOpen(true);
    } else if (t === "Loot" || t === 1) {
      const { inv, overflow } = addItemsToInventory(p.inventory, enc.lootItems || [], p.inventoryCapacity);
      const names = (enc.lootItems || []).map((i) => (i.count > 1 ? `${i.name} ×${i.count}` : i.name)).join(", ");
      const finalPlayer = { ...p, inventory: inv, money: p.money + (enc.moneyReward || 0), xp: p.xp + (enc.xpReward || 0) };
      onPlayerChange?.(finalPlayer);

      addFeed({
        kind: "loot", icon: "📦", title: `Клетка ${next + 1} · ${enc.title || "Находка"}`,
        text: `${names ? `Найдено: ${names}. ` : ""} +${enc.moneyReward || 0} ₽`,
      });

      if (overflow) {
        addFeed({ kind: "trap", icon: "⚠️", title: "Инвентарь полон", text: "Часть лута потеряна!" });
      }
    } else if (t === "Scene" || t === 3) {
      addFeed({ kind: enc.icon === "💭" ? "thought" : "scene", icon: enc.icon || "·", title: `Клетка ${next + 1} · ${enc.title}`, text: enc.description });
    } else if (t === "Trap" || t === 5) {
      const hpAfterTrap = Math.max(1, p.hp - (enc.damage || 5));
      onPlayerChange?.({ ...p, hp: hpAfterTrap });
      addFeed({ kind: "trap", icon: "⚠️", title: `Клетка ${next + 1} · Ловушка`, text: `−${enc.damage || 5} HP` });
    } else {
      addFeed({ kind: "empty", icon: "·", title: `Клетка ${next + 1} · Тишина`, text: "Пусто." });
    }

    if (foundText) {
      addFeed({
        kind: "loot",
        icon: "📦",
        title: `Клетка ${next + 1} · Находка`,
        text: foundText,
      });
    }

    if (next === encounters.length - 1) {
      addFeed({ kind: "info", icon: "🚪", title: "Выход рядом", text: "Ты дошёл до конца рейда" });
    }
  };

  const onCombatEnd = (updatedPlayer, result) => {
    setCombatOpen(false);

    if (result?.error) {
      onToast?.(`Не удалось начать бой: ${result.error}`);
      onPlayerChange?.(updatedPlayer);
      onExit?.(updatedPlayer);
      return;
    }

    onPlayerChange?.(updatedPlayer);

    if (result?.playerWon) {
      const { inv, overflow } = addItemsToInventory(updatedPlayer.inventory, result.droppedLoot || [], updatedPlayer.inventoryCapacity);
      const newPlayer = {
        ...updatedPlayer,
        inventory: inv,
        money: updatedPlayer.money + (result.droppedMoney || 0),
        xp: updatedPlayer.xp + (result.droppedXp || 0),
      };
      onPlayerChange?.(newPlayer);
      addFeed({
        kind: "win", icon: "🏆", title: "Победа",
        text: `+${result.droppedMoney || 0} ₽ · +${result.droppedXp || 0} XP`,
      });
      if (overflow) addFeed({ kind: "trap", icon: "⚠️", title: "Инвентарь полон", text: "Часть лута из боя потеряна!" });
    } else if (result?.playerFled) {
      addFeed({ kind: "info", icon: "🏃", title: "Отступление", text: "Ты сбежал из боя" });
    } else if (result?.playerDied) {
      setFinished(true); // блокируем кнопки, пока идёт обработка смерти
      addFeed({ kind: "death", icon: "☠️", title: "Ты погиб", text: "Рейд провален. Весь лут потерян." });
      (async () => {
        try {
          // Смерть обрабатывает сервер: чистит экипировку и инвентарь.
          const dead = await api.die();
          onPlayerChange?.(dead);
          setTimeout(() => onExit({ ...dead, died: true, raidCells: currentIndex + 1 }), 1500);
        } catch (e) {
          console.error(e);
          onExit({ ...updatedPlayer, hp: 0, died: true });
        }
      })();
    }
  };

  // Отдых разрешён, если стоим на клетке и ещё не отдыхали именно на ней.
  const canRest = !finished && !combatOpen && currentIndex >= 0 && currentIndex !== lastRestedAt;

  const rest = () => {
    if (!canRest) return;
    const maxHp = player.maxHp || 100;
    const maxSt = player.maxStamina || 100;
    const hpGain = Math.max(1, Math.round(maxHp * 0.05));
    const stGain = Math.max(1, Math.round(maxSt * 0.05));
    const newPlayer = {
      ...player,
      hp: Math.min(maxHp, (player.hp || 0) + hpGain),
      stamina: Math.min(maxSt, (player.stamina || 0) + stGain),
    };
    setLastRestedAt(currentIndex);
    onPlayerChange?.(newPlayer);
    addFeed({ kind: "info", icon: "😴", title: "Отдых", text: `+${hpGain} HP, +${stGain} выносливости` });
  };

  const finishRaid = () => {
    if (finished) return;
    setFinished(true);
    onToast?.("Рейд завершён");
    // Синхронизацией и статистикой занимается App.jsx (onExit).
    onExit({ ...player, raidCells: encounters.length });
  };

  const abandonRaid = () => {
    if (!window.confirm("Покинуть рейд? Вы получите только добычу, найденную по пути.")) return;
    if (finished) return;
    setFinished(true);
    onExit({ ...player, hp: Math.max(1, player.hp - 5), raidCells: currentIndex + 1 });
  };

  if (encounters.length === 0) return <div className="loading">Готовим рейд...</div>;

  return (
    <div className="raid-screen">
      <div className="raid-banner" style={location?.image ? { backgroundImage: `url(${location.image})` } : undefined}>
        <div className="raid-banner-overlay" />
        <div className="raid-banner-content">
          <h3>{location?.name}</h3>
          <div className="raid-banner-stats">
            <span>❤️ {player.hp}/{player.maxHp}</span>
            <span>⚡ {player.stamina}/{player.maxStamina}</span>
            <span>🍖 {player.hunger}</span>
            <span>💧 {player.thirst}</span>
            <span>⚖ {player.currentWeight ?? 0}/{player.maxWeight ?? 50}</span>
            <span>💰 {player.money}</span>
          </div>
        </div>
      </div>

      <div className="raid-body">
        <div className="raid-main">
          <div className="raid-progress-bar">
            <div className="raid-progress-fill" style={{ width: `${(Math.max(currentIndex + 1, 0) / encounters.length) * 100}%` }} />
            <span className="raid-progress-text">{Math.max(currentIndex + 1, 0)} / {encounters.length}</span>
          </div>

          <div className="raid-track" style={{ flexWrap: "wrap", justifyContent: "center" }}>
            {encounters.map((enc, i) => {
              const isDone = i < currentIndex;
              const isCurrent = i === currentIndex;
              const isNext = i === currentIndex + 1;
              return (
                <div key={i} className={`raid-cell ${isDone ? "done" : ""} ${isCurrent ? "current" : ""} ${isNext ? "next" : ""}`} style={{ flex: "0 0 40px", minWidth: "40px", height: "40px" }}>
                  <div className="raid-cell-index" style={{ fontSize: "8px" }}>{i + 1}</div>
                  <div className="raid-cell-icon" style={{ fontSize: "16px" }}>{isDone ? "✓" : isCurrent ? "🙂" : isNext ? "▶" : ""}</div>
                </div>
              );
            })}
          </div>

          <div className="raid-linear-buttons">
            {!finished && currentIndex < encounters.length - 1 && !combatOpen && (
              <button className="btn-next" onClick={nextStep}>▶ Идти дальше</button>
            )}
            {canRest && (
              <button className="btn-next" onClick={rest}>😴 Отдохнуть</button>
            )}
            {currentIndex === encounters.length - 1 && !finished && !combatOpen && (
              <button className="btn-exit-raid" onClick={finishRaid}>🚪 Выйти из рейда</button>
            )}
            {!finished && (
              <button className="btn-abandon" onClick={abandonRaid}>🏃 Покинуть рейд</button>
            )}
          </div>
        </div>
        <RaidFeed events={feed} />
      </div>

      {combatOpen && (
        <Combat
          player={player}
          locationId={location?.id}
          onEnd={onCombatEnd}
          onPlayerChange={onPlayerChange}
        />
      )}
    </div>
  );
}