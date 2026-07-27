# Feature Specification: Syncfusion-Aligned Document Management

**Feature branch (proposed):** `003-syncfusion-document-management`  
**Created:** 2026-07-27  
**Status:** Spec ready · implementation phased (see [plan.md](plan.md))  
**Input:** Greenfield Syncfusion Blazor document management review + TIKR gap analysis  

**Related:** [research.md](research.md), [gap-analysis.md](gap-analysis.md), [docs/sf-document-agent-tools.md](../../docs/sf-document-agent-tools.md)

---

## Problem statement

TIKR’s Documents page is a **strong institutional library** (upload, AI tags, semantic search, agent extract, Word/Excel edit) but does not fully follow Syncfusion’s **documented document-management composition**: File Manager for browse/organize, type-specific editors as the primary work surface, and **save-back** for all editable formats including PDF annotations/forms.

Clerks need Explorer-like confidence (“my files on the NAS”) plus TIKR’s AI/RAG value — without cloud DMS.

---

## Goals

1. Mirror **documented Syncfusion methods** for open → work → save across PDF, Word, Excel.  
2. Preserve TIKR **metadata, AI, RAG, library scan, vault extract**.  
3. Reduce clerk friction: open documents quickly; never lose markup.  
4. Decide UI shape: **modes on `/documents`**, not a second competing app unless needed.  
5. Keep local-first NAS storage and package pin **34.1.x**.

## Non-goals

- Cloud DMS (SharePoint/Box UI parity)  
- Multi-tenant collaborative co-authoring (Document Editor collab server)  
- Full digital signature PKI / legal e-sign product  
- Replacing Assistant with only Smart PDF Viewer  
- Rewriting Agent Tools Storage Mode (already aligned)

---

## User personas

| Persona | Need |
|---------|------|
| Deb (clerk) | Open today’s PDF agenda, highlight, save; find last year’s fee schedule by meaning |
| Successor clerk | Same folder mental model + Vault extract; no “where did the markup go?” |
| Steve (ops) | Proven licensed E2E; no silent data loss; inventory updated |

---

## User stories & acceptance

### US1 — PDF markup survives close (Priority: P0)

**Story:** Deb opens a PDF full screen, adds highlights/sticky notes or fills form fields, clicks **Save to NAS**, closes, re-opens — markup is present.

**Acceptance:**

1. Given a PDF in the library, When Deb annotates and saves, Then `PUT /api/documents/{id}/content` stores updated bytes.  
2. When she re-opens the same document, Then annotations/form values appear.  
3. If save fails, Then a clear toast; document remains dirty.  
4. Implementation uses Syncfusion-documented path: `GetDocumentAsync` (or equivalent) after edits — not a custom re-render hack.

**Independent test:** API content hash changes; manual or automated open-annotate-save-reopen.

---

### US2 — Open is one gesture (Priority: P0)

**Story:** Deb double-clicks a grid row (or uses Open) and lands in the full Syncfusion workspace for that file type.

**Acceptance:**

1. Double-click / Enter on row opens fullscreen workspace.  
2. Context menu **Open Full Screen** remains.  
3. Unsupported types show convert-to-PDF guidance (existing).

---

### US3 — Unsaved guard (Priority: P0)

**Story:** Deb tries to close the workspace with unsaved PDF/Word/Excel edits and is warned.

**Acceptance:**

1. Dirty flag set on DocumentEdited / content change signals.  
2. Close / Escape / navigate away prompts confirm.  
3. Save clears dirty; Discard reloads last saved.

---

### US4 — Word/Excel save discoverable (Priority: P1)

**Story:** Deb saves Word/Excel from a control that matches Syncfusion toolbar patterns.

**Acceptance:**

1. Custom **Save** on DocumentEditor toolbar (`ToolbarItems` + `OnToolbarClick`) and Spreadsheet ribbon/action calls existing NAS save.  
2. Existing “Save changes to NAS” may remain as duplicate primary action.  
3. Uses `SaveAsBlobAsync(Docx)` / `SaveAsStreamAsync` per docs.

---

### US5 — File browse mode (Priority: P1)

**Story:** Deb can switch Documents to a Syncfusion **File Manager** view over the NAS document root for rename/move/upload/download like Explorer.

**Acceptance:**

