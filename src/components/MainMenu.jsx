import { useState, useEffect } from "react";
import { api } from "../api";
import { audio, useAudioState } from "../audio";

const SOUND_KEYS = [
  ["music", "Музыка / кассеты"],
  ["ambient", "Эмбиент в игре"],
  ["sfx", "Звуки интерфейса"],
];

export default function MainMenu({ playerName, canContinue, onNewGame, onContinue }) {
  const [modal, setModal] = useState(null); // "authors", "patchnotes", "load"
  const [soundOpen, setSoundOpen] = useState(false);
  const [saves, setSaves] = useState([]);
  const [loadingSaves, setLoadingSaves] = useState(false);
  const audioState = useAudioState();

  // Загружаем список сохранений, когда открываем модалку "load"
  useEffect(() => {
    if (modal === "load") {
      setLoadingSaves(true);
      api.getSaves()
        .then(setSaves)
        .catch(console.error)
        .finally(() => setLoadingSaves(false));
    }
  }, [modal]);

  const handleLoad = async (filename) => {
    try {
      await api.loadGame(filename);
      onContinue?.();
    } catch (e) {
      alert("Ошибка загрузки: " + e.message);
    }
  };

  const handleDelete = async (filename) => {
    if (!window.confirm(`Удалить сохранение ${filename}?`)) return;
    try {
      await api.deleteSave(filename);
      setSaves((list) => list.filter((s) => s.name !== filename));
    } catch (e) {
      alert("Ошибка удаления: " + e.message);
    }
  };

  const handleNewGame = () => {
    if (canContinue && !window.confirm("Начать новую игру? Текущий прогресс будет сброшен.")) return;
    onNewGame();
  };

  const saveLabel = (s) => {
    if (s.name === "autosave.json") return "🔄 Автосохранение";
    return s.name.replace(".json", "").replace("save_", "Слот ");
  };

  return (
    <div className="main-menu-bg">
      <div className="main-menu-overlay" />
      <div className="main-menu-container">
        <h1 className="game-title">РЕЙД</h1>
        <p className="game-subtitle">Выживи. Найди. Вернись.</p>

        <div className="menu-buttons">
          <button
            className="menu-btn"
            onClick={onContinue}
            disabled={!canContinue}
            title={canContinue ? `Продолжить за ${playerName || "выжившего"}` : "Нет сохранённого прогресса"}
          >
            Продолжить{canContinue && playerName ? ` (${playerName})` : ""}
          </button>

          <button className="menu-btn" onClick={handleNewGame}>
            Новая игра
          </button>

          <button className="menu-btn" onClick={() => setModal("load")}>
            Загрузить
          </button>

          <button
            className="menu-btn"
            onClick={() => {
              audio.setScene("menu");
              setSoundOpen(true);
            }}
          >
            🔊 Звук
          </button>

          <div className="menu-row">
            <button className="menu-btn small" onClick={() => setModal("authors")}>Авторы</button>
            <button className="menu-btn small" onClick={() => setModal("patchnotes")}>Патч-ноуты</button>
          </div>

          <a
            href="https://forms.gle/Lbki239TQbAnKyuR7"
            target="_blank"
            rel="noreferrer"
            className="menu-btn small danger"
            style={{ textDecoration: "none", textAlign: "center", display: "block" }}
          >
            🐞 Багрепорт
          </a>
        </div>

        <div className="version-tag">v0.5.0-alpha</div>
      </div>

      {/* Модалка Авторов */}
      {modal === "authors" && (
        <div className="modal-bg" onClick={() => setModal(null)}>
          <div className="modal" onClick={(e) => e.stopPropagation()}>
            <div className="modal-head">
              <h3>Авторы</h3>
              <button className="close-btn" onClick={() => setModal(null)}>✖</button>
            </div>
            <div className="death-text">
              <b>Разработчик:</b> Геолог<br /><br />
              <b>Источники вдохновения и спрайтов:</b><br />
              S.T.A.L.K.E.R., Escape from Tarkov, DayZ
            </div>
          </div>
        </div>
      )}

      {/* Модалка Патч-ноутов */}
      {modal === "patchnotes" && (
        <div className="modal-bg" onClick={() => setModal(null)}>
          <div className="modal" onClick={(e) => e.stopPropagation()}>
            <div className="modal-head">
              <h3>Патч-ноуты</h3>
              <button className="close-btn" onClick={() => setModal(null)}>✖</button>
            </div>
            <div className="death-text">
              <b>v0.5.0 (Текущая)</b>
              <ul>
                <li>Музыка меню и эмбиент в игре, громкость в настройках.</li>
                <li>Кассетный проигрыватель в убежище и кассеты в луте.</li>
                <li>Звуки наведения на кнопки, клика и завершения задания.</li>
                <li>Сюжет 10×10, лояльность торговцев, градация лута.</li>
                <li>Новый инвентарь в стиле S.T.A.L.K.E.R. и RPG-бой.</li>
                <li>Покупка оптом, свободная игра и новые стволы.</li>
              </ul>
            </div>
          </div>
        </div>
      )}

      {/* Модалка Загрузки */}
      {modal === "load" && (
        <div className="modal-bg" onClick={() => setModal(null)}>
          <div className="modal" onClick={(e) => e.stopPropagation()}>
            <div className="modal-head">
              <h3>Загрузить игру</h3>
              <button className="close-btn" onClick={() => setModal(null)}>✖</button>
            </div>
            {loadingSaves ? (
              <p className="desc">Загрузка списка...</p>
            ) : saves.length === 0 ? (
              <p className="desc">Нет доступных сохранений.</p>
            ) : (
              <div className="save-list">
                {saves.map((s) => (
                  <div key={s.name} className="save-item">
                    <div className="save-info">
                      <b>{saveLabel(s)}</b>
                      <span>
                        {s.playerName ? `${s.playerName} · ур. ${s.level} · ` : ""}
                        {s.storyTotal > 0 ? `сюжет ${s.storyDone}/${s.storyTotal} · ` : ""}
                        {s.time}
                      </span>
                    </div>
                    <div className="save-actions">
                      <button className="btn-ok" onClick={() => handleLoad(s.name)}>
                        Загрузить
                      </button>
                      <button className="btn-danger" onClick={() => handleDelete(s.name)}>
                        🗑
                      </button>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>
        </div>
      )}

      {/* Модалка звука (доступна из главного меню, без входа в игру) */}
      {soundOpen && (
        <div className="modal-bg" onClick={() => setSoundOpen(false)}>
          <div className="modal" onClick={(e) => e.stopPropagation()}>
            <div className="modal-head">
              <h3>🔊 Звук</h3>
              <button className="close-btn" onClick={() => setSoundOpen(false)}>✖</button>
            </div>
            <p className="desc">Громкость музыки, эмбиента и звуков. Меняется сразу, без входа в игру.</p>
            {SOUND_KEYS.map(([key, label]) => (
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
            <div className="combat-buttons">
              <button className="btn-ok" onClick={() => setSoundOpen(false)}>Готово</button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
