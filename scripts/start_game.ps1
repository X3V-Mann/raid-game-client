[CmdletBinding()]
param(
    [switch]$CheckOnly,
    [switch]$NoBrowser
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

function Write-Head([string]$Text) { Write-Host $Text -ForegroundColor Yellow }
function Write-Ok([string]$Text) { Write-Host "  [OK] $Text" -ForegroundColor Green }
function Write-Err([string]$Text) { Write-Host "  [ОШИБКА] $Text" -ForegroundColor Red }
function Fail([string]$Text) { Write-Err $Text; exit 1 }

function Wait-Http {
    param(
        [string[]]$Urls,
        [int]$TimeoutSeconds,
        [string]$Marker
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        foreach ($url in $Urls) {
            try {
                $resp = Invoke-WebRequest -UseBasicParsing -Uri $url -TimeoutSec 2
                if ($resp.StatusCode -eq 200 -and $resp.Content -match $Marker) {
                    return $true
                }
            } catch {
                # сервис ещё стартует
            }
        }
        Start-Sleep -Milliseconds 600
    }
    return $false
}

Write-Host ""
Write-Head "=========================================="
Write-Head "            ЗАПУСК ИГРЫ ""РЕЙД"""
Write-Head "=========================================="
Write-Host ""

# --- Проверка окружения ---------------------------------------------------
$dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
if (-not $dotnet) {
    Fail "Не найден .NET SDK. Установите .NET 8 SDK: https://dotnet.microsoft.com/download"
}

$node = Get-Command node -ErrorAction SilentlyContinue
if (-not $node) {
    Fail "Не найден Node.js. Установите LTS-версию: https://nodejs.org/"
}

$dotnetVersion = (& dotnet --version) | Select-Object -First 1
$nodeVersion = (& node --version) | Select-Object -First 1
Write-Ok "dotnet $dotnetVersion"
Write-Ok "node $nodeVersion"

$apiDir = Join-Path $root 'GameApi'
if (-not (Test-Path $apiDir)) {
    Fail "Не найдена папка GameApi рядом со скриптом: $apiDir"
}
Write-Ok "папка сервера: $apiDir"

$hasModules = Test-Path (Join-Path $root 'node_modules')
if ($hasModules) {
    Write-Ok "зависимости клиента установлены"
} else {
    Write-Host "  [..] Зависимости клиента не установлены — будет выполнен npm install" -ForegroundColor DarkYellow
}

if ($CheckOnly) {
    Write-Host ""
    Write-Ok "Проверка завершена. Всё готово к запуску."
    exit 0
}

# --- Зависимости клиента ---------------------------------------------------
if (-not $hasModules) {
    Write-Host ""
    Write-Host "[0/2] Установка зависимостей клиента (npm install)..." -ForegroundColor Cyan
    & npm.cmd install
    if ($LASTEXITCODE -ne 0) {
        Fail "Не удалось установить зависимости клиента."
    }
}

# --- Сервер ----------------------------------------------------------------
Write-Host ""
Write-Host "[1/2] Запуск сервера (C# ASP.NET Core)..." -ForegroundColor Cyan
Start-Process -FilePath 'cmd.exe' -ArgumentList '/k', 'dotnet run' -WorkingDirectory $apiDir | Out-Null

Write-Host "      Ожидание http://localhost:5000/api/health ..."
$ready = Wait-Http -Urls @('http://localhost:5000/api/health', 'http://127.0.0.1:5000/api/health') -TimeoutSeconds 90 -Marker 'ok'

if (-not $ready) {
    Fail "Сервер не ответил за 90 секунд. Посмотрите окно ""Game API Server"" — там должна быть причина."
}
Write-Ok "сервер готов"

# --- Клиент ----------------------------------------------------------------
Write-Host ""
Write-Host "[2/2] Запуск клиента (React + Vite)..." -ForegroundColor Cyan
Start-Process -FilePath 'cmd.exe' -ArgumentList '/k', 'npm run dev' -WorkingDirectory $root | Out-Null

Write-Host "      Ожидание http://localhost:5173 ..."
$ready = Wait-Http -Urls @('http://localhost:5173/', 'http://127.0.0.1:5173/') -TimeoutSeconds 90 -Marker 'id="root"'

if (-not $ready) {
    Write-Err "Клиент не ответил за 90 секунд. Посмотрите окно ""Game Client (React)"" (возможно, занят порт 5173)."
    exit 1
}
Write-Ok "клиент готов"

if (-not $NoBrowser) {
    Start-Process 'http://localhost:5173'
}

Write-Host ""
Write-Head "=========================================="
Write-Head "  Игра запускается в отдельных окнах."
Write-Head "  Не закрывайте эти окна во время игры."
Write-Head "=========================================="
Write-Host ""
exit 0
