const { contextBridge, ipcRenderer } = require("electron");

contextBridge.exposeInMainWorld("desktop", {
  isDesktop: true,
  quit: () => ipcRenderer.invoke("desktop:quit"),
  getDisplay: () => ipcRenderer.invoke("desktop:get-display"),
  setDisplay: (mode) => ipcRenderer.invoke("desktop:set-display", mode),
});
