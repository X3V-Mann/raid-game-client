import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    // Если порт занят — лучше упасть с понятной ошибкой, чем тихо уехать на 5174
    // (сервер всё равно ждёт клиент на 5173).
    strictPort: true,
    proxy: {
      // Благодаря прокси клиенту не нужен CORS: /api уходит на C#-сервер.
      "/api": {
        target: "http://localhost:5000",
        changeOrigin: true,
      },
    },
  },
});
