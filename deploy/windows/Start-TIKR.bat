@echo off
title TIKR - Clerk's Vault
cd /d "%~dp0"
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Start-TIKR.ps1"
pause