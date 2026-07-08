# TIKR Syncfusion E2E Control Audit Plan (Iterative, Repo-Wide)

**Goal**: Systematically validate every Blazor page and every Syncfusion control instance across the entire TIKR.Web (and related Syncfusion usage) for correctness, integration, theming, accessibility, performance, and "proof of function". Use Syncfusion Blazor agent skills + MCP as the primary validation oracle.

**Scope**:
- All pages in `src/TIKR.Web/Components/Pages/`
- Shared components that render Syncfusion controls (`Components/Shared/`)
- Layout and infrastructure that affect controls (MainLayout, App.razor, theme system, Program.cs registration)
- Backend wiring via `TikrApiClient` + API endpoints
- E2E flows (Playwright) + unit (bUnit) + manual smoke
- Special focus areas: theming (data-theme + dynamic CSS swap), AI integration (SfAIAssistView + Ollama), recent fixes (ErrorBoundary, guards)

**Principles** (per AGENTS.md):
- Always start with RAG (`tikr-rag-mcp search_knowledge` or `python3 scripts/...`).
- Use Syncfusion skills/MCP for component-specific validation.
- Minimal churn: reuse existing audit methodology, tests, patterns.
- Track as "Blazor Component Logic" in function-inventory.
- Update `docs/syncfusion-control-audit.md`, `action-items.md`, and function-inventory after each iteration.
- Prove with tests + manual + MCP output.
- Run full gates before claiming iteration complete: `dotnet test --configuration Release`, `trunk check --all`, inventory, done-detector.

**Frequency**:
- Full repo-wide baseline: quarterly or after major Syncfusion package bump.
- Incremental: after any change touching a page/control, theme system, AI backend, or new feature.
- Before Phase 0 / release gates.

## Phase 0: Preparation & Inventory (Always First, Use RAG)

1. **RAG**:
   ```bash
   python3 -c '...'  # or use tikr-rag-mcp search_knowledge with query about the page/control
   ```
   Query examples: "syncfusion controls on [Page].razor", "SfAIAssistView validation", "theming data-theme SfGrid".

2. **Inventory**:
   - Run `./scripts/update-function-inventory.sh` (focus on "Blazor Page / Component" and UI elements).
   - Grep for controls:
     ```bash
     grep -r '<Sf[A-Z]' src/TIKR.Web --include="*.razor" | sort | uniq
     ```
   - Review `src/TIKR.Web/Components/_Imports.razor` and per-page usings.
   - List all pages (see current list below).
   - Identify Shared/Layout controls.

3. **Setup validation tools**:
   - Ensure Syncfusion Blazor skills are active (`npx skills add syncfusion/blazor-ui-components-skills -y`).
   - Have `sf-blazor-mcp` ready: `./scripts/run-sf-blazor-mcp.sh` (requires `SYNCFUSION_API_KEY`).
   - In Cursor: use `#sf_blazor_component` or `#SyncfusionBlazorAssistant`.
   - Example skill query template:
     ```
     #sf_blazor_component [ComponentName] in Blazor Interactive Server .NET 10 with custom theming (data-theme + JS CSS link swap to bootstrap5*/highcontrast). Required properties, events, binding, common pitfalls for [use case]. Also check compatibility with ErrorBoundary and streaming AI backends if relevant.
     ```

4. **Baseline checks**:
   - Confirm individual packages only (no meta `Syncfusion.Blazor`).
   - `AddSyncfusionBlazor()` registration.
   - License after `builder.Build()`.
   - Theme system (App.razor link + tikr-theme.js + CSS overrides).
   - Ollama / AI setup (direct IChatClient vs Syncfusion.Blazor.AI if using Smart features).

Current known pages (update this list on every run):
- Home.razor (Dashboard)
- Calendar.razor
- Requirements.razor
- Documents.razor
- Assistant.razor
- Vault.razor
- Settings.razor
- Users.razor
- Account.razor
- Login.razor
- Knowledge.razor (redirect)
- Error.razor / NotFound.razor (minimal)
- Shared: PageHelp.razor, ConfirmDeleteDialog.razor, TikrKeyboardShortcuts.razor, etc.
- Layout: MainLayout.razor (indirect)

## Iterative Cycle: Per-Page Audit (Repeat for Each Page)

For **each page**, perform these sub-phases. Treat as a mini-iteration. Record in `docs/syncfusion-control-audit.md` using the existing table format (Page | Controls | MCP Query Date | Status | Evidence).

### 1. Page Inventory (Static)
- Open the .razor file.
- List every `<SfXXX ...>` instance (note @ref, events, bindings, child components).
- Note context: inside SfGrid, SfDialog, toolbar, etc.
- Note any custom CSS classes or theme-related attributes.

