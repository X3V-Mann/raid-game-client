import { useEffect, useState } from "react";
import { api } from "../api";

const SLOTS = [1, 2, 3, 4, 5, 6];

export default function Saves({ onToast, refreshPlayer }) {
  const [saves, setSaves] = useState([]);
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);

  const loadSavesList = async () => {
    setLoading(true);
    try {
      const list = await api.getSaves();
      setSaves(list);
    } catch (e) {
      onToast?.(e.message || "Ошибка загрузки списка");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadSavesList();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const handleSave = async (slot) => {
    if (busy) return;
    setBusy(true);
    try {
      await api.saveGame(String(slot));
      onToast?.(`Игра сохранена в слот ${slot}`);
      await loadSavesList();
    } catch (e) {
      onToast?.(e.message || "Ошибка сохранения");
    } finally {
      setBusy(false);
    }
  };

  const handleLoad = async (filename) => {
    if (busy) return;
    if (!window.confirm(`Загрузить сохранение ${filename}? Текущий прогресс будет перезаписан.`)) return;
    setBusy(true);
    try {
      await api.loadGame(filename);
      await refreshPlayer();
      onToast?.("Игра загружена");
    } catch (e) {
      onToast?.(e.message || "Ошибка загрузки");
    } finally {
      setBusy(false);
    }
  };

  const handleDelete = async (filename) => {
    if (busy) return;
    if (!window.confirm(`Удалить сохранение ${filename}? Это действие необратимо.`)) return;
    setBusy(true);
    try {
      await api.deleteSave(filename);
      await loadSavesList();
      onToast?.("Сохранение удалено");
    } catch (e) {
      onToast?.(e.message || "Ошибка удаления");
    } finally {
      setBusy(false);
    }
  };

  const handleDeleteAll = async () => {
    if (busy || saves.length === 0) return;
    if (!window.confirm("Удалить ВСЕ сохранения? Текущая игра в памяти останется, но файлы исчезнут.")) return;
    setBusy(true);
    try {
      for (const s of saves) {
        await api.deleteSave(s.name);
      }
      await loadSavesList();
      onToast?.("Все сохранения удалены");
    } catch (e) {
      onToast?.(e.message || "Ошибка удаления");
      await loadSavesList();
    } finally {
      setBusy(false);
    }
  };

  return (
    <div className="panel">
      <h3>💾 Сохранения</h3>

      <div className="settings-block" style={{ borderLeftColor: "#6aaaff" }}>
        <h4 style={{ color: "#6aaaff" }}>Создать новое сохранение</h4>
        <div className="save-slots">
          {SLOTS.map((n) => (
            <button key={n} className="btn-heal" onClick={() => handleSave(n)} disabled={busy}>
              💾 Слот {n}
            </button>
          ))}
        </div>
      </div>

      <div className="settings-block" style={{ borderLeftColor: "#d4a04a" }}>
        <div className="saves-head">
          <h4 style={{ color: "#d4a04a" }}>Ваши сохранения</h4>
          {saves.length > 0 && (
            <button className="btn-danger small" onClick={handleDeleteAll} disabled={busy}>
              🗑 Удалить все
            </button>
          )}
        </div>

        {loading ? (
          <p className="desc">Загрузка списка...</p>
        ) : saves.length === 0 ? (
          <p className="desc">Нет сохранений. Создайте первое!</p>
        ) : (
          <div className="save-list">
            {saves.map((s) => (
              <div key={s.name} className="save-item">
                <div className="save-info">
                  <b>
                    {s.name === "autosave.json"
                      ? "🔄 Автосохранение"
                      : s.name.replace(".json", "").replace("save_", "Слот ")}
                  </b>
                  <span>
                    {s.playerName ? `${s.playerName} · ур. ${s.level} · ` : ""}
                    {s.storyTotal > 0 ? `сюжет ${s.storyDone}/${s.storyTotal} · ` : ""}
                    {s.time}
                  </span>
                </div>
                <div className="save-actions">
                  <button className="btn-ok" onClick={() => handleLoad(s.name)} disabled={busy}>
                    ⬆ Загрузить
                  </button>
                  <button className="btn-danger" onClick={() => handleDelete(s.name)} disabled={busy}>
                    🗑 Удалить
                  </button>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
