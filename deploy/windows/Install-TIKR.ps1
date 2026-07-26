# One-time setup on the Dell laptop (run as Administrator for firewall rule).
# Prefer Install-TIKR.cmd (double-click friendly). Prefer Setup-TIKR.exe when IT builds it.
#Requires -RunAsAdministrator
param(
    [int]$WebPort = 8080,
    [switch]$SkipOllama
)

$ErrorActionPreference = "Stop"
Write-Host "TIKR installer — USB / folder mode" -ForegroundColor Cyan

$ruleName = "TIKR Web ($WebPort)"
$existing = Get-NetFirewallRule -DisplayName $ruleName -ErrorAction SilentlyContinue
if (-not $existing) {
    New-NetFirewallRule -DisplayName $ruleName -Direction Inbound -Action Allow -Protocol TCP -LocalPort $WebPort | Out-Null
    Write-Host "Firewall: allowed inbound TCP $WebPort" -ForegroundColor Green
} else {
    Write-Host "Firewall: rule already present" -ForegroundColor DarkGray
}

$license = Join-Path $PSScriptRoot "syncfusion-license.txt"
$licenseExample = Join-Path $PSScriptRoot "syncfusion-license.txt.example"
if (-not (Test-Path $license)) {
    if (Test-Path $licenseExample) {
        Copy-Item $licenseExample $license
        Write-Host "Created syncfusion-license.txt — open in Notepad, paste Syncfusion key (ONE LINE), save." -ForegroundColor Yellow
    } else {
        Write-Warning "syncfusion-license.txt.example missing."
    }
} else {
    Write-Host "syncfusion-license.txt already exists." -ForegroundColor DarkGray
}

if (-not $SkipOllama) {
    $ensure = Join-Path $PSScriptRoot "Ensure-Ollama.ps1"
    if (Test-Path $ensure) {
        Write-Host "Preparing Ollama (install + models)..." -ForegroundColor Cyan
        & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $ensure
    }
}

Write-Host ""
Write-Host "Next:" -ForegroundColor Green
Write-Host "  1. Edit syncfusion-license.txt in Notepad (one line = key)"
Write-Host "  2. Double-click Start-TIKR.bat"
Write-Host "Preferred long-term: Setup-TIKR.exe (Program Files + Start Menu)." -ForegroundColor DarkGray
