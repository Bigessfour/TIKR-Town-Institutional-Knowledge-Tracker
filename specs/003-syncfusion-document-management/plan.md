# Implementation plan: Syncfusion Document Management alignment

**Spec:** [spec.md](spec.md) · **Gaps:** [gap-analysis.md](gap-analysis.md) · **Research:** [research.md](research.md)

## Principles

1. **Documented Syncfusion APIs only** for editors (LoadAsync after Created, GetDocumentAsync, SaveAsBlobAsync, SaveAsStreamAsync, FileManager Ajax).  
2. **Preserve TIKR domain AI** (tags, RAG, scan, vault).  
3. **Clerk-safe defaults** — no silent data loss; dirty guards.  
4. **Ship in thin vertical slices** with proof after each phase.

---

## Phase P0 — Close the edit loop (no FileManager yet)

**Goal:** PDF save-back + open friction + dirty guard. Highest clerk value.

| Task | Detail | Proof |
|------|--------|-------|
| P0.1 | PDF **Save to NAS**: `GetDocumentAsync` → `PUT /documents/{id}/content` | Manual annotate/reopen; API size change |
| P0.2 | Enable save button when PDF dirty (`DocumentEdited` / annotation events) | bUnit or manual |
| P0.3 | Dirty guard on workspace close | Manual Escape/close |
| P0.4 | Grid **double-click / Enter** opens workspace | DocumentsPageTests + Playwright |
| P0.5 | Re-extract + best-effort re-embed after content replace | Unit test on service hook |
| P0.6 | Action log: `UI.Documents.PdfSave` / failed | Log inspection |

**Out of P0:** FileManager package, Smart PDF, XFDF sidecars, version history.

**Estimated focus:** Documents.razor + DocumentService.ReplaceContent + HybridAi embed hook.

---

## Phase P1 — Syncfusion-native discoverability + Browse mode

**Goal:** Mirror greenfield open/save patterns; optional Explorer browse.

| Task | Detail | Proof |
|------|--------|-------|
| P1.1 | Word: custom toolbar **Save** (`ToolbarItems` + `OnToolbarClick` → existing save) | Manual + existing PUT tests |
| P1.2 | Spreadsheet: ribbon/primary **Save to NAS** using `SaveAsStreamAsync` | Manual |
| P1.3 | Add NuGet `Syncfusion.Blazor.FileManager` (34.1.32) | Build |
| P1.4 | FileManager **API** on TIKR.Api (Physical or custom over `IFileStorageService` root) | Integration tests |
| P1.5 | Documents **Browse** mode toggle + AjaxSettings + JWT `OnSend` | Playwright smoke |
| P1.6 | `OnFileOpen` → workspace (resolve Document by StoragePath or import) | E2E |
| P1.7 | Map rename/move/delete to Document rows + audit | Unit tests |
| P1.8 | Update `05d-document-lifecycle.mmd` + architecture feature map | Docs |

**Design note:** Prefer **custom provider** over raw PhysicalFileProvider if Document metadata must stay consistent — wrap `IFileStorageService` + EF.

---

## Phase P2 — Enhanced management abilities

| Task | Detail |
|------|--------|
| P2.1 | Thumbnails (GetImage or grid icon by type) |
| P2.2 | Annotation export/import JSON/XFDF optional tools |
| P2.3 | `SfSmartPdfViewer` behind flag + Ollama `IChatInferenceService` |
| P2.4 | Inline side-pane mini preview (optional) for PDF first page |
| P2.5 | Folder ACL / multi-user root (if multi-user towns expand) |

---

## Phase P3 — Institutional polish

| Task | Detail |
|------|--------|
| P3.1 | Document version history (prior bytes retention policy) |
| P3.2 | Soft-delete / recycle bin |
| P3.3 | Cross-link Requirements ↔ Document from workspace |
| P3.4 | Redact/sign Agent Tools registration if municipal policy needs |

---

## Dependency graph

```
P0.1 PDF save ──┬──► P0.2 dirty ──► P0.3 guard
P0.4 open UX    │
P0.5 re-embed ◄─┘
        │
        ▼
P1.1–1.2 toolbar save
        │
        ▼
P1.3–1.7 FileManager browse
        │
        ▼
P2 Smart PDF / thumbs
```

