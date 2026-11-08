import { useEffect, useState } from "react";
import { audio, useAudioState } from "../audio";

const THEMES = [
  { id: "stalker", name: "Сталкер", desc: "Коричневый, пыльный, ржавый", accent: "#d4a04a" },
  { id: "metro", name: "Метро", desc: "Холодный бетон, сине-зелёный свет", accent: "#6fb7c9" },
  { id: "tarkov", name: "Тарков", desc: "Олива, хаки, военный терминал", accent: "#a7b06a" },
  { id: "fallout", name: "Фоллаут", desc: "Янтарный CRT и зелёный фосфор", accent: "#7ee787" },
  { id: "tf2", name: "Team Fortress 2", desc: "Mann Co.: RED/BLU, индустриальный крафт", accent: "#c84b3c" },
];

export const applyTheme = (id) => {
  document.documentElement.dataset.theme = id || "stalker";
  try {
    localStorage.setItem("raid.theme", id || "stalker");
  } catch {
    // localStorage может быть недоступен
  }
};

export const loadTheme = () => {
  try {
    return localStorage.getItem("raid.theme") || "stalker";
  } catch {
    return "stalker";
  }
};

export default function Settings() {
  const [theme, setTheme] = useState(loadTheme());
  const audioState = useAudioState();

  useEffect(() => {
    applyTheme(theme);
  }, [theme]);

  return (
    <div className="panel">
      <h3>Настройки</h3>

      <div className="settings-block">
        <h4>🎨 Тема интерфейса</h4>
        <p className="desc">Меняет оформление меню, панелей и диалогов.</p>
        <div className="theme-grid">
          {THEMES.map((t) => (
            <button
              key={t.id}
              className={`theme-card ${theme === t.id ? "active" : ""}`}
              onClick={() => setTheme(t.id)}
              style={{ "--theme-preview": t.accent }}
            >
              <span className="theme-swatch" />
              <b>{t.name}</b>
              <span>{t.desc}</span>
            </button>
          ))}
        </div>
      </div>

      <div className="settings-block">
        <h4>🔊 Звук</h4>
        <p className="desc">
          Музыка играет только в меню, эмбиент — только в игре. Кассеты в убежище
          используют громкость музыки.
        </p>

        {[
          ["music", "Музыка / кассеты"],
          ["ambient", "Эмбиент в игре"],
          ["sfx", "Звуки интерфейса"],
        ].map(([key, label]) => (
          <div className="volume-row" key={key}>
            <span>{label}</span>
            <input
              type="range"
              min="0"
              max="100"
              step="1"
              value={Math.round(audioState.settings[key] * 100)}
              onChange={(e) => audio.setVolume(key, Number(e.target.value) / 100)}
            />
            <b>{Math.round(audioState.settings[key] * 100)}%</b>
          </div>
        ))}
      </div>
    </div>
  );
}
