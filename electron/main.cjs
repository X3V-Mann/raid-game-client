const { app, BrowserWindow, ipcMain, shell } = require("electron");
const path = require("path");
const fs = require("fs");
const { spawn } = require("child_process");
const http = require("http");

let mainWindow = null;
let apiProcess = null;
let apiExitedCode = null;
let isQuitting = false;

const isDev = !app.isPackaged;
const MIN_W = 1024;
const MIN_H = 768;

function log(msg) {
  try {
    const line = `[${new Date().toISOString()}] ${msg}\n`;
    const dir = app.getPath("userData");
    fs.mkdirSync(dir, { recursive: true });
    fs.appendFileSync(path.join(dir, "desktop.log"), line, "utf8");
  } catch {}
}

function setupAutoUpdates() {
  if (isDev) return;
  try {
    const { autoUpdater } = require("electron-updater");
    autoUpdater.autoDownload = true;
    autoUpdater.checkForUpdatesAndNotify().catch(() => {});
  } catch {}
}

function getApiPort() {
  return Number(process.env.RAID_API_PORT || 5000);
}
function getApiUrl() {
  return `http://127.0.0.1:${getApiPort()}`;
}

function waitForApi(url, timeoutMs = 45000) {
  const started = Date.now();
  return new Promise((resolve, reject) => {
    const tick = () => {
      if (apiExitedCode != null) return reject(new Error(`Сервер игры завершился (код ${apiExitedCode}). См. лог: ${path.join(app.getPath("userData"), "desktop.log")}`));
      const req = http.get(url + "/api/health", (res) => {
        res.resume();
        if (res.statusCode && res.statusCode >= 200 && res.statusCode < 500) return resolve();
        if (Date.now() - started > timeoutMs) return reject(new Error("Таймаут ожидания сервера игры (порт 5000). Возможно, порт занят или сервер не запустился."));
        setTimeout(tick, 450);
      });
      req.on("error", () => {
        if (Date.now() - started > timeoutMs) return reject(new Error("Таймаут ожидания сервера игры (порт 5000). Возможно, порт занят или сервер не запустился."));
        setTimeout(tick, 450);
      });
      req.setTimeout(2000, () => req.destroy());
    };
    tick();
  });
}

function findApiExe() {
  if (isDev) {
    const devExe = path.join(__dirname, "..", "GameApi", "bin", "Debug", "net8.0", "GameApi.exe");
    if (fs.existsSync(devExe)) return { exe: devExe, cwd: path.dirname(devExe) };
    return null;
  }
  const candidates = [
    path.join(process.resourcesPath, "api", "GameApi.exe"),
    path.join(process.resourcesPath, "api", "GameApi"),
    path.join(path.dirname(process.execPath), "resources", "api", "GameApi.exe"),
  ];
  for (const exe of candidates) if (fs.existsSync(exe)) return { exe, cwd: path.dirname(exe) };
  return null;
}

async function ensureApi() {
  const url = getApiUrl();
  apiExitedCode = null;
  try {
    await waitForApi(url, 2000);
    log("API already running at " + url);
    return { ok: true, url };
  } catch {}

  const found = findApiExe();
  if (!found) {
    if (isDev) return { ok: true, url, external: true };
    throw new Error("Не найден сервер игры (resources/api/GameApi.exe).");
  }

  const contentRoot = isDev ? path.join(__dirname, "..", "GameApi") : found.cwd;
  const saveDir = isDev
    ? path.join(__dirname, "..", "GameApi", "saves")
    : path.join(app.getPath("userData"), "saves");
  try { fs.mkdirSync(saveDir, { recursive: true }); } catch {}

  log(`Starting API: ${found.exe} cwd=${found.cwd} url=${url}`);
  const out = fs.openSync(path.join(app.getPath("userData"), "desktop-api.log"), "a");
  apiProcess = spawn(found.exe, [], {
    cwd: found.cwd,
    env: {
      ...process.env,
      ASPNETCORE_URLS: url,
      ASPNETCORE_CONTENTROOT: contentRoot,
      ASPNETCORE_ENVIRONMENT: isDev ? "Development" : "Production",
      RAID_SAVE_DIR: saveDir,
    },
    stdio: ["ignore", out, out],
    windowsHide: true,
  });
  apiProcess.on("exit", (code) => {
    apiExitedCode = code == null ? -1 : code;
    apiProcess = null;
    log(`API exited: ${apiExitedCode}`);
    try { fs.closeSync(out); } catch {}
  });
  await waitForApi(url, 45000);
  log("API ready");
  return { ok: true, url };
}

