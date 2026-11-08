import { useEffect, useState } from "react";
import { api } from "../api";
import { audio } from "../audio";
import RubleIcon from "./RubleIcon";

export default function TraderQuestsModal({ traderId, traderName, onClose }) {
  const [quests, setQuests] = useState([]);
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);

  const load = async () => {
    setLoading(true);
    try {
      const data = await api.getTraderQuests(traderId);
      setQuests(data.quests || []);
    } catch (e) {
      console.error(e);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [traderId]);

  const act = async (fn, mission = false) => {
    if (busy) return;
    setBusy(true);
    try {
      await fn();
      if (mission) audio.playSfx("mission");
      await load();
    } catch (e) {
      alert(e.message || "Ошибка");
    } finally {
      setBusy(false);
    }
  };

  return (
    <div className="modal-bg" onClick={onClose}>
      <div className="modal trader-quests" onClick={(e) => e.stopPropagation()}>
        <div className="modal-head">
          <h3>📋 Задания — {traderName}</h3>
          <button className="close-btn" onClick={onClose}>✖</button>
        </div>

        {loading ? (
          <p className="desc">Загрузка...</p>
        ) : quests.length === 0 ? (
          <p className="desc">У этого торговца пока нет заданий.</p>
        ) : (
          <div className="quest-list">
            {quests.map((q) => (
              <div key={q.id} className={`quest ${q.isCompleted ? "done" : ""}`}>
                <b>
                  {q.isDaily && <span title="Ежедневное задание">📅 </span>}
                  {q.title}
                </b>
                <p>{q.description}</p>
                {q.isAccepted && !q.isCompleted && (
                  <span>
                    Прогресс: {q.progress}/{q.targetCount}
                  </span>
                )}
                <span>
                  Награда: {q.reward.money} <RubleIcon size={13} />{q.reward.xp > 0 ? ` · ${q.reward.xp} XP` : ""}
                  {q.reward.items.map((i) => ` + ${i.name}`)}
                </span>
                <div>
                  {!q.isAccepted && !q.isCompleted && (
                    <button
                      disabled={busy}
                      onClick={() => act(() => api.acceptQuest(q.id))}
                    >
                      Принять
                    </button>
                  )}
                  {q.isAccepted && !q.isCompleted && (
                    <button
                      disabled={busy}
                      onClick={() => act(() => api.completeQuest(q.id), true)}
                    >
                      Сдать
                    </button>
                  )}
                  {q.isCompleted && (
                    <span className="done-mark">✅ Выполнено</span>
                  )}
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}