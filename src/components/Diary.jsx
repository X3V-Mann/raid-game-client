import { useEffect, useState } from "react";
import { api } from "../api";
import { audio } from "../audio";
import RubleIcon from "./RubleIcon";

function iconImg(src) {
  if (!src) return null;
  return <img src={src} alt="" style={{ width: 22, height: 22, verticalAlign: "middle", marginRight: 6, objectFit: "contain" }} onError={(e)=>{e.currentTarget.style.display="none";}} />;
}

export default function Diary({ act }) {
  const [story, setStory] = useState(null);
  const [diary, setDiary] = useState(null);
  const [sub, setSub] = useState("quests"); // quests | completed | thoughts | notes | ref
  const [refTab, setRefTab] = useState("items");
  const [busy, setBusy] = useState(false);
  const [note, setNote] = useState("");
  const [endingMsg, setEndingMsg] = useState("");

  const load = async () => {
    try {
      const [s, d] = await Promise.all([api.getStory(), api.getDiary()]);
      setStory(s); setDiary(d);
    } catch (e) { console.error(e); }
  };
  useEffect(() => { load(); }, []);

  const doAction = async (fn, mission = false) => {
    if (busy) return; setBusy(true);
    try {
      const res = await fn();
      if (res?.requiresChoice) {
        // финальный выбор — UI отрисует кнопки по s4_07
      } else {
        if (mission) audio.playSfx("mission");
      }
      if (res?.message) setEndingMsg(res.message);
      await load(); if (act) await act(() => Promise.resolve());
      return res;
    }
    catch (e) { alert(e.message || "Ошибка"); } finally { setBusy(false); }
  };

  const makeChoice = async (choice) => {
    if (busy) return; setBusy(true);
    try {
      const res = await api.endingChoice(choice);
      if (res?.message) setEndingMsg(res.message);
      audio.playSfx("mission");
      await load(); if (act) await act(() => Promise.resolve());
    } catch (e) { alert(e.message || "Ошибка"); } finally { setBusy(false); }
  };

  const QuestCard = ({ q, locked = false }) => (
    <div className={`quest ${q.isCompleted ? "done" : ""} ${locked ? "locked" : ""}`}>
      <b>{iconImg(q.icon)}{q.chapter ? <span className="quest-chapter">Гл.{q.chapter} </span> : null}{q.title}</b>
      <p>{q.description}</p>
      {q.isAccepted && !q.isCompleted && q.type !== 0 && q.type !== "Fetch" && q.targetCount > 0 && q.id !== "s4_07" && <span>Прогресс: {q.progress}/{q.targetCount}</span>}
      {q.isAccepted && !q.isCompleted && (q.type === 0 || q.type === "Fetch") && <span>Нужно: {q.targetItemName || q.targetItemCode} ×{q.targetCount} (из рейда)</span>}
      <span className="quest-reward">Награда: {q.reward?.money || 0} <RubleIcon size={13} />{q.reward?.xp ? ` · ${q.reward.xp} опыта` : ""}{q.reward?.unlockLocation ? " · 🔓 локация" : ""}{q.reward?.unlockTrader ? " · 🤝 торговец" : ""}</span>
      {q.journalNote && <p className="desc"><i>«{q.journalNote}»</i></p>}
      <div>
        {!q.isAccepted && !q.isCompleted && !locked && <button disabled={busy} onClick={() => doAction(() => api.acceptQuest(q.id))}>Принять</button>}
        {q.isAccepted && !q.isCompleted && q.id !== "s4_07" && <button disabled={busy} onClick={() => doAction(() => api.completeQuest(q.id), true)}>Сдать</button>}
        {q.isAccepted && !q.isCompleted && q.id === "s4_07" && !story?.endingChoice && (
          <div style={{display:"flex",gap:8,flexWrap:"wrap",alignItems:"center",marginTop:6}}>
            <img src="/blocks/atomic_bomb_for_final.png" alt="бомба" style={{width:42,height:42,objectFit:"contain"}} onError={(e)=>{e.currentTarget.style.display="none";}} />
            <button disabled={busy} onClick={() => makeChoice("detonate")} title="Концовка (заглушка DLC)">☢ Взорвать бомбу</button>
            <button disabled={busy} onClick={() => makeChoice("hide")}>📦 Спрятать бомбу (freeplay)</button>
          </div>
        )}
        {q.isCompleted && <span className="done-mark">✅ Выполнено</span>}
        {locked && <span className="locked-mark">🔒 Сначала предыдущее</span>}
      </div>
    </div>
  );

  if (!story) return <div className="panel"><h3>Личный дневник</h3><p className="desc">Загрузка...</p></div>;
  const { current, chapterQuests, completed, completedAll, allCount, chapters, gameCompleted } = story;
  const doneList = completedAll || completed || [];
  const time = diary?.time;

  return (
    <div className="panel">
      <h3>📓 Личный дневник</h3>
      <p className="desc">Сюжетные задания, мысли ГГ и справочник. {time ? <span>{time.text}{time.isNight ? " 🌙" : " ☀️"}</span> : null}</p>
      <div className="tabs">
        <button className={sub === "quests" ? "active" : ""} onClick={() => setSub("quests")}>📖 Сюжет ({completed?.length || 0}/{allCount || 0})</button>
        <button className={sub === "completed" ? "active" : ""} onClick={() => setSub("completed")}>✅ Выполненные ({doneList.length})</button>
        <button className={sub === "thoughts" ? "active" : ""} onClick={() => setSub("thoughts")}>💭 Мысли ГГ</button>
        <button className={sub === "notes" ? "active" : ""} onClick={() => setSub("notes")}>📝 Заметки</button>
        <button className={sub === "ref" ? "active" : ""} onClick={() => setSub("ref")}>📚 Справочник</button>
      </div>

      {sub === "quests" && (
        <div className="quest-list">
          {endingMsg && <div className="ending-card" style={{marginBottom:12,whiteSpace:"pre-wrap"}}><h4>Финал</h4><p>{endingMsg}</p></div>}
          {gameCompleted && !endingMsg && (
            <div className="ending-card" style={{marginBottom:12}}>
              <h4>{story?.endingChoice === "detonate" ? "☢ Игра закончена: бомба взорвана" : story?.endingChoice === "hide" ? "📦 Бомба спрятана: свободная игра" : "☢ Финал пройден"}</h4>
              <p className="desc">{story?.endingChoice === "detonate" ? "Вы уничтожили объект на Градирнях. (Заглушка DLC-ветки последствий.)" : story?.endingChoice === "hide" ? "Заряд спрятан (спрайт /blocks/atomic_bomb_for_final.png). Можно продолжать в freeplay." : "Можно продолжить."}</p>
              {story?.bombHidden && <img src="/blocks/atomic_bomb_for_final.png" alt="бомба" style={{width:64,height:64,objectFit:"contain"}} onError={(e)=>{e.currentTarget.style.display="none";}} />}
            </div>
          )}
          {current ? (
            <>
              <h4>Глава {current.chapter}. {chapters?.find((c) => c.chapter === current.chapter)?.title || ""}</h4>
              <p className="desc">Открыто только текущее задание. Следующее появится после выполнения.</p>
              {(chapterQuests || []).slice(0, 1).map((q) => <QuestCard key={q.id} q={q} />)}
            </>
          ) : <div className="quest done"><b>🏁 Текущая сюжетная глава завершена</b><p>Проверьте задания торговцев (Диалог) и доску объявлений.</p></div>}
          <details><summary>Все главы</summary>
            <div className="chapter-list">{(chapters||[]).map(c=>(<div key={c.chapter} className={`chapter-row ${c.completed>=c.total?"done":""}`}><span className="chapter-num">{c.chapter}</span><div><b>{c.title}</b><span>{c.completed}/{c.total}</span></div></div>))}</div>
          </details>
        </div>
      )}

      {sub === "completed" && (
        <div className="quest-list">
          <p className="desc">Все выполненные сюжетные задания и задания торговцев.</p>
          {doneList.length === 0 ? <p className="desc">Пока нет выполненных заданий.</p> : doneList.map((q) => <QuestCard key={q.id} q={q} />)}
        </div>
      )}

      {sub === "thoughts" && (
        <div>
          <p className="desc">Мысли главного героя — записываются сами по ходу событий и сюжета.</p>
          {(diary?.thoughts||[]).length === 0 ? <p className="desc">Пока тихо… события оставят след здесь.</p> : (
            <ul>{(diary.thoughts||[]).slice().reverse().map((t,i)=><li key={i}>{t}</li>)}</ul>
          )}
        </div>
      )}

      {sub === "notes" && (
        <div>
          <p className="desc">Ваши личные заметки (метки, планы, напоминания).</p>
          <div style={{display:"flex",gap:8,marginBottom:8}}>
            <input value={note} onChange={(e)=>setNote(e.target.value)} placeholder="Записать заметку..." style={{flex:1}} />
            <button onClick={async()=>{ if(!note.trim()) return; try{await api.addNote(note.trim()); setNote(""); await load();}catch(e){alert(e.message);} }}>Добавить</button>
          </div>
          {(diary?.notes||[]).length === 0 ? <p className="desc">Заметок пока нет.</p> : (
            <ul>{(diary.notes||[]).slice().reverse().map((t,i)=><li key={i}>{t}</li>)}</ul>
          )}
        </div>
      )}

      {sub === "ref" && (
        <div>
          <div className="tabs">
            <button className={refTab==="items"?"active":""} onClick={()=>setRefTab("items")}>Предметы</button>
            <button className={refTab==="zone"?"active":""} onClick={()=>setRefTab("zone")}>Обитатели зоны</button>
          </div>
          {refTab==="items" && <div className="trade-cells" style={{maxHeight:420,overflow:"auto"}}>{(diary?.referenceItems||[]).map(it=>(<div key={it.code} className="trade-cell" title={it.description||it.name} style={{cursor:"default"}}><div className="trade-cell-icon">{it.icon && !it.icon.includes("error_or_missing") ? <img src={it.icon} alt="" onError={(e)=>{e.currentTarget.style.display="none";}} /> : <span>❓</span>}</div><div style={{fontSize:10}}>{it.name}</div></div>))}</div>}
          {refTab==="zone" && <div>{(diary?.referenceLocations||[]).map(loc=>(<details key={loc.id}><summary>{loc.icon} {loc.name} (T{loc.tier})</summary><p className="desc">{loc.description}</p><ul>{(loc.enemies||[]).map((e,i)=><li key={i}>{e.icon ? <img src={e.icon} alt="" style={{width:18,height:18,verticalAlign:"middle",marginRight:4}} onError={(ev)=>{ev.currentTarget.style.display="none";}}/>:null}{e.name} — HP {e.baseHp}, урон {e.baseDamage}{e.ranged?" (дальний)":""}</li>)}</ul></details>))}</div>}
        </div>
      )}
    </div>
  );
}