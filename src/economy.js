export const sellPriceFor = (item, trader, multiplier = 1) =>
  Math.max(1, Math.floor((item?.price || 0) * (trader?.buyRate ?? 0.5) * (multiplier || 1)));

export const buyPriceFor = (item, trader, multiplier = 1) =>
  Math.max(1, Math.round((item?.price || 0) * (trader?.priceMultiplier ?? 1) * (multiplier || 1)));

export const traderBuys = (trader, item) =>
  !!trader && (!!trader.buysEverything || (trader.buys || []).includes(item?.type));

export const TYPE_LABELS = {
  WeaponMelee: "Ближний бой",
  WeaponRanged: "Огнестрел",
  Helmet: "Шлем",
  Armor: "Броня",
  Backpack: "Рюкзак",
  Food: "Еда",
  Drink: "Вода",
  Medicine: "Медицина",
  Material: "Материал",
  Misc: "Прочее",
  Cassette: "Кассета",
};

export const RARITY_LABELS = ["Обычное", "Необычное", "Редкое", "Эпическое", "Легендарное"];
