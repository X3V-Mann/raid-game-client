import { useState } from "react";

export const MISSING_ICON = "/error_or_missing.png";
export const RAID_FOUND_ICON = "/Item_from_Raid.png";

export const rarityClass = (r) =>
  ["r-common", "r-uncommon", "r-rare", "r-epic", "r-legendary"][r] || "r-common";

export const rarityName = (r) =>
  ["Обычное", "Необычное", "Редкое", "Эпическое", "Легендарное"][r] || "Обычное";

export const ItemIcon = ({ item, size = null }) => {
  const [broken, setBroken] = useState(false);
  const icon = item?.icon;
  const foundInRaid = !!(item?.foundInRaid ?? item?.FoundInRaid);

  const style = size ? { width: size, height: size } : undefined;

  const main =
    typeof icon === "string" && icon.startsWith("/") && !broken ? (
      <img
        src={icon}
        alt={item?.name || ""}
        style={style}
        onError={() => setBroken(true)}
      />
    ) : (
      <img
        src={MISSING_ICON}
        alt={item?.name ? `${item.name} (нет спрайта)` : "нет спрайта"}
        className="missing-icon"
        style={style}
      />
    );

  if (!foundInRaid) return main;
  return (
    <>
      {main}
      <img src={RAID_FOUND_ICON} alt="Найдено в рейде" className="raid-found-badge" title="Найдено в рейде" />
    </>
  );
};
