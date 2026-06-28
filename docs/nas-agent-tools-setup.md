# NAS + Syncfusion Agent Tools — Setup Tracker

**Status:** Blocked — Synology NAS hardware not available yet. Use Docker locally until NAS is provisioned.

**Product reference:** [Syncfusion AI Agent Tools for Document SDK](https://www.syncfusion.com/explore/ai-agent-tools-for-document-sdk/)  
**TIKR config reference:** [sf-document-agent-tools.md](sf-document-agent-tools.md)

---

## Document-type coverage (honest)

Syncfusion ships agent tools for **six capability groups**. The NuGet package (`Syncfusion.DocumentSDK.AI.AgentTools`) includes all of them; TIKR only wires a **clerk-scan subset** today.

| Syncfusion group | Available in NuGet | Registered in TIKR (`SyncfusionDocumentAgentToolRegistry`) | Deterministic clerk path (`SyncfusionDocumentAgentExtractor`) | Clerk upload works today |
|------------------|-------------------|--------------------------------------------------------------|---------------------------------------------------------------|--------------------------|
| **PDF** | Full toolkit | Content + operations (`PdfContentExtraction`, `PdfOperations`) | `.pdf` → `PDF_ExtractText` + table count | Yes (license + flag) |
| **Word** | Full toolkit | Import/export + operations | `.doc`/`.docx` → `Word_GetText` | Yes (license + flag) |
| **Excel** | Full toolkit | Worksheet tools + Office→PDF | `.xlsx`/`.xls` → `ConvertExcelToPdf` → `PDF_ExtractText` | Yes (license + flag; needs valid workbook) |
| **PowerPoint** | Full toolkit | Content + operations | `.ppt`/`.pptx` → `PresentationContentAgentTools.GetText` | Yes (license + flag; needs valid deck) |
| **Office → PDF** | `OfficeToPdfAgentTools` | Registered for orchestration + Excel deterministic path | Used by Excel extraction | Partial |
| **Smart Data Extraction** | Tables, KV, forms | `DataExtractionAgentTools` | PDF table count via `ExtractTableAsJson` | Partial (count only) |
| **Plain text** | N/A | N/A | `DocumentTextExtractionService` | Yes (CI default) |

**Not in scope (vNext):** PDF security/redact/sign, Word mail merge, Excel pivot/charts, full Smart Data JSON to UI. Security agent tool classes intentionally omitted.

**PR split:** Document tool coverage belongs in **`feature/phase10c-document-tool-coverage`** — keep separate from **`feature/phase10c-extraction-badge`** (UI banner only).

---

## NAS setup checklist (record results when hardware available)

### Prerequisites

- [ ] Synology NAS with Container Manager (DS225+ or equivalent)
- [ ] Shared folder for TIKR data (e.g. `/volume1/tikr/data`)
- [ ] Syncfusion Document SDK license key (`SYNCFUSION_LICENSE_KEY`)
- [ ] Network access to pull Ollama models

### Deploy stack

- [ ] Clone/copy repo to NAS
- [ ] `cp docker/.env.example docker/.env`
- [ ] Set in `docker/.env`:
  - [ ] `SYNCFUSION_LICENSE_KEY=<key>`
  - [ ] `USE_SYNCFUSION_AGENT_TOOLS=true`
  - [ ] `USE_SYNCFUSION_AGENT_ORCHESTRATION=true` (optional — Ollama tool loop)
  - [ ] `TIKR_STORAGE_LABEL=<NAS model>` (e.g. `Synology DS225+`)
  - [ ] `FILE_STORAGE_PATH=/data/documents`
- [ ] Container Manager → Project → `docker/docker-compose.yml`
- [ ] Map `tikr-data` volume to shared folder
- [ ] `docker exec -it tikr-ollama ollama pull llama3.2:3b`
- [ ] `docker exec -it tikr-ollama ollama pull nomic-embed-text`

### Smoke tests (record date + result)

| # | Test | Expected | Done | Date | Notes |
|---|------|----------|------|------|-------|
| 1 | `/requirements` → AI Scan → `wiley-periodic-report.txt` | Dialog pre-fill; banner “plain-text extraction” | | | Works in CI/Docker without license |
| 2 | AI Scan → `minimal-clerk-report.pdf` | Description contains “Wiley clerk report”; banner “Syncfusion extraction” | | | Requires license |
| 3 | AI Scan → `clerk-memo.docx` | Word text in dialog | | | Requires license |
| 4 | Orchestration path (if enabled) | Ollama invokes `PDF_ExtractText` / `Word_GetText` | | | Check API logs |
| 5 | Volume inspect `agent-scans/` and `agent-scans/sf-work/` | Files persist after scan | | | |
| 6 | `/documents` download | File saves from NAS storage | | | |
| 7 | Footer shows correct `TIKR_STORAGE_LABEL` | Matches physical NAS | | | |

### Optional CI proof (no NAS required)

- [ ] Add repo secret `SYNCFUSION_LICENSE_KEY`
- [ ] Run [`.github/workflows/tikr-syncfusion-agent-smoke.yml`](../.github/workflows/tikr-syncfusion-agent-smoke.yml) (`workflow_dispatch`)
- [ ] Record run URL in table below

| Workflow run | Date | PDF | DOCX | Notes |
|--------------|------|-----|------|-------|
| | | | | |

---

## Interim: Docker on dev machine (until NAS ready)

Same checklist as NAS but on localhost:

```bash
cp docker/.env.example docker/.env
# Edit SYNCFUSION_LICENSE_KEY + USE_SYNCFUSION_AGENT_TOOLS=true
docker compose -f docker/docker-compose.yml up --build
# Web: http://localhost:8080  API: http://localhost:5000
```

Docker proves the stack; NAS smoke proves volume paths, Container Manager, and clerk-facing latency on real hardware.

---

## Related docs

- [README — Synology NAS Deployment](../README.md#synology-nas-deployment)
- [sf-document-agent-tools.md](sf-document-agent-tools.md) — env vars, E2E tiers
- [requirements-working-tree.md](requirements-working-tree.md) — Phase 10C status
