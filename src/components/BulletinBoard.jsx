import { useEffect, useState } from "react";
import { api } from "../api";
import { audio } from "../audio";
import RubleIcon from "./RubleIcon";

function fmtDeadline(q, time) {
  if (!q.isUrgent || !q.deadlineGameMinutes || !time) return null;
  const left = q.deadlineGameMinutes - (time.totalMinutes ?? 0);
  if (left <= 0) return "истёк срок";
  const h = Math.floor(left / 60), m = left % 60;
  return `осталось ${h}ч ${m}м (игр.)`;
}

export default function BulletinBoard({ act, onToast }) {
  const [data, setData] = useState({ quests: [], time: null });
  const [busy, setBusy] = useState(false);

  const load = async () => {
    try { setData(await api.getBoard()); } catch (e) { console.error(e); }
  };
  useEffect(() => { load(); }, []);

  const doAction = async (fn, mission = false) => {
    if (busy) return;
    setBusy(true);
    try { await fn(); if (mission) audio.playSfx("mission"); await load(); if (act) await act(() => Promise.resolve()); }
    catch (e) { onToast ? onToast(e.message || "Ошибка") : alert(e.message || "Ошибка"); }
    finally { setBusy(false); }
  };

  const time = data.time;
  return (
    <div className="panel">
      <h3>📌 Доска объявлений</h3>
      <p className="desc">Побочные задания: убийства, доставки, разведка, здания, время суток. Задания с пометкой <b>СРОЧНО</b> имеют лимит игрового времени. {time ? <span> · {time.text}{time.isNight ? " 🌙" : " ☀️"}</span> : null}</p>
      <div className="quest-list">
        {(data.quests || []).map((q) => {
          const dl = fmtDeadline(q, time);
          const expired = q.isFailed || dl === "истёк срок";
          return (
            <div key={q.id} className={`quest ${q.isCompleted ? "done" : ""} ${expired ? "locked" : ""}`}>
              <b>
                {q.icon ? <img src={q.icon} alt="" style={{ width: 20, height: 20, verticalAlign: "middle", marginRight: 6, objectFit: "contain" }} onError={(e)=>{e.currentTarget.style.display="none";}} /> : null}
                {q.isUrgent ? <span style={{ color: "#ff6b6b" }}>СРОЧНО </span> : null}
                {q.title}
              </b>
              <p>{q.description}{q.targetLocationName ? ` · 📍 ${q.targetLocationName}` : ""}{q.timeOfDay === 2 || q.timeOfDay === "Night" ? " · 🌙 ночь" : (q.timeOfDay === 1 || q.timeOfDay === "Day" ? " · ☀️ день" : "")}</p>
              {q.isAccepted && !q.isCompleted && !expired && q.type !== 0 && q.type !== "Fetch" && q.targetCount > 0 && (
                <span>Прогресс: {q.progress}/{q.targetCount}{dl ? ` · ⏱ ${dl}` : ""}</span>
              )}
              {q.isAccepted && !q.isCompleted && (q.type === 0 || q.type === "Fetch") && (
                <span>Нужно: {q.targetItemName || q.targetItemCode} ×{q.targetCount} (из рейда){dl ? ` · ⏱ ${dl}` : ""}</span>
              )}
              <span className="quest-reward">Награда: {q.reward?.money || 0} <RubleIcon size={13} />{q.reward?.xp ? ` · ${q.reward.xp} XP` : ""}</span>
              <div>
                {!q.isAccepted && !q.isCompleted && !expired && (
                  <button disabled={busy} onClick={() => doAction(() => api.acceptQuest(q.id))}>Принять</button>
                )}
                {q.isAccepted && !q.isCompleted && !expired && (
                  <button disabled={busy} onClick={() => doAction(() => api.completeQuest(q.id), true)}>Сдать</button>
                )}
                {q.isCompleted && <span className="done-mark">✅ Выполнено</span>}
                {expired && <span className="locked-mark">⏱ Срок истёк</span>}
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
}