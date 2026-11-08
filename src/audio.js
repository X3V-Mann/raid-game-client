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

const clamp01 = (v) => Math.max(0, Math.min(1, Number(v) || 0));

class AudioManager {
  constructor() {
    this.settings = this.loadSettings();
    this.scene = "menu";
    this.unlocked = false;
    this.cassette = null;
    this.listeners = new Set();
    this.lastHover = null;
    this.lastInteractAt = 0;

    this.menuAudio = this.createAudio("/audio/menu.mp3", true, "auto");
    this.ambientAudio = this.createAudio("/audio/ambient.mp3", true, "none");
    this.cassetteAudio = new Audio();
    this.cassetteAudio.loop = true;
    this.cassetteAudio.preload = "none";
    this.cassetteAudio.addEventListener("play", () => this.notify());
    this.cassetteAudio.addEventListener("pause", () => this.notify());
    this.cassetteAudio.addEventListener("ended", () => this.onCassetteEnded());

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

  playMenu() {
    if (!this.unlocked || this.scene !== "menu") return;
    this.menuAudio.play().catch(() => {});
  }

  pauseMenu() {
    this.menuAudio.pause();
  }

  playAmbient() {
    if (!this.unlocked || this.scene !== "game" || this.cassette) return;
    this.ambientAudio.play().catch(() => {});
  }

  pauseAmbient() {
    this.ambientAudio.pause();
  }

  playScene() {
    if (!this.unlocked) return;
    if (this.scene === "menu") {
      this.pauseAmbient();
      this.playMenu();
    } else {
      this.pauseMenu();
      if (!this.cassette) this.playAmbient();
    }
  }

  setScene(scene) {
    if (scene === this.scene) return;
    this.scene = scene;
    if (scene === "menu") {
      this.stopCassette();
      this.pauseAmbient();
      this.playMenu();
    } else {
      this.pauseMenu();
      if (!this.cassette) this.playAmbient();
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

  stopCassette() {
    this.cassette = null;
    this.cassetteAudio.pause();
    this.cassetteAudio.currentTime = 0;
    if (this.scene === "game") this.playAmbient();
    this.notify();
  }

  // Трек закончился — плавный переход на эмбиент (музыка играла и за пределами убежища).
  onCassetteEnded() {
    this.stopCassette();
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
