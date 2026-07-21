# TIKR — ensure Ollama is installed, running, and required models are present.
# Called from Setup-TIKR.exe ([Run]) and from Start-TIKR-Installed.ps1.
# Non-fatal by default: writes warnings and exits 0 so TIKR can still start without AI.
param(
    [switch]$SkipInstall,
    [switch]$SkipPull,
    [switch]$FailOnError,
    [string]$ChatModel = "llama3.2:3b",
    [string]$EmbedModel = "nomic-embed-text",
    [string]$OllamaSetupUrl = "https://ollama.com/download/OllamaSetup.exe",
    [int]$ReadyTimeoutSec = 120
)

$ErrorActionPreference = "Continue"

function Write-TikrInfo([string]$Message) { Write-Host "[TIKR/Ollama] $Message" }
function Write-TikrWarn([string]$Message) { Write-Warning "[TIKR/Ollama] $Message" }

function Get-OllamaExe {
    $cmd = Get-Command ollama -ErrorAction SilentlyContinue
    if ($cmd -and $cmd.Source) { return $cmd.Source }

    $candidates = @(
        (Join-Path $env:LOCALAPPDATA "Programs\Ollama\ollama.exe"),
        (Join-Path $env:LOCALAPPDATA "Ollama\ollama.exe"),
        (Join-Path ${env:ProgramFiles} "Ollama\ollama.exe"),
        (Join-Path ${env:ProgramFiles(x86)} "Ollama\ollama.exe")
    )
    foreach ($path in $candidates) {
        if ($path -and (Test-Path $path)) { return $path }
    }
    return $null
}

function Refresh-Path {
    $machine = [Environment]::GetEnvironmentVariable("Path", "Machine")
    $user = [Environment]::GetEnvironmentVariable("Path", "User")
    $env:Path = @($machine, $user) -join ";"
}

function Test-OllamaApi {
    try {
        $resp = Invoke-WebRequest -Uri "http://localhost:11434/" -UseBasicParsing -TimeoutSec 3
        return $resp.StatusCode -ge 200
    } catch {
        return $false
    }
}

function Wait-OllamaReady([int]$TimeoutSec) {
    $deadline = (Get-Date).AddSeconds($TimeoutSec)
    while ((Get-Date) -lt $deadline) {
        if (Test-OllamaApi) { return $true }
        Start-Sleep -Seconds 2
    }
    return (Test-OllamaApi)
}

function Install-OllamaApp {
    $scriptRoot = $PSScriptRoot
    $installRoot = Split-Path -Parent $scriptRoot
    $bundled = @(
        (Join-Path $scriptRoot "redist\OllamaSetup.exe"),
        (Join-Path $installRoot "redist\OllamaSetup.exe"),
        (Join-Path $scriptRoot "OllamaSetup.exe")
    ) | Where-Object { $_ -and (Test-Path $_) } | Select-Object -First 1

    $setupPath = $bundled
    if (-not $setupPath) {
        Write-TikrInfo "Ollama not found — downloading installer (needs internet once)..."
        $setupPath = Join-Path $env:TEMP "TIKR-OllamaSetup.exe"
        try {
            Invoke-WebRequest -Uri $OllamaSetupUrl -OutFile $setupPath -UseBasicParsing
        } catch {
            Write-TikrWarn "Could not download OllamaSetup.exe: $($_.Exception.Message)"
            return $false
        }
    } else {
        Write-TikrInfo "Using bundled Ollama installer: $setupPath"
    }

    Write-TikrInfo "Installing Ollama (silent)..."
    $argsList = @("/VERYSILENT", "/NORESTART", "/SUPPRESSMSGBOXES")
    $p = Start-Process -FilePath $setupPath -ArgumentList $argsList -Wait -PassThru
    if ($p.ExitCode -ne 0 -and $p.ExitCode -ne $null) {
        Write-TikrInfo "Retrying Ollama setup with /S..."
        $p = Start-Process -FilePath $setupPath -ArgumentList @("/S") -Wait -PassThru
    }

    Refresh-Path
    Start-Sleep -Seconds 3
    return $null -ne (Get-OllamaExe)
}

function Start-OllamaIfNeeded([string]$OllamaExe) {
    if (Test-OllamaApi) {
        Write-TikrInfo "Ollama API already responding on :11434"
        return $true
    }

    Write-TikrInfo "Starting Ollama..."
    # App install usually auto-starts tray app; kick serve as fallback
    Start-Process -FilePath $OllamaExe -ArgumentList @("serve") -WindowStyle Hidden | Out-Null
    if (-not (Wait-OllamaReady -TimeoutSec $ReadyTimeoutSec)) {
        Write-TikrWarn "Ollama did not become ready on http://localhost:11434 within ${ReadyTimeoutSec}s"
        return $false
    }
    return $true
}

function Ensure-Model([string]$OllamaExe, [string]$Model) {
    Write-TikrInfo "Ensuring model '$Model'..."
    $listOut = & $OllamaExe list 2>$null | Out-String
    if ($listOut -and ($listOut -match [regex]::Escape($Model))) {
        Write-TikrInfo "Model already present: $Model"
        return $true
    }

    & $OllamaExe pull $Model
    if ($LASTEXITCODE -ne 0) {
        Write-TikrWarn "ollama pull $Model failed (exit $LASTEXITCODE)"
        return $false
    }
    return $true
}

# --- main --------------------------------------------------------------------
$failed = $false

Refresh-Path
$ollamaExe = Get-OllamaExe

if (-not $ollamaExe -and -not $SkipInstall) {
    if (-not (Install-OllamaApp)) {
        Write-TikrWarn "Ollama install failed or was skipped. Assistant AI will be unavailable until Ollama is installed."
        $failed = $true
    } else {
        Refresh-Path
        $ollamaExe = Get-OllamaExe
    }
}

if (-not $ollamaExe) {
    Write-TikrWarn "ollama.exe not found on PATH or in common install folders."
    $failed = $true
} else {
    Write-TikrInfo "Using $ollamaExe"
    if (-not (Start-OllamaIfNeeded -OllamaExe $ollamaExe)) {
        $failed = $true
    } elseif (-not $SkipPull) {
        $okChat = Ensure-Model -OllamaExe $ollamaExe -Model $ChatModel
        $okEmbed = Ensure-Model -OllamaExe $ollamaExe -Model $EmbedModel
        if (-not ($okChat -and $okEmbed)) { $failed = $true }
        else { Write-TikrInfo "Ollama ready (chat=$ChatModel, embed=$EmbedModel)" }
    }
}

if ($failed -and $FailOnError) {
    exit 1
}
exit 0
