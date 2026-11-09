export default function TopBar({ player }) {
  const t = player?.gameTimeMinutes ?? 480;
  const day = Math.floor(t / 1440) + 1;
  const mins = ((t % 1440) + 1440) % 1440;
  const hh = String(Math.floor(mins / 60)).padStart(2, "0");
  const mm = String(mins % 60).padStart(2, "0");
  const night = mins < 360 || mins >= 1260;

  return (
    <div className="top-bar">
      <div className="top-bar-left">
        <span className="top-bar-icon">👤</span>
        <b className="top-bar-name">{player?.name || "Безымянный"}</b>
      </div>
      <div className="top-bar-right">
        <span className="top-bar-date">День {day}</span>
        <span className="top-bar-time" title={night ? "Ночь" : "День"}>
          {night ? "🌙" : "☀️"} {hh}:{mm}
        </span>
      </div>
    </div>
  );
}