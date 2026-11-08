export const sellPriceFor = (item, trader, multiplier = 1) =>
  Math.max(1, Math.floor((item?.price || 0) * (trader?.buyRate ?? 0.5) * (multiplier || 1)));

// Скидка за лояльность: 2.5% за каждый уровень выше первого (см. TraderLoyalty).
const loyaltyDiscount = (trader) =>
  1 - 0.025 * (Math.max(1, trader?.loyaltyLevel ?? 1) - 1);

export const buyPriceFor = (item, trader, multiplier = 1) =>
  Math.max(
    1,
    Math.round(
      (item?.price || 0) *
        (trader?.priceMultiplier ?? 1) *
        loyaltyDiscount(trader) *
        (multiplier || 1),
    ),
  );

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
