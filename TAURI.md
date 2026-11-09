# Десктопная версия (Tauri v2, portable)

Electron удалён, добавлен каркас Tauri. Игра по-прежнему использует фронт (React/Vite) + бэкенд `GameApi` (ASP.NET Core). В десктопе Tauri запускает `GameApi` как sidecar и открывает `http://127.0.0.1:5000` (API отдаёт `wwwroot`).

## Что нужно установить (Windows)
1. **Node.js/npm** и **.NET SDK 8**.
2. **Rust**: https://rustup.rs/ (`rustup`, `cargo`, `rustc`).
3. Зависимости Tauri для Windows (WebView2 обычно уже есть; при необходимости MSVC Build Tools): https://v2.tauri.app/start/prerequisites/

Проверь: `cargo --version`, `rustc --version`, `dotnet --version`.

## Установка JS-зависимостей
```powershell
npm install
```

## Режим разработки (десктоп)
1. Запусти бэкенд отдельно (Tauri dev открывает Vite на 5173, который проксирует `/api` на 5000):
   ```powershell
   dotnet run --project .\GameApi\GameApi.csproj --urls http://localhost:5000
   ```
2. В другом терминале:
   ```powershell
   npm run tauri:dev
   ```

## Сборка portable/exe (Windows)
Скрипт собирает фронт в `GameApi/wwwroot`, публикует `GameApi` как self-contained `win-x64` в `src-tauri/binaries/` и копирует sidecar с нужным именем таргета (`GameApi-x86_64-pc-windows-msvc.exe`), затем запускает `tauri build`.

```powershell
npm run tauri:build
```

Артефакты появятся в `src-tauri/target/release/bundle/` (NSIS/установщики в зависимости от таргета; для строго portable/single-exe поведение зависит от настроек bundle Tauri — при необходимости можно добавить/настроить таргет и конфиг). Данные (фронт в `wwwroot` + рантайм .NET) идут внутри публикации sidecar/API.

## Что реализовано в каркасе
- `src-tauri/` (Tauri v2): окно 1280×800, **min 1024×768**, запуск sidecar `GameApi`, ожидание порта 5000, переход на `http://127.0.0.1:5000`.
- Команды: `desktop_quit`, `desktop_get_display`, `desktop_set_display` (fullscreen/оконный).
- Фронт: `src/desktop.js` + кнопка **«Выйти из игры»** и настройки режима экрана (работают, когда доступны Tauri API).
- `GameApi` поддерживает `RAID_SAVE_DIR` (сохранения для десктопа).

## Если что-то не взлетит
- Rust не установлен — Tauri не соберётся (ошибка про `cargo`).
- Sidecar: убедись, что после `tauri:prepare-api` в `src-tauri/binaries/` есть `GameApi-x86_64-pc-windows-msvc.exe`.
- Иконки в `src-tauri/icons/` сейчас заглушки — для нормальной сборки/вида замени на реальные (`npx @tauri-apps/cli icon <source.png>` при готовом Rust/Tauri окружении).
- Если при загрузке с `localhost:5000` Tauri IPC недоступен в вебвью, игра всё равно запустится; управление окном можно донастроить (инъекция/конфиг Tauri) — напиши, что именно не работает, поправлю точечно.
