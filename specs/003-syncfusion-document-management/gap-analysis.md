# Gap analysis: Syncfusion-intended vs TIKR today

**Date:** 2026-07-27  
**Code anchors:** `Documents.razor`, `TIKR.Api` document routes, `HybridAiService`, `SyncfusionDocumentAgent*`, `LocalFileStorageService`, `LibraryScanService`

Legend: **Have** · **Partial** · **Missing** · **N/A (by design)**

---

## 1. Capability matrix

### 1.1 Browse & organize (Layer A)

| Capability (Syncfusion docs) | TIKR today | Status | Notes |
|------------------------------|------------|--------|-------|
| `SfFileManager` UI | Custom `SfTreeView` + `SfGrid` | **Partial** | Metadata-first library, not Explorer |
| FileOperations API (read/create/rename/move/copy) | No FileManager provider | **Missing** | Folders are AI `SuggestedFolder` strings, not real FS hierarchy for all ops |
| Upload via FileManager UploadUrl | `SfUploader` → `POST /api/documents` | **Partial** | Works; not FileManager-native |
| Download | `GET /api/documents/{id}/content` + JS | **Have** | Audit F1 done |
| GetImage thumbnails | — | **Missing** | No thumbnail column/pane |
| Virtualization for large sets | Grid paging (12) | **Partial** | OK for small towns; not virtualized FileManager |
| Library path scan (NAS drop folder) | `LibraryScanService` + settings | **Have** | TIKR-specific strength |
| Drag-drop reorganize | — | **Missing** | |
| Multi-select bulk ops | Grid bulk delete/re-tag | **Partial** | No bulk move/rename |

### 1.2 View / edit (Layer B)

| Capability | TIKR today | Status | Notes |
|------------|------------|--------|-------|
| Open PDF in `SfPdfViewer2` | Full-screen dialog after Created + LoadAsync | **Have** | Solid Syncfusion lifecycle |
| PDF toolbars (zoom, search, thumbs, print) | Enabled in fullscreen | **Have** | |
| Annotation toolbar | `EnableAnnotationToolbar=true` | **Partial** | **No save-back** of annotated PDF |
| Form designer / fill | `EnableFormDesigner=true` | **Partial** | Fill UI on; no persist |
| `GetDocumentAsync` → server | — | **Missing** | **P0 gap** vs Syncfusion save docs |
| Annotation export JSON/XFDF | — | **Missing** | Optional sidecar |
| Word `SfDocumentEditorContainer` | Fullscreen + OpenAsync SFDT | **Have** | |
| Word save to NAS | Custom Save → `PUT /content` | **Partial** | Works; not toolbar-integrated Save item |
| Spreadsheet open/edit | `DataSource` bytes + ribbon | **Have** | |
| Spreadsheet save to NAS | `SaveAsStreamAsync` path via API | **Partial** | Confirm parity with docs; AllowOpen=false |
| Open from list (row → workspace) | Select + “Open Full Screen” / context menu | **Partial** | Not double-click default; no FileManager OnFileOpen |
| Inline side preview | Metadata + text snippet only | **Partial** | Full render only in dialog |
| Image preview | Convert-to-PDF path | **Partial** | No native image viewer |

### 1.3 Process / automate (Layer C)

| Capability | TIKR today | Status | Notes |
|------------|------------|--------|-------|
| Document SDK Agent Tools Storage Mode | `NasSyncfusionDocumentStorage` | **Have** | |
| Extract text/tables (PDF/Word/Excel) | Agent extract + OCR flags | **Have** | Stub in CI; licensed on NAS |
| Office/image → PDF convert | API convert endpoints | **Have** | Wired in Documents UI |
| OCR sparse scans | `SyncfusionDocumentOcrService` | **Have** | Feature flag |
| AI tag + folder suggest | `HybridAiService.TagDocumentAsync` | **Have** | TIKR domain |
| Chunk embeddings + semantic search | `EmbeddingChunks` + RAG labels | **Have** | Enhanced topic labels 2026-07-27 |
| Requirements AI Scan | Document agent on `/requirements` | **Have** | Separate from library UI |
| Council packet generation | Document SDK generators | **Have** | |
| Redact / digital sign tools | — | **Missing** | Product exists; not registered |

### 1.4 AI-augmented viewing (Layer D)

| Capability | TIKR today | Status | Notes |
|------------|------------|--------|-------|
| `SfSmartPdfViewer` | Not referenced | **Missing** | Package not in Web.csproj |
| In-viewer summarizer | Assistant RAG only | **Partial** | Different surface |
| Smart form fill | — | **Missing** | |
| Smart redact | — | **Missing** | |

### 1.5 Cross-cutting / clerk UX

| Capability | TIKR today | Status | Notes |
|------------|------------|--------|-------|
| Local-first NAS storage | `IFileStorageService` | **Have** | Core product |
| Transient vs keep-for-RAG | `IsTransient` checkbox | **Have** | |
| Full-text + semantic search | Dual mode on Documents | **Have** | FileManager search is filename-only by default |
| Extract to Knowledge Vault | Context menu / toolbar | **Have** | |
| Audit log on doc ops | Partial | **Partial** | Upload/delete stronger than edit-save |
| Multi-user ACL per folder | Identity roles coarse | **Partial** | No per-folder ACL |
| Version history | — | **Missing** | |
| Dirty-state / unsaved warning | — | **Missing** | Important once PDF save ships |
| Row-click open | Checkbox selection + button | **Partial** | Clerk friction |

