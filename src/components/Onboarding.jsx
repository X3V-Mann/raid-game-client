import { useState } from "react";
import { api } from "../api";
import { AVATARS } from "../avatars";

export default function Onboarding({ onDone }) {
  const [name, setName] = useState("");
  const [avatar, setAvatar] = useState(AVATARS[0]?.id || "messer");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");

  const submit = async () => {
    const trimmed = name.trim();
    if (trimmed.length < 2) {
      setError("Имя минимум 2 символа");
      return;
    }
    if (trimmed.length > 20) {
      setError("Имя максимум 20 символов");
      return;
    }
    setError("");
    setBusy(true);
    try {
      const updated = await api.onboard(trimmed, avatar);
      onDone(updated);
    } catch (e) {
      setError(e.message || "Ошибка");
    } finally {
      setBusy(false);
    }
  };

  return (
    <div className="onboarding-bg">
      <div className="onboarding-card">
        <h1 className="onboarding-title">Пролог</h1>
        <p className="onboarding-sub">
          Бункер. Холодный свет лампы. Ты — один из немногих, кто ещё дышит.
          Назови себя, выживший.
        </p>

        {/* Имя */}
        <div className="onboarding-field">
          <label>Имя</label>
          <input
            type="text"
            value={name}
            onChange={(e) => setName(e.target.value)}
            maxLength={20}
            autoFocus
            placeholder="Введи имя"
            onKeyDown={(e) => e.key === "Enter" && submit()}
          />
        </div>

        {/* Аватар */}
        <div className="onboarding-field">
          <label>Лицо</label>
          <div className="onboarding-avatars">
            {AVATARS.map((a) => (
              <button
                key={a.id}
                type="button"
                className={`onboarding-avatar ${avatar === a.id ? "active" : ""}`}
                onClick={() => setAvatar(a.id)}
                title={a.name}
              >
                <img
                  src={`/avatars/${a.id}.${a.ext || "png"}`}
                  alt={a.name}
                  onError={(e) => {
                    e.currentTarget.style.display = "none";
                  }}
                />
              </button>
            ))}
          </div>
        </div>

        {error && <div className="onboarding-error">{error}</div>}

        <button
          className="onboarding-submit"
          onClick={submit}
          disabled={busy || name.trim().length < 2}
        >
          {busy ? "..." : "Начать"}
        </button>
      </div>
    </div>
  );
}