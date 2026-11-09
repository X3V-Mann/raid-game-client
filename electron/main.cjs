const { app, BrowserWindow, ipcMain, shell } = require("electron");
const path = require("path");
const fs = require("fs");
const { spawn } = require("child_process");
const http = require("http");

let mainWindow = null;
let apiProcess = null;
let isQuitting = false;

const isDev = !app.isPackaged;
const MIN_W = 1024;
const MIN_H = 768;

function setupAutoUpdates() {
  if (isDev) return;
  try {
    const { autoUpdater } = require("electron-updater");
    autoUpdater.autoDownload = true;
    autoUpdater.checkForUpdatesAndNotify().catch(() => {});
  } catch {
    // обновления не настроены/зависимость недоступна — не критично
  }
}

function getApiPort() {
  return Number(process.env.RAID_API_PORT || 5000);
}

function getApiUrl() {
  return `http://127.0.0.1:${getApiPort()}`;
}

function waitForApi(url, timeoutMs = 30000) {
  const started = Date.now();
  return new Promise((resolve, reject) => {
    const tick = () => {
      const req = http.get(url + "/api/health", (res) => {
        res.resume();
        if (res.statusCode && res.statusCode >= 200 && res.statusCode < 500) return resolve();
        if (Date.now() - started > timeoutMs) return reject(new Error("API timeout"));
        setTimeout(tick, 400);
      });
      req.on("error", () => {
        if (Date.now() - started > timeoutMs) return reject(new Error("API timeout"));
        setTimeout(tick, 400);
      });
      req.setTimeout(2000, () => req.destroy());
    };
    tick();
  });
}

function findApiExe() {
  if (isDev) {
    // В dev можно запускать уже запущенный API или собранный exe; если exe есть - используем.
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
  try {
    await waitForApi(url, 2500);
    return; // уже запущен (удобно в dev)
  } catch {}

  const found = findApiExe();
  if (!found) {
    if (isDev) return; // в dev ожидаем внешний dotnet run/vite proxy setup
    throw new Error("Не найден сервер игры (GameApi).");
  }

  const contentRoot = isDev
    ? path.join(__dirname, "..", "GameApi")
    : found.cwd;
  const saveDir = isDev
    ? path.join(__dirname, "..", "GameApi", "saves")
    : path.join(app.getPath("userData"), "saves");
  try { fs.mkdirSync(saveDir, { recursive: true }); } catch {}

  apiProcess = spawn(found.exe, [], {
    cwd: found.cwd,
    env: {
      ...process.env,
      ASPNETCORE_URLS: url,
      ASPNETCORE_CONTENTROOT: contentRoot,
      ASPNETCORE_ENVIRONMENT: isDev ? "Development" : "Production",
      RAID_SAVE_DIR: saveDir,
    },
    stdio: "ignore",
    windowsHide: true,
  });
  apiProcess.on("exit", () => { apiProcess = null; });
  await waitForApi(url, 30000);
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

  if (isDev && process.env.ELECTRON_START_URL) {
    mainWindow.loadURL(process.env.ELECTRON_START_URL);
  } else if (isDev) {
    // Без dev-сервера пробуем локальный API (если собран wwwroot или запущен)
    mainWindow.loadURL(getApiUrl());
  } else {
    mainWindow.loadURL(getApiUrl());
  }
}

ipcMain.handle("desktop:quit", () => {
  isQuitting = true;
  app.quit();
});

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
  try {
    await ensureApi();
  } catch (e) {
    // Покажем окно с ошибкой, чтобы было понятно пользователю.
    console.error(e);
  }
  createWindow();
  setupAutoUpdates();
  app.on("activate", () => {
    if (BrowserWindow.getAllWindows().length === 0) createWindow();
  });
});

app.on("before-quit", () => {
  isQuitting = true;
  if (apiProcess && !apiProcess.killed) {
    try { apiProcess.kill(); } catch {}
  }
});

app.on("window-all-closed", () => {
  if (process.platform !== "darwin" || isQuitting) app.quit();
});
