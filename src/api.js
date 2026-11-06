// В dev-режиме Vite проксирует /api на http://localhost:5000 (см. vite.config.js).
// Для десктопной сборки можно задать VITE_API_URL, например http://127.0.0.1:5000/api.
const RAW_BASE = import.meta.env.VITE_API_URL || "/api";
const BASE = RAW_BASE.replace(/\/+$/, "");

async function req(path, opts = {}) {
  const res = await fetch(BASE + path, {
    headers: { "Content-Type": "application/json" },
    ...opts,
    body: opts.body ? JSON.stringify(opts.body) : undefined,
  });

  const text = await res.text();
  let data = null;
  if (text) {
    try {
      data = JSON.parse(text);
    } catch {
      data = text;
    }
  }

  if (!res.ok) {
    const message = typeof data === "string" && data ? data : "Ошибка запроса";
    throw new Error(message);
  }
  return data;
}

export const api = {
  // --- Игрок ---
  getPlayer: () => req("/player"),
  syncPlayer: (p) => req("/player/sync", { method: "POST", body: p }),
  die: () => req("/player/die", { method: "POST" }),
  resetGame: () => req("/dev/reset", { method: "POST" }),
  setAvatar: (avatar) => req("/player/avatar", { method: "POST", body: { avatar } }),
  onboard: (name, avatar) => req("/player/onboard", { method: "POST", body: { name, avatar } }),
  syncStats: (cells, survived) => req("/stats/sync", { method: "POST", body: { cells, survived } }),
  getAchievements: () => req("/achievements"),

  // --- Инвентарь / экипировка ---
  equip: (id) => req(`/equip/${id}`, { method: "POST" }),
  unequip: (slot) => req(`/unequip/${slot}`, { method: "POST" }),
  consume: (id) => req(`/consume/${id}`, { method: "POST" }),
  sell: (id, traderId) =>
    req(`/sell/${id}${traderId ? `?traderId=${encodeURIComponent(traderId)}` : ""}`, { method: "POST" }),
  sellPartial: (itemId, count, traderId) =>
    req("/sell-partial", { method: "POST", body: { itemId, count, traderId } }),
  drop: (id) => req(`/inventory/drop/${id}`, { method: "POST" }),
  addItems: (items) => req("/inventory/add", { method: "POST", body: { items } }),

  // --- Торговцы ---
  getTraders: () => req("/traders"),
  buyBatch: (traderId, entries) => req(`/traders/${traderId}/buy-batch`, { method: "POST", body: entries }),
  barter: (traderId, offerId) =>
    req(`/traders/${traderId}/barter/${offerId}`, { method: "POST" }),

  // --- Убежище ---
  getShelterInfo: () => req("/shelter/info"),
  upgradeShelter: (mod) => req(`/shelter/upgrade/${mod}`, { method: "POST" }),
  getRegenInfo: () => req("/shelter/regen-info"),
  stashStore: (id) => req(`/stash/store/${id}`, { method: "POST" }),
  stashTake: (id) => req(`/stash/take/${id}`, { method: "POST" }),

  // --- Перки ---
  getPerks: () => req("/perks"),
  buyPerk: (id) => req(`/perks/${id}`, { method: "POST" }),

  // --- Задания ---
  getQuests: () => req("/quests"),
  getStory: () => req("/story"),
  acceptQuest: (id) => req(`/quests/${id}/accept`, { method: "POST" }),
  completeQuest: (id) => req(`/quests/${id}/complete`, { method: "POST" }),
  questProgress: (type, count) => req("/quests/progress", { method: "POST", body: { type, code: null, count } }),
  getTraderQuests: (traderId) => req(`/quests/trader/${traderId}`),

  // --- Рейд ---
  getLocations: () => req("/raid/locations"),
  startRaid: (tier, locationId) => req("/raid/start", { method: "POST", body: { tier, locationId } }),
  combatStart: (locationId) => req("/raid/combat/start", { method: "POST", body: { locationId } }),
  combatAction: (action, enemyId, itemId) =>
    req("/raid/combat/action", { method: "POST", body: { action, enemyId, itemId } }),

  // --- Сохранения ---
  getSaves: () => req("/saves/list"),
  saveGame: (slot) => req(`/saves/save/${slot}`, { method: "POST" }),
  loadGame: (filename) => req(`/saves/load/${filename}`, { method: "POST" }),
  deleteSave: (filename) => req(`/saves/delete/${filename}`, { method: "DELETE" }),
  autoSave: () => req("/saves/save/auto", { method: "POST" }),
};
