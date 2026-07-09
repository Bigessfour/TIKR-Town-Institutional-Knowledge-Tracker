TIKR — Clerk's Vault (Windows thumb-drive / laptop test)
========================================================

WHAT'S ON THIS USB
  TIKR-Deploy\     Copy this entire folder to the PC (e.g. Desktop).
  Two programs:    TIKR.Api (data + AI) and TIKR.Web (browser UI).
  Your data lives in TIKR-Deploy\Data\ (database + documents).

BEFORE FIRST RUN (once)
  1. Copy TIKR-Deploy to the Dell (Desktop is fine).
  2. Right-click Install-TIKR.ps1 -> Run with PowerShell (Run as Administrator).
     - Opens firewall for port 8080
     - Creates tikr-secrets.ps1 from the example
  3. Open tikr-secrets.ps1 in Notepad and paste your Syncfusion license key.

OPTIONAL — LOCAL AI (recommended)
  Install Ollama from https://ollama.com
  In PowerShell:
    ollama pull llama3.2:3b
    ollama pull nomic-embed-text
  Leave Ollama running while you use TIKR.

EVERY DAY
  1. Double-click Start-TIKR.bat
  2. Browser opens http://localhost:8080
  3. First time? Settings -> "Show me around TIKR" (guided tour v2)
  4. To stop: close the two black console windows, or run Stop-TIKR.ps1

URLS
  Web UI:  http://localhost:8080
  API:     http://localhost:5000/health

NAS PRODUCTION (Synology)
  Use Docker, not this folder. See docs\deb-nas-install.md in the full repo,
  or copy the docker\ folder from the project and use Container Manager.

BUILD THIS PACKAGE (on your Mac, before copying to USB)
  ./scripts/package-thumb-drive.sh
  Then copy publish/TIKR-Deploy to the thumb drive.