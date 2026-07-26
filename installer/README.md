# TIKR Windows installer (Inno Setup)

Professional installer for **TIKR — Clerk's Vault** on a municipal Windows PC (e.g. town clerk Dell).

| Piece                         | Role                                                          |
| ----------------------------- | ------------------------------------------------------------- |
| `tikr-setup.iss`              | Inno Setup 6 script → `Setup-TIKR.exe`                        |
| `scripts/`                    | Start (hidden), stop, firewall helpers installed with the app |
| `assets/README-Installed.txt` | Clerk-facing getting-started text                             |
| `license.local.iss.example`   | Optional pre-baked Syncfusion key (never commit real keys)    |

**Stack:** two self-contained processes (same as USB deploy):

- `TIKR.Api.exe` → `http://localhost:5000`
- `TIKR.Web.exe` → `http://localhost:8080`

Data is stored under **`C:\ProgramData\TIKR`** (writable without admin). Binaries go to **`C:\Program Files\TIKR`**.

> **Until you compile this on a Windows PC with Inno 6**, Deb uses folder mode: copy `publish/TIKR-Deploy` → `Install-TIKR.cmd` → `Start-TIKR.bat` ([docs/clerk-windows-install.md](../docs/clerk-windows-install.md)).
>
> Long-term production for the clerk remains **Docker on Synology** ([docs/deb-nas-install.md](../docs/deb-nas-install.md)). This installer is for **laptop / interim** use with Program Files + ProgramData paths.

---

## 1. Publish the apps (build machine)

### From this repo (Mac or Windows with .NET 10 SDK)

```bash
# Produces publish/TIKR-Deploy/TIKR.Api and .../TIKR.Web
SKIP_TESTS=1 ./scripts/package-thumb-drive.sh
```

Copy or map that folder to the Windows PC that will compile the installer, e.g.:

```text
C:\TIKR\TIKR.Api\TIKR.Api.exe
C:\TIKR\TIKR.Web\TIKR.Web.exe
```

Or keep the thumb-drive layout:

```text
C:\TIKR-Deploy\TIKR.Api\...
C:\TIKR-Deploy\TIKR.Web\...
```

### Manual publish (Windows)

```powershell
dotnet publish src\TIKR.Api\TIKR.Api.csproj -c Release -r win-x64 --self-contained true -o C:\TIKR\TIKR.Api
dotnet publish src\TIKR.Web\TIKR.Web.csproj -c Release -r win-x64 --self-contained true -o C:\TIKR\TIKR.Web
```

Copy production appsettings from `deploy/windows/` if you publish manually:

```powershell
Copy-Item deploy\windows\appsettings.Production.Api.json C:\TIKR\TIKR.Api\appsettings.Production.json
Copy-Item deploy\windows\appsettings.Production.Web.json C:\TIKR\TIKR.Web\appsettings.Production.json
```

---

## 2. Install Inno Setup 6

Download: [https://jrsoftware.org/isinfo.php](https://jrsoftware.org/isinfo.php)

CLI compiler (typical path):

```text
C:\Program Files (x86)\Inno Setup 6\ISCC.exe
```

---

## 3. Compile `Setup-TIKR.exe`

### Default source (`C:\TIKR`)

```powershell
cd path\to\repo\installer
& "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" .\tikr-setup.iss
```

### Custom source folder

```powershell
& "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" `
  /DMySourceDir="C:\TIKR-Deploy" `
  /DMyAppVersion="1.0.0" `
  .\tikr-setup.iss
```

### Optional: bake license for one municipal machine

```powershell
copy .\license.local.iss.example .\license.local.iss
# Edit license.local.iss — paste key (file is gitignored)
& "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" .\tikr-setup.iss
```

**Output:** `installer\Output\Setup-TIKR.exe`

---

## 4. Install on the clerk PC

1. Run **`Setup-TIKR.exe`** as a user who can approve UAC (admin).
2. Accept default folder: `C:\Program Files\TIKR`.
3. Paste **Syncfusion license key** on the wizard page (or leave blank if already set on the machine).
4. Leave **firewall** checked (opens TCP **5000** and **8080**).
5. Finish → optional **Start TIKR now**.
6. Desktop / Start Menu folder: **TIKR - Clerk's Vault** → **Start TIKR**.

### First-run checklist

| Step                 | Action                                                           |
| -------------------- | ---------------------------------------------------------------- |
| Browser              | `http://localhost:8080`                                          |
| Tour                 | Settings → **Show me around TIKR**                               |
| Ollama (optional AI) | Install from ollama.com; pull `llama3.2:3b` + `nomic-embed-text` |
| Backup               | Copy `C:\ProgramData\TIKR` to external drive regularly           |

---

## What the installer does

| Item         | Behavior                                                                 |
| ------------ | ------------------------------------------------------------------------ |
| Install path | `{autopf}\TIKR` → `C:\Program Files\TIKR`                                |
| Data path    | `C:\ProgramData\TIKR` (DB, documents, data-protection keys)              |
| Start Menu   | `TIKR - Clerk's Vault` (Start / Stop / README / Uninstall)               |
| Desktop      | **Start TIKR** (optional task, on by default)                            |
| Launch       | VBS → PowerShell with **hidden** console windows                         |
| License      | Machine env var `SYNCFUSION_LICENSE_KEY` (HKLM Environment)              |
| Firewall     | Inbound allow for API + Web ports (optional task)                        |
| Uninstall    | Stops processes, removes firewall rules + env var; **keeps** ProgramData |

---

## Compile-time defines

| Define                   | Default   | Meaning                                                |
| ------------------------ | --------- | ------------------------------------------------------ |
| `MySourceDir`            | `C:\TIKR` | Folder containing `TIKR.Api` and `TIKR.Web`            |
| `MyAppVersion`           | `1.0.0`   | Version stamped on Setup                               |
| `MyApiPort`              | `5000`    | API URL + firewall                                     |
| `MyWebPort`              | `8080`    | Web UI URL + firewall                                  |
| `MyAppSyncfusionLicense` | empty     | Optional bake-in (prefer wizard / `license.local.iss`) |

---

## Troubleshooting

| Symptom                         | Fix                                                                                                  |
| ------------------------------- | ---------------------------------------------------------------------------------------------------- |
| Compiler: files not found       | Publish first; pass `/DMySourceDir=...`                                                              |
| License banner in UI            | Re-run Setup with key, or set machine `SYNCFUSION_LICENSE_KEY`, then **sign out/in** (env refresh)   |
| Browser empty / API errors      | Run **Start TIKR**; check `http://localhost:5000/health`                                             |
| Port in use                     | Stop other apps; or recompile with `/DMyWebPort=8081` and document the new URL                       |
| Need visible consoles for debug | Run: `powershell -File "C:\Program Files\TIKR\scripts\Start-TIKR-Installed.ps1" -WindowStyle Normal` |

---

## Related

- USB / folder deploy (no Setup.exe): [docs/windows-thumb-drive-deploy.md](../docs/windows-thumb-drive-deploy.md)
- `deploy/windows/*` — scripts packaged on the thumb drive
- NAS production: [docs/deb-nas-install.md](../docs/deb-nas-install.md)

### Windows Service (optional next step)

This installer starts **user-session** processes (hidden windows). A Windows Service wrapper (so TIKR survives logoff) is a separate deliverable—say the word if you want that variant.
