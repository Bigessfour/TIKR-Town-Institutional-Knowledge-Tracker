TIKR on the Dell — EASY MODE (3 steps)
======================================

No WSL. No .sh scripts. Deb only uses Windows files:
  Install-TIKR.cmd   Start-TIKR.bat   Stop-TIKR.ps1   syncfusion-license.txt

Forget "client" and "server" folders. On this PC you install ONE package.
Two programs start together; your browser is the only "client."

  TIKR.Api  = brain (database, documents, AI)   → http://localhost:5000
  TIKR.Web  = screens (what Deb uses)           → http://localhost:8080
  Data\     = YOUR FILES (database + uploads)   → stays next to the apps

------------------------------------------------
FIRST TIME (once)
------------------------------------------------
1. Copy the whole TIKR-Deploy folder to the Desktop (or C:\TIKR).
2. Right-click Install-TIKR.cmd → Run as administrator
     (creates license file + firewall rule + tries to set up Ollama)
3. Open syncfusion-license.txt in Notepad.
     Replace the placeholder with your Syncfusion key (ONE LINE ONLY).
     Save and close.

------------------------------------------------
EVERY DAY
------------------------------------------------
1. Double-click Start-TIKR.bat
2. Browser opens http://localhost:8080
3. Keep the two black windows open while you work.
4. To stop: double-click Stop-TIKR.ps1 (or close those windows)

------------------------------------------------
IF SOMETHING FAILS
------------------------------------------------
• "Missing TIKR.Api.exe" → you copied the wrong folder; need whole TIKR-Deploy.
• Black window flashes and closes → run Start-TIKR.bat again; read the error.
• Trial banners on grids → syncfusion-license.txt is empty or wrong.
• AI / Assistant offline → install Ollama from https://ollama.com (or re-run Install-TIKR.cmd).
• Prefer a normal Windows installer later → Setup-TIKR.exe (IT builds that; apps go to
  Program Files, data goes to C:\ProgramData\TIKR). Same two programs, cleaner paths.

------------------------------------------------
NAS (Synology) IS DIFFERENT — Phase 2
------------------------------------------------
Do NOT use this USB folder on the NAS. NAS uses Docker (deb-nas-install.md).
