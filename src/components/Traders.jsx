import { useEffect, useMemo, useState } from "react";
import { api } from "../api";
import { rarityClass, ItemIcon } from "./ItemVisual";
import { buyPriceFor, sellPriceFor, traderBuys, TYPE_LABELS } from "../economy";
import TraderDialogueModal from "./TraderDialogueModal";
import ItemDetails from "./ItemDetails";
import RubleIcon from "./RubleIcon";

const TRADER_PORTRAITS = {
  prapor: { image: "/traders/prapor.png", emoji: "🪖", label: "Шматко", sub: "Оружие и боеприпасы" },
  therapist: { image: "/traders/therapist.png", emoji: "💉", label: "Сергей", sub: "Только медицина" },
  ragman: { image: "/traders/ragman.png", emoji: "🧥", label: "Сидорович", sub: "Экипировка и рюкзаки" },
  blacksmith: { image: "/traders/blacksmith.png", emoji: "⚒", label: "Гриша", sub: "Броня и материалы" },
  buyer: { image: "/traders/buyer.png", emoji: "🕵", label: "Скупщик", sub: "Скупает всё подряд" },
  bartender: { image: "/traders/barman.png", emoji: "🍺", label: "Бармен", sub: "Еда и напитки" },
};

// Названия вкладок торговцев (как на вкладках/в интерфейсе).
const TRADER_TAB_LABELS = {
  prapor: "Шматко",
  therapist: "Медик",
  ragman: "Сидорович",
  blacksmith: "Гриша",
  buyer: "Скупщик",
  bartender: "Бармен",
};

const BUYS_LABELS = {
  WeaponMelee: "ближний бой",
  WeaponRanged: "оружие",
  Helmet: "шлемы",
  Armor: "броню",
  Backpack: "рюкзаки",
  Food: "еду",
  Drink: "питьё",
  Medicine: "медицину",
  Material: "материалы",
  Misc: "ценности",
};

const STACKABLE_TYPES = ["Food", "Drink", "Medicine", "Material", "Misc", "Ammo"];