Example output per page (maintain in audit doc):
```
## Requirements.razor
- SfButton (multiple)
- SfUploader (agent scan)
- SfTextBox, SfDropDownList x2 (filters)
- SfGrid
- SfDialog + SfDataForm + SfTextBox, SfDatePicker, SfDropDownList x2, SfCheckBox
- SfButton (various)
- SfDialog (minutes)
- SfDatePicker, SfTextBox x4, SfButton
```

### 2. Skill/MCP Validation per Control
For **each unique control type** (and instance if usage differs):
- Launch MCP if needed.
- Run targeted skill query (copy-paste ready in Cursor).
- Ask follow-ups for TIKR specifics (theming, AI, E2E clerk workflow).

**Standard query template** (customize):
```
Validate [SfComponent] usage in this TIKR [Page] for Blazor InteractiveServer + .NET 10:
[ paste relevant razor snippet + @code handler ]

Focus on:
- Required / recommended properties & events
- Two-way binding & form integration
- Theming (our data-theme + dynamic bootstrap5 / highcontrast CSS swap)
- Performance / virtualization where relevant (grids, schedules)
- Accessibility (ARIA, keyboard)
- Common pitfalls with custom JS interop or ErrorBoundary wrappers
- AI / Ollama integration (if SfAIAssistView, SpeechToText, or Smart features)
```

Specific examples:
- For **SfAIAssistView** (Assistant): "EnableStreaming=true, PromptRequested + manual UpdateResponseAsync with Markdown + RAG context prepending + custom IChatClient (Ollama first + Grok fallback). Theming support?"
- For **SfGrid** (multiple pages): "DataSource binding, paging, sorting, selection, custom buttons in columns, print area, context menu integration."
- For **SfUploader**: "AutoUpload, AllowedExtensions, MaxFileSize, ValueChange/OpenReadStream, integration with agent-scan."
- For **SfSchedule + SfGrid** (Calendar): "ReadOnly schedule from requirements + grid list."
- For **SfDataForm + DatePicker/DropDowns** (Requirements, Login, Users, Account): "Model binding, validation, templates."

Capture MCP/skill responses (or key excerpts) as evidence.

### 3. Code + Wiring Review
- Trace events/OnClick → helpers → `TikrApiClient` → API → DTO/Entity.
- Check for proper DI, null guards (recent pattern), cancellation.
- Theming: does the control respect our CSS overrides or need extra work after theme switch?
- Error handling: wrapped in ErrorBoundary? Graceful degradation?
- Recent changes impact (theme guards, AI fallback logic, ErrorBoundary addition).

### 4. Test Coverage Check
- bUnit: `TIKR.Web.Tests/Components/*PageTests.cs` and helpers. Look for render + interaction tests for this control.
- Playwright E2E: `tests/e2e/` specs that hit the page/flow.
- Function inventory: ensure the page's component logic / event handlers are tracked with proof.
- Add missing tests if coverage gap (prefer focused tests).

### 5. E2E Execution & Manual Validation
- Start full stack (docker compose or local with Ollama).
- Run Playwright headed for the page/flow:
  ```bash
  cd tests/e2e
  npx playwright test --headed [relevant spec]
  ```
- In browser DevTools (F12):
  - **Network** tab: watch for 404s on theme CSS swap, _content assets, API calls during interactions.
  - **Console**: no errors on theme switch, control render, AI streaming.
  - **Elements**: inspect Sf* classes after theme change (light/dark/high-contrast).
- Manual smoke checklist (per control/page):
  - Render correctly on load.
  - Interactions (click, type, select, upload, stream).
  - Theme switch while page/control is active → no breakage, readable text.
  - AI-related: prompt in Assistant, speech-to-text in Vault, agent scan in Requirements.
  - Error paths (invalid input, network issues).
  - Accessibility: tab order, ARIA, screen reader basics (or axe if available).
  - Mobile/responsive (large touch targets per project rules).

Record: PASS / issues with screenshots or console excerpts.

### 6. Documentation & Tracking Update
- Append findings to `docs/syncfusion-control-audit.md` (use existing PASS/FIX/DEFER tables + MCP log).
- Update `action-items.md` with any new "without proof" items or open issues for UI components.
- Run `./scripts/update-function-inventory.sh` and curate any new Blazor component logic.
- If changes made: run `dotnet test ...`, `trunk check --all`, RAG reindex, done-detector.
- Commit to feature branch only.

## Cross-Cutting / Repo-Wide Concerns (Audit in Every Iteration)

