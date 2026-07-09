# Clerk walkthrough — deployment notes

## What ships in the image

| Asset | Path |
|-------|------|
| Tour step catalog | `src/TIKR.Web/ClerkTour/ClerkTourCatalog.cs` |
| Stable anchor IDs | `src/TIKR.Web/ClerkTour/ClerkTourIds.cs` |
| Spotlight UI | `wwwroot/js/tikr-tour.js` |
| User guide markdown | `wwwroot/help/clerk-user-guide.md` |
| Per-user tour prefs (auth on) | `ApplicationUser.ClerkTourCompletedVersion`, `ClerkTourAutoDisabled` + `/api/auth/me/tour` |

## Version bump

When you add or reorder tour steps, increment **`ClerkTourCatalog.CurrentVersion`** (e.g. `v2` → `v3`). Existing clerks who completed an older version will see the auto-tour again once.

Browser-only installs use `localStorage` key `tikr-tour-completed-version`.

## Verify before tag

```bash
./scripts/package-for-deployment.sh
```

With the stack running on port 8080, Playwright runs `tests/e2e/clerk-tour-anchors.spec.ts`.

## NAS rollout

1. Pull or build `tikr-api` and `tikr-web` images.
2. Restart API first (applies EF migration for tour columns when auth is enabled).
3. Restart Web.
4. Hard-refresh browsers (new `tikr-tour.js` and anchors).

## Operator checklist

- [ ] Settings shows **Show me around TIKR** and **Open user guide**
- [ ] Each clerk route has **Tour this page**
- [ ] Sidebar `data-tour` anchors present (DevTools → `[data-tour]`)
- [ ] Skip tour → does not block app; checkbox disables auto-start
