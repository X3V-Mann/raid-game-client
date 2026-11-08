import { ItemIcon, rarityClass, rarityName } from "./ItemVisual";
import { TYPE_LABELS, buyPriceFor, sellPriceFor, traderBuys } from "../economy";
import RubleIcon from "./RubleIcon";

export default function ItemDetails({
  item,
  trader = null,
  buyMultiplier = 1,
  sellMultiplier = 1,
  onClose,
  title = "Предмет",
  quantity = 1,
  onQuantity = null,
  onBuy = null,
  busy = false,
}) {
  if (!item) return null;

  const stats = [];
  if (item.damage > 0) stats.push(["⚔", "Урон", item.damage]);
  if (item.defense > 0) stats.push(["🛡", "Защита", item.defense]);
  if (item.backpackSlots > 0) stats.push(["🎒", "Слотов", item.backpackSlots]);
  if (item.restore > 0) stats.push(["✚", "Восстановление", item.restore]);
  if (item.weight > 0) stats.push(["⚖", "Вес", `${(item.weight * (item.count || 1)).toFixed(1)} кг`]);
  // Параметры оружия
  if (item.accuracy > 0) stats.push(["🎯", "Меткость", `${item.accuracy}`]);
  if (item.rangeEffective > 0) stats.push(["📏", "Дальнобойность", `${item.rangeEffective} кл.`]);
  if (item.rangeMax > 0 && item.rangeMax !== item.rangeEffective) stats.push(["🔭", "Макс. дальность", `${item.rangeMax} кл.`]);
  if (item.requiredSkill > 0) stats.push(["📚", "Требуется навык", `${item.requiredSkill} лvl`]);
  // Параметры брони
  if (item.slowdown > 0) stats.push(["🐌", "Замедление", `-${item.slowdown}%`]);
  if (item.blastProtection > 0) stats.push(["💥", "Защита от взрыва", `${item.blastProtection}%`]);
  if (item.bulletProtection > 0) stats.push(["🔫", "Защита от пуль", `${item.bulletProtection}%`]);
  if (item.physicalProtection > 0) stats.push(["👊", "Защита от физ. урона", `${item.physicalProtection}%`]);

  const canSell = trader ? traderBuys(trader, item) : false;
  const loyaltyLevel = trader?.loyaltyLevel ?? 1;
  const loyaltyLocked = trader && (item.requiredLoyalty || 0) > loyaltyLevel;
  const buy = trader ? buyPriceFor(item, trader, buyMultiplier) : null;
  const sell = trader && canSell ? sellPriceFor(item, trader, sellMultiplier) : null;
  const canBuy = !!onBuy && !loyaltyLocked;

  return (
    <div className="item-details stalker-desc-block">
      <div className="item-details-head">
        <span>{title}</span>
        {onClose && (
          <button className="close-btn small" onClick={onClose} title="Закрыть описание">
            ✖
          </button>
        )}
      </div>

      <div className="item-details-body">
        <div className={`item-details-icon ${rarityClass(item.rarity)}`}>
          <ItemIcon item={item} />
        </div>

        <div className="item-details-info">
          <div className="item-details-title">
            <b>{item.name}</b>
            {item.count > 1 && <span className="item-count">×{item.count}</span>}
          </div>

          <div className="item-details-tags">
            <span className={`rarity-tag ${rarityClass(item.rarity)}`}>{rarityName(item.rarity)}</span>
            <span className="type-tag">
              {(item.type === "Armor" && (item.isSuit || item.IsSuit))
                ? "Одежда"
                : (TYPE_LABELS[item.type] || item.type)}
            </span>
            {item.requiredLoyalty > 1 && (
              <span className={`type-tag ${loyaltyLocked ? "tag-locked" : ""}`}>
                🔒 лояльность {item.requiredLoyalty}
              </span>
            )}
          </div>

          <p className="item-details-desc">{item.description || "Описание пока отсутствует."}</p>

          {stats.length > 0 && (
            <div className="item-details-stats">
              {stats.map(([icon, label, value]) => (
                <span key={label}>{icon} {label}: <b>{value}</b></span>
              ))}
            </div>
          )}

          {trader && (
            <div className="item-details-prices">
              <span>Купить: <b>{loyaltyLocked ? "—" : <>{buy} <RubleIcon size={13} /></>}</b></span>
              <span className={canSell ? "" : "muted"}>
                {canSell ? <>Продать: {sell} <RubleIcon size={13} /></> : `${trader.name} это не покупает`}
              </span>
            </div>
          )}

          {loyaltyLocked && (
            <p className="loyalty-warning">
              🔒 Нужен {item.requiredLoyalty}-й уровень лояльности у {trader.name}.
              Сейчас {loyaltyLevel}-й.
            </p>
          )}

          {canBuy && (
            <div className="buy-controls">
              <span className="buy-controls-label">Количество</span>
              <div className="qty-stepper big">
                <button
                  disabled={quantity <= 1}
                  onClick={() => onQuantity?.(Math.max(1, quantity - 1))}
                >
                  −
                </button>
                <input
                  type="number"
                  min="1"
                  max="9999"
                  value={quantity}
                  onChange={(e) => {
                    const v = parseInt(e.target.value, 10);
                    onQuantity?.(Number.isFinite(v) && v >= 1 ? Math.min(9999, v) : 1);
                  }}
                  onBlur={(e) => {
                    const v = parseInt(e.target.value, 10);
                    if (!Number.isFinite(v) || v < 1) onQuantity?.(1);
                  }}
                  style={{ width: 64, textAlign: "center" }}
                />
                <button onClick={() => onQuantity?.(Math.min(9999, quantity + 1))}>＋</button>
              </div>
              <button className="buy-button" disabled={busy} onClick={() => onBuy?.(item, Math.max(1, quantity || 1))}>
                🛒 Купить за {buy * Math.max(1, quantity || 1)} <RubleIcon size={14} />
              </button>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
