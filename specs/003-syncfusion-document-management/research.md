# Research: Syncfusion Document Management (greenfield model)

**Sources (primary):**

- Syncfusion Blazor MCP (`sf_blazor_assistant`) — FileManager, SfPdfViewer2, DocumentEditor, Spreadsheet  
- Repo agent skills: `syncfusion-blazor-file-manager`, `syncfusion-blazor-pdf-viewer`, `syncfusion-blazor-docx-editor`, `syncfusion-blazor-spreadsheet-editor`, `syncfusion-blazor-smart-pdf-viewer`  
- Syncfusion Document Processing AI Agent Tools docs (already captured in `docs/sf-document-agent-tools.md`)  
- Official patterns: Physical File Provider sample (`ej2-aspcore-file-provider`), PDF Viewer load/save/annotation export  

**Syncfusion version baseline for TIKR:** 34.1.32

---

## 1. How Syncfusion frames “document management”

Syncfusion does **not** ship a single “Document Management System” product for Blazor. Instead, a **document management application** is composed from four layers:

| Layer | Syncfusion surface | Role |
|-------|-------------------|------|
| **A. Browse / organize files** | `SfFileManager` + File Provider API | Explorer UX: folders, upload, download, rename, move, copy, delete, search, thumbnails |
| **B. View / edit content** | `SfPdfViewer2`, `SfDocumentEditorContainer`, `SfSpreadsheet` | Type-specific workspaces with full toolbars |
| **C. Process / automate** | Document SDK + **AI Agent Tools** (PDF/Word/Excel/PPT) | Server-side extract, convert, OCR, stamp — not Blazor UI |
| **D. AI-augmented viewing (optional)** | `SfSmartPdfViewer` | Summarize, smart fill, smart redact with an `IChatInferenceService` backend |

A greenfield Syncfusion-aligned app wires **A → B** (open file from manager into editor) and optionally **C/D** for automation.

```
┌─────────────────────────────────────────────────────────────┐
│  Blazor Interactive Server UI                               │
│  ┌──────────────────┐   open file    ┌───────────────────┐  │
│  │ SfFileManager    │ ─────────────► │ Type workspace    │  │
│  │ (browse/upload)  │                │ PDF / Word / XLS  │  │
│  └────────┬─────────┘                └─────────┬─────────┘  │
│           │ Ajax FileOperations                │ Load/Save  │
└───────────┼────────────────────────────────────┼────────────┘
            ▼                                    ▼
┌───────────────────────┐            ┌───────────────────────┐
│ File Provider API     │            │ Content APIs          │
│ Read/Create/Delete/   │            │ Load bytes / SFDT /   │
│ Rename/Copy/Move/     │            │ GetDocumentAsync /    │
│ Upload/Download       │            │ ReplaceContent        │
└───────────┬───────────┘            └───────────┬───────────┘
            │                                    │
            └────────────┬───────────────────────┘
                         ▼
              Physical storage (NAS volume)
                         +
              Optional: Document SDK Agent Tools (extract/OCR/convert)
```

---

## 2. Layer A — File Manager (browse & organize)

### 2.1 Documented greenfield setup

From Syncfusion FileManager getting started and Physical File Provider samples:

```razor
@rendermode InteractiveServer
@using Syncfusion.Blazor.FileManager

<SfFileManager TValue="FileManagerDirectoryContent">
    <FileManagerAjaxSettings
        Url="/api/FileManager/FileOperations"
        UploadUrl="/api/FileManager/Upload"
        DownloadUrl="/api/FileManager/Download"
        GetImageUrl="/api/FileManager/GetImage" />
    <FileManagerEvents TValue="FileManagerDirectoryContent"
                       OnFileOpen="OnFileOpen" />
</SfFileManager>
```

