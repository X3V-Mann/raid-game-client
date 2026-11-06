import js from "@eslint/js";
import globals from "globals";
import reactHooks from "eslint-plugin-react-hooks";
import reactRefresh from "eslint-plugin-react-refresh";
import { defineConfig, globalIgnores } from "eslint/config";

export default defineConfig([
  globalIgnores(["dist", "GameApi"]),
  {
    files: ["**/*.{js,jsx}"],
    extends: [
      js.configs.recommended,
      reactHooks.configs.flat.recommended,
      reactRefresh.configs.vite,
    ],
    languageOptions: {
      globals: globals.browser,
      parserOptions: { ecmaFeatures: { jsx: true } },
    },
    rules: {
      // В проекте компоненты-помощники (Bar, StatRow) объявлены рядом с основным —
      // это осознанный стиль, производительность здесь не критична.
      "react-hooks/static-components": "off",
      // Загрузка данных в useEffect с setState — нормальный паттерн для этого приложения.
      "react-hooks/set-state-in-effect": "off",
      // Math.random вызывается в обработчиках событий, а не в рендере.
      "react-hooks/purity": "off",
      // Inventory.jsx намеренно экспортирует утилиты (rarityClass, ItemIcon).
      "react-refresh/only-export-components": "off",
      // Пустые catch в drag&drop — ожидаемый фолбэк.
      "no-empty": ["error", { allowEmptyCatch: true }],
      "no-unused-vars": ["error", { argsIgnorePattern: "^_", varsIgnorePattern: "^_" }],
    },
  },
]);
