import { useEffect, useState } from "react";

const STORAGE_KEY = "raid.audio.settings";

const DEFAULT_SETTINGS = {
  music: 0.35,
  ambient: 0.30,
  sfx: 0.55,
};

const SFX_FILES = {
  interact: "/audio/interact.mp3",
  click: "/audio/click.mp3",
  mission: "/audio/mission.mp3",
};

const AMBIENT_DAY = [
  "/audio/ambient/Jupiter's Theme. Day.mp3",
  "/audio/ambient/Theme of Pripyat. Day.mp3",
  "/audio/ambient/Theme of Zaton. Day.mp3",
].map((p) => encodeURI(p));
const AMBIENT_NIGHT = [
  "/audio/ambient/Theme of Jupiter. Night.mp3",
  "/audio/ambient/Theme of Pripyat. Night.mp3",
  "/audio/ambient/Zaton Theme. Night.mp3",
].map((p) => encodeURI(p));

const TRADER_MUSIC = [
  "/audio/Traders/CS_Bandits_theme.ogg",
  "/audio/Traders/CS_Clear_Sky_theme.ogg",
  "/audio/Traders/CS_Duty_theme.ogg",
  "/audio/Traders/CS_Freedom_theme.ogg",
].map((p) => encodeURI(p));

const clamp01 = (v) => Math.max(0, Math.min(1, Number(v) || 0));

function shuffle(arr) {
  const a = [...arr];
  for (let i = a.length - 1; i > 0; i--) {
    const j = Math.floor(Math.random() * (i + 1));
    [a[i], a[j]] = [a[j], a[i]];
  }
  return a;
}

class AudioManager {
  constructor() {
    this.settings = this.loadSettings();
    this.scene = "menu";
    this.unlocked = false;
    this.cassette = null;
    this.listeners = new Set();
    this.lastHover = null;
    this.lastInteractAt = 0;

    // Музыка убежища: в меню зациклена, вне убежища играет один раз и затем переходит в эмбиент.
    // preload "none", чтобы не тянуть 3+ МБ при загрузке страницы.
    this.menuAudio = this.createAudio("/audio/menu.mp3", false, "none");
    this.menuAudio.addEventListener("ended", () => this.onMenuEnded());

    this.ambientQueue = [];
    this.isNight = false;
    this.ambientAudio = new Audio();
    this.ambientAudio.preload = "none";
    this.ambientAudio.addEventListener("ended", () => this.playNextAmbient());
    this.ambientAudio.addEventListener("error", () => this.playNextAmbient());

    this.cassetteAudio = new Audio();
    this.cassetteAudio.loop = true;
    this.cassetteAudio.preload = "none";
    this.cassetteAudio.addEventListener("play", () => this.notify());
    this.cassetteAudio.addEventListener("pause", () => this.notify());
    this.cassetteAudio.addEventListener("ended", () => this.onCassetteEnded());

    // Музыка во вкладке торговцев (случайный трек из папки Traders).
    this.traderActive = false;
    this.traderAudio = new Audio();
    this.traderAudio.loop = true;
    this.traderAudio.preload = "none";

    this.applyVolumes();
    this.bindUnlock();
    this.bindSfx();
  }

  createAudio(src, loop, preload) {
    const el = new Audio(src);
    el.loop = loop;
    el.preload = preload;
    return el;
  }

  loadSettings() {
    try {
      const raw = localStorage.getItem(STORAGE_KEY);
      if (!raw) return { ...DEFAULT_SETTINGS };
      const parsed = JSON.parse(raw);
      return {
        music: clamp01(parsed.music ?? DEFAULT_SETTINGS.music),
        ambient: clamp01(parsed.ambient ?? DEFAULT_SETTINGS.ambient),
        sfx: clamp01(parsed.sfx ?? DEFAULT_SETTINGS.sfx),
      };
    } catch {
      return { ...DEFAULT_SETTINGS };
    }
  }

