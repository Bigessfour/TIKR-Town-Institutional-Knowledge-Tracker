# Helper: prepare NAS Docker deploy from a Windows machine (after DS225+ is on the network).
# Does not auto-install on Synology — points you at Container Manager + compose.
param(
    [string]$NasShare = ""
)

Write-Host "TIKR — NAS deployment helper" -ForegroundColor Cyan
Write-Host ""
Write-Host "Production uses Docker on Synology (not the Windows .exe folder)." -ForegroundColor Yellow
Write-Host ""
Write-Host "On the NAS you need:"
Write-Host "  - docker/docker-compose.prod.yml"
Write-Host "  - docker/.env (from docker/.env.example + SYNCFUSION_LICENSE_KEY)"
Write-Host "  - validate-prod.sh"
Write-Host ""
Write-Host "Steps:"
Write-Host "  1. Copy the TIKR repo (or release zip) to the NAS."
Write-Host "  2. Container Manager -> Project -> Create -> compose file: docker/docker-compose.prod.yml"
Write-Host "  3. Set TIKR_DATA_PATH and pull Ollama models (see deb-nas-install.md)."
Write-Host ""

if ($NasShare -and (Test-Path $NasShare)) {
    Write-Host "NAS share detected: $NasShare" -ForegroundColor Green
    Write-Host "Copy the repo docker\ folder and .env there, then run compose on the NAS via SSH or Container Manager."
} else {
    Write-Host "Tip: re-run with -NasShare '\\NAS\share\path' when the NAS drive is mapped."
}