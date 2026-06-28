# Syncfusion Document SDK AI Agent Tools — TIKR configuration

Captured from [Syncfusion Getting Started](https://help.syncfusion.com/document-processing/ai-agent-tools/getting-started), [Tools reference](https://help.syncfusion.com/document-processing/ai-agent-tools/tools), and the [product overview](https://www.syncfusion.com/explore/ai-agent-tools-for-document-sdk/).

**sf-blazor-mcp:** Use `sf_blazor_assistant` for Blazor UI questions. Document Agent Tools run in **TIKR.Api / Infrastructure** (not Blazor). MCP requires `SYNCFUSION_API_KEY` (developer key) — separate from runtime `SYNCFUSION_LICENSE_KEY`. If MCP returns invalid API key, use the help links above.

## TIKR deployment model (NAS / local-first)

| Syncfusion guidance | TIKR choice |
|---------------------|-------------|
| **Storage Mode** for web APIs, stateless, scalable | **Yes** — `NasSyncfusionDocumentStorage` implements `IDocumentStorage` on the NAS file volume |
| **In-Memory Mode** for desktop/console | No — clerk uploads are ephemeral API requests |
| **Azure Blob / S3** storage backends | No — local disk via `IFileStorageService` |
| **OpenAI** agent orchestration | **Deferred (10C-A3)** — use deterministic tool calls in A2; Ollama + `Microsoft.Extensions.AI` in A3 |
| **License** | Same `SYNCFUSION_LICENSE_KEY` as Blazor (Document SDK entitlement; no extra purchase) |

## Startup (required)

Register the license before any Document SDK call (API `Program.cs`):

```csharp
var licenseKey = builder.Configuration["SYNCFUSION_LICENSE_KEY"];
if (!string.IsNullOrWhiteSpace(licenseKey))
    Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(licenseKey);
```

## Environment

| Variable | Purpose |
|----------|---------|
| `SYNCFUSION_LICENSE_KEY` | Runtime Document SDK + Agent Tools license (docker/.env, user-secrets) |
| `USE_SYNCFUSION_AGENT_TOOLS` | `true` → `SyncfusionDocumentAgentExtractionBackend`; `false` → stub (CI default) |
| `TIKR_AGENT_STORAGE_KEY` | Optional AES for persisted agent-scan blobs (A1) |
| `SYNCFUSION_API_KEY` | MCP developer key only — not used at runtime |

## Phase 10C tool mapping

| Clerk flow | Syncfusion tool class | Mode |
|------------|----------------------|------|
| AI Scan PDF → requirement text | `PdfContentExtractionAgentTools.ExtractText` | Storage |
| AI Scan PDF → table count | `DataExtractionAgentTools.ExtractTableAsJson` | Storage |
| AI Scan Word → text | `WordImportExportAgentTools.GetText` | Storage |
| Plain `.txt` / `.csv` | TIKR `DocumentTextExtractionService` (no AgentTools) | — |
| Future: agent picks tools | `DataExtractionAgentTools`, full tool registry | A3 + Ollama |

## NuGet (Infrastructure)

- `Syncfusion.DocumentSDK.AI.AgentTools` (33.2.15 — aligned with Blazor packages)
- Pulls DocIO, PDF, SmartDataExtractor, etc. automatically

## E2E proof tiers

| Tier | What it proves | How to run |
|------|----------------|------------|
| **1 — Unit/API (stub)** | HTTP → storage → txt extraction; `usedSyncfusionTools: false` | Main **TIKR CI** — `DocumentAgentEndpointTests`, fixtures in `tests/fixtures/agent-scan/` |
| **2 — Playwright (stub)** | Clerk uploads txt on `/requirements` → dialog pre-fill + banner | Manual: Docker up, then `cd tests/e2e && npm test` (see `requirements-agent-scan.spec.ts`) |
| **3 — Docker smoke (stub)** | API container agent-scan with txt fixture | TIKR CI docker step (`continue-on-error`) |
| **4 — Licensed Syncfusion** | PDF/DOCX extraction via Storage Mode; `usedSyncfusionTools: true` | GitHub Actions **TIKR Syncfusion Agent Smoke** (`workflow_dispatch` or weekly); requires repo secret `SYNCFUSION_LICENSE_KEY` |
| **5 — Manual NAS** | Full stack with license on Synology volume | See checklist below |

### Fixtures (`tests/fixtures/agent-scan/`)

| File | Purpose |
|------|---------|
| `wiley-periodic-report.txt` | Stub path — unique string for CI/API/Playwright |
| `minimal-clerk-report.pdf` | Syncfusion PDF text extraction (`Wiley clerk report`) |
| `clerk-memo.docx` | Syncfusion Word text extraction |

### Manual NAS checklist

```bash
cp docker/.env.example docker/.env
# Edit: USE_SYNCFUSION_AGENT_TOOLS=true, SYNCFUSION_LICENSE_KEY=<Document SDK key>
docker compose -f docker/docker-compose.yml up --build
# /requirements → AI Scan → minimal-clerk-report.pdf
# Expect dialog description to contain "Wiley clerk report"
# Optional: docker exec inspect tikr-data volume for agent-scans/ and agent-scans/sf-work/
```

### Licensed CI workflow

Add repository secret **`SYNCFUSION_LICENSE_KEY`**, then run [`.github/workflows/tikr-syncfusion-agent-smoke.yml`](../.github/workflows/tikr-syncfusion-agent-smoke.yml) manually or wait for the weekly schedule.

## References

- [GitHub: document-sdk-ai-agent-tools](https://github.com/syncfusion/document-sdk-ai-agent-tools)
- [Example prompts](https://help.syncfusion.com/document-processing/ai-agent-tools/example-prompts)
- Working tree: [requirements-working-tree.md](requirements-working-tree.md) Phase 10C
