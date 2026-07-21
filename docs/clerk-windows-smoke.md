# Clerk Windows smoke checklist (Setup-TIKR.exe)

Run after installing **Setup-TIKR.exe** on the municipal Dell (or a stand-in VM).  
Day-1 install steps: [clerk-windows-install.md](clerk-windows-install.md).

**Auth:** off (Deb + Paige on trusted shared PC).

---

## Pre-flight (IT)

- [ ] `publish/TIKR-Deploy` rebuilt from ship branch (`SKIP_TESTS=1 ./scripts/package-thumb-drive.sh` or full tests)
- [ ] `installer/Output/Setup-TIKR.exe` compiled with Inno 6 (`./scripts/build-windows-setup-notes.sh`)
- [ ] Syncfusion Community license key ready for the wizard

---

## Install smoke

- [ ] Run `Setup-TIKR.exe` as admin → default `C:\Program Files\TIKR`
- [ ] Paste Syncfusion license on wizard page
- [ ] Leave firewall task checked
- [ ] Leave **Install & prepare Ollama** checked
- [ ] Finish → **Start TIKR now** (or Desktop **Start TIKR**)

---

## Runtime smoke

| Check | Pass? |
|-------|-------|
| Browser opens `http://localhost:8080` | |
| `http://localhost:5000/health` returns healthy | |
| No Syncfusion trial banner on main grids | |
| `http://localhost:11434` responds (Ollama) | |
| Settings shows Ollama **Connected** (or equivalent) | |
| Assistant Send returns a reply (or models still pulling noted) | |
| Settings → **Show me around TIKR** starts | |
| Create one requirement and save | |
| Upload one document; download it back | |
| Data present under `C:\ProgramData\TIKR\` (`tikr.db`, `documents\`) | |
| Stop TIKR from Start Menu; Start again; data still there | |

---

## Backup ownership

- [ ] Named owner: __________________
- [ ] Backup target (external / share): __________________
- [ ] First backup of `C:\ProgramData\TIKR` completed

---

## Sign-off

| Role | Name | Date |
|------|------|------|
| IT / installer | | |
| Deb or Paige (walkthrough) | | |

Walkthrough script: [demo-deb.md](demo-deb.md). Mark bus-factor in [action-items.md](action-items.md) only after walkthrough.

Handoff sheet (owners + walkthrough): [clerk-windows-handoff.md](clerk-windows-handoff.md).
