export default function RaidFeed({ events, quests = [] }) {
  return (
    <aside className="raid-feed">
      <div className="raid-feed-head">
        <b>📻 Журнал</b>
      </div>
      <div className="raid-feed-list">
        {events.length === 0 ? (
          <div className="raid-feed-empty">Пока ничего не произошло...</div>
        ) : (
          events.map((e, i) => (
            <div key={i} className={`raid-feed-item feed-${e.kind || "info"}`}>
              <span className="feed-icon">{e.icon || "·"}</span>
              <div className="feed-body">
                {e.title && <div className="feed-title">{e.title}</div>}
                {e.text && <div className="feed-text">{e.text}</div>}
              </div>
            </div>
          ))
        )}
      </div>

      <div className="raid-feed-head" style={{ marginTop: 10 }}>
        <b>📜 Активные квесты</b>
      </div>
      <div className="raid-feed-list raid-active-quests">
        {quests.length === 0 ? (
          <div className="raid-feed-empty">Нет активных заданий.</div>
        ) : (
          quests.map((q) => (
            <div key={q.id} className="raid-feed-item feed-info quest">
              <span className="feed-icon">🎯</span>
              <div className="feed-body">
                <div className="feed-title">{q.title}</div>
                {q.description && <div className="feed-text">{q.description}</div>}
                {q.targetCount > 0 && (q.type === 0 || q.type === "Fetch") ? (
                  <div className="feed-text">Нужно: {q.targetItemName || q.targetItemCode} ×{q.targetCount}</div>
                ) : q.targetCount > 0 ? (
                  <div className="feed-text">Прогресс: {q.progress || 0}/{q.targetCount}</div>
                ) : null}
              </div>
            </div>
          ))
        )}
      </div>
    </aside>
  );
}