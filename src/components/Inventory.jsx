import { useState, useEffect, useRef } from "react";
import { api } from "../api";
import { ItemIcon, rarityClass } from "./ItemVisual";
import ItemDetails from "./ItemDetails";
import RubleIcon from "./RubleIcon";

const EQUIP_TYPES = {
  WeaponMelee: "weaponMelee",
  WeaponRanged: "weaponRanged",
  Helmet: "helmet",
  Armor: "armor",
  Backpack: "backpack",
};
const CONSUMABLE_TYPES = ["Food", "Drink", "Medicine"];

const slotOf = (item) =>
  item && item.isSuit ? "suit" : (EQUIP_TYPES[item?.type] || null);

const ContextMenu = ({ x, y, item, equippedSlot, onClose, onAction }) => {
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

  if (!item) return null;

  const slot = slotOf(item);
  const isConsumable = CONSUMABLE_TYPES.includes(item.type);

  return (
    <div ref={ref} className="ctx-menu" style={{ top: y, left: x }} onClick={(e) => e.stopPropagation()}>
      <div className="ctx-title">{item.name}</div>
      {equippedSlot && (
        <button onClick={() => { onAction("unequip", item, equippedSlot); onClose(); }}>⬇ Снять</button>
      )}
      {!equippedSlot && slot && (
        <button onClick={() => { onAction("equip", item); onClose(); }}>⬆ Надеть</button>
      )}
      {!equippedSlot && isConsumable && (
        <button onClick={() => { onAction("consume", item); onClose(); }}>🍽 Использовать</button>
      )}
      {!equippedSlot && (
        <>
          <button onClick={() => { onAction("stash", item); onClose(); }}>📦 В схрон</button>
          <div className="ctx-sep" />
          <button className="ctx-danger" onClick={() => { onAction("drop", item); onClose(); }}>🗑 Выбросить</button>
        </>
      )}
    </div>
  );
};

const StatRow = ({ icon, label, value, danger = false }) => (
  <div className={`stalker-stat ${danger ? "danger" : ""}`}>
    <span>{icon} {label}</span>
    <b>{value}</b>
  </div>
);