function loadStatus(message, isError = false) {
  if (!mainWindow) return;
  const safe = String(message).replace(/&/g,"&amp;").replace(/</g,"&lt;").replace(/>/g,"&gt;");
  const color = isError ? "#ff8b7a" : "#d4a04a";
  mainWindow.loadURL(`data:text/html;charset=utf-8,` + encodeURIComponent(`<!doctype html><html><body style="margin:0;background:#1a1714;color:${color};font:16px/1.5 Segoe UI,Arial;display:grid;place-items:center;height:100vh"><div style="max-width:720px;padding:24px"><h2 style="margin:0 0 12px;color:#f3e7d3">Raid Game</h2><div>${safe}</div></div></body></html>`));
}

function createWindow() {
  mainWindow = new BrowserWindow({
    width: 1280,
    height: 800,
    minWidth: MIN_W,
    minHeight: MIN_H,
    backgroundColor: "#1a1714",
    autoHideMenuBar: true,
    show: false,
    webPreferences: {
      preload: path.join(__dirname, "preload.cjs"),
      contextIsolation: true,
      nodeIntegration: false,
      sandbox: false,
    },
  });
  mainWindow.once("ready-to-show", () => mainWindow.show());
  mainWindow.webContents.setWindowOpenHandler(({ url }) => {
    shell.openExternal(url);
    return { action: "deny" };
  });
}

ipcMain.handle("desktop:quit", () => { isQuitting = true; app.quit(); });
ipcMain.handle("desktop:get-display", () => {
  if (!mainWindow) return { fullscreen: false, minWidth: MIN_W, minHeight: MIN_H };
  return { fullscreen: mainWindow.isFullScreen(), minWidth: MIN_W, minHeight: MIN_H };
});
ipcMain.handle("desktop:set-display", (_e, mode) => {
  if (!mainWindow) return { fullscreen: false };
  if (mode === "fullscreen") mainWindow.setFullScreen(true);
  else {
    mainWindow.setFullScreen(false);
    const [w, h] = mainWindow.getSize();
    if (w < MIN_W || h < MIN_H) mainWindow.setSize(Math.max(w, MIN_W), Math.max(h, MIN_H));
  }
  return { fullscreen: mainWindow.isFullScreen(), minWidth: MIN_W, minHeight: MIN_H };
});

app.whenReady().then(async () => {
  createWindow();
  setupAutoUpdates();
  const target = isDev && process.env.ELECTRON_START_URL ? process.env.ELECTRON_START_URL : getApiUrl();
  loadStatus("Запуск сервера игры...");
  try {
    const res = await ensureApi();
    const url = isDev && process.env.ELECTRON_START_URL ? process.env.ELECTRON_START_URL : (res?.url || getApiUrl());
    log("Loading " + url);
    await mainWindow.loadURL(url);
  } catch (e) {
    const msg = e?.message || String(e);
    log("Startup error: " + msg);
    loadStatus(msg + `\n\nЛог: ${path.join(app.getPath("userData"), "desktop.log")}`, true);
  }
  app.on("activate", () => {
    if (BrowserWindow.getAllWindows().length === 0) createWindow();
  });
});

app.on("before-quit", () => {
  isQuitting = true;
  if (apiProcess && !apiProcess.killed) try { apiProcess.kill(); } catch {}
});
app.on("window-all-closed", () => {
  if (process.platform !== "darwin" || isQuitting) app.quit();
});