1. Mode toggle: **Library** (metadata grid) | **Browse** (SfFileManager).  
2. FileManager Ajax endpoints implemented with auth; root = configured library/storage path.  
3. `OnFileOpen` opens the same workspace as Library mode when the path maps to a Document (or imports on demand).  
4. Operations: at least Read, Upload, Download, Delete, Rename, Create folder; Move/Copy when safe.  
5. Changes that affect indexed docs trigger re-tag/re-embed or library rescan as designed.

**Independent test:** Playwright rename + open PDF; unit tests on provider mapping.

---

### US6 — Library mode stays AI-first (Priority: P1 / preserve)

**Story:** Deb still uses AI tags, semantic search, transient checkbox, bulk re-tag.

**Acceptance:**

1. Library mode retains TreeView folders, Grid, dual search, bulk toolbar.  
2. Topic-labeled RAG context remains for Assistant.  
3. No regression in upload → tag → embed.

---

### US7 — Post-save re-index (Priority: P1)

**Story:** After Deb saves edited content, Assistant still finds updated text.

**Acceptance:**

1. Successful content replace refreshes FullTextContent when extractable and re-embeds chunks.  
2. Failures logged; clerk not blocked on embed outage (best-effort, same as tag).

---

### US8 — Smart PDF workspace (Priority: P2)

**Story:** Deb can summarize or smart-fill a form PDF inside the viewer using local Ollama.

**Acceptance:**

1. Optional package `Syncfusion.Blazor.SfSmartPdfViewer` behind feature flag.  
2. AI backend is local Ollama via documented `IChatInferenceService` pattern.  
3. Does not replace classic viewer until save-back proven.

---

## UI specification

### Page: `/documents`

| Mode | Layout | Primary Syncfusion controls |
|------|--------|----------------------------|
| **Library** (default) | Uploader + search + Splitter: Tree \| Grid \| selection pane | Existing + row double-click |
| **Browse** | Full-height FileManager (+ optional detail drawer) | `SfFileManager` |
| **Workspace** | Modal or route full viewport | `SfPdfViewer2` / `SfDocumentEditorContainer` / `SfSpreadsheet` |

Workspace chrome (both modes):

- File: Download, Convert to PDF, Delete, **Save to NAS** (when dirty or editable)  
- AI: Re-tag, Extract to Vault  
- PDF: Syncfusion annotation/form toolbars + Save  
- Close with dirty guard  

**Separate top-nav “Document Manager” item:** Not required for P0–P1. Revisit if Library + Browse modes overwhelm a single page.

---

## API / data requirements

| Endpoint / concern | Spec |
|--------------------|------|
| `GET/PUT /api/documents/{id}/content` | Keep; PDF save uses PUT |
| FileManager controller group | New: FileOperations, Upload, Download, GetImage |
| Document ↔ path map | StoragePath is source of truth; Browse open resolves Id |
| Embed after replace | Hook in ReplaceContent / service layer |
| Auth | JWT on FileManager Ajax (`OnSend`) |

No schema change strictly required for P0. Optional later: `DocumentVersion` table for history (out of scope P0–P1).

---

## Success metrics

| Metric | Target |
|--------|--------|
| PDF annotate → save → reopen | 100% pass licensed manual smoke |
| Word/Excel save discoverable | Toolbar Save present |
| Double-click open | Works in Library grid |
| FileManager browse | Optional but planned P1; rename/open works |
| Regression | Documents + RAG + agent-scan tests green |

---

## Risks & mitigations

| Risk | Mitigation |
|------|------------|
| FileManager vs Document entity desync | Single write path; rescan/import on orphan files |
| Large PDF GetDocumentAsync memory | Interactive Server only; dispose streams; chunk messages |
| Clerk confusion Library vs Browse | Clear mode labels + one-line help; default Library |
| License entitlement for FileManager | Same Essential Studio key; verify in Settings SDK status |
| Scope creep Smart PDF | Gated P2 after G1 |

---

## Open questions (resolved in plan unless reopened)

| Q | Decision |
|---|----------|
| Separate DM UI page? | **Modes on `/documents`** |
| Replace Grid with FileManager? | **No** — hybrid |
| XFDF sidecar vs full PDF save? | **Full PDF save first** |
| When re-embed? | **On successful content replace** |
