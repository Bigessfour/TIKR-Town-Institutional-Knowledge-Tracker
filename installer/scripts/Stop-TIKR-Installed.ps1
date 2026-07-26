# Stops TIKR.Api and TIKR.Web (installed Windows stack).
Get-Process -Name "TIKR.Api", "TIKR.Web" -ErrorAction SilentlyContinue | Stop-Process -Force
Write-Host "Stopped TIKR processes (if any were running)."
