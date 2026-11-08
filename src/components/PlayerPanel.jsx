import { AVATARS } from "../avatars";
import RubleIcon from "./RubleIcon";

export default function PlayerPanel({ player, onAvatarClick, onAvatarChange }) {
  const dmg = 5 + (player.weapon?.damage ?? 0);
  const def = (player.helmet?.defense ?? 0) + (player.armor?.defense ?? 0) + (player.suit?.defense ?? 0);

  const avatar = AVATARS.find((a) => a.id === player.avatar);
  const avatarSrc = `/avatars/${player.avatar || "messer"}.${avatar?.ext || "png"}`;

  const Bar = ({ value, max, cls }) => {
    const safeMax = Math.max(1, max || 1);
    const pct = Math.max(0, Math.min(100, (100 * (value || 0)) / safeMax));
    return (
      <div className="bar-outer">
        <div className={`bar-inner ${cls}`} style={{ width: `${pct}%` }} />
        <span className="bar-label">
          {value}/{max}
        </span>
      </div>
    );
  };

  return (
    <div className="player-panel">
      <div className="hero-bar">
        {/* Аватар ГГ — кликабельный, открывает выбор */}
        <button
          className="hero-avatar"
          onClick={onAvatarChange}
          title="Сменить аватар"
        >
          <img
            src={avatarSrc}
            alt="Аватар"
            className="hero-img"
            onError={(e) => {
              e.currentTarget.style.display = "none";
              e.currentTarget.nextSibling.style.display = "flex";
            }}
          />
          <span className="hero-avatar-fallback" style={{ display: "none" }}>
            👤
          </span>
        </button>

        {/* Кнопка инвентаря */}
        <button
          className="hero-inventory-btn"
          onClick={onAvatarClick}
          title="Инвентарь"
        >
          <img
            src="/backpack.png"
            alt="Инвентарь"
            className="hero-backpack-img"
          />
          <span className="hero-inventory-label">Инвентарь</span>
        </button>
      </div>

      <div className="stats">
        <div className="row">
          <b>Ур. {player.level}</b>
          <span className="money"><RubleIcon size={15} /> {player.money}</span>
        </div>
        <div className="row">
          <span>⚔️ Урон: {dmg}</span>
          <span>🛡️ Защита: {def}</span>
        </div>
        <div className="row">
          <span>❤️ HP</span>
          <Bar value={player.hp} max={player.maxHp} cls="hp" />
        </div>
        <div className="row">
          <span>⚡ Выносл.</span>
          <Bar value={player.stamina} max={player.maxStamina} cls="stam" />
        </div>
        <div className="row">
          <span>🍖 Голод</span>
          <Bar value={player.hunger} max={100} cls="food" />
        </div>
        <div className="row">
          <span>💧 Жажда</span>
          <Bar value={player.thirst} max={100} cls="water" />
        </div>
        <div className="row">
          <span>✨ Опыт</span>
          <Bar
            value={player.xpIntoLevel ?? 0}
            max={player.xpForNextLevel ?? 100}
            cls="xp"
          />
        </div>
        <div className="row">
          <span>⚖ Вес</span>
          <Bar
            value={player.currentWeight ?? 0}
            max={player.maxWeight ?? 50}
            cls="weight"
          />
        </div>
        {player.perkPoints > 0 && (
          <div className="row">
            <span>🧬 Очки навыков</span>
            <b style={{ color: "#c9a6ff" }}>{player.perkPoints}</b>
          </div>
        )}
      </div>
    </div>
  );
}