**Server:** Controllers implementing the FileManager contract (or `PhysicalFileProvider` / custom provider). Syncfusion’s sample: [ej2-aspcore-file-provider](https://github.com/SyncfusionExamples/ej2-aspcore-file-provider).

**Operations (11 core):** Read, Create, Delete, Rename, Search, Details, Copy, Move, Upload, Download, GetImage.

**UI modes:**

| Property / area | Intent |
|-----------------|--------|
| `View` = LargeIcons / Details | Thumbnail vs list |
| Toolbar | New folder, upload, download, rename, delete, sort, refresh, selection, view, details |
| Context menu | Open, delete, download, rename, details |
| Virtualization / pagination | Large libraries |
| Drag-and-drop | Upload + reorganize |
| `OnFileOpen` | **Bridge to editors** — open PDF/Word/Excel in a workspace pane |

### 2.2 Data binding patterns Syncfusion documents

1. **AjaxSettings → remote provider** (recommended for real storage)  
2. **OnRead local list** (in-memory / custom DB mapping)  
3. **Injected service** for complex multi-tenant roots  

For NAS/local-first (TIKR), the **Physical provider** mapping onto `/data/documents` (or library scan path) is the closest documented match. Auth: `OnSend` to attach JWT; server sets root folder per user.

### 2.3 What FileManager is *not*

- Not a metadata/AI tag database  
- Not semantic search  
- Not document versioning history (unless built on top)  
- Not a substitute for type-specific editors  

---

## 3. Layer B — Type-specific workspaces

### 3.1 PDF — `SfPdfViewer2`

**Documented lifecycle (greenfield):**

1. Host full-height viewer (`Height="100%"`, `Width="100%"`).  
2. Wait for **`Created`** before **`LoadAsync`** (byte[] / stream / base64 / path).  
3. Enable toolbars intentionally:  
   - `EnableToolbar`, `EnableAnnotationToolbar`, `EnableNavigationToolbar`  
   - `EnableTextSearch`, `EnableTextSelection`, `EnableThumbnailPanel`, `EnableBookmarkPanel`  
   - `EnableFormDesigner` / form fields when filling municipal forms  
4. **Save-back pattern (documented):**  
   - After annotate / form fill → `GetDocumentAsync()` (or download events) → persist bytes server-side  
   - Annotations also export/import as **JSON / XFDF** (sidecar or embedded)  
5. Events: `DocumentLoaded`, `DocumentLoadFailed`, `DocumentEdited`, `DownloadEnd`, `ExportSucceed`.

**Syncfusion does not** treat PDF as “preview only” when annotation toolbar is on — the intended product use is **view + mark up + save**.

### 3.2 Word — `SfDocumentEditorContainer`

**Documented lifecycle:**

1. Convert DOCX → SFDT (server WordDocument load or client open).  
2. `DocumentEditor.OpenAsync(sfdt)`.  
3. Full toolbar (`EnableToolbar=true`); optional custom **Save** toolbar item via `ToolbarItems` + `OnToolbarClick`.  
4. **Save to server (documented):**  
   - `SaveAsBlobAsync(FormatType.Docx)` → base64 → byte[] → API/storage  
   - Or `SaveAsync(fileName, FormatType.Docx)` for client download  
5. Memory hygiene (documented): null SFDT/stream after open/save on large docs.

### 3.3 Excel — `SfSpreadsheet`

**Documented lifecycle:**

1. Load with `DataSource` byte[] or open APIs.  
2. Ribbon / formula bar / sheet tabs for edit.  
3. **Save to server:** `SaveAsStreamAsync()` → stream to API (Interactive Server can write disk or POST to API).  
4. Client export: `SaveAsync(SaveOptions { SaveType = Xlsx, FileName = ... })`.

### 3.4 Open-from-FileManager pattern (intended UX)

Syncfusion demos and docs consistently show:

```
FileManager.OnFileOpen
  → if IsFile
      → determine extension
      → load content into matching component (PDF / Word / Spreadsheet)
      → show editor region (splitter pane or full-screen dialog)
```

Optional: convert non-PDF to PDF server-side (DocIO) then open in PDF Viewer only — documented alternative when Word editor is not desired.

---

## 4. Layer C — Document SDK + AI Agent Tools (server)

Documented modes:

| Mode | Use |
|------|-----|
| **Storage Mode** | Web APIs, NAS paths, scalable agent tools (`IDocumentStorage`) |
| **In-Memory Mode** | Desktop / ephemeral console |

TIKR already chose **Storage Mode** via `NasSyncfusionDocumentStorage` (see `docs/sf-document-agent-tools.md`).

**Clerk-relevant tools:** extract text/tables, Office→PDF, OCR, content ops. Security/redact/sign tools exist in the product suite but are optional.

Agent tools are **not** the UI for document management; they feed intake, RAG, and requirements AI Scan.

---

## 5. Layer D — Smart PDF Viewer (optional AI UI)

`SfSmartPdfViewer` (separate NuGet: `Syncfusion.Blazor.SfSmartPdfViewer`) adds:

- Document summarizer (AssistView in viewer)  
- Smart form fill  
- Smart redaction patterns  

Requires AI backend (`IChatInferenceService` / Azure OpenAI / custom). For TIKR, natural fit is **Ollama** via the same Hybrid AI stack — not yet packaged.

---

## 6. Greenfield “Syncfusion assistant” application shape

If the Syncfusion Blazor assistant scaffolded a **new** municipal document app from docs only, it would look like:

### 6.1 UI structure

```
/documents (or /file-manager)
├── Top: breadcrumb + search (FileManager built-in)
├── Main: SfFileManager (full height) — Details view for clerks
└── On open:
    └── Route or modal workspace:
        ├── /documents/view/{id}  OR  full-screen dialog
        │     SfPdfViewer2 | SfDocumentEditorContainer | SfSpreadsheet
        └── Toolbar: Save to server · Download · Close
```

**Alternative (richer metadata apps):** Split layout  

- Left 30%: FileManager  
- Right 70%: Editor  

or  

- Left: folder tree + metadata grid  
- Right: preview / open full editor  

Both appear in Syncfusion samples; FileManager is the **canonical** browser.

### 6.2 API surface (greenfield)

| Endpoint group | Responsibility |
|----------------|----------------|
| `/api/FileManager/*` | FileOperations, Upload, Download, GetImage |
| `/api/documents/{id}/content` | GET/PUT binary (or path-based open) |
| `/api/documents/convert/*` | Office/image → PDF (optional bridge) |
| `/api/ai/*` | Tag, embed, search (app-specific, not Syncfusion) |

### 6.3 Scripts & license (documented)

- Themes CSS + `syncfusion-blazor.min.js`  
- PDF Viewer / Spreadsheet / WordProcessor **service scripts** as required by package  
- `SyncfusionLicenseProvider.RegisterLicense` after build  

### 6.4 Non-functional

- Interactive Server for large byte round-trips (WASM less ideal for multi-MB DOCX/PDF)  
- Chunk messages for large PDFs (`EnableChunkMessages`)  
- Explicit memory cleanup after SFDT/base64  

---

## 7. E2E process (Syncfusion-intended)

See also diagram: `docs/diagrams/05f-document-management-sf.mmd`.

1. **Ingest** — Upload via FileManager UploadUrl or drag-drop.  
2. **Organize** — Create folders, rename, move, search in FileManager.  
3. **Open** — `OnFileOpen` → load into type-specific control.  
4. **Work** — Annotate PDF / edit Word / edit Excel using Syncfusion toolbars.  
5. **Save** — GetDocument / SaveAsBlob / SaveAsStream → server storage.  
6. **Downstream (app-specific)** — Tag, embed, RAG, requirements link, vault extract (TIKR value-add).  
7. **Automate (optional)** — Agent Tools OCR/extract/convert on upload or AI Scan.  

Steps 1–5 are **Syncfusion-owned patterns**. Steps 6–7 are **application domain** on top.

---

## 8. Key documentation links (external)

| Topic | URL |
|-------|-----|
| File Manager overview | https://blazor.syncfusion.com/documentation/file-manager/getting-started |
| Physical provider sample | https://github.com/SyncfusionExamples/ej2-aspcore-file-provider |
| PDF Viewer 2 getting started | https://blazor.syncfusion.com/documentation/pdfviewer-2/getting-started/server-side-application |
| PDF annotation import/export | https://help.syncfusion.com/document-processing/pdf/pdf-viewer/blazor/import-export-annotation |
| Document Editor save server | Help topics: “Save document to server”, custom toolbar Save |
| Spreadsheet SaveAsStreamAsync | CR: `SfSpreadsheet.SaveAsStreamAsync` |
| Document SDK AI Agent Tools | https://help.syncfusion.com/document-processing/ai-agent-tools/getting-started |
| Smart PDF Viewer | https://blazor.syncfusion.com/documentation (SfSmartPdfViewer) |

---

## 9. Research conclusions for TIKR

1. **Canonical Syncfusion DM UI = FileManager + type workspaces**, not Grid-only.  
2. **Save-back is first-class** for PDF annotations and Office edits; preview-only is incomplete vs docs.  
3. **TIKR’s Grid + TreeView + AI tags** is a valid *metadata library* pattern — complementary to FileManager, not a replacement for Explorer ops.  
4. **Agent Tools are already aligned** with Storage Mode; UI lag is on browse/edit/save loop.  
5. **Separate nav page optional** — modes on `/documents` better for one-person clerk UX.  
6. **Smart PDF Viewer** is the next Syncfusion-native AI surface after classic viewer save-back works.