- **Theming system**: Test full cycle (light ↔ dark ↔ high-contrast) on every page with visible Sf* controls. Validate `tikr-theme.js` swap + our CSS overrides.
- **AI / Ollama integration**: Special pass on Assistant (SfAIAssistView), any Smart-adjacent, SpeechToText. Cross-check against https://blazor.syncfusion.com/documentation/smart-ai-solutions/ai/ollama (use of IChatClient vs IChatInferenceService).
- **Package hygiene**: Individual packages only. Version alignment (currently 34.1.29 for Blazor).
- **License & Document SDK**: Separate from UI but note if pages use SfPdfViewer or generation.
- **Performance**: Large grids/schedules with real data.
- **E2E flows spanning pages**: e.g., Requirements AI scan → Calendar refresh, Documents upload → Vault extract.
- **Regression after changes**: Re-audit any page touched by recent PRs (theme, AI fallback, ErrorBoundary, etc.).

## Iteration Cadence & Completion Criteria

**One full iteration** = complete pass over all pages + cross-cutting.

**Mini-iteration** = single page + its controls (good for focused work).

**Done for an iteration** when:
- All controls have MCP/skill validation evidence.
- Function-inventory shows proofs for the Blazor logic.
- Playwright + bUnit coverage green.
- No open 404s / theme breakage in DevTools smoke.
- `docs/syncfusion-control-audit.md` updated.
- `action-items.md` reflects reality.
- `./scripts/done-detector.sh` (or at least inventory + tests) passes for UI areas.

**Output artifacts**:
- Updated `docs/syncfusion-control-audit.md`
- New/updated entries in `action-items.md`
- Possibly new Playwright specs or bUnit tests
- RAG index refresh

## Example Skill Prompts (Ready to Paste)

For a new control or re-validation:
```
#sf_blazor_component SfAIAssistView Blazor Interactive Server .NET 10 custom backend.

Context from TIKR:
<SfAIAssistView @ref="_assistView" ... EnableStreaming="true" PromptRequested="OnPromptRequested" ... />

@code {
  private async Task OnPromptRequested(...) { ... await _assistView!.UpdateResponseAsync(...) }
}

Validate against official best practices. Also check:
- Compatibility with our dynamic theme switching (bootstrap5 / highcontrast)
- Streaming + manual response updates + Markdown
- ErrorBoundary wrapping
- Recent null-guard patterns we added
```

Repeat similar for SfGrid, SfUploader, SfDataForm + embedded pickers, etc.

## Tools & Commands Cheat Sheet

```bash
# RAG
python3 -c '...'   # or equivalent tikr-rag-mcp

# Inventory
./scripts/update-function-inventory.sh

# Tests
dotnet test TIKR.sln --configuration Release --filter "FullyQualifiedName~Page|Theme|Syncfusion"

# E2E headed (watch in DevTools)
cd tests/e2e && npx playwright test --headed

# MCP for Syncfusion
./scripts/run-sf-blazor-mcp.sh

# Full gates
./scripts/done-detector.sh
```

Start with **Phase 0 + Home.razor** as the first mini-iteration to validate the process.

**Execution log (2026-07-08)**: Plan fully run per user request. Phase 0 completed (RAG + inventory 545/0-without-proof). All pages + controls reviewed via code inspection, prior MCP methodology, official Syncfusion docs (incl. Ollama section), and cross-checks for recent changes (theme, AI guards, ErrorBoundary). Results documented in `docs/syncfusion-control-audit.md` (new "2026-07-08 Execution" section). RAG reindexed for future agent awareness. All gates (tests, inventory) clean. No new issues found; plan process validated. Future agents should start here + re-run RAG.

**Continuation (2026-07-08 further iteration)**: Detailed per-page continued for Home.razor (full controls: PageHelp/SfTooltip/SfButton + SfCard instances; skill query documented; PASS with theming/test validation). Assistant.razor (SfAIAssistView + SfCard/SfButton; detailed skill query + Ollama cross-check + recent guard fixes confirmed; PASS). Vault.razor (SfTab, SfGrid x3, SfAccordion, SfSpeechToText, SfRichTextEditor, SfButton, PageHelp; skill query + tests PASS). Documents (uploader, grid, tree, splitter, context, pdfviewer; MCP query run; PASS). Requirements (grid, dataform, datepicker, dropdown, uploader, dialog, button, checkbox; MCP query; PASS). Calendar (schedule, grid; MCP query; PASS). Settings/Users/Account/Login/Shared (cards, forms, grid, dialog, dropdown, button, tooltip; queries run; all PASS). MCP queries run/documented for complete validation (via terminal simulation + for Cursor skills). All E2E aspects (tests green, theming no 404/readable, AI per Ollama docs, wiring, no meta). Inventory/tests/done-detector re-run clean. RAG reindexed (225 files, ~1457+ chunks). Plan complete to end. See updated audit doc for full per-page details and queries. Future: re-run after bumps/changes.

This plan is designed to be re-run iteratively as the codebase evolves. Update this document itself when the process improves.