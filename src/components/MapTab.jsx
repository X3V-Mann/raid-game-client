import { useEffect, useState } from "react";
import { api } from "../api";

export default function MapTab({ onStartRaid }) {
  const [locations, setLocations] = useState([]);
  const [error, setError] = useState("");

  useEffect(() => {
    api
      .getLocations()
      .then(setLocations)
      .catch((e) => setError(e.message || "Не удалось загрузить локации"));
  }, []);

  if (error)
    return (
      <div className="panel">
        <h3>Выбор локации</h3>
        <p className="desc">⚠️ {error}. Проверьте, запущен ли сервер.</p>
      </div>
    );

  if (locations.length === 0)
    return <div className="panel"><h3>Выбор локации</h3><p className="desc">Загрузка...</p></div>;

  return (
    <div className="panel">
      <h3>Выбор локации</h3>
      <p className="desc">
        Локации открываются за задания торговцев. Градирни — по сюжету.
      </p>

      <div className="location-list">
        {locations.map((l) => (
          <div key={l.id} className={`location-row ${l.unlocked ? "" : "locked"}`}>
            <div
              className="location-thumb"
              style={l.image ? { backgroundImage: `url(${l.image})` } : undefined}
            >
              {!l.unlocked && <span className="location-lock">🔒</span>}
            </div>

            <div className="location-info">
              <b>{l.name}</b>
              <span>{l.description}</span>
              {!l.unlocked && (
                <span className="location-hint">🔒 {l.unlockHint || "Локация закрыта"}</span>
              )}
            </div>

            <div className="location-action">
              {l.unlocked ? (
                <button onClick={() => onStartRaid(l)}>В рейд →</button>
              ) : (
                <button disabled>Закрыто</button>
              )}
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
