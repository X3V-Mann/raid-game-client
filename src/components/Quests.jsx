import { useEffect, useState } from "react";
import { api } from "../api";
import { audio } from "../audio";
import RubleIcon from "./RubleIcon";

const TRADER_NAMES = {
  prapor: "Прапор",
  therapist: "Медик",
  bartender: "Бармен",
  ragman: "Старьёвщик",
  blacksmith: "Кузнец",
  buyer: "Скупщик",
};

export default function Quests({ story }) {
  const [quests, setQuests] = useState([]);
  const [storyData, setStoryData] = useState(null);
  const [subTab, setSubTab] = useState("chapter");
  const [busy, setBusy] = useState(false);

  const load = async () => {
    try {
      if (story) {
        const d = await api.getStory();
        setStoryData(d);
      } else {
        const d = await api.getQuests();
        setQuests(d.quests || []);
      }
    } catch (e) {
      console.error(e);
    }
  };

  useEffect(() => {
    load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [story, subTab]);

  const doAction = async (fn, mission = false) => {
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

  const QuestCard = ({ q, done = false, locked = false }) => (
    <div className={`quest ${done ? "done" : ""} ${locked ? "locked" : ""}`}>
      <b>
        {q.isDaily && <span title="Ежедневное задание">📅 </span>}
        {q.isStory && q.chapter ? <span className="quest-chapter">Глава {q.chapter}.</span> : null}
        {q.title}
      </b>
      <p>{q.description}</p>
      {q.isAccepted && !q.isCompleted && q.type !== "Fetch" && (
        <span>Прогресс: {q.progress}/{q.targetCount}</span>
      )}
      {q.isAccepted && !q.isCompleted && q.type === "Fetch" && (
        <span>Нужно принести: {q.targetItemName || q.targetItemCode} ×{q.targetCount}</span>
      )}
      <span className="quest-reward">
        Награда: {q.reward.money} <RubleIcon size={13} />
        {q.reward.xp > 0 ? ` · ${q.reward.xp} XP` : ""}
        {(q.reward.items || []).map((i) => ` · ${i.name}`).join("")}
        {q.reward.unlockLocation ? " · 🔓 новая локация" : ""}
      </span>
      <div>
        {!q.isAccepted && !q.isCompleted && !locked && (
          <button disabled={busy} onClick={() => doAction(() => api.acceptQuest(q.id))}>
            Принять
          </button>
        )}
        {q.isAccepted && !q.isCompleted && (
          <button disabled={busy} onClick={() => doAction(() => api.completeQuest(q.id), true)}>
            Сдать
          </button>
        )}
        {q.isCompleted && <span className="done-mark">✅ Выполнено</span>}
        {locked && <span className="locked-mark">🔒 Сначала предыдущее задание</span>}
      </div>
    </div>
  );

  // ===== Обычные квесты торговцев =====
  if (!story) {
    const groups = quests.reduce((acc, q) => {
      const key = q.traderId || "other";
      (acc[key] = acc[key] || []).push(q);
      return acc;
    }, {});

    return (
      <div className="panel">
        <h3>Задания торговцев</h3>
        <p className="desc">
          Цепочки заданий открываются последовательно. 📅 — ежедневные задания,
          они обновляются раз в сутки.
        </p>
        {quests.length === 0 ? (
          <p className="desc">Пока нет доступных заданий. Загляните к торговцам позже.</p>
        ) : (
          Object.entries(groups).map(([trader, list]) => (
            <div key={trader} className="quest-group">
              <h4>{TRADER_NAMES[trader] || trader}</h4>
              <div className="quest-list">
                {list.map((q) => <QuestCard key={q.id} q={q} />)}
              </div>
            </div>
          ))
        )}
      </div>
    );
  }

  // ===== Сюжет =====
  if (!storyData)
    return <div className="panel"><h3>Сюжет</h3><p className="desc">Загрузка...</p></div>;

  const { current, chapterQuests, completed, allCount, chapters, gameCompleted } = storyData;

  if (gameCompleted) {
    return (
      <div className="panel">
        <h3>Сюжет</h3>
        <div className="ending-card">
          <h2>☢ КОНЕЦ</h2>
          <p>
            Ты нажал кнопку. Градирни исчезли в белой вспышке — вместе с ложью,
            ставками и теми, кто решил поиграть в войну.
          </p>
          <p>Дальше была только тишина. И она была честнее всего, что ты видел.</p>
          <p className="desc">
            Пройдено глав: {chapters.filter((c) => c.completed >= c.total).length} из {chapters.length}.
            Дальше — свободная игра: рейды, торговцы, ежедневные задания и прокачка.
          </p>
        </div>
      </div>
    );
  }

  return (
    <div className="panel">
      <h3>Сюжет</h3>

      <div className="tabs">
        <button className={subTab === "chapter" ? "active" : ""} onClick={() => setSubTab("chapter")}>
          📖 Текущая глава
        </button>
        <button className={subTab === "all" ? "active" : ""} onClick={() => setSubTab("all")}>
          🗺 Все главы
        </button>
        <button className={subTab === "done" ? "active" : ""} onClick={() => setSubTab("done")}>
          ✅ Завершённые ({completed.length}/{allCount})
        </button>
      </div>

      {subTab === "chapter" && (
        <div className="quest-list">
          {current ? (
            <>
              <h4>
                Глава {current.chapter}. {chapters.find((c) => c.chapter === current.chapter)?.title}
              </h4>
              {(chapterQuests || []).map((q) => (
                <QuestCard
                  key={q.id}
                  q={q}
                  done={q.isCompleted}
                  locked={!q.isCompleted && !q.isAccepted && q.order > (current.order || 0)}
                />
              ))}
            </>
          ) : (
            <div className="quest done">
              <b>🏁 Сюжет пройден</b>
              <p>Ты завершил все главы. Дальше — только личная история выживания.</p>
            </div>
          )}
        </div>
      )}

      {subTab === "all" && (
        <div className="chapter-list">
          {chapters.map((c) => {
            const isCurrent = current && current.chapter === c.chapter;
            const isDone = c.completed >= c.total;
            return (
              <div key={c.chapter} className={`chapter-row ${isDone ? "done" : ""} ${isCurrent ? "current" : ""}`}>
                <span className="chapter-num">{c.chapter}</span>
                <div>
                  <b>{c.title}</b>
                  <span>{c.completed}/{c.total} заданий</span>
                </div>
                <span className="chapter-state">
                  {isDone ? "✅" : isCurrent ? "▶" : "🔒"}
                </span>
              </div>
            );
          })}
        </div>
      )}

      {subTab === "done" && (
        <div className="quest-list">
          {completed.length === 0 ? (
            <p className="desc">Пока ни одна глава не завершена.</p>
          ) : (
            completed.map((q) => <QuestCard key={q.id} q={q} done={true} />)
          )}
        </div>
      )}
    </div>
  );
}
