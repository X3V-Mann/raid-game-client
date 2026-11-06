import { AVATARS } from "../avatars";

export default function AvatarPicker({ current, onSelect, onClose }) {
  return (
    <div className="modal-bg" onClick={onClose}>
      <div className="modal avatar-picker" onClick={(e) => e.stopPropagation()}>
        <div className="modal-head">
          <h3>Выбор аватара</h3>
          <button className="close-btn" onClick={onClose}>✖</button>
        </div>

        <div className="avatar-grid">
          {AVATARS.map((a) => (
            <button
              key={a.id}
              className={`avatar-choice ${current === a.id ? "active" : ""}`}
              onClick={() => onSelect(a.id)}
              title={a.name}
            >
              <img src={`/avatars/${a.id}.${a.ext || "png"}`} alt={a.name}
                onError={(e) => {
                  e.currentTarget.style.display = "none";
                  e.currentTarget.nextSibling.style.display = "flex";
                }}
              />
              <span className="avatar-fallback" style={{ display: "none" }}>
                {a.name[0]}
              </span>
              <span className="avatar-name">{a.name}</span>
            </button>
          ))}
        </div>
      </div>
    </div>
  );
}