# Syncfusion Control Audit — TIKR Web

**Status:** Complete (2026-06-28) — all FIX, DEFER, and next-iteration bUnit items done  
**Host model:** Blazor Interactive Server (`@rendermode InteractiveServer`)  
**Validation tool:** `#sf_blazor_assistant` (sf-blazor-mcp) + code trace + bUnit  
**Backend:** `TikrApiClient` → TIKR.Api minimal endpoints  

## Purpose

Confirm every Syncfusion control on clerk-facing pages matches official Blazor guidance (required properties, events, binding) and is wired to the correct API handler.

## Methodology

1. **Inventory** — List all `Sf*` markup and child settings on the page.
2. **MCP review** — `#sf_blazor_assistant {Component} Blazor Interactive Server: required properties, events, binding for {use case}`.
3. **Attribute check** — Compare markup to MCP / [Blazor API reference](https://help.syncfusion.com/cr/blazor/).
4. **Backend trace** — Event/handler → `TikrApiClient` method → API route → DTO field.
5. **Smoke** — Manual or bUnit: happy path + error path.
6. **Record** — `PASS` | `FIX` | `DEFER` per control.

## Execution order (nav top → bottom)

| # | Page | Route | Pass | Fix | Defer |
|---|------|-------|------|-----|-------|
| 1 | Dashboard | `/` | 2 | 0 | 0 |
| 2 | Calendar | `/calendar` | 2 | 0 | 0 |
| 3 | Requirements | `/requirements` | 12 | 0 | 0 |
| 4 | Documents | `/documents` | 9 | 0 | 0 |
| 5 | AI Assistant | `/assistant` | 4 | 0 | 0 |
| 6 | Knowledge Vault | `/vault` | 13 | 0 | 0 |
| 7 | Settings | `/settings` | 3 | 0 | 0 |
| 8 | Login | `/login` | 4 | 0 | 0 |
| 9 | Account | `/account` | 4 | 0 | 0 |
| 10 | Users (admin) | `/settings/users` | 5 | 0 | 0 |
| 11 | Shared | — | 4 | 0 | 0 |

**Totals:** 62 PASS · 0 FIX · 0 DEFER

## Fix backlog

| ID | Page | Issue | Status |
|----|------|-------|--------|
| F1 | Documents | Download → `GET /api/documents/{id}/content` + `tikr-download.js` | **Done** |
| F2 | Vault | Voice notes hydrated from knowledge API on load | **Done** |

## Defer backlog

| ID | Page | Issue | Status |
|----|------|-------|--------|
| D1 | Calendar | `NavigationManager.LocationChanged` refresh | **Done** |
| D2 | Requirements | `SfDatePicker` in requirement dialog | **Done** |
| D3 | Requirements | Agent scan uses `SfUploader` + `ValueChange` | **Done** |
| D4 | Documents | Removed redundant grid `AllowSelection`; manual checkbox column | **Done** |

---

## 1. Dashboard (`/`)

### PageHelp — SfTooltip + SfButton
- **Status:** PASS — `PageHelpTests`

### SfCard (empty state + priority cards)
- Backend: `GET /api/ai/dashboard-priorities`
- **Status:** PASS — `HomePageTests`

---

## 2. Calendar (`/calendar`)

### SfSchedule
- ScheduleField: Id, StartTime, EndTime, Subject, Description
- Backend: `GET /api/requirements`
- **Status:** PASS

### SfGrid (requirements list)
- **Status:** PASS — `CalendarPageTests`

### Navigation refresh (D1)
- `IDisposable` + `LocationChanged` → `LoadAsync()` when route is `/calendar`
- **Status:** PASS (implemented)

---

## 3. Requirements (`/requirements`)

### Toolbar — SfButton ×3
- **Status:** PASS

### Agent upload — SfUploader (D3)
- `AutoUpload`, `UploaderEvents ValueChange="OnAgentUploadAsync"`
- Backend: `POST /api/ai/agent-scan`
- **Status:** PASS — `RequirementsPageTests` (`e-upload`)

### Filters — SfTextBox + SfDropDownList ×2
- **Status:** PASS

### SfGrid + CRUD
- **Status:** PASS — `RequirementsPageTests`

### SfDialog + SfDataForm
- Due date: `SfDatePicker` (D2) — **Status:** PASS — `RequirementsPageTests` (dialog open)

### ConfirmDeleteDialog
- **Status:** PASS

---

## 4. Documents (`/documents`)

### SfUploader
- **Status:** PASS — `DocumentsPageTests`

### Search + SfSplitter + SfTreeView + SfGrid
- Grid selection: manual checkbox only (D4)
- **Status:** PASS

### Download (F1)
- `DownloadDocumentAsync` → `GetDocumentContentAsync` → `tikrDownload.bytes`
- **Status:** PASS — `DocumentsPageTests.Documents_ShowsDownloadControlInGrid`, `TikrApiClientTests`

### SfContextMenu
- Download/retag/delete wired
- **Status:** PASS

---

## 5. AI Assistant (`/assistant`)

### SfAIAssistView + SfCard + SfButton
- **Status:** PASS — `AssistantPageTests`

---

## 6. Knowledge Vault (`/vault`)

### SfTab / SfGrid / SfRichTextEditor / Voice Notes
- Voice notes: `LoadEntriesAsync` hydrates via `VaultVoiceNoteMapper` (F2)
- **Status:** PASS — `VaultPageTests.Vault_HydratesVoiceNotesFromKnowledgeApi`

---

## 7–11. Settings, Login, Account, Users, Shared

All controls **PASS** (see prior audit pass for detail).

---

## MCP query log

| Component | Query date | Result used |
|-----------|------------|-------------|
| SfCard | 2026-06-28 | CardHeader/CardContent structure |
| SfSchedule | 2026-06-28 | ScheduleField Id/StartTime/EndTime mandatory |
| SfUploader | 2026-06-28 | AutoUpload + ValueChange + OpenReadStream |
| SfAIAssistView | 2026-06-28 | PromptRequested + streaming UpdateResponseAsync |
| SfDataForm | 2026-06-28 | EditForm + DataAnnotationsValidator integration |
| SfDatePicker | 2026-06-28 | FormItem template binding |

---

## bUnit coverage (audit completion)

| Area | Test |
|------|------|
| Document download URL | `DocumentsPageTests.Documents_WiresDownloadToDocumentContentApi` |
| Vault page smoke | `VaultPageTests.Vault_ShowsEmergencyBanner` |
| SfUploader agent scan | `RequirementsPageTests.Requirements_RendersAgentScanUploadControl` |
| SfDatePicker dialog | `RequirementsPageTests.Requirements_UsesSfDatePickerWhenDialogOpen` |
| Voice note hydration (F2) | `DocumentSelectionStateTests.VaultVoiceNoteMapper_*` |
| Download API client | `TikrApiClientTests.GetDocumentContentAsync_*` |

---

## Manual smoke script

1. **Documents:** upload → row → Download saves file from NAS
2. **Requirements:** AI Scan → dialog pre-fill → save → grid + calendar
3. **Calendar:** edit requirement → navigate back → schedule refreshes
4. **Vault:** voice note persists after reload
5. **Assistant:** streamed Ollama reply

---

## Next iteration

- [x] F1–F2, D1–D4 implemented
- [x] bUnit smoke for download + voice notes + Requirements Syncfusion controls
- [ ] Re-run MCP pass after Syncfusion package bump (pinned **33.2.15** in `TIKR.Web.csproj`)
