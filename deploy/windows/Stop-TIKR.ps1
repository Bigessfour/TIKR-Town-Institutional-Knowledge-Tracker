# Stops TIKR.Api and TIKR.Web started from this deploy folder.
Get-Process -Name "TIKR.Api", "TIKR.Web" -ErrorAction SilentlyContinue | Stop-Process -Force
Write-Host "Stopped TIKR processes (if any were running)." -ForegroundColor Green