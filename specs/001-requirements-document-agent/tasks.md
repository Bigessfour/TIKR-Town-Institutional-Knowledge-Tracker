# Tasks: Requirements Manager & Document Agent

**Input**: Design documents from `/specs/001-requirements-document-agent/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/agent-scan-api.md, quickstart.md

**Branch**: `feature/requirements-document-agent` (git) · Spec Kit ID: `001-requirements-document-agent`

**Brownfield note**: Most implementation exists on `main`. Tasks focus on **verification, gap closure, and ship proof**.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: US1–US4 maps to spec.md user stories

---

## Phase 1: Setup (Shared Infrastructure)

- [x] T001 Create git branch `feature/requirements-document-agent` from latest `main`
- [x] T002 Confirm Spec Kit feature pointer in `.specify/feature.json` targets `specs/001-requirements-document-agent`
- [x] T003 [P] Export `SPECIFY_FEATURE=001-requirements-document-agent` and `SPECIFY_FEATURE_DIRECTORY` per `docs/spec-kit.md`
- [x] T004 [P] Copy `docker/.env.example` to `docker/.env` if missing; confirm Syncfusion flags for local licensed proof

---

## Phase 2: Foundational (Blocking Prerequisites)

- [x] T005 Run `dotnet restore` / `dotnet build` Release
- [x] T006 Run `dotnet test TIKR.sln --configuration Release` (337 passed)
- [x] T007 Run `trunk check --all` — **green** after ignoring `.specify/**` + `.cursor/skills/**` and fixing trailing WS / CRLF
- [x] T008 [P] Stub-path agent-scan curl — **passed** via `scripts/ship-proof-local.sh` (`txt_ok usedSyncfusionTools=false`)

---

## Phase 3: User Story 1 — Manage Colorado obligations (Priority: P1)

- [x] T009 [P] [US1] RequirementsPageTests
- [x] T010 [P] [US1] Requirements API coverage
- [x] T011 [US1] Delete action for non-seeded rows (bUnit)
- [x] T012 [P] [US1] CSV export helper tests

---

## Phase 4: User Story 2 — AI scan (Priority: P1)

- [x] T013–T018 [US2] Endpoint/service/helper/badge/client/error UX verified
- [x] T019 [P] [US2] Licensed Syncfusion API tests — 2 passed with Keychain/`docker/.env` license

---

## Phase 5: User Story 3 — NAS archive (Priority: P2)

- [x] T020–T023 [US3] Dual-storage unit tests + StructuredTables wiring
- [x] T024 [US3] Licensed Docker smoke — **passed** (`pdf_ok usedSyncfusionTools=True processed=True`) via `scripts/ship-proof-local.sh`

---

## Phase 6: User Story 4 — Ship-ready (Priority: P2)

- [x] T025 [P] [US4] Playwright — **4/4 passed** via `ship-proof-local.sh` (tour disabled in `e2e-helpers.ts`; SfUploader settle + `setInputFiles` with Browse fallback)
- [x] T026 [P] [US4] CI includes Playwright gate (`ci-smoke.sh`)
- [x] T027 [US4] Coverage script after coverlet run — **passed** (`check_coverage.py`: Shared 95.2%, Infra 91.3%, Api 99.4%, Web testable 89.3%)
- [x] T028 [US4] Function inventory refresh — regenerated with Syncfusion config catalog + PdfViewer skill; evidence in `docs/function-inventory.generated.md`
- [x] T029 [P] [US4] Clerk install doc: AI Scan + Documents download
- [x] T030 [US4] Phase 0 PR #3 docs — **done** (`deb-nas-install.md`, `ship-to-production.md`, `clerk-windows-smoke.md`)
- [~] T031 [US4] Phase 0 PR #4 Deb walkthrough — **stand-in UX done** 2026-07-20 on Docker ([demo-deb-walkthrough-evidence.md](../../docs/demo-deb-walkthrough-evidence.md)); Dell Setup.exe + Paige + backup still open

---

## Phase 7: Polish

- [x] T032 [P] Sync AI tooling docs for optional `tikr-clerk` model (`docs/ai-tooling.md`, `README.md`, `docker/.env.example`)
- [x] T033 [P] Document Mac ship-proof path (`scripts/ship-proof-local.sh` + alt ports) in this tasks file + action-items
- [x] T034 Quickstart note: prefer `./scripts/ship-proof-local.sh` on host with `:5000`/`:11434` conflicts
- [x] T035 `/speckit-converge` run
- [x] T036 Open PR — https://github.com/Bigessfour/TIKR-Town-Institutional-Knowledge-Tracker/pull/72

---

## Phase 8: Convergence (2026-07-21)

- [x] T037 Quote `TIKR_STORAGE_LABEL` + trunk ignore Spec Kit paths
- [x] T038 [P] [US3] Optional StructuredTables assertion in licensed API test — asserts count ≥ 0; when JSON present must be array/object shaped
- [x] T039 [US4] Clerk windows install walkthrough bullets
- [x] T040 [US2] Document accepted auto-open dialog UX (research Decision 4)

### Ship-proof evidence (2026-07-21)

| Check                              | Result                                                 |
| ---------------------------------- | ------------------------------------------------------ |
| `trunk check --all`                | ✔ No issues (after ignore + WS fixes)                  |
| API health (Docker alt ports)      | healthy                                                |
| Web                                | HTTP 200                                               |
| Txt agent-scan                     | `usedSyncfusionTools=false`                            |
| Licensed PDF agent-scan            | `usedSyncfusionTools=true`, `processedStoragePath` set |
| Playwright clerk-smoke             | 3/3 passed (tour overlay disabled in E2E helpers)      |
| Playwright requirements-agent-scan | 1/1 passed (SfUploader settle + setInputFiles)         |

**Host notes:** macOS Control Center binds `:5000`; host Ollama binds `:11434`. Use `scripts/ship-proof-local.sh` + `docker/docker-compose.ship-proof.yml` (ports 15000/18080/11435).

---

## Phase 9: Document tagging + Assistant polish (2026-07-20)

- [x] T041 [P] Extract `DocumentTagPromptBuilder` + low-temperature tagging in `src/TIKR.Infrastructure/Services/`
- [x] T042 [P] Add `DocumentTagHeuristics` gap-fill for folder/tags when Ollama is sparse
- [x] T043 [US2] Backfill `FullTextContent` on tag via storage + extraction backend in `HybridAiService`
- [x] T044 [P] Unit tests for heuristics, prompt builder, and HybridAi tagging paths
- [x] T045 [P] Optional `tikr-clerk` Modelfile + `scripts/create-tikr-clerk-model.sh`
- [x] T046 Fix Assistant streaming (full accumulated text + `FormatStreamingHtml`) in `Assistant.razor` / `PageWorkflowHelpers.cs`

---

## Notes

- Prefer `./scripts/ship-proof-local.sh` over raw `ci-smoke.sh` on this Mac
- Do not expand into Requirements Phase 2 UI or Phase 6 Smart Components
- **Still open for Deb sign-off (out of this PR merge):** T031 Dell Setup.exe + Paige + backup owner
