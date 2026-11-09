import { useEffect, useMemo, useState } from "react";
import { api } from "../api";
import { audio } from "../audio";
import RubleIcon from "./RubleIcon";

export default function TraderDialogueModal({ traderId, traderName, onClose, onUpdate }) {
  const [data, setData] = useState(null);
  const [busy, setBusy] = useState(false);
  const [nodeId, setNodeId] = useState("root");
  const [info, setInfo] = useState("");

  const load = async (keepNode = false) => {
    try {
      const d = await api.getTraderDialogue(traderId);
      setData(d);
      if (!keepNode) setNodeId("root");
      // Прогресс заданий типа "Поговорить"
      try { await api.talkTrader(traderId); } catch {}
    } catch (e) { console.error(e); }
  };
  useEffect(() => { setNodeId("root"); setInfo(""); load(false); }, [traderId]);

  const actQuest = async (fn, mission = false) => {
    if (busy) return; setBusy(true); setInfo("");
    try { await fn(); if (mission) audio.playSfx("mission"); await load(true); if (onUpdate) await onUpdate(); }
    catch(e){ setInfo(e.message || "Ошибка"); } finally { setBusy(false); }
  };

  const tree = data?.dialogueTree || data?.DialogueTree || [];
  const nodeMap = useMemo(() => {
    const m = new Map();
    for (const n of tree) m.set(n.id || n.Id, n);
    return m;
  }, [tree]);
  const currentNode = nodeMap.get(nodeId) || nodeMap.get("root") || (tree.length ? tree[0] : null);

  const handleChoice = async (choice) => {
    if (busy) return;
    const action = choice.action || choice.Action;
    const actionId = choice.actionId || choice.ActionId;
    const nextId = choice.nextId || choice.NextId;
    setInfo("");
    try {
      if (action === "accept_current_quest") {
        const qid = data?.currentQuestId || data?.CurrentQuestId || (data?.quests || []).find((q) => !q.isAccepted && !q.isCompleted)?.id;
        if (!qid) { setInfo("Сейчас нет задания, которое можно взять."); }
        else await actQuest(() => api.acceptQuest(qid));
      } else if (action === "accept_quest" && actionId) {
        await actQuest(() => api.acceptQuest(actionId));
      } else if (action === "complete_quest" && actionId) {
        await actQuest(() => api.completeQuest(actionId), true);
      } else if (action === "talk") {
        setBusy(true);
        try { await api.talkTrader(traderId); await load(true); } catch {}
        setBusy(false);
      } else if (action === "close") {
        onClose?.();
        return;
      }
      if (nextId) setNodeId(nextId);
    } catch (e) {
      setInfo(e.message || "Ошибка");
      if (nextId) setNodeId(nextId);
    }
  };

  if (!data) return (
    <div className="modal-bg" onClick={onClose}><div className="modal" onClick={(e)=>e.stopPropagation()}><div className="modal-head"><h3>Диалог — {traderName}</h3><button className="close-btn" onClick={onClose}>✖</button></div><p className="desc">Загрузка...</p></div></div>
  );

  const t = data.trader || {};
  const flatDialogue = data.dialogue || data.Dialogue || [];
  const hasTree = !!currentNode;

  return (
    <div className="modal-bg" onClick={onClose}>
      <div className="modal" onClick={(e)=>e.stopPropagation()} style={{maxWidth:760,width:"95%"}}>
        <div className="modal-head">
          <h3>💬 Диалог — {t.name || traderName}</h3>
          <button className="close-btn" onClick={onClose}>✖</button>
        </div>
        {!t.isUnlocked ? <p className="desc">Этот торговец пока недоступен. Прогрессируйте по сюжету и заданиям.</p> : (
          <>
            <div className="death-text" style={{marginBottom:10}}>
              <p><i>{t.description || ""}</i></p>
              {hasTree ? (
                <div style={{marginTop:8}}>
                  <p><b>{currentNode.speaker || currentNode.Speaker || t.name}:</b> {currentNode.text || currentNode.Text}</p>
                  <div style={{display:"flex", flexDirection:"column", gap:6, marginTop:10}}>
                    {(currentNode.choices || currentNode.Choices || []).map((c, i) => (
                      <button key={i} disabled={busy} style={{textAlign:"left"}} onClick={() => handleChoice(c)}>
                        {c.text || c.Text}
                      </button>
                    ))}
                  </div>
                </div>
              ) : (
                flatDialogue.length === 0 ? <p>«Поговорим, когда будет дело».</p> : flatDialogue.map((l,i)=>(<p key={i}><b>{l.speaker || t.name}:</b> {l.text}</p>))
              )}
              {info ? <p style={{color:"#ff8b7a", marginTop:8}}>{info}</p> : null}
            </div>
            <h4>Задания (сюжет/цепочка)</h4>
            <div className="quest-list" style={{maxHeight:320,overflow:"auto"}}>
              {(data.quests||[]).length === 0 ? <p className="desc">Нет доступных заданий. Новые появятся после выполнения предыдущих (выполненные — во вкладке «Выполненные» дневника).</p> : (data.quests||[]).slice(0,1).map(q=> (
                <div key={q.id} className={`quest ${q.isCompleted?"done":""}`}>
                  <b>{q.icon ? <img src={q.icon} alt="" style={{width:18,height:18,verticalAlign:"middle",marginRight:4}} onError={(e)=>{e.currentTarget.style.display="none";}}/>:null}{q.title}</b>
                  <p>{q.description}</p>
                  {q.isAccepted && !q.isCompleted && (q.type===0||q.type==="Fetch") && <span>Нужно: {q.targetItemName||q.targetItemCode} ×{q.targetCount} (из рейда)</span>}
                  {q.isAccepted && !q.isCompleted && q.type!==0 && q.type!=="Fetch" && q.targetCount>0 && <span>Прогресс: {q.progress}/{q.targetCount}</span>}
                  <span className="quest-reward">Награда: {q.reward?.money||0} <RubleIcon size={13} />{q.reward?.xp?` · ${q.reward.xp} опыта`:""}{q.reward?.unlockTrader?" · 🤝 торговец":""}{q.reward?.unlockLocation?" · 🔓 локация":""}</span>
                  <div>
                    {!q.isAccepted && !q.isCompleted && <button disabled={busy} onClick={()=>actQuest(()=>api.acceptQuest(q.id))}>Принять</button>}
                    {q.isAccepted && !q.isCompleted && <button disabled={busy} onClick={()=>actQuest(()=>api.completeQuest(q.id),true)}>Сдать</button>}
                    {q.isCompleted && <span className="done-mark">✅</span>}
                  </div>
                </div>
              ))}
            </div>
          </>
        )}
      </div>
    </div>
  );
}