---

## Suggested first PR (minimal shippable)

**Title:** Documents: PDF annotation save-back + double-click open + dirty close guard  

**Files (expected):**

- `Documents.razor` — Save PDF, dirty state, row open  
- `TikrApiClient` if needed  
- `DocumentService` / embed after replace  
- Tests: Infrastructure ReplaceContent + Web DocumentsPageTests  
- Spec checklist: mark US1–US3 in progress  

---

## Verification checklist (phase exit)

### P0 exit

- [ ] Annotate PDF → Save → reopen shows markup  
- [ ] Double-click opens workspace  
- [ ] Close with unsaved prompts  
- [ ] Word/Excel save still works  
- [ ] Semantic search still returns hits after text doc save  

### P1 exit

- [ ] Browse mode lists NAS files via FileManager  
- [ ] Open from Browse loads workspace  
- [ ] Rename/delete stays consistent with Library mode  
- [ ] Function inventory + control audit updated  

### Licensed NAS

- [ ] Update `docs/nas-agent-tools-setup.md` smoke section with PDF save step  
- [ ] Quarterly walk in `syncfusion-e2e-audit-plan.md` includes DM E2E  

---

## Package & license

| Item | Action |
|------|--------|
| FileManager NuGet | Add at P1 with pin 34.1.32 |
| SmartPdfViewer | Add at P2 only |
| License | Same `SYNCFUSION_LICENSE_KEY`; confirm FileManager entitlement on Settings page |
| Scripts | App.razor already hosts PDF/Spreadsheet scripts; verify FileManager has no extra script requirement |

---

## Tracking

- Gap IDs G1–G10 live in [gap-analysis.md](gap-analysis.md) — flip status when closed.  
- Link PR numbers here as phases ship.

### Implemented 2026-07-27 (main implementation pass)

| Item | Status |
|------|--------|
| P0.1 PDF GetDocumentAsync → PUT content | Done (`Documents.razor` SavePdfPreviewAsync) |
| P0.2 Dirty flag (DocumentEdited) | Done |
| P0.3 Close dirty guard | Done (ConfirmDeleteDialog discard) |
| P0.4 Double-click open | Done (OnRecordDoubleClick) |
| P0.5 Re-embed after replace | Done (API PUT content best-effort EmbedDocumentAsync) |
| P1.1 Word toolbar Save to NAS | Done (CustomToolbarItem + SaveAsBlobAsync) |
| P1.2 Spreadsheet SaveAsStreamAsync | Done (SaveWorkspaceAsync) |
| P1.3 FileManager package 34.1.32 | Done |
| P1.4–1.7 Browse mode OnRead provider | Done (virtual folders from SuggestedFolder + metadata PATCH) |
| Tests | DocumentFileManagerLogic, Mapper, UpdateMetadata, PATCH API, Documents mode toggle |

### Implemented 2026-07-27 (deferred P2/P3 close-out)

| Item | Status |
|------|--------|
| P2.1 Type icons (thumbnail substitute) | Done — grid type column |
| P2.2 Annotation export/import JSON/XFDF | Done — workspace toolbar |
| P2.3 Smart PDF Viewer | Still deferred (optional product) |
| P2.4 Inline side-pane PDF preview | Done — compact SfPdfViewer2 |
| P2.5 Folder ACL | Still deferred |
| P3.1 Version history | Done — DocumentVersion entity + restore |
| P3.2 Soft-delete / recycle bin | Done — DeletedAt + restore/purge |
| P3.3 Requirements cross-link | Done — side panel links |
| P3.4 Redact/sign agent tools | Still deferred |

### Production embedding recovery (2026-07-27)

| Item | Status |
|------|--------|
| Reindex skips deleted/transient | Done |
| Corpus health excludes recycle bin | Done |
| Auto-recovery host when Ollama returns / coverage gap | Done (`EmbeddingRecoveryHostedService`) |
| Cooldown + Settings status | Done |
| `GET /api/ai/embedding-recovery-status` | Done |
