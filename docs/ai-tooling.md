# AI Tooling for TIKR

TIKR uses AI in two layers: **developer-time** tools in Cursor (skills + MCP) and **runtime** chat for clerks in the Blazor app.

## Secrets matrix

| Secret                   | Used by                                              | Storage                                                 |
| ------------------------ | ---------------------------------------------------- | ------------------------------------------------------- |
| `SYNCFUSION_LICENSE_KEY` | Runtime Blazor components (removes trial banner)     | `docker/.env`, Web user-secrets                         |
| `SYNCFUSION_API_KEY`     | Syncfusion Blazor MCP in Cursor (Agentic UI Builder) | User env only — **not** the license key                 |
| `GROK_API_KEY`           | API advanced AI (`/api/ai/ask-advanced`)             | `docker/.env`, Api user-secrets                         |
| `OLLAMA_HOST`            | API, Web `IChatClient`, Ollama MCP                   | `docker/.env` (default `http://ollama:11434` in Docker) |

**Important:** `SYNCFUSION_LICENSE_KEY` (Community License for running components) is different from `SYNCFUSION_API_KEY` (MCP developer tools from your [Syncfusion account](https://www.syncfusion.com/account)).

**macOS:** store keys in the Passwords app, then run `./scripts/setup-local-secrets.sh` to merge them into gitignored `docker/.env` and dotnet user-secrets. The scripts never log secret values.

---

## Part A — Developer-time (Cursor IDE)

### 1. Syncfusion Agent Skills

Component-aware skill guides come from [syncfusion/blazor-ui-components-skills](https://github.com/syncfusion/blazor-ui-components-skills). They are **not** committed to this repo (`.agents/` is gitignored — ~15MB). Versions are pinned in [`skills-lock.json`](../skills-lock.json) at the repo root.

Install locally:

```bash
npx skills add syncfusion/blazor-ui-components-skills -y
```

Cursor auto-detects skills from `.agents/skills/` after install. Priority skills for TIKR:

- `syncfusion-blazor-schedule` — Calendar page
- `syncfusion-blazor-grid` — Documents, Knowledge, Dashboard
- `syncfusion-blazor-uploader` — Document uploads
- `syncfusion-blazor-common` — hosting model, imports
- `syncfusion-blazor-license` — license registration

To refresh skills after upstream updates:

```bash
npx skills add syncfusion/blazor-ui-components-skills -y
```

Re-run the command above when `skills-lock.json` changes on `main`.

### 2. MCP servers

Copy the template and set your Syncfusion API key in the environment:

```bash
cp .cursor/mcp.json.example .cursor/mcp.json
export SYNCFUSION_API_KEY="your-syncfusion-account-api-key"
```

Configured servers:

| Server            | Purpose                                                                                                   |
| ----------------- | --------------------------------------------------------------------------------------------------------- |
| `sf-blazor-mcp`   | Syncfusion Blazor Assistant — UI builder, component API, layouts, theming                                 |
| `microsoft-learn` | Authoritative .NET 10, Blazor, EF Core, `IChatClient`, Docker docs                                        |
| `ollama`          | Test prompts against local Ollama (`llama3.2:3b`, etc.)                                                   |
| `tikr-rag-mcp`    | **Mandatory before code** — semantic search over repo (`search_knowledge`); `refresh_index` after changes |

**Setup tikr-rag-mcp:**

```bash
./scripts/setup-cursor-mcp.sh   # creates .venv, installs requests, writes .cursor/mcp.json
ollama pull nomic-embed-text    # embedding model for index
.venv/bin/python3 scripts/update_tikr_rag_index.py   # initial / refresh index
```

**Agent workflow:** call `search_knowledge` before implementing; run `update_tikr_rag_index.py` after large merges or doc updates. Index lives in `.rag_index/` (gitignored).

**Invoke in Cursor:**

- `#SyncfusionBlazorAssistant` or `#sf_blazor_component How do I add filtering to SfGrid?`
- Natural language with “Syncfusion” keyword for the Blazor MCP
- Ask Microsoft Learn MCP for `IChatClient`, Blazor Interactive Server, etc.

**Best practice:** Keep ≤4 active MCP servers in Cursor to avoid tool-selection ambiguity. For a **UI readiness audit**, temporarily disable `ollama` or `microsoft-learn` and enable `chrome-devtools` (see below).

**Verify:** Cursor Settings → Tools & MCP → `sf-blazor-mcp` shows connected (green).

### 3. Browser audit MCP (localhost:8080)

[Chrome DevTools MCP](https://github.com/ChromeDevTools/chrome-devtools-mcp) drives real Chrome (navigate, click, snapshot, console, network). TIKR uses **slim + headless** in `.cursor/mcp.json.example` for clerk smoke on `http://localhost:8080`.

```bash
./scripts/setup-cursor-mcp.sh --with-chrome-devtools
# Restart Cursor → Settings → Tools & MCP → chrome-devtools connected
```

Or Grok CLI: `grok mcp add chrome-devtools npx chrome-devtools-mcp@latest`

Record findings in [ui-readiness-audit.md](ui-readiness-audit.md). Complement with `tests/e2e/page-readiness.spec.ts` and `./scripts/ci-smoke.sh`.

### 4. Other optional MCP

| MCP                  | TIKR use                                                       |
| -------------------- | -------------------------------------------------------------- |
| `cursor-ide-browser` | Built-in Cursor browser tools (alternative to chrome-devtools) |
| `MCP_DOCKER`         | Container ops + bundled browser tools (optional)               |

---

## Part B — Runtime (Blazor app for clerks)

### How Ollama relates to RAG (important)

Ollama is **not** the knowledge base. It only:

1. **Embeds** text with `nomic-embed-text` (vectors stored in Postgres/SQLite as `EmbeddingChunks`)
2. **Chats** with the local chat model (answers using retrieved passages)

TIKR owns retrieval: chunk → embed → hybrid search → grounded prompt → cite sources. Spec: [specs/002-bulletproof-clerk-rag](../specs/002-bulletproof-clerk-rag/spec.md).

### Clerk documentation RAG

- Documents and vault entries are split into overlapping passages (`TextChunker`) and stored in `EmbeddingChunks`
- Search blends vector similarity + keyword overlap; weak hits below `minScore` (default ~0.38) are dropped
- `/assistant` packs passages into the prompt, requires Sources, and soft-fails when embedding is offline
- After model/schema changes or bulk imports: `POST /api/ai/reindex-embeddings` (also `TikrApiClient.ReindexEmbeddingsAsync`)

### NAS library scan (existing documents → Assistant RAG)

For a shared folder of town documents already on the NAS (not uploaded through TIKR yet):

**Policy (Deb/Paige bulk corpus):** Prefer **accuracy and completeness over speed**. A first-pass of 200+ filings may take days or weeks across poller runs; that is intentional. Resume via content fingerprints; fix OCR/embed failures before treating a file as done. See [action-items.md](./action-items.md) — *High-accuracy corpus compilation*.

1. Bind-mount the share into the API container and set `TIKR_LIBRARY_SCAN_PATH` (optional `TIKR_LIBRARY_SCAN_INTERVAL_SECONDS`, default 300).
2. Settings → **NAS document library** → **Scan library now** (or wait for the background poller).
3. Files are **copied** into `FILE_STORAGE_PATH`, tagged, and written to `EmbeddingChunks`. Source files are never moved or deleted.
4. Deb/Paige ask questions on `/assistant` — existing RAG (`semantic-search`) pulls those passages into the chat.
5. Ollama agent tools also get `search_town_documents` (same vector store) when Syncfusion orchestration is enabled.
6. After large imports, use **Reindex embeddings** if Ollama was offline during the scan.

**Formats:** PDF, Word, Excel, and plain text/email (town office). Scanned **PDF/Word** get Syncfusion Tesseract OCR when native text is sparse (`TIKR_OCR_ENABLED`, default on). TIFF/image archives are out of scope for town ingest.

API: `GET /api/library/scan-status`, `POST /api/library/scan`.

### PDF / Word OCR

- Package: `Syncfusion.PDF.OCR.Net.Core` (Tesseract 5) via `SyncfusionDocumentOcrService`
- PDF: extract text → if sparse → `OCRProcessor.PerformOCR` → re-extract
- Word: extract text → if sparse → DocIO → PDF → OCR → text
- Wired into `SyncfusionDocumentAgentExtractor` (requires `USE_SYNCFUSION_AGENT_TOOLS=true` on the API)
- Optional: `TIKR_TESSADATA_PATH` for custom language data; `TIKR_OCR_ENABLED=false` to disable

### AI Assistant page (`/assistant`)

- **Local chat (default):** `SfAIAssistView` streams responses via `IChatClient` → Ollama on NAS
- **Clerk context:** Upcoming deadlines from `/api/ai/dashboard-priorities` prepended to prompts
- **RAG context:** Town docs (`/api/ai/semantic-search`) + vault (`semantic-search-knowledge`) + **TIKR product help** (`ProductHelpCatalog`: how-to + Syncfusion workspace coaching) packed into each turn
- **Interactive UX:** Suggestion chips, proactive due-out brief, energetic system prompt with next-step guidance; chat history + machine-locked Deb/Paige memory
- **Per-user chat history (SQLite):** Conversations + messages are stored per clerk `UserId` (`/api/assistant/session`). Deb and Paige each get isolated threads. Ollama still receives only the last **8** turns (cost/context cap); the DB keeps the full active thread across reloads. **Clear conversation** archives the thread and starts a new one. Follow-up-looking prompts rewrite the *retrieval* query from recent user turns; old RAG packs are not re-injected into history. Messages are capped (~16k chars). Missing identity fails closed (401).
- **Machine-locked clerk identity (auth off):** Chat history and memory follow **this computer**, not Windows login. NAS backup inventory maps `DESKTOP-KN6INHL` → **Deb Dillon** (`local:deb`) and `DESKTOP-O9TCKP1` → **Paige Lindo** (`local:paige`) via `ChatClerkProfiles.MachineNameMap` / `Environment.MachineName`. Optional install env `TIKR_CLERK_PROFILE=deb|paige`. Rare Settings override stored in `localStorage` (`tikr-chat-clerk-override`). Dashboard, Settings, and Assistant show a **Chat memory** banner. Header `X-Tikr-Chat-User` carries the resolved profile.
- **Durable memory facts:** Lightweight extractors persist facts like birthday / preferred name / “remember that …” into `UserMemoryFacts` and inject them into the system prompt. Facts survive Clear conversation and are never shared across users. Multiple “remember that” notes use distinct keys (`note:{hash}`). The Assistant page lists facts with **Forget** (DELETE `/api/assistant/memory/{id}`).
- **Ask Advanced AI:** POST to `/api/ai/ask-advanced` → Grok when `USE_GROK=true` on the API

Business AI logic (tagging, audit, Grok gating, indexing) stays in `TIKR.Api` / `HybridAiService`. The Web chat is conversational UX only.

### Packages (TIKR.Web)

- `Syncfusion.Blazor.InteractiveChat` — `SfAIAssistView`
- `Markdig` — markdown in chat responses
- `Microsoft.Extensions.AI` + `OllamaSharp` — local Ollama via `IChatClient`

**Note on Syncfusion Ollama integration (reviewed from official docs):**
Syncfusion provides `Syncfusion.Blazor.AI` + `IChatInferenceService` / `SyncfusionAIService` wrapper for Smart AI features (Smart Paste, Smart TextArea, data restructuring in TreeGrid/Grid, etc.). See https://blazor.syncfusion.com/documentation/smart-ai-solutions/ai/ollama.
TIKR currently uses lower-level direct `IChatClient` injection for the custom RAG-aware assistant (sufficient for `SfAIAssistView` + streaming + context prepending).

**Syncfusion.Blazor.AI implemented (2026-07-08)**: Package added to TIKR.Web.csproj. In Program.cs (after AddChatClient for Ollama):
```csharp
// Register for Smart AI-powered controls, connected to project (shared Ollama; use IChatInferenceService with TIKR context like RAG hits/_contextSummary for Smart components).
builder.Services.AddSingleton<IChatInferenceService, SyncfusionAIService>();
```
This enables Smart features (e.g. SmartTextArea in forms/Vault) with project awareness. Current custom RAG remains for streaming control. See Ollama docs for GenerateResponseAsync usage with context.

### Configuration

Web reads Ollama settings from the same env/appsettings keys as the API:

- `OLLAMA_HOST` / `AI:OllamaHost`
- `OLLAMA_CHAT_MODEL` / `AI:ChatModel`

#### Optional: `tikr-clerk` model (clerk-tuned tagging)

Document tagging uses a few-shot prompt plus low temperature (`~0.15`) in `HybridAiService`. For a stronger default SYSTEM prompt at the model layer, create a custom Ollama model from `docker/ollama/Modelfile.tikr-clerk` (FROM `llama3.2:3b`):

```bash
# Host Ollama
./scripts/create-tikr-clerk-model.sh

# Or Docker Ollama
docker exec -i tikr-ollama ollama create tikr-clerk -f - < docker/ollama/Modelfile.tikr-clerk
```

Then set `OLLAMA_CHAT_MODEL=tikr-clerk` in `docker/.env` (or host env) and restart `tikr-api` / Web so they pick up the model name. Default remains `llama3.2:3b` if you skip this step.

---

## Part C — Smart Components (shipped)

| Component          | Target page                               | Value                                       |
| ------------------ | ----------------------------------------- | ------------------------------------------- |
| **Smart Paste**    | Requirements dialog                       | Clipboard → form fields via Ollama          |
| **Smart TextArea** | Requirements description + Vault AI draft | Sentence completion for clerk notes         |
| **Calendar NL**    | Calendar                                  | Plain English → create Requirement deadline |

Requires `Syncfusion.Blazor.SmartComponents` + `AddSyncfusionSmartComponents().InjectOpenAIInference()` with shared Ollama `IChatClient`. See [Syncfusion Smart AI + Ollama](https://blazor.syncfusion.com/documentation/smart-ai-solutions/ai/ollama).

---

## Troubleshooting

| Issue                                 | Fix                                                                                                                                                       |
| ------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------- |
| MCP `sf-blazor-mcp` fails to connect  | Set `SYNCFUSION_API_KEY` in shell env; restart Cursor                                                                                                     |
| Trial banner on Blazor pages          | Set `SYNCFUSION_LICENSE_KEY` (Community License)                                                                                                          |
| `/assistant` says Ollama unavailable  | Start Ollama (`docker compose up ollama` or local `ollama serve`)                                                                                         |
| Advanced AI unavailable               | API: `USE_GROK=true` and valid `GROK_API_KEY`                                                                                                             |
| Duplicate Syncfusion component errors | Do not mix `Syncfusion.Blazor` meta-package with individual packages (e.g. `InteractiveChat`). TIKR uses individual packages only — see `TIKR.Web.csproj` |

---

## Verification checklist

- [ ] Cursor Settings → Tools & MCP shows `sf-blazor-mcp` connected
- [ ] `#sf_blazor_component` returns Syncfusion-accurate Grid/Schedule answers
- [ ] Microsoft Learn MCP returns current .NET 10 / `IChatClient` docs
- [ ] Agent Skills visible in Cursor Rules → Agent Decides
- [ ] `/assistant` streams Ollama responses when Docker Ollama is running
- [ ] “Ask Advanced AI” gated by `USE_GROK` on the API
- [ ] Comprehensive Syncfusion control validation follows the iterative E2E repo-wide plan in `docs/syncfusion-e2e-audit-plan.md` (per-page + per-control, using skills/MCP + Playwright + bUnit + function-inventory proofs)