---

## 2. Architecture comparison

### Syncfusion greenfield (docs)

```
FileManager ──open──► Type Editor ──save──► File Provider / disk
```

### TIKR today

```
Uploader → Document entity (SQLite) + file on disk
Tree(AI folders) + Grid(metadata) → Fullscreen type editor
  ├── Word/XLS: save → PUT content ✓
  └── PDF: annotate UI only ✗ no save
Agent Tools / OCR / Embed / RAG  (strong backend, parallel path)
Library scan path (ingest without UI upload)
```

**Insight:** TIKR is a **metadata-centric institutional library** with Syncfusion **editors bolted on**. Syncfusion’s intended system is a **file-system-centric explorer** with **editors as the main work surface**. Both can coexist: metadata Grid for AI/search, FileManager for physical layout.

---

## 3. Gap severity (product impact for Deb / “the girls”)

| ID | Gap | Severity | Why it hurts |
|----|-----|----------|--------------|
| G1 | PDF annotation/form changes not saved to NAS | **Closed 2026-07-27** | GetDocumentAsync → PUT content |
| G2 | No real folder move/rename on disk | **Partial** | Browse mode moves SuggestedFolder (metadata folders, not pure FS) |
| G3 | No FileManager / Explorer UX | **Closed 2026-07-27** | Browse mode SfFileManager OnRead |
| G4 | Open requires extra click (not row-dblclick / OnFileOpen) | **Closed 2026-07-27** | Double-click + FileManager OnFileOpen |
| G5 | Word Save not on Syncfusion toolbar | **Closed 2026-07-27** | Custom Save to NAS toolbar item |
| G6 | No dirty/unsaved guard | **Closed 2026-07-27** | DocumentEdited + discard confirm |
| G7 | No thumbnails | **Partial 2026-07-27** | File-type icons in grid + inline PDF side preview |
| G8 | Smart PDF / in-doc AI | **Deferred** | Assistant RAG covers Q&A; SfSmartPdfViewer still optional |
| G9 | Annotation XFDF/JSON export | **Closed 2026-07-27** | ExportAnnotationAsStreamAsync + ImportAnnotationAsync |
| G10 | Version history | **Closed 2026-07-27** | DocumentVersion + restore API (max 10) |
| G11 | Soft-delete / recycle bin | **Closed 2026-07-27** | DeletedAt + restore/purge |
| G12 | Requirements cross-link | **Closed 2026-07-27** | GET /documents/{id}/requirements in side panel |

---

## 4. What TIKR does *better* than vanilla Syncfusion samples

Do **not** discard these when aligning to FileManager:

1. **AI tags + suggested folder** on upload  
2. **Semantic search + topic-labeled RAG** for Assistant  
3. **Transient filing** (keep out of long-term context)  
4. **Library scan** of a NAS drop folder  
5. **Requirements linkage** + AI Scan intake  
6. **Vault extract** for succession  
7. **Convert to PDF** + council packet  
8. **Local-first / no cloud** document path  

Any FileManager work must **map FileManager items ↔ Document entities** (or treat FileManager as pure FS and re-index on change).

---

## 5. Recommended target architecture (hybrid)

```
/documents modes:
  [Library]  metadata Grid + AI folder tree + search     ← keep
  [Browse]   SfFileManager over NAS root (optional pane) ← add
  [Workspace] full-screen PDF/Word/Sheet                 ← strengthen

On any open:
  resolve DocumentId or path → load editor → Save writes
  file + updates Document row + re-embed if content changed
```

**Separate nav item?** Prefer **modes on one page** for succession product simplicity. Optional second route `/documents/browse` only if Library becomes crowded.

---

## 6. Package inventory vs needs

| Package | In TIKR.Web 34.1.32 | Needed for target |
|---------|---------------------|-------------------|
| SfPdfViewer | Yes | Yes |
| WordProcessor | Yes | Yes |
| Spreadsheet | Yes | Yes |
| FileManager | **No** | Yes for G2/G3 |
| PhysicalFileProvider (or custom API) | **No** | Yes if FileManager |
| SfSmartPdfViewer | **No** | P2 optional |
| DocumentSDK.AI.AgentTools | In Infrastructure | Keep |

---

## 7. Proof / acceptance hooks (when closing gaps)

| Gap | Proof |
|-----|-------|
| G1 PDF save | Annotate → Save → re-open → annotation present; API PUT content size change |
| G2 folders | Rename/move in FileManager → disk path + Document StoragePath consistent |
| G3 FileManager | bUnit smoke + Playwright open file |
| G5 Word toolbar Save | Custom toolbar item → PUT → re-open text change |
| G6 dirty | Close dialog with edits → confirm discard |

---

## 8. Summary table (one glance)

| Layer | Syncfusion intent | TIKR maturity |
|-------|-------------------|---------------|
| A Browse | FileManager | Partial (Grid/Tree) |
| B Edit | Full editors + save | Partial (PDF save missing) |
| C Process | Agent Tools | Strong |
| D Smart UI | SmartPdfViewer | Missing |
| Domain AI | (app-specific) | Strong (TIKR) |
