// Desktop bridge: supports Tauri v2 (invoke) and legacy Electron preload (`window.desktop`).
function getTauriInvoke() {
  try {
    return window.__TAURI__?.core?.invoke || null;
  } catch {
    return null;
  }
}

export function isDesktop() {
  return !!(window.desktop?.isDesktop || window.__TAURI_INTERNALS__ || window.__TAURI__);
}

export async function desktopQuit() {
  if (window.desktop?.quit) return window.desktop.quit();
  const invoke = getTauriInvoke();
  if (invoke) return invoke("desktop_quit");
  window.close();
}

export async function desktopGetDisplay() {
  if (window.desktop?.getDisplay) return window.desktop.getDisplay();
  const invoke = getTauriInvoke();
  if (invoke) return invoke("desktop_get_display");
  return { fullscreen: !!document.fullscreenElement, minWidth: 1024, minHeight: 768 };
}

export async function desktopSetDisplay(mode) {
  if (window.desktop?.setDisplay) return window.desktop.setDisplay(mode);
  const invoke = getTauriInvoke();
  if (invoke) return invoke("desktop_set_display", { mode });
  // browser fallback
  try {
    if (mode === "fullscreen") await document.documentElement.requestFullscreen?.();
    else await document.exitFullscreen?.();
  } catch {}
  return { fullscreen: !!document.fullscreenElement, minWidth: 1024, minHeight: 768 };
}
