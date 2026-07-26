# TIKR — Windows laptop launcher (API + Web). Run from copied TIKR-Deploy folder.
param(
    [int]$ApiPort = 5000,
    [int]$WebPort = 8080
)

$ErrorActionPreference = "Stop"
$DeployRoot = $PSScriptRoot

function Import-TikrSecrets {
    # Prefer plain text (no PowerShell syntax — avoids "unexpected token" when clerks edit files)
    $licenseTxt = Join-Path $DeployRoot "syncfusion-license.txt"
    if (Test-Path $licenseTxt) {
        $key = (Get-Content -Path $licenseTxt -TotalCount 1 -ErrorAction SilentlyContinue)
        if ($key) { $key = $key.Trim() }
        if ($key -and $key -notmatch "PASTE-YOUR-SYNCFUSION") {
            $env:SYNCFUSION_LICENSE_KEY = $key
            return
        }
    }

    $secrets = Join-Path $DeployRoot "tikr-secrets.ps1"
    if (Test-Path $secrets) {
        try {
            . $secrets
            return
        } catch {
            Write-Warning "tikr-secrets.ps1 has a syntax error — use syncfusion-license.txt instead (one line = key only)."
        }
    }

    if (-not $env:SYNCFUSION_LICENSE_KEY) {
        Write-Host ""
        Write-Host "WARNING: No Syncfusion license found." -ForegroundColor Yellow
        Write-Host "  Create syncfusion-license.txt in this folder with ONLY your license key on line 1." -ForegroundColor Yellow
        Write-Host "  (Do not run .ps1 secrets files — just Start-TIKR.bat)" -ForegroundColor Yellow
        Write-Host ""
    }
}

$dataRoot = Join-Path $DeployRoot "Data"
$documents = Join-Path $dataRoot "documents"
$dpKeys = Join-Path $dataRoot ".dpkeys"
foreach ($dir in @($dataRoot, $documents, $dpKeys)) {
    if (-not (Test-Path $dir)) { New-Item -ItemType Directory -Path $dir -Force | Out-Null }
}

Import-TikrSecrets

$env:ASPNETCORE_ENVIRONMENT = "Production"
$env:ConnectionStrings__Default = "Data Source=$(Join-Path $dataRoot 'tikr.db')"
$env:FILE_STORAGE_PATH = $documents
$env:TIKR_DATA_PROTECTION_PATH = $dpKeys
$env:TIKR_API_URL = "http://localhost:$ApiPort"
$env:OLLAMA_HOST = if ($env:OLLAMA_HOST) { $env:OLLAMA_HOST } else { "http://localhost:11434" }

$apiDir = Join-Path $DeployRoot "TIKR.Api"
$webDir = Join-Path $DeployRoot "TIKR.Web"
$apiExe = Join-Path $apiDir "TIKR.Api.exe"
$webExe = Join-Path $webDir "TIKR.Web.exe"

if (-not (Test-Path $apiExe)) { throw "Missing $apiExe — copy the whole TIKR-Deploy folder from IT (not just one subfolder)." }
if (-not (Test-Path $webExe)) { throw "Missing $webExe — copy the whole TIKR-Deploy folder from IT (not just one subfolder)." }

$ensureOllama = Join-Path $DeployRoot "Ensure-Ollama.ps1"
if (Test-Path $ensureOllama) {
    try {
        & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $ensureOllama
    } catch {
        Write-Warning "Ensure-Ollama.ps1 failed: $($_.Exception.Message). Continuing without AI."
    }
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host " TIKR — Clerk's Vault (Windows)" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Data: $dataRoot"
Write-Host "API:  http://localhost:$ApiPort"
Write-Host "Web:  http://localhost:$WebPort"
Write-Host ""

$apiArgs = "--urls", "http://localhost:$ApiPort"
$webArgs = "--urls", "http://localhost:$WebPort"

Start-Process -FilePath $apiExe -WorkingDirectory $apiDir -ArgumentList $apiArgs -WindowStyle Normal
Start-Sleep -Seconds 4
Start-Process -FilePath $webExe -WorkingDirectory $webDir -ArgumentList $webArgs -WindowStyle Normal
Start-Sleep -Seconds 2
Start-Process "http://localhost:$WebPort"

Write-Host "TIKR is starting. Keep both console windows open." -ForegroundColor Green
Write-Host "First visit: use Settings -> Show me around TIKR for the guided tour." -ForegroundColor Green
