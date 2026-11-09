import { useEffect, useState } from "react";
import { api } from "../api";
import { audio } from "../audio";
import RubleIcon from "./RubleIcon";

export default function TraderDialogueModal({ traderId, traderName, onClose, onUpdate }) {
  const [data, setData] = useState(null);
  const [busy, setBusy] = useState(false);

  const load = async () => {
    try {
      const d = await api.getTraderDialogue(traderId);
      setData(d);
      // Прогресс заданий типа "Поговорить"
      try { await api.talkTrader(traderId); } catch {}
    } catch (e) { console.error(e); }
  };
  useEffect(() => { load(); }, [traderId]);

  const actQuest = async (fn, mission=false) => {
    if (busy) return; setBusy(true);
    try { await fn(); if (mission) audio.playSfx("mission"); await load(); if (onUpdate) await onUpdate(); }
    catch(e){ alert(e.message||"Ошибка"); } finally { setBusy(false); }
  };

  if (!data) return (
    <div className="modal-bg" onClick={onClose}><div className="modal" onClick={(e)=>e.stopPropagation()}><div className="modal-head"><h3>Диалог — {traderName}</h3><button className="close-btn" onClick={onClose}>✖</button></div><p className="desc">Загрузка...</p></div></div>
  );

  const t = data.trader || {};
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
              {(data.dialogue||[]).length === 0 ? <p>«Поговорим, когда будет дело».</p> : data.dialogue.map((l,i)=>(<p key={i}><b>{l.speaker || t.name}:</b> {l.text}</p>))}
            </div>
            <h4>Задания (сюжет/цепочка)</h4>
            <div className="quest-list" style={{maxHeight:360,overflow:"auto"}}>
              {(data.quests||[]).length === 0 ? <p className="desc">Нет доступных заданий. Новые появятся после выполнения предыдущих (выполненные — во вкладке «Выполненные» дневника).</p> : data.quests.map(q=> (
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