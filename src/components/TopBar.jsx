import { useEffect, useState } from "react";

export default function TopBar({ player }) {
  const [now, setNow] = useState(new Date());

  useEffect(() => {
    const id = setInterval(() => setNow(new Date()), 1000);
    return () => clearInterval(id);
  }, []);

  const hh = String(now.getHours()).padStart(2, "0");
  const mm = String(now.getMinutes()).padStart(2, "0");
  const ss = String(now.getSeconds()).padStart(2, "0");

  const date = now.toLocaleDateString("ru-RU", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
  });

  return (
    <div className="top-bar">
      <div className="top-bar-left">
        <span className="top-bar-icon">👤</span>
        <b className="top-bar-name">{player.name || "Безымянный"}</b>
      </div>
      <div className="top-bar-right">
        <span className="top-bar-date">{date}</span>
        <span className="top-bar-time">
          {hh}:{mm}:{ss}
        </span>
      </div>
    </div>
  );
}