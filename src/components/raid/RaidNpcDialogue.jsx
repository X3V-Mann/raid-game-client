import { useMemo, useState } from "react";
import { api } from "../../api";
import RubleIcon from "../RubleIcon";

const HOW_REPLIES = [
  "Да живём пока. Зона не щадит, но и мы не сдаёмся.",
  "Бывало и лучше, но жаловаться грех — ещё дышим.",
  "Нормально. Главное — ноги целы да патроны есть.",
  "Потихоньку. День прошёл — и ладно.",
];

const WHAT_REPLIES = [
  "Осматриваюсь тут, хожу по своим делам.",
  "Слежу, чтобы никто лишний не шарился. Тихо вроде.",
  "Путь держу, передохнуть решил. А ты что тут забыл?",
  "Да так, проверяю местность. Всякое в Зоне бывает.",
];

function pick(arr) {
  return arr[Math.floor(Math.random() * arr.length)];
}

export default function RaidNpcDialogue({ npc, player, onClose, onAction, onToast }) {
  const [view, setView] = useState("menu"); // menu | tradeAsk | buy | sell
  const [lines, setLines] = useState([{ who: npc.name, text: "Здорово, сталкер." }]);
  const [busy, setBusy] = useState(false);

  const sellable = useMemo(() => {
    const inv = player?.inventory || [];
    return inv.filter((i) => {
      const code = i.code || i.Code || "";
      if (!code) return true;
      return !/^quest_/i.test(code) && !/^q_/i.test(code) && !/^boss_/i.test(code) && !/^art_/i.test(code);
    });
  }, [player]);

  const say = (who, text) => setLines((prev) => [...prev, { who, text }].slice(-8));

  const doTalk = async () => {
    if (busy) return;
    setBusy(true);
    try {
      const res = await api.npcTalk(npc.id);
      const reply = res?.npcReply || res?.message || "…";
      say(npc.name, reply);
      onAction?.(res);
    } catch (e) {
      onToast?.(e.message || "Не удалось поговорить");
    } finally {
      setBusy(false);
    }
  };

  const buy = async (code) => {
    if (busy) return;
    setBusy(true);
    try {
      const res = await api.npcBuy(npc.id, code, 1);
      say(npc.name, "Договорились. Забирай.");
      onAction?.(res);
      onToast?.(res?.message || "Куплено");
    } catch (e) {
      onToast?.(e.message || "Не удалось купить");
    } finally {
      setBusy(false);
    }
  };

  const sell = async (item) => {
    if (busy) return;
    setBusy(true);
    try {
      const res = await api.npcSell(npc.id, item.id, 1);
      say(npc.name, "Подойдёт, беру.");
      onAction?.(res);
      onToast?.(res?.message || "Продано");
    } catch (e) {
      onToast?.(e.message || "Не удалось продать");
    } finally {
      setBusy(false);
    }
  };

  return (
    <div className="modal-overlay" onClick={onClose} style={{ zIndex: 1000 }}>
      <div className="modal raid-npc-dialogue" onClick={(e) => e.stopPropagation()} style={{ maxWidth: 520 }}>
        <div style={{ display: "flex", alignItems: "center", gap: 10, marginBottom: 10 }}>
          <img
            src={npc.icon}
            alt=""
            style={{ width: 48, height: 48, borderRadius: "50%", objectFit: "cover", border: "2px solid #7ecfff" }}
            onError={(e) => { e.currentTarget.style.display = "none"; }}
          />
          <div style={{ flex: 1 }}>
            <b>{npc.name}</b>
            <div className="desc" style={{ fontSize: 12 }}>Сталкер поблизости</div>
          </div>
          <button className="close-btn" onClick={onClose}>✖</button>
        </div>

        <div className="raid-dialogue-lines" style={{ maxHeight: 180, overflowY: "auto", marginBottom: 10, padding: 8, background: "rgba(0,0,0,0.25)", borderRadius: 6 }}>
          {lines.map((l, i) => (
            <div key={i} style={{ marginBottom: 6 }}>
              <b>{l.who}:</b> <span>«{l.text}»</span>
            </div>
          ))}
        </div>

        {view === "menu" && (
          <div style={{ display: "flex", flexDirection: "column", gap: 6 }}>
            <button disabled={busy} onClick={() => { say("Вы", "Как дела?"); say(npc.name, pick(HOW_REPLIES)); }}>Как дела?</button>
            <button disabled={busy} onClick={() => { say("Вы", "Что делаешь тут?"); say(npc.name, pick(WHAT_REPLIES)); }}>Что делаешь тут?</button>
            <button disabled={busy} onClick={doTalk}>Поговорить</button>
            <button disabled={busy} onClick={() => { say("Вы", "Хочешь поторговать?"); setView("tradeAsk"); }}>Хочешь поторговать?</button>
            <button onClick={onClose}>Уйти</button>
          </div>
        )}

        {view === "tradeAsk" && (
          <div style={{ display: "flex", flexDirection: "column", gap: 6 }}>
            <div className="desc" style={{ marginBottom: 4 }}>
              {npc.canTrade ? `${npc.name}: «А что у тебя есть? Могу и купить, и продать кое-что.»` : `${npc.name}: «Не до торговли мне сейчас.»`}
            </div>
            {npc.canTrade ? (
              <>
                <button disabled={busy} onClick={() => setView("buy")}>Да, показать товары</button>
                <button disabled={busy} onClick={() => setView("sell")}>Да, я хочу продать</button>
                <button disabled={busy} onClick={() => setView("menu")}>Нет, в другой раз</button>
              </>
            ) : (
              <button onClick={() => setView("menu")}>Понятно</button>
            )}
          </div>
        )}

        {view === "buy" && (
          <div>
            <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: 6 }}>
              <b>Покупка</b>
              <span className="desc"><RubleIcon size={14} /> {player?.money ?? 0}</span>
            </div>
            <div style={{ display: "flex", flexDirection: "column", gap: 6, maxHeight: 220, overflowY: "auto" }}>
              {(npc.tradeCodes || []).length === 0 ? (
                <div className="desc">Нет товаров.</div>
              ) : (
                (npc.tradeCodes || []).map((code) => (
                  <div key={code} style={{ display: "flex", justifyContent: "space-between", alignItems: "center", gap: 8 }}>
                    <span>{code}</span>
                    <button disabled={busy} onClick={() => buy(code)}>Купить</button>
                  </div>
                ))
              )}
            </div>
            <div style={{ marginTop: 8, display: "flex", gap: 6 }}>
              <button onClick={() => setView("tradeAsk")}>Назад</button>
              <button onClick={() => setView("sell")}>К продаже</button>
            </div>
          </div>
        )}

        {view === "sell" && (
          <div>
            <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: 6 }}>
              <b>Продажа (сталкеру, ~40%)</b>
              <span className="desc"><RubleIcon size={14} /> {player?.money ?? 0}</span>
            </div>
            <div style={{ display: "flex", flexDirection: "column", gap: 6, maxHeight: 220, overflowY: "auto" }}>
              {sellable.length === 0 ? (
                <div className="desc">Нечего продать.</div>
              ) : (
                sellable.map((item) => (
                  <div key={item.id} style={{ display: "flex", justifyContent: "space-between", alignItems: "center", gap: 8 }}>
                    <span title={item.name}>{item.name || item.code} {item.count > 1 ? `×${item.count}` : ""}</span>
                    <button disabled={busy} onClick={() => sell(item)}>
                      Продать (+{Math.max(1, Math.floor((item.price || 0) * 0.4))} <RubleIcon size={12} />)
                    </button>
                  </div>
                ))
              )}
            </div>
            <div style={{ marginTop: 8, display: "flex", gap: 6 }}>
              <button onClick={() => setView("tradeAsk")}>Назад</button>
              <button onClick={() => setView("buy")}>К покупке</button>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
