# TIKR — installed launcher (Program Files binaries + ProgramData data).
# Starts TIKR.Api then TIKR.Web; opens the clerk UI in the default browser.
param(
    [int]$ApiPort = 5000,
    [int]$WebPort = 8080,
    [ValidateSet("Normal", "Hidden", "Minimized")]
    [string]$WindowStyle = "Hidden"
)

$ErrorActionPreference = "Stop"

# Install root = parent of scripts\ when shipped as {app}\scripts\Start-TIKR-Installed.ps1
$InstallRoot = Split-Path -Parent $PSScriptRoot
if (-not (Test-Path (Join-Path $InstallRoot "TIKR.Web\TIKR.Web.exe"))) {
    # Fallback: script lives directly in {app}
    $InstallRoot = $PSScriptRoot
}

$DataRoot = Join-Path $env:ProgramData "TIKR"
$documents = Join-Path $DataRoot "documents"
$dpKeys = Join-Path $DataRoot ".dpkeys"
foreach ($dir in @($DataRoot, $documents, $dpKeys)) {
    if (-not (Test-Path $dir)) {
        New-Item -ItemType Directory -Path $dir -Force | Out-Null
    }
}

# Prefer machine / user environment (set by installer). Optional local override file.
$localSecrets = Join-Path $InstallRoot "tikr-secrets.ps1"
if (Test-Path $localSecrets) {
    . $localSecrets
}

# Fresh processes often do not inherit a machine env var set in the same session
# (installer "Start TIKR now"). Always re-read Machine + User levels.
if (-not $env:SYNCFUSION_LICENSE_KEY) {
    $env:SYNCFUSION_LICENSE_KEY = [Environment]::GetEnvironmentVariable("SYNCFUSION_LICENSE_KEY", "Machine")
}
if (-not $env:SYNCFUSION_LICENSE_KEY) {
    $env:SYNCFUSION_LICENSE_KEY = [Environment]::GetEnvironmentVariable("SYNCFUSION_LICENSE_KEY", "User")
}

if (-not $env:SYNCFUSION_LICENSE_KEY) {
    Write-Warning "SYNCFUSION_LICENSE_KEY is not set. Syncfusion UI may show a license banner."
    Write-Warning "Re-run the installer or set a machine environment variable, then restart."
}

$env:ASPNETCORE_ENVIRONMENT = "Production"
$env:ConnectionStrings__Default = "Data Source=$(Join-Path $DataRoot 'tikr.db')"
$env:FILE_STORAGE_PATH = $documents
$env:TIKR_DATA_PROTECTION_PATH = $dpKeys
$env:TIKR_API_URL = "http://localhost:$ApiPort"
$env:OLLAMA_HOST = if ($env:OLLAMA_HOST) { $env:OLLAMA_HOST } else { "http://localhost:11434" }

$apiDir = Join-Path $InstallRoot "TIKR.Api"
$webDir = Join-Path $InstallRoot "TIKR.Web"
$apiExe = Join-Path $apiDir "TIKR.Api.exe"
$webExe = Join-Path $webDir "TIKR.Web.exe"

if (-not (Test-Path $apiExe)) { throw "Missing $apiExe — reinstall TIKR." }
if (-not (Test-Path $webExe)) { throw "Missing $webExe — reinstall TIKR." }

# Avoid duplicate stacks if clerk double-clicks Start
$existingApi = Get-Process -Name "TIKR.Api" -ErrorAction SilentlyContinue
$existingWeb = Get-Process -Name "TIKR.Web" -ErrorAction SilentlyContinue
if ($existingApi -and $existingWeb) {
    Start-Process "http://localhost:$WebPort"
    exit 0
}

$ws = [System.Diagnostics.ProcessWindowStyle]::$WindowStyle
$apiArgs = @("--urls", "http://localhost:$ApiPort")
$webArgs = @("--urls", "http://localhost:$WebPort")

Start-Process -FilePath $apiExe -WorkingDirectory $apiDir -ArgumentList $apiArgs -WindowStyle $ws
Start-Sleep -Seconds 4
Start-Process -FilePath $webExe -WorkingDirectory $webDir -ArgumentList $webArgs -WindowStyle $ws
Start-Sleep -Seconds 2
Start-Process "http://localhost:$WebPort"