export default function Traders({ player, act }) {
  const [traders, setTraders] = useState([]);
  const [buyMultiplier, setBuyMultiplier] = useState(1);
  const [sellMultiplier, setSellMultiplier] = useState(1);
  const [currentId, setCurrentId] = useState(null);
  const [sellDeal, setSellDeal] = useState([]);
  const [buyDeal, setBuyDeal] = useState([]);
  const [busy, setBusy] = useState(false);
  const [ctx, setCtx] = useState(null);
  const [questsOpen, setQuestsOpen] = useState(false);
  const [buyQuick, setBuyQuick] = useState(null);
  const [selected, setSelected] = useState(null);
  const [qty, setQty] = useState(1);
  const [notice, setNotice] = useState("");
  const [barterOpen, setBarterOpen] = useState(false);
  const [sellTab, setSellTab] = useState("inv");

  const loadTraders = async () => {
    const data = await api.getTraders();
    setTraders(data.traders || []);
    setBuyMultiplier(data.buyMultiplier ?? 1);
    setSellMultiplier(data.sellMultiplier ?? 1);
    if (!currentId && data.traders?.length) setCurrentId(data.traders[0].id);
  };

  useEffect(() => {
    loadTraders().catch(console.error);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  useEffect(() => {
    setSellDeal([]);
    setBuyDeal([]);
    setSelected(null);
    setNotice("");
    setSellTab("inv");
  }, [currentId]);

  useEffect(() => {
    if (!ctx) return;
    const close = () => setCtx(null);
    document.addEventListener("mousedown", close);
    document.addEventListener("keydown", close);
    return () => {
      document.removeEventListener("mousedown", close);
      document.removeEventListener("keydown", close);
    };
  }, [ctx]);

  useEffect(() => {
    if (!buyQuick) return;
    const close = () => setBuyQuick(null);
    document.addEventListener("mousedown", close);
    return () => document.removeEventListener("mousedown", close);
  }, [buyQuick]);

  const itemByCode = useMemo(() => {
    const map = {};
    traders.forEach((tr) => (tr.stock || []).forEach((i) => (map[i.code] = i)));
    return map;
  }, [traders]);

  // Источники для продажи: инвентарь или схрон убежища.
  const sellSources = useMemo(() => {
    const list = sellTab === "stash"
      ? (player.shelter?.stash || []).map((item) => ({ item, source: "stash" }))
      : (player.inventory || []).map((item) => ({ item, source: "inv" }));
    return list;
  }, [player.inventory, player.shelter, sellTab]);

  const t = traders.find((x) => x.id === currentId) || traders[0];
  if (!t) return null;

  const portrait = TRADER_PORTRAITS[t.id] || { emoji: "👤", label: t.name, sub: "" };

  const sellTotal = sellDeal.reduce(
    (s, i) => s + sellPriceFor(i, t, sellMultiplier) * (i.count || 1),
    0
  );
  const buyTotal = buyDeal.reduce(
    (s, x) => s + buyPriceFor(x.item, t, buyMultiplier) * (x.count || 1),
    0
  );
  const netTotal = buyTotal - sellTotal;

  const loyaltyLevel = t.loyaltyLevel || 1;
  const loyaltyCurrent = t.loyaltyCurrent || 0;
  const loyaltyNext = t.loyaltyNext || 0;
  const loyaltyPct = loyaltyNext > 0
    ? Math.min(100, Math.max(0, Math.round(
        ((t.loyaltyRep - loyaltyCurrent) / Math.max(1, loyaltyNext - loyaltyCurrent)) * 100
      )))
    : 100;

  const flash = (msg) => {
    setNotice(msg);
    setTimeout(() => setNotice(""), 2500);
  };

  const inSellDeal = (id) => sellDeal.some((i) => i.id === id);
  const inBuyDeal = (id) => buyDeal.some((x) => x.item.code === id);

  const addToSellDeal = (item) => {
    if (inSellDeal(item.id)) return;
    if (!traderBuys(t, item)) {
      flash(`${t.name} не покупает: ${TYPE_LABELS[item.type] || item.type}`);
      return;
    }
    setSellDeal([...sellDeal, item]);
  };
  const removeFromSellDeal = (id) => setSellDeal(sellDeal.filter((i) => i.id !== id));

  const isStackableItem = (item) => STACKABLE_TYPES.includes(item.type);

  const stacksWithInventory = (item) =>
    isStackableItem(item) && player.inventory.some((i) => i.code === item.code);

  // Стекуемый предмет занимает один слот, даже если покупаешь 40 штук.
  const slotCost = (item, count = 1) => {
    if (isStackableItem(item)) return stacksWithInventory(item) ? 0 : 1;
    return Math.max(1, count || 1);
  };

  const addToBuyDeal = (item, count = 1) => {
    if (inBuyDeal(item.code)) return;

    if ((item.requiredLoyalty || 0) > (t.loyaltyLevel || 1)) {
      flash(`Нужна лояльность ${item.requiredLoyalty} у ${t.name}`);
      return;
    }

    const availableMoney = player.money + sellTotal - buyTotal;
    if (availableMoney < buyPriceFor(item, t, buyMultiplier) * count) {
      flash("Не хватает денег");
      return;
    }

    const buySlots = buyDeal.reduce((s, x) => s + slotCost(x.item, x.count), 0);
    const freeSlots = player.inventoryCapacity - player.inventory.length + sellDeal.length;
    if (slotCost(item, count) > freeSlots - buySlots) {
      flash("В инвентаре нет места");
      return;
    }

    setBuyDeal([...buyDeal, { item, count }]);
  };

  const setBuyCount = (code, count) => {
    setBuyDeal(
      buyDeal.map((x) => (x.item.code === code ? { ...x, count: Math.max(1, count) } : x))
    );
  };
  const removeFromBuyDeal = (code) => setBuyDeal(buyDeal.filter((x) => x.item.code !== code));

  const clearAll = () => {
    setSellDeal([]);
    setBuyDeal([]);
    setNotice("");
  };

  const refreshPlayer = async () => {
    await act(() => Promise.resolve());
    await loadTraders().catch(() => {});
  };

  const executeDeal = async () => {
    if (busy) return;
    if (sellDeal.length === 0 && buyDeal.length === 0) return;
    setBusy(true);
    try {
      for (const item of sellDeal) {
        await api.sell(item.id, t.id);
      }
      if (buyDeal.length > 0) {
        await api.buyBatch(
          t.id,
          buyDeal.map((x) => ({ code: x.item.code, count: x.count || 1 }))
        );
      }
      clearAll();
      setSelected(null);
      await refreshPlayer();
    } catch (e) {
      console.error(e);
      flash(e.message || "Ошибка сделки");
      await refreshPlayer();
    } finally {
      setBusy(false);
    }
  };

  const doBarter = async (offer) => {
    if (busy) return;
    setBusy(true);
    try {
      await api.barter(t.id, offer.id);
      await refreshPlayer();
      flash("Обмен совершён");
    } catch (e) {
      flash(e.message || "Ошибка обмена");
    } finally {
      setBusy(false);
    }
  };

  // ----- drag & drop -----
  const onDragStartItem = (e, item, source) => {
    e.dataTransfer.setData("application/x-item", JSON.stringify({ source, item }));
    e.dataTransfer.effectAllowed = "move";
  };
  const readDrag = (e) => {
    try {
      return JSON.parse(e.dataTransfer.getData("application/x-item"));
    } catch {
      return null;
    }
  };

  const onDropSell = (e) => {
    e.preventDefault();
    const data = readDrag(e);
    if (data?.source === "inv" || data?.source === "stash") addToSellDeal(data.item);
  };
  const onDropBuy = (e) => {
    e.preventDefault();
    const data = readDrag(e);
    if (data?.source === "stock") addToBuyDeal(data.item, 1);
  };
  const onDropInv = (e) => {
    e.preventDefault();
    const data = readDrag(e);
    if (data?.source === "sellDeal") removeFromSellDeal(data.item.id);
  };
  const onDropStock = (e) => {
    e.preventDefault();
    const data = readDrag(e);
    if (data?.source === "buyDeal") removeFromBuyDeal(data.item.code);
  };
  const allowDrop = (e) => e.preventDefault();

  const canBarter = (offer) => {
    return offer.cost.every(
      (c) => player.inventory.filter((i) => i.code === c.code).reduce((s, i) => s + (i.count || 1), 0) >= c.count
    );
  };

  return (
    <div className="panel traders-panel">
      <h3>Торговцы</h3>

      <div className="tabs">
        {traders.map((x) => (
          <button
            key={x.id}
            className={t.id === x.id ? "active" : ""}
            onClick={() => setCurrentId(x.id)}
          >
            {TRADER_TAB_LABELS[x.id] || x.name}
          </button>
        ))}
      </div>

      <div className="trader-layout">
        <aside className="trader-portrait">
          <div className="portrait-frame">
            {portrait.image ? (
              <img
                className="portrait-img"
                src={portrait.image}
                alt={portrait.label}
                onError={(e) => {
                  e.currentTarget.style.display = "none";
                  e.currentTarget.nextSibling.style.display = "block";
                }}
              />
            ) : null}
            <div
              className="portrait-emoji"
              style={{ display: portrait.image ? "none" : "block" }}
            >
              {portrait.emoji}
            </div>
          </div>
          <div className="portrait-name">{portrait.label}</div>
          <div className="portrait-sub">{portrait.sub}</div>
          <div className="portrait-desc">{t.description}</div>

          <div className="portrait-money">
            <span><RubleIcon size={14} /> Баланс</span>
            <b>{player.money} <RubleIcon size={14} /></b>
            {(sellTotal > 0 || buyTotal > 0) && (
              <span className="cart-delta">
                {buyTotal > 0 && <>−{buyTotal} <RubleIcon size={12} /></>} {sellTotal > 0 && <>+{sellTotal} <RubleIcon size={12} /></>}
                {netTotal !== 0 && <> = {netTotal > 0 ? "−" : "+"}{Math.abs(netTotal)} <RubleIcon size={12} /></>}
              </span>
            )}
          </div>

          <div className="trader-rules">
            <span>Покупает:</span>
            <b>
              {t.buysEverything
                ? "всё (дёшево)"
                : (t.buys || []).map((x) => BUYS_LABELS[x] || x).join(", ") || "ничего"}
            </b>
            <span>Ставка выкупа: {Math.round((t.buyRate ?? 0.5) * 100)}%</span>
          </div>

          <div className="loyalty-panel">
            <div className="loyalty-head">
              <span>🤝 Лояльность</span>
              <b>{loyaltyLevel}/10</b>
            </div>
            <div className="loyalty-bar">
              <div style={{ width: `${loyaltyPct}%` }} />
            </div>
            <span className="loyalty-rep">
              {t.loyaltyRep} реп. {loyaltyNext > 0 ? `· до ${loyaltyNext}` : "· максимум"}
            </span>
          </div>

          <button className="portrait-quests-btn" onClick={() => setQuestsOpen(true)} disabled={t.isUnlocked === false}>
            💬 Диалог
          </button>
          {t.isUnlocked === false && <div className="hint">Торговец закрыт. Откройте через сюжет/задания.</div>}
          <button
            className="portrait-quests-btn barter-btn"
            onClick={() => setBarterOpen((v) => !v)}
          >
            🔄 Бартер {barterOpen ? "▲" : "▼"}
          </button>
        </aside>

        <section className="trader-trade">
          <div className="trade-grid-3">
            {/* Инвентарь / Схрон для продажи */}
            <div className="trade-panel" onDragOver={allowDrop} onDrop={onDropInv}>
              <div className="trade-tabs">
                <button className={sellTab === "inv" ? "active" : ""} onClick={() => setSellTab("inv")}>
                  📦 Инвентарь
                </button>
                <button className={sellTab === "stash" ? "active" : ""} onClick={() => setSellTab("stash")}>
                  🧰 Схрон
                </button>
              </div>
              <div className="trade-head">
                <b>
                  {sellTab === "inv"
                    ? `📦 Инвентарь (${player.inventory.length}/${player.inventoryCapacity})`
                    : `🧰 Схрон (${(player.shelter?.stash || []).length})`}
                </b>
              </div>
              <div className="trade-cells">
                {sellSources.map(({ item, source }) => {
                  const buyable = traderBuys(t, item);
                  return (
                    <div
                      key={item.id}
                      className={`trade-cell ${rarityClass(item.rarity)} ${
                        inSellDeal(item.id) ? "dealt" : ""
                      } ${buyable ? "" : "not-buyable"}`}
                      draggable={buyable && !inSellDeal(item.id)}
                      onDragStart={(e) => onDragStartItem(e, item, source)}
                      onClick={() => setSelected(item)}
                      onContextMenu={(e) => {
                        e.preventDefault();
                        if (!buyable) return flash(`${t.name} это не покупает`);
                        setCtx({ x: e.clientX, y: e.clientY, item });
                      }}
                      onDoubleClick={() => buyable && addToSellDeal(item)}
                      title={buyable ? item.name : `${item.name} — не покупается`}
                    >
                      <div className="trade-cell-icon">
                        <ItemIcon item={item} />
                        {item.count > 1 && <span className="item-count-badge">×{item.count}</span>}
                      </div>
                    </div>
                  );
                })}
                {Array.from({
                  length: Math.max(
                    0,
                    (sellTab === "inv"
                      ? player.inventoryCapacity
                      : (player.shelter?.stashSlots || 50)) -
                      (sellTab === "inv"
                        ? player.inventory.length
                        : (player.shelter?.stash || []).length)
                  ),
                }).map((_, i) => (
                  <div key={`e${i}`} className="trade-cell empty" />
                ))}
              </div>
              <div className="trade-hint">
                {sellTab === "inv"
                  ? "Клик — описание · двойной клик — продать · перетащи в центр"
                  : "Клик — описание · двойной клик — продать · перетащи в центр. Продажа напрямую из схрона."}
              </div>
            </div>

            {/* Сделки */}
            <div className="trade-deals">
              <div className="trade-panel deal-panel" onDragOver={allowDrop} onDrop={onDropSell}>
                <div className="trade-head sell">
                  <b>💸 Вы продаёте</b>
                  <span>{sellTotal} <RubleIcon size={13} /></span>
                </div>
                <div className="trade-cells deals">
                  {sellDeal.map((item) => (
                    <div
                      key={item.id}
                      className={`trade-cell ${rarityClass(item.rarity)}`}
                      draggable
                      onDragStart={(e) => onDragStartItem(e, item, "sellDeal")}
                      onClick={() => setSelected(item)}
                      onDoubleClick={() => removeFromSellDeal(item.id)}
                      title="Двойной клик — убрать"
                    >
                      <div className="trade-cell-icon">
                        <ItemIcon item={item} />
                        {item.count > 1 && <span className="item-count-badge">×{item.count}</span>}
                      </div>
                      <div className="trade-cell-price">
                        +{sellPriceFor(item, t, sellMultiplier) * (item.count || 1)}
                      </div>
                    </div>
                  ))}
                  {sellDeal.length === 0 && (
                    <div className="deal-empty">Перетащи сюда предмет, чтобы продать</div>
                  )}
                </div>
              </div>

              <div className="trade-panel deal-panel" onDragOver={allowDrop} onDrop={onDropBuy}>
                <div className="trade-head buy">
                  <b>🛒 Вы покупаете</b>
                  <span>{buyTotal} <RubleIcon size={13} /></span>
                </div>
                <div className="trade-cells deals">
                  {buyDeal.map(({ item, count }) => (
                    <div
                      key={item.code}
                      className={`trade-cell ${rarityClass(item.rarity)}`}
                      onClick={() => setSelected(item)}
                      onDoubleClick={() => removeFromBuyDeal(item.code)}
                      title="Двойной клик — убрать"
                    >
                      <div className="trade-cell-icon">
                        <ItemIcon item={item} />
                      </div>
                      <div className="trade-cell-count">×{count || 1}</div>
                      <div className="trade-cell-count-controls" onClick={(e) => e.stopPropagation()}>
                        <button onClick={() => setBuyCount(item.code, (count || 1) - 1)}>−</button>
                        <button onClick={() => setBuyCount(item.code, (count || 1) + 1)}>+</button>
                      </div>
                    </div>
                  ))}
                  {buyDeal.length === 0 && (
                    <div className="deal-empty">Перетащи сюда предмет из стока, чтобы купить</div>
                  )}
                </div>
              </div>
            </div>

            {/* Сток торговца */}
            <div className="trade-panel" onDragOver={allowDrop} onDrop={onDropStock}>
              <div className="trade-head">
                <b>🏪 {portrait.label}</b>
              </div>
              <div className="trade-cells">
                {(t.stock || []).map((item) => {
                  const locked = (item.requiredLoyalty || 0) > loyaltyLevel;
                  return (
                    <div
                      key={item.id}
                      className={`trade-cell ${rarityClass(item.rarity)} ${
                        inBuyDeal(item.code) ? "dealt" : ""
                      } ${locked ? "loyalty-locked" : ""}`}
                      draggable={!locked && !inBuyDeal(item.code)}
                      onDragStart={(e) => !locked && onDragStartItem(e, item, "stock")}
                      onClick={() => {
                        setSelected(item);
                        setQty(1);
                      }}
                      onDoubleClick={() => !locked && addToBuyDeal(item, 1)}
                      onContextMenu={(e) => {
                        e.preventDefault();
                        if (locked) {
                          flash(`Нужна лояльность ${item.requiredLoyalty} у ${t.name}`);
                          return;
                        }
                        setBuyQuick({ x: e.clientX, y: e.clientY, item });
                      }}
                      title={locked ? `${item.name} — нужна лояльность ${item.requiredLoyalty}` : item.name}
                    >
                      <div className="trade-cell-icon">
                        <ItemIcon item={item} />
                        {locked && <span className="loyalty-lock">🔒</span>}
                      </div>
                      <div className="trade-cell-price">
                        {locked ? `лор. ${item.requiredLoyalty}` : <>{buyPriceFor(item, t, buyMultiplier)} <RubleIcon size={11} /></>}
                      </div>
                    </div>
                  );
                })}
              </div>
              <div className="trade-hint">
                Клик — описание · двойной клик — купить · ПКМ — количество
              </div>
            </div>
          </div>

          {/* Описание предмета по клику (не всплывает поверх предметов) */}
          {selected && (
            <ItemDetails
              item={selected}
              trader={t}
              buyMultiplier={buyMultiplier}
              sellMultiplier={sellMultiplier}
              quantity={qty}
              onQuantity={setQty}
              onBuy={
                (t.stock || []).some((s) => s.code === selected.code)
                  ? (item, count) => addToBuyDeal(item, count)
                  : null
              }
              busy={busy}
              onClose={() => setSelected(null)}
            />
          )}

          {notice && <div className="trade-notice">⚠ {notice}</div>}

          {barterOpen && (
            <div className="barter-panel">
              <div className="barter-head">
                <b>🔄 Бартер — {portrait.label}</b>
                <span>Обмен без денег (или с доплатой)</span>
              </div>
              {(t.barter || []).length === 0 ? (
                <p className="desc">Этот торговец не меняется.</p>
              ) : (
                <div className="barter-list">
                  {t.barter.map((offer) => {
                    const gift = itemByCode[offer.givesCode];
                    const locked = (offer.requiredLoyalty || 0) > loyaltyLevel;
                    const ok = !locked && canBarter(offer);
                    return (
                      <div
                        key={offer.id}
                        className={`barter-offer ${ok ? "ready" : ""} ${locked ? "loyalty-locked" : ""}`}
                      >
                        <div className="barter-gift">
                          <ItemIcon item={gift || { name: offer.givesCode }} />
                          <span>
                            {gift?.name || offer.givesCode}{offer.givesCount > 1 ? ` ×${offer.givesCount}` : ""}
                          </span>
                        </div>
                        <div className="barter-cost">
                          {locked && <span className="no">🔒 лояльность {offer.requiredLoyalty}</span>}
                          {!locked && offer.cost.map((c) => {
                            const costItem = itemByCode[c.code];
                            const have = player.inventory
                              .filter((i) => i.code === c.code)
                              .reduce((s, i) => s + (i.count || 1), 0);
                            return (
                              <span key={c.code} className={have >= c.count ? "ok" : "no"}>
                                {costItem?.name || c.code} {have}/{c.count}
                              </span>
                            );
                          })}

                        </div>
                        <button disabled={!ok || busy} onClick={() => doBarter(offer)}>
                          Обменять
                        </button>
                      </div>
                    );
                  })}
                </div>
              )}
            </div>
          )}

          <div className="trade-footer">
            <div className="trade-totals">
              <div className="trade-total-row">
                <span>Продажа:</span>
                <b className="plus">+{sellTotal} <RubleIcon size={13} /></b>
              </div>
              <div className="trade-total-row">
                <span>Покупка:</span>
                <b className="minus">−{buyTotal} <RubleIcon size={13} /></b>
              </div>
              <div className="trade-total-row total">
                <span>Итого:</span>
                <b className={netTotal > 0 ? "minus" : "plus"}>
                  {netTotal > 0 ? "−" : netTotal < 0 ? "+" : ""}
                  {Math.abs(netTotal)} <RubleIcon size={13} />
                </b>
              </div>
            </div>

            <div className="trade-actions">
              <button
                className="trade-btn-clear"
                onClick={clearAll}
                disabled={sellDeal.length === 0 && buyDeal.length === 0}
              >
                ✕ Очистить
              </button>
              <button
                className="trade-btn-execute"
                onClick={executeDeal}
                disabled={
                  busy ||
                  (sellDeal.length === 0 && buyDeal.length === 0) ||
                  player.money + sellTotal < buyTotal
                }
              >
                {busy
                  ? "Сделка..."
                  : `🤝 Торговать (${sellDeal.length}↓ / ${buyDeal.length}↑)`}
              </button>
            </div>
          </div>
        </section>
      </div>

      {/* ПКМ по инвентарю — быстрая продажа */}
      {ctx && (
        <div
          className="ctx-menu"
          style={{ top: ctx.y, left: ctx.x }}
          onMouseDown={(e) => e.stopPropagation()}
        >
          <div className="ctx-title">{ctx.item.name}</div>
          <button
            onClick={() => {
              addToSellDeal(ctx.item);
              setCtx(null);
            }}
          >
            <span style={{ display: "inline-flex", alignItems: "center", gap: 4 }}>
              💸 Продать ({sellPriceFor(ctx.item, t, sellMultiplier) * (ctx.item.count || 1)} <RubleIcon size={12} />)
            </span>
          </button>
        </div>
      )}

      {/* ПКМ на стоке — количество */}
      {buyQuick && (
        <div
          className="ctx-menu"
          style={{ top: buyQuick.y, left: buyQuick.x }}
          onMouseDown={(e) => e.stopPropagation()}
        >
          <div className="ctx-title" style={{ display: "flex", alignItems: "center", gap: 4 }}>
            {buyQuick.item.name} — {buyPriceFor(buyQuick.item, t, buyMultiplier)} <RubleIcon size={12} />
          </div>
          {[1, 5, 10].map((n) => (
            <button
              key={n}
              onClick={() => {
                addToBuyDeal(buyQuick.item, n);
                setBuyQuick(null);
              }}
            >
              <span style={{ display: "inline-flex", alignItems: "center", gap: 4 }}>
                🛒 Купить ×{n} ({buyPriceFor(buyQuick.item, t, buyMultiplier) * n} <RubleIcon size={12} />)
              </span>
            </button>
          ))}
          <button
            onClick={() => {
              const input = window.prompt("Сколько купить?", "1");
              const n = parseInt(input, 10);
              if (!isNaN(n) && n > 0) addToBuyDeal(buyQuick.item, n);
              setBuyQuick(null);
            }}
          >
            🛒 Ввести количество…
          </button>
        </div>
      )}

      {questsOpen && (
        <TraderDialogueModal
          traderId={t.id}
          traderName={TRADER_TAB_LABELS[t.id] || t.name}
          onClose={() => setQuestsOpen(false)}
          onUpdate={async () => { await act(() => Promise.resolve()); }}
        />
      )}
    </div>
  );
}
