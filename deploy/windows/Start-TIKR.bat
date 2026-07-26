@echo off
title TIKR - Clerk's Vault
cd /d "%~dp0"

REM Prefer plain-text license (no PowerShell editing)
if exist "syncfusion-license.txt" (
  set /p SYNCFUSION_LICENSE_KEY=<syncfusion-license.txt
)

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Start-TIKR.ps1"
pause
