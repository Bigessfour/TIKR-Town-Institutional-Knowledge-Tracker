# Deb & Paige handoff (Windows day-1)

Use after IT has `Setup-TIKR.exe` and has passed [clerk-windows-smoke.md](clerk-windows-smoke.md).

**Agent / IT prep status (2026-07-13):** Docs + `publish/TIKR-Deploy` payload ready. Compile Setup on Windows (Inno 6), then complete the human rows below.

**Stand-in UX walkthrough (2026-07-20):** Stephen ran [demo-deb.md](demo-deb.md) against local Docker (ship-proof ports). Evidence: [demo-deb-walkthrough-evidence.md](demo-deb-walkthrough-evidence.md). Does **not** replace Dell Setup.exe / Paige / backup ownership rows.

---

## Owners

| Role | Name | Notes |
|------|------|-------|
| Backup owner (`C:\ProgramData\TIKR`) | | External drive / share path: |
| Deb (primary clerk) | Stephen (stand-in UX) / Deb TBD | Stand-in completed product script 2026-07-20 |
| Paige (shared PC user) | | |
| IT contact | | |

---

## Walkthrough (human)

Follow [demo-deb.md](demo-deb.md) on the Dell (day-1 Windows mode), or the five steps in [clerk-windows-install.md](clerk-windows-install.md).

- [x] Deb completed guided tour — **stand-in UX** on Docker 2026-07-20 ([evidence](demo-deb-walkthrough-evidence.md)); Dell tour still recommended
- [ ] Paige started TIKR from Desktop and opened Dashboard
- [ ] Both know: Start / Stop / where data lives / who backs up
- [ ] First backup of `C:\ProgramData\TIKR` completed by backup owner
- [x] Record date of walkthrough: **2026-07-20** (stand-in / Docker); Dell date: __________

When the boxes above are checked, mark the Phase 0 PR #4 / bus-factor items in [action-items.md](action-items.md).

---

## Auth (locked for day-1)

Login stays **off** for Deb + Paige on this trusted shared PC. Revisit only if the app leaves that machine.
