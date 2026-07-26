TIKR — Clerk's Vault
====================
Town Institutional Knowledge Tracker for one-person municipal clerks.

HOW TO START (every day)
  1. Double-click "Start TIKR" on your Desktop
     (or Start Menu → TIKR - Clerk's Vault → Start TIKR)
  2. Your browser opens http://localhost:8080
  3. First time? Settings → "Show me around TIKR"

HOW TO STOP
  Start Menu → TIKR - Clerk's Vault → Stop TIKR
  (or close TIKR from Task Manager if needed)

URLS
  Web UI:  http://localhost:8080
  API:     http://localhost:5000/health

WHERE YOUR DATA LIVES
  C:\ProgramData\TIKR\
    tikr.db          — knowledge, requirements, documents index
    documents\       — uploaded files
    .dpkeys\         — local encryption keys for the app

  Back up the entire C:\ProgramData\TIKR folder regularly
  (copy to an external drive or town shared folder).

LICENSE (SYNCFUSION UI)
  The installer sets SYNCFUSION_LICENSE_KEY as a machine environment
  variable. If grids or the assistant show a license banner, ask IT
  to re-run the installer or set that variable, then restart the PC.

OPTIONAL — LOCAL AI
  Install Ollama from https://ollama.com
  In PowerShell:
    ollama pull llama3.2:3b
    ollama pull nomic-embed-text
  Leave Ollama running while you use TIKR Assistant features.

HELP
  In-app: Settings and guided tour
  Town docs: see the Help shortcut in Start Menu
  Maintainer: docs\windows-thumb-drive-deploy.md and docs\deb-nas-install.md
  in the project repository

PRODUCTION NOTE
  Long-term production for the clerk is Docker on the town Synology NAS.
  This Windows install is for laptop / interim use on a municipal Dell.

UNINSTALL
  Windows Settings → Apps → TIKR — Clerk's Vault → Uninstall
  Your data under C:\ProgramData\TIKR is kept by default so nothing is lost.
