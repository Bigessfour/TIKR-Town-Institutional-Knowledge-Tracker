# Deb walkthrough evidence (stand-in)

**Date:** 2026-07-20 (Mountain)  
**Stand-in for Deb:** Stephen  
**Environment:** Local Docker ship-proof ports (Web `http://localhost:18080`, API `http://localhost:15000`) — **not** Dell `Setup-TIKR.exe`  
**Script:** [demo-deb.md](demo-deb.md)

> Closes the **product UX walkthrough** rehearsal for Phase 0 PR #4. Windows Setup install + Paige Desktop start remain open until Dell smoke ([clerk-windows-smoke.md](clerk-windows-smoke.md)).

## Live validation checklist (UI path)

| Check | Result | Notes |
|-------|--------|-------|
| Dashboard loads priorities | PASS | Overdue + High/Low cards (Sales Tax, Open Meetings, Audit, TABOR, …) |
| Requirements: agent scan → pre-filled dialog | PASS | Playwright `requirements-agent-scan.spec.ts` vs live stack (txt fixture → Add requirement dialog + Plain-text badge) |
| Documents: upload → library | PASS | API upload `wiley-walkthrough.txt`; UI also uploaded `Stephen_McKitrick_Resume.pdf` (5 docs in tree) |
| Documents: download | PASS | `GET /api/documents/{id}/content` → HTTP 200 (26294 bytes) |
| Vault: handoff affordances | PASS | Knowledge Vault + “Copy Everything for New Clerk” / handover package buttons visible |
| Assistant: local status | PARTIAL | Settings/footer: Ollama Connected (`llama3.2:3b`); model pulled mid-walkthrough. First chat failed before pull (demo phrase applies). `ask-advanced` returned local-failure string with `usedGrok: false` (Grok off — expected Act A). |
| Settings: storage + AI + license | PASS | Town Wiley; Storage Synology NAS; Syncfusion license Valid (UI + Document SDK); Agent tools Enabled; Grok Disabled; audit shows walkthrough uploads |
| Tour control | PASS | “Show me around TIKR” + “Don’t show walkthrough automatically” on Settings |
| Syncfusion trial banner | PASS | Settings: “Blazor UI probe: Valid — trial banner should not block clicks” |

## Acts executed (demo-deb.md)

| Act | Status |
|-----|--------|
| 0:00 Dashboard hook | Done |
| 2:00 Requirements + AI Scan | Done (automated E2E proof) |
| 5:00 Documents upload/download | Done |
| 8:00 Vault | Done (surface verified; no voice note recorded) |
| 10:00 Assistant local Q | Partial (Ollama ready after model pull; re-ask in UI recommended) |
| 12:00 Grok Act A (off) | Done — Grok Disabled |
| 12:00 Grok Act B (on) | Skipped (no key flip for this stand-in) |
| 15:00 Trust & audit | Done — audit log lists walkthrough uploads |
| 17:00 Close | Leave on Settings / Dashboard |

## Sign-off (stand-in)

| Role | Name | Date |
|------|------|------|
| Stand-in for Deb (UX walkthrough) | Stephen | 2026-07-20 |
| IT / Windows Setup.exe smoke | _pending Dell_ | |
| Paige Desktop start | _pending_ | |
| Backup of `C:\ProgramData\TIKR` | _N/A this env (Docker volume)_ | |

**Recommended follow-up in the open browser:** open Assistant, ask *“What should I prioritize this week for Wiley?”* now that `llama3.2:3b` is pulled, then optionally **Show me around TIKR**.
