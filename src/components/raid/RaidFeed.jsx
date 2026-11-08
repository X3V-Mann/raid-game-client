export default function RaidFeed({ events }) {
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
    </aside>
  );
}