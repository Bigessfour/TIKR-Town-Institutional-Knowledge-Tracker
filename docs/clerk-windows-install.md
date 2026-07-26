# TIKR for Deb & Paige — Windows install (one pager)

**Today (easy mode):** copy the **`TIKR-Deploy`** folder → run **`Install-TIKR.cmd`** as Admin → edit license → **`Start-TIKR.bat`**.

**Later (nicer):** IT builds **`Setup-TIKR.exe`** (Inno Setup) → double-click Setup → Program Files + Start Menu. Same two programs; cleaner paths.

Synology NAS Docker is **Phase 2** — [deb-nas-install.md](deb-nas-install.md).

IT build notes: [installer/README.md](../installer/README.md) · Tour: [demo-deb.md](demo-deb.md)

---

## Client vs server? (short answer)

**Not two installs.** On the Dell, one folder runs **both**:

| Piece      | What it is              | URL / path              |
| ---------- | ----------------------- | ----------------------- |
| `TIKR.Web` | Screens in the browser  | `http://localhost:8080` |
| `TIKR.Api` | Database, documents, AI | `http://localhost:5000` |
| Browser    | The only “client”       | Chrome / Edge           |

NAS “server” is a different Phase 2 story (Docker). Do not put this USB folder on the Synology.

---

## Paths: folder mode vs Setup.exe

| Mode                       | Programs                 | Your data (backup this) |
| -------------------------- | ------------------------ | ----------------------- |
| **USB / folder (now)**     | Inside `TIKR-Deploy\`    | `TIKR-Deploy\Data\`     |
| **Setup-TIKR.exe (later)** | `C:\Program Files\TIKR\` | `C:\ProgramData\TIKR\`  |

---

## What goes on the thumb drive (today)

| Item                           | Required?      | Notes                                                             |
| ------------------------------ | -------------- | ----------------------------------------------------------------- |
| Whole **`TIKR-Deploy`** folder | **Yes**        | IT builds on a Mac (`.sh`). Deb never runs those — no WSL needed. |
| Syncfusion license key         | **Yes**        | Paste into `syncfusion-license.txt` (one line)                    |
| `Setup-TIKR.exe`               | Optional later | When IT compiles Inno installer                                   |

Deb only double-clicks: `Install-TIKR.cmd`, `Start-TIKR.bat`, `Stop-TIKR.ps1`.

---

## Three steps (first day — folder mode)

1. Copy **`TIKR-Deploy`** to the Desktop (or `C:\TIKR`).
2. Right-click **`Install-TIKR.cmd` → Run as administrator** (firewall + license file + Ollama helper).
3. Open **`syncfusion-license.txt`** in Notepad → paste key → save → double-click **`Start-TIKR.bat`**.

Browser should open `http://localhost:8080`. Keep the two black windows open while working.

One-pager inside the folder: `README-QuickStart.txt`.

---

## Every day

| Action | How                                                |
| ------ | -------------------------------------------------- |
| Start  | Double-click **Start-TIKR.bat**                    |
| Use    | `http://localhost:8080`                            |
| Stop   | **Stop-TIKR.ps1** or close the two console windows |

---

## Where data lives (folder mode)

```text
TIKR-Deploy\Data\
  tikr.db        — database
  documents\     — uploads
  .dpkeys\       — local app keys
```

Backup = copy the whole `Data` folder (or whole `TIKR-Deploy` if you want the apps too).

---

## Auth note (Deb + Paige)

Auth stays **off** for this shared trusted PC. Enable later only if the app leaves that machine.

---

## When Setup-TIKR.exe exists

1. Run Setup → defaults (`C:\Program Files\TIKR`, Ollama, Desktop shortcut).
2. Paste Syncfusion key in the wizard (or use Start Menu → Start TIKR after install).
3. Backup owner copies **`C:\ProgramData\TIKR`** regularly.

Smoke: [clerk-windows-smoke.md](clerk-windows-smoke.md) · Handoff: [clerk-windows-handoff.md](clerk-windows-handoff.md).