  saveSettings() {
    try {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(this.settings));
    } catch {
      // localStorage может быть недоступен
    }
  }

  subscribe(fn) {
    this.listeners.add(fn);
    return () => this.listeners.delete(fn);
  }

  notify() {
    this.listeners.forEach((fn) => {
      try {
        fn();
      } catch {
        // ignore
      }
    });
  }

  getSettings() {
    return { ...this.settings };
  }

  setVolume(key, value) {
    if (!(key in this.settings)) return;
    this.settings[key] = clamp01(value);
    this.applyVolumes();
    this.saveSettings();
    this.notify();
  }

  applyVolumes() {
    this.menuAudio.volume = this.settings.music;
    this.cassetteAudio.volume = this.settings.music;
    this.traderAudio.volume = this.settings.music;
    this.ambientAudio.volume = this.settings.ambient;
  }

  bindUnlock() {
    if (typeof document === "undefined") return;
    const unlock = () => {
      if (this.unlocked) return;
      this.unlocked = true;
      this.playScene();
    };
    document.addEventListener("pointerdown", unlock, { once: true, capture: true });
    document.addEventListener("keydown", unlock, { once: true, capture: true });
  }

  bindSfx() {
    if (typeof document === "undefined") return;

    document.addEventListener("pointerover", (e) => {
      const el = e.target?.closest?.("button, a");
      if (!el || el.disabled) return;
      if (e.relatedTarget && el.contains(e.relatedTarget)) return;
      if (this.lastHover === el) return;

      this.lastHover = el;
      const now = Date.now();
      if (now - this.lastInteractAt < 70) return;
      this.lastInteractAt = now;
      this.playSfx("interact");
    });

    document.addEventListener("pointerout", (e) => {
      const el = e.target?.closest?.("button, a");
      if (el && this.lastHover === el && !el.contains(e.relatedTarget)) this.lastHover = null;
    });

    document.addEventListener("click", (e) => {
      const el = e.target?.closest?.("button, a");
      if (el && !el.disabled) this.playSfx("click");
    });
  }

  playSfx(name) {
    if (!this.unlocked) return;
    const src = SFX_FILES[name];
    if (!src) return;
    try {
      const sfx = new Audio(src);
      sfx.volume = this.settings.sfx;
      sfx.play().catch(() => {});
    } catch {
      // ignore
    }
  }

  playMenu(loop = true) {
    if (!this.unlocked) return;
    if (this.cassette) return;
    this.menuAudio.loop = !!loop;
    if (loop || this.menuAudio.paused) this.menuAudio.play().catch(() => {});
  }

  pauseMenu() {
    this.menuAudio.pause();
  }

  setNight(isNight) {
    const next = !!isNight;
    if (this.isNight === next) return;
    this.isNight = next;
    this.ambientQueue = []; // смена суток — обновим подборку при следующем треке
  }

  ensureAmbientQueue() {
    if (this.ambientQueue.length === 0) this.ambientQueue = shuffle(this.isNight ? AMBIENT_NIGHT : AMBIENT_DAY);
  }

  playNextAmbient() {
    if (!this.unlocked || this.cassette || this.traderActive) return;
    // Эмбиент играет в игровой сцене (в меню — музыка убежища)
    if (this.scene !== "game") return;
    this.ensureAmbientQueue();
    const src = this.ambientQueue.shift();
    if (!src) return;
    this.ambientAudio.src = src;
    this.ambientAudio.volume = this.settings.ambient;
    this.ambientAudio.play().catch(() => {});
  }

  playAmbient() {
    if (!this.unlocked || this.scene !== "game" || this.cassette || this.traderActive) return;
    if (!this.ambientAudio.paused && this.ambientAudio.src) return;
    this.playNextAmbient();
  }

  pauseAmbient() {
    this.ambientAudio.pause();
  }

  onMenuEnded() {
    // Музыка меню играет только в меню; в игре этот хук не используется.
  }

  playScene() {
    if (!this.unlocked) return;
    if (this.scene === "menu") {
      this.pauseAmbient();
      this.playMenu(true);
    } else {
      this.pauseMenu();
      if (!this.cassette && !this.traderActive) this.playAmbient();
    }
  }

  setScene(scene) {
    if (scene === this.scene) return;
    this.scene = scene;
    if (scene === "menu") {
      this.stopTraderMusic();
      this.stopCassette(false);
      this.pauseAmbient();
      this.playMenu(true);
    } else {
      // Музыка меню — только в меню. В игре: эмбиент (кассета/торговец перекрывает).
      this.pauseMenu();
      if (!this.cassette && !this.traderActive) this.playAmbient();
    }
    this.notify();
  }

  playCassette(track) {
    if (!track?.src) return;
    this.scene = "game";
    this.pauseMenu();
    this.pauseAmbient();

    if (this.cassette?.id === track.id) {
      if (this.cassetteAudio.paused) this.cassetteAudio.play().catch(() => {});
      else this.cassetteAudio.pause();
      this.notify();
      return;
    }

    this.cassette = track;
    this.cassetteAudio.src = track.src;
    this.cassetteAudio.loop = true;
    this.cassetteAudio.volume = this.settings.music;
    this.cassetteAudio.play().catch(() => {});
    this.notify();
  }

  stopCassette(resume = true) {
    this.cassette = null;
    this.cassetteAudio.pause();
    this.cassetteAudio.currentTime = 0;
    if (resume && this.scene === "game" && !this.traderActive) {
      // Возобновляем текущий эмбиент, если он был на паузе (кассета играла в убежище),
      // иначе запускаем следующий трек.
      if (this.ambientAudio.src && !this.ambientAudio.ended) {
        this.ambientAudio.volume = this.settings.ambient;
        this.ambientAudio.play().catch(() => this.playNextAmbient());
      } else {
        this.playNextAmbient();
      }
    }
    this.notify();
  }

  // Трек закончился — плавный переход (кассета играла в т.ч. вне убежища).
  onCassetteEnded() {
    this.stopCassette(true);
  }

  playTraderMusic() {
    if (!this.unlocked) return;
    if (this.traderActive && this.traderAudio.src && !this.traderAudio.paused) return;
    this.traderActive = true;
    this.pauseAmbient();
    this.pauseMenu();
    if (this.cassette && !this.cassetteAudio.paused) {
      this._cassettePausedByTrader = true;
      this.cassetteAudio.pause();
    }
    if (!this.traderAudio.src || this.traderAudio.ended) {
      this.traderAudio.src = TRADER_MUSIC[Math.floor(Math.random() * TRADER_MUSIC.length)];
    }
    this.traderAudio.volume = this.settings.music;
    this.traderAudio.play().catch(() => {});
    this.notify();
  }

  stopTraderMusic() {
    if (!this.traderActive && !this.traderAudio.src) return;
    const wasActive = this.traderActive;
    this.traderActive = false;
    this.traderAudio.pause();
    this.traderAudio.currentTime = 0;
    this.traderAudio.removeAttribute("src");
    this.traderAudio.load?.();
    if (this._cassettePausedByTrader && this.cassette) {
      this._cassettePausedByTrader = false;
      this.cassetteAudio.play().catch(() => {});
      this.notify();
      return;
    }
    this._cassettePausedByTrader = false;
    if (wasActive) {
      if (this.scene === "game" && !this.cassette) this.playAmbient();
      else if (this.scene === "menu") this.playMenu(true);
    }
    this.notify();
  }

  isCassettePlaying() {
    return !!this.cassette && !this.cassetteAudio.paused;
  }
}

export const audio = new AudioManager();

export function useAudioState() {
  const [, force] = useState(0);
  useEffect(() => {
    return audio.subscribe(() => force((n) => n + 1));
  }, []);
  return {
    settings: audio.getSettings(),
    cassette: audio.cassette,
    playing: audio.isCassettePlaying(),
  };
}
