@echo off
REM TIKR one-time USB setup — prefer this over Install-TIKR.ps1 on FAT/exFAT sticks (Mac copy-safe).
title TIKR Install
cd /d "%~dp0"

echo TIKR installer — USB / folder mode
echo.

if not exist "syncfusion-license.txt" (
  if exist "syncfusion-license.txt.example" (
    copy /Y "syncfusion-license.txt.example" "syncfusion-license.txt" >nul
    echo Created syncfusion-license.txt
    echo Open it in Notepad and replace the placeholder with your Syncfusion key (ONE LINE ONLY).
  ) else (
    echo WARNING: syncfusion-license.txt.example missing.
  )
) else (
  echo syncfusion-license.txt already exists.
)

netsh advfirewall firewall show rule name="TIKR Web (8080)" >nul 2>&1
if errorlevel 1 (
  echo Adding firewall rule for port 8080 (may need Run as administrator)...
  netsh advfirewall firewall add rule name="TIKR Web (8080)" dir=in action=allow protocol=TCP localport=8080
)

if exist "Ensure-Ollama.ps1" (
  echo Preparing Ollama...
  powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Ensure-Ollama.ps1"
) else (
  echo Ensure-Ollama.ps1 not found — you can still Start TIKR; install Ollama later if needed.
)

echo.
echo Next:
echo   1. Edit syncfusion-license.txt in Notepad — one line = your Syncfusion key
echo   2. Double-click Start-TIKR.bat
echo   Do NOT double-click tikr-secrets.ps1
echo.
pause
