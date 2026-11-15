# TIKR production release plan

**Created:** 2026-11-15  
**Owner environment:** Synology DS225+ NAS (`http://mr-storage:8080` — read-only smoke only; never mutate production data from agents).  
**Baseline inputs:** [tikr-ux-evaluation.md](./tikr-ux-evaluation.md) (PR #169), [functions-inventory.md](./functions-inventory.md) + [release-blockers.md](./release-blockers.md) + [tests/e2e/functions-inventory.json](../tests/e2e/functions-inventory.json) (PR #170).

This document is the **single coordination artifact** for shipping TIKR to production clerks. Cursor agents must execute **one batch at a time**, mark the checklist here, and close matching rows in `docs/release-blockers.md` in the **same PR**.

---

## Goal

Ship a **production-ready** TIKR for one-person town clerks on the NAS: every inventoried control behaves as documented, security is on, AI answers within clerk-tolerable time with visible progress, and release gates are green.

## Definition of done (release)

| Gate | Requirement |
|------|-------------|
| **Functions inventory** | All **226** items in `tests/e2e/functions-inventory.json` pass E2E (0 known failures); `scripts/check-functions-inventory-coverage.py` green |
| **Release blockers** | No open **Blocker** or **Must** class items in `docs/release-blockers.md` (RB-001…RB-024 resolved or downgraded with owner sign-off) |
| **Auth** | Authentication **enabled** in production; `/account` works; unauthenticated users cannot reach Settings secrets/keys; admin routes gated |
| **AI** | **Grok-only** (xAI): chat with **streaming**, semantic/knowledge search via xAI embeddings, health/status that proves a real completion; **no Ollama** in runtime, compose, or clerk copy |
| **Unit tests** | `dotnet test TIKR.sln --configuration Release` — **747+** passed (count may rise; never regress) |
| **Lint** | `trunk check --all` clean on touched files |
| **Live smoke** | Read-only smoke against `http://mr-storage:8080` per E2E smoke-safe inventory (112 items); owner sign-off recorded in this doc |

---

## Owner decision record: Grok-only AI (2026-11-15)

**Decision:** Remove **Ollama entirely**. Use **xAI (Grok) API only** for chat, tagging/smart paste where AI is required, and **document/knowledge embeddings/search**.

**Reason:** DS225+ (4-core Celeron, no GPU) runs `qwen2.5:7b` on CPU; Assistant prompts routinely **time out ~140s**. There is no Mac AI server; Windows desktops are **not** AI hosts.

**Implementation constraints for agents:**

- API key: `XAI_API_KEY` / config `GROK_API_KEY` via `docker/.env` and user-secrets — **never commit**.
- Chat: xAI OpenAI-compatible `POST https://api.x.ai/v1/chat/completions` with **streaming** to the Blazor Assistant (and other chat surfaces).
- Embeddings: xAI offers `POST https://api.x.ai/v1/embeddings` and `GET /v1/embedding-models` (e.g. model id `v1`). Use **`query:`** / **`passage:`** prefixes per xAI docs for retrieval quality. Reindex path must call xAI embeddings, not Ollama.
- **Remove Ollama** from: code, `appsettings*`, `docker-compose*.yml`, health/status UI, Settings copy, deploy scripts, CI triage that assumes local Ollama, and clerk-visible jargon.
- **Privacy (owner-accepted):** Snippets of town documents and vault text sent to xAI for chat and embedding. Record in Settings help: data leaves the NAS for inference; no silent third-party besides xAI unless owner approves a documented alternative.
- **Fallback if embeddings unavailable:** Do **not** silently pick another vendor. Document the gap in the PR and propose the simplest alternative (e.g. **FTS/BM25** via existing SQLite/EF search) as an **owner decision**; implement only after explicit approval.

**References:** [HybridAiService](../src/TIKR.Infrastructure/Services/HybridAiService.cs), [Assistant.razor](../src/TIKR.Web/Components/Pages/Assistant.razor), [docker/docker-compose.yml](../docker/docker-compose.yml), [docker/.env.example](../docker/.env.example).

---

## Batch checklist (track status here)

Update **Status**, **PR**, and **Date** when a batch merges or is in flight.

| Batch | Title | RB ids | Status | PR | Date |
|:-----:|-------|--------|--------|----|------|
| 1 | Auth on + `/account` + protect Settings keys | RB-020 | ☐ Not started | | |
| 2 | Replace Ollama with Grok-only AI | RB-003, RB-006, RB-007, RB-021, RB-017 | ☐ Not started | | |
| 3 | Vault create entry | RB-005 | ☐ Not started | | |
| 4 | Dead buttons + dashboard quick actions | RB-001, RB-002, RB-004, RB-008, RB-009, RB-012, RB-014, RB-015, RB-016, RB-018, RB-019, RB-024 | ☐ Not started | | |
| 5 | Calendar / time fixes | RB-010, RB-011, RB-013 | ☐ Not started | | |
| 6 | Accessibility | RB-022, RB-023 | ☐ Not started | | |
| 7 | Clerk verbiage + nav cleanup | (supports multiple RB UX rows) | ☐ Not started | | |
| 8 | Data quality (index coverage, tags, dupes) | (ops + partial RB-012 context) | ☐ Not started | | |
| 9 | Release checklist + owner sign-off | (all gates) | ☐ Not started | | |

---

## Batch 1 — Auth on + `/account` + protect Settings keys

**Closes:** RB-020

**Scope**

- Enable authentication in production overlays (`deploy/windows/*`, NAS env).
- Fix `/account` **HTTP 500** when auth is enabled (`Account.razor`, auth middleware, `Routes.razor`).
- Require login for Settings and API key surfaces; hide or disable “Keys” when anonymous.
- Clarify LAN deployment: auth is **on** for production clerk use.

**Likely files**

- `src/TIKR.Web/Components/Pages/Account.razor`, `Login.razor`, `Routes.razor`, `MainLayout.razor`
- `src/TIKR.Web/Components/Pages/Settings.razor`
- `src/TIKR.Api/Program.cs` (auth policies if any)
- `deploy/windows/appsettings.Production.Web.json`, `docker/.env.example`
- `tests/TIKR.Web.Tests/**`, E2E login routes in `tests/e2e/**`

**Acceptance criteria**

- Logged-out user → redirected to `/login`; `/settings` not reachable.
- `/account` loads and saves profile without 500.
- E2E: `inv-account-route-account-page-route` and login flows pass.
- Remove RB-020 from `docs/release-blockers.md`; flip any `test.fail` tied to RB-020 in `functions-inventory.spec.ts`.

**Tests (must pass)**

```bash
dotnet test TIKR.sln --configuration Release --filter "FullyQualifiedName~Account|FullyQualifiedName~Auth|FullyQualifiedName~Login"
dotnet test TIKR.sln --configuration Release
# When E2E present on branch:
# npx playwright test tests/e2e/functions-inventory.spec.ts --grep account
trunk check --all
```

---

## Batch 2 — Replace Ollama with Grok-only AI

**Closes:** RB-003, RB-006, RB-007, RB-021, RB-017 (and RB-008 doc alignment)

**Scope**

- **Remove** Ollama client registration, compose service `tikr-ollama`, volume mounts, healthchecks, and env vars (`OLLAMA_*`, `USE_GROK` toggle → Grok always when key present).
- **Chat:** Streaming completions via xAI; immediate Assistant progress (“Thinking…”, “Searching your files…”); friendly timeout/retry copy (no raw “Response stopped.”).
- **Search:** Embeddings + reindex via `POST /v1/embeddings`; full re-embed job for migration off Ollama vectors (dimension/model change — document one-time reindex in Settings).
- **Health:** Single consolidated status — ping proves **chat completion** (or embeddings smoke), not contradictory “Ready” + “Ollama offline”.
- **Settings:** “Refresh embeddings” / reindex visible when API up (RB-017).
- **Assistant:** `EnableStreaming="true"` (or equivalent UX); clear default prompt leak (RB-007); parallel doc + vault search where applicable.
- Update tests/mocks to stub xAI, not Ollama.

**Likely files**

- `src/TIKR.Infrastructure/Services/HybridAiService.cs`, `EmbeddingRecoveryHostedService.cs`, `TownDocumentSearchToolRegistry.cs`
- `src/TIKR.Api/Program.cs` (`/api/ai/*`, `/system/local-status`)
- `src/TIKR.Web/Program.cs`, `Assistant.razor`, `Settings.razor`, `TikrStatusFooter.razor`, `TikrApiClient.cs`
- `docker/docker-compose.yml`, `docker-compose.host-ollama.yml` (remove or archive), `docker/.env.example`, `docker/README.md`
- `deploy/windows/*`, `scripts/deploy-tikr-nas.sh`, `scripts/Ensure-Ollama.ps1` (remove or replace)
- `.github/workflows/ci.yml` (Ollama triage job)
- `docs/architecture.md`, `docs/ai-tooling.md`, `AGENTS.md` (Grok-only wording)
- `tests/TIKR.Api.Tests/**`, `tests/TIKR.Infrastructure.Tests/**`, `tests/TIKR.Web.Tests/**`, `tests/e2e/**` assistant tests

**Acceptance criteria**

- With valid `XAI_API_KEY`, Assistant returns a streamed answer within configured timeout; progress visible **before** network-heavy RAG.
- Semantic search and knowledge search return results using xAI embeddings.
- `/api/ai/status` (or successor) reports one coherent Grok health story.
- No `ollama` service in default `docker compose up`.
- Privacy note in Settings (owner-accepted xAI transmission).
- All RB rows above removed or marked fixed; related `test.fail` removed in E2E.

**Tests (must pass)**

```bash
dotnet test TIKR.sln --configuration Release --filter "FullyQualifiedName~HybridAi|FullyQualifiedName~Embedding|FullyQualifiedName~Assistant|FullyQualifiedName~Semantic"
dotnet test TIKR.sln --configuration Release
trunk check --all
```

---

## Batch 3 — Vault create entry

**Closes:** RB-005

**Scope**

- Add **New how-to / tribal knowledge** entry from empty state and toolbar.
- API create path + Vault UI dialog/editor flow (mirror contact create patterns).

**Likely files**

- `src/TIKR.Web/Components/Pages/Vault.razor`
- `src/TIKR.Api/Program.cs` or knowledge endpoints
- `src/TIKR.Infrastructure/Services/KnowledgeService.cs`
- `tests/TIKR.Web.Tests/**`, `tests/TIKR.Api.Tests/**`

**Acceptance criteria**

- Clerk can create a vault entry without voice import; appears in grid after save.
- E2E inventory item for new control added; RB-005 closed.

**Tests**

```bash
dotnet test TIKR.sln --configuration Release --filter "FullyQualifiedName~Vault|FullyQualifiedName~Knowledge"
dotnet test TIKR.sln --configuration Release
```

---

## Batch 4 — Dead buttons + dashboard quick actions

**Closes:** RB-001, RB-002, RB-004, RB-008, RB-009, RB-012, RB-014, RB-015, RB-016, RB-018, RB-019, RB-024

**Scope (by RB)**

| RB | Fix summary |
|----|-------------|
| RB-001 | **AI Scan** on Dashboard → open Requirements scan/uploader, or honest rename |
| RB-002 | **Call Steve** → real `tel:`/`mailto:` or copy IT contact (not toast-only) |
| RB-004 | Error boundary **Try again** → reload route or reset child |
| RB-008 | Remove **Ask Advanced** from guide/tour; align with Grok-only auto routing |
| RB-009 | Calendar **Create deadline** → visible disabled state + hint when empty |
| RB-012 | Documents grid **row click** selects/opens preview |
| RB-014 | **User guide** reopens on second open |
| RB-015 | **Page help (i)** → drawer/dialog on click, not tooltip-only |
| RB-016 | **Upload document** from Dashboard → focus file input / open chooser |
| RB-018 | **Print council packet** → print packet template/PDF, not bare `window.print()` |
| RB-019 | Distinct tour entry points (sidebar vs settings) |
| RB-024 | **Add requirement** → open create dialog on arrival |

**Likely files**

- `Home.razor`, `Settings.razor`, `MainLayout.razor`, `Documents.razor`, `Requirements.razor`
- `PageHelp.razor`, `ClerkUserGuideDialog` / `ClerkUserGuideService`, `ClerkTourCatalog.cs`, `clerk-user-guide.md`
- `Calendar.razor`, print/packet handlers in API/Web

**Acceptance criteria**

- Each listed control meets **expected** column in `functions-inventory.md` for its `testId`.
- After fixes: remove `test.fail` for each RB in `tests/e2e/functions-inventory.spec.ts`; regenerate inventory if labels change.

**Tests**

```bash
dotnet test TIKR.sln --configuration Release
python3 scripts/check-functions-inventory-coverage.py
# npx playwright test tests/e2e/functions-inventory.spec.ts
trunk check --all
```

---

## Batch 5 — Calendar / time fixes

**Closes:** RB-010, RB-011, RB-013

**Scope**

- All-day deadlines (`IsAllDay`, timezone-safe midnight–1AM display).
- Recurring deadlines advance to next occurrence after due.
- Document grid **UploadedAt** shown in clerk local time (match preview pane).

**Likely files**

- `src/TIKR.Infrastructure/**` (calendar events, recurrence)
- `src/TIKR.Web/Components/Pages/Calendar.razor`, `Documents.razor`
- Shared DTOs in `TIKR.Shared`

**Acceptance criteria**

- Unit/integration tests cover all-day and recurrence advancement.
- RB-010, RB-011, RB-013 closed in release-blockers.

**Tests**

```bash
dotnet test TIKR.sln --configuration Release --filter "FullyQualifiedName~Calendar|FullyQualifiedName~Recurrence|FullyQualifiedName~UploadedAt"
dotnet test TIKR.sln --configuration Release
```

---

## Batch 6 — Accessibility

**Closes:** RB-022, RB-023

**Scope**

- Dashboard **overdue** strip contrast ≥ **4.5:1** (`.priority-overdue`).
- Syncfusion grid filters: human `aria-label` per column (no `_Filter` leakage).

**Likely files**

- `wwwroot/css/**`, `Home.razor`, grid column definitions in Requirements/Documents/Vault

**Acceptance criteria**

- Automated or manual a11y check documents contrast ratio.
- E2E or axe snapshot for filter labels if available.

**Tests**

```bash
dotnet test TIKR.sln --configuration Release
trunk check --all
```

---

## Batch 7 — Clerk verbiage + navigation cleanup

**Scope**

- Remove developer jargon from **clerk screens**: Ollama, Syncfusion, NAS hostnames, “embeddings”, DESKTOP-* computer IDs.
- Apply rewrites from [tikr-ux-evaluation.md §2](./tikr-ux-evaluation.md#2-verbiage-audit-clerk-facing-copy) (Assistant, Home, Settings, footer, Documents semantic search labels).
- Move **Tools tour** (`/demo/clerk-tools`) out of primary clerk nav; keep under Settings or admin-only link.
- Tighten Settings/footer/banners per UX audit.

**Likely files**

- `MainLayout.razor`, `Assistant.razor`, `Home.razor`, `Settings.razor`, `Documents.razor`, `TikrStatusFooter.razor`, `PageHelp` strings, `ClerkTourCatalog.cs`

**Acceptance criteria**

- No Ollama/Syncfusion/NAS strings on Dashboard, Assistant, Documents, Vault, Calendar main views (Settings “Advanced” may mention license status in clerk language only).
- Tools tour not in default sidebar for clerk role.

**Tests**

```bash
dotnet test TIKR.sln --configuration Release
rg -i "ollama|syncfusion|embedding|DESKTOP-" src/TIKR.Web/Components/Pages --glob "*.razor"  # should be empty or Settings-advanced only
```

---

## Batch 8 — Data quality

**Scope**

- **Search coverage:** ~**15%** of **20,243** docs indexed — run full xAI reindex; monitor progress in UI; document expected duration on NAS uplink.
- **Duplicates:** identify and dedupe strategy (owner approval for deletes).
- **Bad tags:** batch retag or rules cleanup.
- **“Steve's Birthday”** (or similar junk calendar/knowledge item): remove or reclassify with audit trail.

**Likely files**

- `HybridAiService` reindex, `LibraryScanService`, admin/settings reindex controls
- SQL/scripts for one-off data fixes (in PR with migration notes only — **no** direct prod DB writes from agents)

**Acceptance criteria**

- Coverage metric reported in Settings or status API; target agreed with owner (e.g. >90% of active library docs).
- Documented runbook for duplicate/tag cleanup.

**Tests**

```bash
dotnet test TIKR.sln --configuration Release --filter "FullyQualifiedName~Reindex|FullyQualifiedName~Embedding"
```

**Production rule:** Data-fix scripts run by **owner** on NAS backup/snapshot; agents supply scripts and verification queries only.

---

## Batch 9 — Release checklist + owner sign-off

**Scope**

| Step | Action |
|------|--------|
| Syncfusion | `SYNCFUSION_LICENSE_KEY` on **tikr-api** and Web; council packet / agenda / minutes / handover PDF smoke |
| Backups | Backup DB + library path; **restore test** on non-prod volume |
| CI | `dotnet test TIKR.sln --configuration Release` (747+); `trunk check --all`; E2E inventory 0 failures |
| Live smoke | Read-only Playwright/smoke against `http://mr-storage:8080` for **smoke-safe** inventory rows only |
| Docs | `docs/release-blockers.md` empty of Blocker/Must; this checklist all ☑ |
| Sign-off | Owner name/date below |

**Owner sign-off**

| Field | Value |
|-------|-------|
| Approved for production clerk use | ☐ |
| Owner | |
| Date | |
| Notes | |

---

## Agent workflow (every batch PR)

1. Read this file; pick the **first unchecked** batch (or the one the owner assigns).
2. Branch `feature/*`, `fix/*`, or `cursor/*` per repo policy; **do not** commit to `main`.
3. Run RAG / repo search; implement batch scope only.
4. Update **Batch checklist** table (Status → In progress / Done, PR link).
5. Update `docs/release-blockers.md` — remove or resolve RB ids claimed by the batch.
6. If controls change: `python3 scripts/generate-functions-inventory.py` and fix E2E `test.fail` entries.
7. Run tests listed for the batch + full solution test before PR.
8. **Never** write/delete production data on `mr-storage` except documented read-only smoke.

---

## Related documents

| Doc | Purpose |
|-----|---------|
| [release-blockers.md](./release-blockers.md) | RB-001…RB-024 tracker |
| [functions-inventory.md](./functions-inventory.md) | Human-readable 226-control inventory |
| [tikr-ux-evaluation.md](./tikr-ux-evaluation.md) | UX audit, verbiage rewrites, Assistant flow |
| [incremental-plan.md](./incremental-plan.md) | Phased product roadmap |
| [.cursor/rules/production-release.mdc](../.cursor/rules/production-release.mdc) | Always-on agent pointer |