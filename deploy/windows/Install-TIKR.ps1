# One-time setup on the Dell laptop (run as Administrator for firewall rule).
#Requires -RunAsAdministrator
param(
    [int]$WebPort = 8080
)

$ErrorActionPreference = "Stop"
Write-Host "TIKR installer — laptop mode" -ForegroundColor Cyan

$ruleName = "TIKR Web ($WebPort)"
$existing = Get-NetFirewallRule -DisplayName $ruleName -ErrorAction SilentlyContinue
if (-not $existing) {
    New-NetFirewallRule -DisplayName $ruleName -Direction Inbound -Action Allow -Protocol TCP -LocalPort $WebPort | Out-Null
    Write-Host "Firewall: allowed inbound TCP $WebPort" -ForegroundColor Green
} else {
    Write-Host "Firewall: rule already present" -ForegroundColor DarkGray
}

$example = Join-Path $PSScriptRoot "tikr-secrets.ps1.example"
$secrets = Join-Path $PSScriptRoot "tikr-secrets.ps1"
if (-not (Test-Path $secrets)) {
    Copy-Item $example $secrets
    Write-Host "Created tikr-secrets.ps1 — edit and paste your Syncfusion license key." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Next: double-click Start-TIKR.bat (or run Start-TIKR.ps1)." -ForegroundColor Green
Write-Host "Install Ollama for local AI: https://ollama.com — then: ollama pull llama3.2:3b && ollama pull nomic-embed-text" -ForegroundColor Green