# TIKR for Deb & Paige — Windows install (one pager)

**Canonical day-1 deploy:** put **`Setup-TIKR.exe`** on a thumb drive → on the Dell, double-click Setup → finish.
Ollama (local AI) is part of that same Setup (not a separate project). Auth stays off for Deb and Paige on this trusted PC.

Synology NAS Docker is **Phase 2** — [deb-nas-install.md](deb-nas-install.md).

IT build: [installer/README.md](../installer/README.md) · Tour: [demo-deb.md](demo-deb.md)

---

## What you put on the thumb drive

| Item                       | Required?                 | Notes                                                  |
| -------------------------- | ------------------------- | ------------------------------------------------------ |
| `Setup-TIKR.exe`           | **Yes**                   | Built by IT with Inno Setup from `publish/TIKR-Deploy` |
| Syncfusion license key     | **Yes** (paste in wizard) | Not a file on the stick if IT prefers typing it        |
| Whole `TIKR-Deploy` folder | No                        | Only for USB/folder mode without Setup.exe             |

You do **not** need Deb to copy loose `TIKR.Api` / `TIKR.Web` folders when using Setup.exe.

---

## Five steps (first day)

1. **Thumb drive → Dell** — Copy `Setup-TIKR.exe` (and run it from the stick or Desktop).
2. **Run Setup** — Accept UAC; keep `C:\Program Files\TIKR`; leave defaults checked (**Ollama**, **Desktop shortcut**, **Start TIKR when I sign in**).
3. **License** — Paste Syncfusion Community key (clears trial banners).
4. **Wait for Ollama** — First install may download Ollama + models (`llama3.2:3b`, `nomic-embed-text`) for several minutes (needs internet unless IT bundled `redist\OllamaSetup.exe`).
5. **Start + tour + backup** — **Start TIKR** → `http://localhost:8080` → Settings → **Show me around TIKR**. Backup owner copies `C:\ProgramData\TIKR` regularly.

---

## Every day

| Action                     | How                                                                                                     |
| -------------------------- | ------------------------------------------------------------------------------------------------------- |
| Start                      | Automatic after Windows sign-in, or Desktop **Start TIKR** (also ensures Ollama is up)                  |
| Use                        | `http://localhost:8080`                                                                                 |
| **Requirements + AI Scan** | **Requirements** → upload a `.txt` report → **AI Scan uploaded doc** → review pre-filled form → save    |
| **Documents**              | **Documents** → select a file → **Download** (stored on this PC under `C:\ProgramData\TIKR\documents\`) |
| Stop                       | Start Menu → **Stop TIKR**                                                                              |

---

## Where data lives

```text
C:\ProgramData\TIKR\
  tikr.db        — database
  documents\     — uploads
  .dpkeys\       — local app keys
```

Ollama models live under the Ollama user folder (separate from ProgramData). Uninstall keeps ProgramData by default.

---

## Auth note (Deb + Paige)

Auth stays **off** for this shared trusted PC. Enable later only if the app leaves that machine (README → Optional multi-user auth).

---

## Alternate: USB folder without Setup.exe

Copy `TIKR-Deploy` → run `Install-TIKR.ps1` (Admin) → `Start-TIKR.bat`. Same `Ensure-Ollama.ps1` runs automatically. Prefer Setup.exe for Start Menu / Program Files.

---

## After install

IT smoke: [clerk-windows-smoke.md](clerk-windows-smoke.md) · Handoff: [clerk-windows-handoff.md](clerk-windows-handoff.md).
