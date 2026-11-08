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

function Test-HttpMarker {
    param(
        [string]$Url,
        [string]$Marker
    )

    try {
        $resp = Invoke-WebRequest -UseBasicParsing -Uri $Url -TimeoutSec 2
        return ($resp.StatusCode -eq 200 -and $resp.Content -match $Marker)
    } catch {
        return $false
    }
}

function Test-PortListening {
    param([int]$Port)

    # Get-NetTCPConnection видит и IPv4, и IPv6 (Vite часто слушает ::1).
    try {
        $conn = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue
        if ($conn) { return $true }
    } catch {
        # fallback ниже
    }

    $client = $null
    try {
        $client = New-Object System.Net.Sockets.TcpClient
        $iar = $client.BeginConnect('127.0.0.1', $Port, $null, $null)
        if (-not $iar.AsyncWaitHandle.WaitOne(300)) { return $false }
        $client.EndConnect($iar)
        return $true
    } catch {
        return $false
    } finally {
        if ($client) { $client.Close() }
    }
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
Write-Host "[1/2] Сервер (C# ASP.NET Core)..." -ForegroundColor Cyan

$apiReady = Test-HttpMarker -Url 'http://localhost:5000/api/health' -Marker 'ok'
if ($apiReady) {
    Write-Ok "сервер уже запущен — переиспользую http://localhost:5000"
} else {
    if (Test-PortListening -Port 5000) {
        Write-Host "      Порт 5000 занят, но /api/health не отвечает. Ждём немного..." -ForegroundColor DarkYellow
    } else {
        Start-Process -FilePath 'cmd.exe' -ArgumentList '/k', 'dotnet run' -WorkingDirectory $apiDir | Out-Null
    }

    Write-Host "      Ожидание http://localhost:5000/api/health ..."
    $ready = Wait-Http -Urls @('http://localhost:5000/api/health', 'http://127.0.0.1:5000/api/health') -TimeoutSeconds 90 -Marker 'ok'

    if (-not $ready) {
        Fail "Сервер не ответил за 90 секунд. Посмотрите окно ""Game API Server"" — там должна быть причина."
    }
    Write-Ok "сервер готов"
}

# --- Клиент ----------------------------------------------------------------
Write-Host ""
Write-Host "[2/2] Клиент (React + Vite)..." -ForegroundColor Cyan

$clientPort = $null
$clientUrl = $null

# Если Vite уже запущен и отдаёт нашу игру — второй раз не запускаем.
foreach ($port in 5173..5180) {
    if (Test-HttpMarker -Url "http://localhost:$port/" -Marker 'id="root"') {
        $clientPort = $port
        $clientUrl = "http://localhost:$port"
        break
    }
}

if ($clientPort) {
    Write-Ok "клиент уже запущен — переиспользую $clientUrl"
} else {
    # Ищем свободный порт: 5173 мог остаться занятым старым процессом.
    foreach ($port in 5173..5180) {
        if (-not (Test-PortListening -Port $port)) {
            $clientPort = $port
            break
        }
    }

    if (-not $clientPort) {
        Fail "Не нашёл свободный порт в диапазоне 5173–5180. Закройте лишние окна Node/Vite и попробуйте снова."
    }

    $clientUrl = "http://localhost:$clientPort"
    Start-Process -FilePath 'cmd.exe' -ArgumentList '/k', "npm.cmd run dev -- --port $clientPort --strictPort" -WorkingDirectory $root | Out-Null

    Write-Host "      Ожидание $clientUrl ..."
    $ready = Wait-Http -Urls @("$clientUrl/", "http://127.0.0.1:$clientPort/") -TimeoutSeconds 90 -Marker 'id="root"'

    if (-not $ready) {
        Fail "Клиент не ответил за 90 секунд. Посмотрите окно ""Game Client (React)"" — там должна быть причина."
    }
    Write-Ok "клиент готов на $clientUrl"
}

if (-not $NoBrowser) {
    Start-Process $clientUrl
}

Write-Host ""
Write-Head "=========================================="
Write-Head "  Игра запускается в отдельных окнах."
Write-Head "  Не закрывайте эти окна во время игры."
Write-Head "=========================================="
Write-Host ""
exit 0
