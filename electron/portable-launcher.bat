@echo off
setlocal
cd /d "%~dp0resources\api"
set ASPNETCORE_URLS=http://127.0.0.1:5000
set ASPNETCORE_ENVIRONMENT=Production
set RAID_SAVE_DIR=%APPDATA%\Raid Game\saves
if not exist "%RAID_SAVE_DIR%" mkdir "%RAID_SAVE_DIR%"
start "" /min "GameApi.exe"
timeout /t 2 /nobreak >nul
start "" "http://127.0.0.1:5000"