export default function Inventory({ player, act, onClose }) {
  const [draggedItem, setDraggedItem] = useState(null);
  const [ctx, setCtx] = useState(null);
  const [selected, setSelected] = useState(null);

  const capacity = player.inventoryCapacity || 12;
  const cells = [...(player.inventory || [])];
  while (cells.length < capacity) cells.push(null);

  const currentWeight = player.currentWeight ?? 0;
  const maxWeight = player.maxWeight ?? 50;
  const overweight = !!player.overweight || currentWeight > maxWeight;
  const weightPct = Math.min(100, (currentWeight / Math.max(1, maxWeight)) * 100);

  const dmg = 5 + (player.weapon?.damage ?? 0);
  const def = (player.helmet?.defense ?? 0) + (player.armor?.defense ?? 0) + (player.suit?.defense ?? 0);

  const handleDragStart = (e, item) => {
    setDraggedItem(item);
    e.dataTransfer.effectAllowed = "move";
    e.dataTransfer.setData("text/plain", item.id);
  };

  const handleDropOnSlot = (e, slotKey) => {
    e.preventDefault();
    e.stopPropagation();
    if (!draggedItem) return;
    if (slotOf(draggedItem) === slotKey) {
      act(() => api.equip(draggedItem.id));
    }
    setDraggedItem(null);
  };

  const handleDragOver = (e) => {
    e.preventDefault();
    e.dataTransfer.dropEffect = "move";
  };

  const handleContextMenu = (e, item, equippedSlot = null) => {
    e.preventDefault();
    e.stopPropagation();
    if (!item) return;
    setCtx({ x: e.clientX, y: e.clientY, item, equippedSlot });
  };

  const handleAction = (action, item, extra) => {
    switch (action) {
      case "equip": act(() => api.equip(item.id)); break;
      case "unequip": act(() => api.unequip(extra)); break;
      case "consume": act(() => api.consume(item.id)); break;
      case "stash": act(() => api.stashStore(item.id)); break;
      case "drop": act(() => api.drop(item.id)); break;
      default: break;
    }
  };

  const EquipSlot = ({ icon, item, slotKey, label }) => (
    <div
      className={`stalker-equip-slot ${item ? rarityClass(item.rarity) : ""} ${
        draggedItem && slotOf(draggedItem) === slotKey ? "drop-target" : ""
      }`}
      onClick={() => item && setSelected(item)}
      onContextMenu={(e) => item && handleContextMenu(e, item, slotKey)}
      onDragOver={handleDragOver}
      onDrop={(e) => handleDropOnSlot(e, slotKey)}
      title={item ? `${item.name} — ПКМ: снять` : label}
    >
      <div className="slot-icon">{item ? <ItemIcon item={item} /> : icon}</div>
      <span>{item ? item.name : label}</span>
    </div>
  );

  return (
    <div className="modal-bg" onClick={onClose}>
      <div className="stalker-inventory" onClick={(e) => e.stopPropagation()} style={{ position: "relative" }}>
        <button
          className="close-btn"
          onClick={onClose}
          title="Закрыть"
          style={{ position: "absolute", top: 8, right: 8, zIndex: 5 }}
        >
          ✖
        </button>
        <div className="stalker-inv-head" style={{ paddingRight: 46 }}>
          <div className="stalker-title">РЮКЗАК</div>

          <div className="weight-block">
            <div className="weight-label">
              <span>ВЕС</span>
              <b className={overweight ? "over" : ""}>{currentWeight.toFixed(1)} / {maxWeight} кг</b>
            </div>
            <div className="weight-bar">
              <div className={`weight-fill ${overweight ? "over" : ""}`} style={{ width: `${weightPct}%` }} />
            </div>
          </div>

          <div className="stalker-money"><RubleIcon size={15} /> {player.money}</div>
        </div>

        <div className="stalker-inv-body">
          <aside className="stalker-equip-col">
            <h4>ЭКИПИРОВКА</h4>
            <EquipSlot icon="🔪" item={player.weaponMelee} slotKey="weaponMelee" label="Ближний бой" />
            <EquipSlot icon="🔫" item={player.weaponRanged} slotKey="weaponRanged" label="Огнестрел" />
            <EquipSlot icon="🪖" item={player.helmet} slotKey="helmet" label="Шлем" />
            <EquipSlot icon="🦺" item={player.armor} slotKey="armor" label="Броня" />
            <EquipSlot icon="🧥" item={player.suit} slotKey="suit" label="Костюм" />
            <EquipSlot icon="🎒" item={player.backpack} slotKey="backpack" label="Рюкзак" />

            <h4>ПАРАМЕТРЫ</h4>
            <div className="stalker-stats">
              <StatRow icon="❤️" label="Здоровье" value={`${player.hp}/${player.maxHp}`} danger={player.hp < player.maxHp * 0.3} />
              <StatRow icon="⚡" label="Выносливость" value={`${player.stamina}/${player.maxStamina}`} danger={player.stamina < 25} />
              <StatRow icon="🍖" label="Голод" value={player.hunger} danger={player.hunger < 25} />
              <StatRow icon="💧" label="Жажда" value={player.thirst} danger={player.thirst < 25} />
              <StatRow icon="⚔️" label="Урон" value={dmg} />
              <StatRow icon="🛡️" label="Защита" value={def} />
              <StatRow icon="📦" label="Слотов" value={`${player.inventory.length}/${capacity}`} />
            </div>
          </aside>

          <section className="stalker-grid-col">
            <div className="stalker-section-head" style={{ display: "flex", alignItems: "center", gap: 8 }}>
              <span>ИНВЕНТАРЬ</span>
              <span style={{ marginLeft: "auto" }}>{player.inventory.length}/{capacity}</span>
              <button
                onClick={() => act(() => api.sortInventory())}
                title="Отсортировать инвентарь по категории (порядок сохранится)"
                style={{ fontSize: 12 }}
              >
                Сортировать по категории
              </button>
            </div>

            <div className="stalker-grid">
              {cells.map((item, i) => (
                <div
                  key={item ? item.id : `empty-${i}`}
                  className={`stalker-cell ${item ? rarityClass(item.rarity) : "empty"} ${
                    selected?.id === item?.id && item ? "selected" : ""
                  }`}
                  draggable={!!item}
                  onDragStart={(e) => item && handleDragStart(e, item)}
                  onDragEnd={() => setDraggedItem(null)}
                  onClick={() => item && setSelected(item)}
                  onContextMenu={(e) => handleContextMenu(e, item)}
                >
                  {item && (
                    <>
                      <ItemIcon item={item} />
                      {item.count > 1 && <span className="item-count-badge">×{item.count}</span>}
                    </>
                  )}
                </div>
              ))}
            </div>

            <div className="stalker-hint">
              ЛКМ — описание · ПКМ — действия · перетащи в слот экипировки
            </div>
          </section>

          <aside className="stalker-desc-col">
            {selected ? (
              <ItemDetails item={selected} onClose={() => setSelected(null)} title="ОПИСАНИЕ" />
            ) : (
              <div className="desc-empty">
                <span>ОПИСАНИЕ</span>
                <p>Выберите предмет, чтобы прочитать описание и характеристики.</p>
              </div>
            )}
          </aside>
        </div>
      </div>

      {ctx && (
        <ContextMenu
          x={ctx.x}
          y={ctx.y}
          item={ctx.item}
          equippedSlot={ctx.equippedSlot}
          onClose={() => setCtx(null)}
          onAction={handleAction}
        />
      )}
    </div>
  );
}
