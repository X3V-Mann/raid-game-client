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
  sortInventory: () => req("/inventory/sort", { method: "POST" }),

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

  // --- Навыки / характеристики ---
  getSkills: () => req("/skills"),
  upgradeSkill: (id) => req(`/skills/${id}/upgrade`, { method: "POST" }),

  // --- Задания / сюжет / доска / дневник ---
  getQuests: () => req("/quests"),
  getStory: () => req("/story"),
  getBoard: () => req("/quests/board"),
  getDiary: () => req("/diary"),
  addThought: (text) => req("/diary/thought", { method: "POST", body: { text } }),
  addNote: (text) => req("/diary/note", { method: "POST", body: { text } }),
  getGameTime: () => req("/gametime"),
  acceptQuest: (id) => req(`/quests/${id}/accept`, { method: "POST" }),
  completeQuest: (id) => req(`/quests/${id}/complete`, { method: "POST" }),
  questProgress: (type, count) => req("/quests/progress", { method: "POST", body: { type, code: null, count } }),
  getTraderQuests: (traderId) => req(`/quests/trader/${traderId}`),
  getTraderDialogue: (traderId) => req(`/traders/${traderId}/dialogue`),
  talkTrader: (traderId) => req(`/traders/${traderId}/talk`, { method: "POST" }),
  endingChoice: (choice) => req("/story/ending-choice", { method: "POST", body: { choice } }),

  // --- Рейд ---
  getLocations: () => req("/raid/locations"),
  getRaidState: () => req("/raid/state"),
  startRaid: (tier, locationId) => req("/raid/start", { method: "POST", body: { tier, locationId } }),
  moveRaid: (x, y) => req("/raid/move", { method: "POST", body: { x, y } }),
  enterBuilding: (buildingId) => req("/raid/enter", { method: "POST", body: { buildingId } }),
  lootBuilding: (buildingId) =>
    req("/raid/loot-building", { method: "POST", body: { buildingId } }),
  lootBuildingStash: (buildingId) =>
    req("/raid/loot-building-stash", { method: "POST", body: { buildingId } }),
  lootStash: (x, y) => req("/raid/loot-stash", { method: "POST", body: { x, y } }),
  lootCar: (x, y) => req("/raid/loot-car", { method: "POST", body: { x, y } }),
  lootFurniture: (buildingId, furnitureId) =>
    req("/raid/loot-furniture", { method: "POST", body: { buildingId, furnitureId } }),
  npcTalk: (npcId) => req("/raid/npc/talk", { method: "POST", body: { npcId } }),
  npcBuy: (npcId, code, count = 1) => req("/raid/npc/buy", { method: "POST", body: { npcId, code, count } }),
  npcSell: (npcId, itemId, count = 1) => req("/raid/npc/sell", { method: "POST", body: { npcId, itemId, count } }),
  exitRaid: () => req("/raid/exit", { method: "POST" }),
  abandonRaid: () => req("/raid/abandon", { method: "POST" }),
  combatAction: (action, enemyId, itemId, targetX, targetY) =>
    req("/raid/combat/action", {
      method: "POST",
      body: { action, enemyId, itemId, targetX, targetY },
    }),
  combatEnemy: () => req("/raid/combat/enemy", { method: "POST" }),
  lootCorpse: (enemyId, itemId) =>
    req("/raid/combat/loot", { method: "POST", body: { enemyId, itemId } }),
  closeCombat: () => req("/raid/combat/close", { method: "POST" }),

  // --- Сохранения ---
  getSaves: () => req("/saves/list"),
  saveGame: (slot) => req(`/saves/save/${slot}`, { method: "POST" }),
  loadGame: (filename) => req(`/saves/load/${filename}`, { method: "POST" }),
  deleteSave: (filename) => req(`/saves/delete/${filename}`, { method: "DELETE" }),
  autoSave: () => req("/saves/save/auto", { method: "POST" }),
};
