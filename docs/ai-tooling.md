# AI Tooling for TIKR

TIKR uses AI in two layers: **developer-time** tools in Cursor (skills + MCP) and **runtime** chat for clerks in the Blazor app.

## Secrets matrix

| Secret | Used by | Storage |
|--------|---------|---------|
| `SYNCFUSION_LICENSE_KEY` | Runtime Blazor components (removes trial banner) | `docker/.env`, Web user-secrets |
| `SYNCFUSION_API_KEY` | Syncfusion Blazor MCP in Cursor (Agentic UI Builder) | User env only — **not** the license key |
| `GROK_API_KEY` | API advanced AI (`/api/ai/ask-advanced`) | `docker/.env`, Api user-secrets |
| `OLLAMA_HOST` | API, Web `IChatClient`, Ollama MCP | `docker/.env` (default `http://ollama:11434` in Docker) |

**Important:** `SYNCFUSION_LICENSE_KEY` (Community License for running components) is different from `SYNCFUSION_API_KEY` (MCP developer tools from your [Syncfusion account](https://www.syncfusion.com/account)).

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

| Server | Purpose |
|--------|---------|
| `sf-blazor-mcp` | Syncfusion Blazor Assistant — UI builder, component API, layouts, theming |
| `microsoft-learn` | Authoritative .NET 10, Blazor, EF Core, `IChatClient`, Docker docs |
| `ollama` | Test prompts against local Ollama (`llama3.2:3b`, etc.) |

**Invoke in Cursor:**

- `#SyncfusionBlazorAssistant` or `#sf_blazor_component How do I add filtering to SfGrid?`
- Natural language with “Syncfusion” keyword for the Blazor MCP
- Ask Microsoft Learn MCP for `IChatClient`, Blazor Interactive Server, etc.

**Best practice:** Keep ≤3–4 active MCP servers in Cursor to avoid tool-selection ambiguity.

**Verify:** Cursor Settings → Tools & MCP → `sf-blazor-mcp` shows connected (green).

### 3. Already enabled (optional)

| MCP | TIKR use |
|-----|----------|
| `cursor-ide-browser` | E2E test Blazor at `localhost:8080` |
| `user-MCP_DOCKER` | Container ops from agent (optional) |

---

## Part B — Runtime (Blazor app for clerks)

### AI Assistant page (`/assistant`)

- **Local chat (default):** `SfAIAssistView` streams responses via `IChatClient` → Ollama on NAS
- **Clerk context:** Upcoming deadlines from `/api/ai/dashboard-priorities` prepended to prompts
- **Ask Advanced AI:** POST to `/api/ai/ask-advanced` → Grok when `USE_GROK=true` on the API

Business AI logic (tagging, audit, Grok gating) stays in `TIKR.Api` `HybridAiService`. The Web chat is conversational UX only.

### Packages (TIKR.Web)

- `Syncfusion.Blazor.InteractiveChat` — `SfAIAssistView`
- `Markdig` — markdown in chat responses
- `Microsoft.Extensions.AI` + `OllamaSharp` — local Ollama via `IChatClient`

### Configuration

Web reads Ollama settings from the same env/appsettings keys as the API:

- `OLLAMA_HOST` / `AI:OllamaHost`
- `OLLAMA_CHAT_MODEL` / `AI:ChatModel`

---

## Part C — vNext roadmap (Smart Components)

After AssistView MVP, planned Syncfusion Smart AI features:

| Component | Target page | Value |
|-----------|-------------|-------|
| **Smart Paste** | Knowledge vault, custom requirement forms | Paste clipboard → auto-fill multiple fields |
| **Smart TextArea** | Knowledge entry authoring | AI sentence completion for how-to guides |
| **Scheduler AI** | Calendar | Natural language recurring events |
| **Grid semantic search** | Documents | When embeddings storage is added |

Requires `Syncfusion.Blazor.AI` and `AddSyncfusionSmartComponents()` with Ollama integration. See [Syncfusion Smart AI + Ollama](https://blazor.syncfusion.com/documentation/smart-ai-solutions/ai/ollama).

---

## Troubleshooting

| Issue | Fix |
|-------|-----|
| MCP `sf-blazor-mcp` fails to connect | Set `SYNCFUSION_API_KEY` in shell env; restart Cursor |
| Trial banner on Blazor pages | Set `SYNCFUSION_LICENSE_KEY` (Community License) |
| `/assistant` says Ollama unavailable | Start Ollama (`docker compose up ollama` or local `ollama serve`) |
| Advanced AI unavailable | API: `USE_GROK=true` and valid `GROK_API_KEY` |
| Duplicate Syncfusion component errors | Do not mix `Syncfusion.Blazor` meta-package with individual packages (e.g. `InteractiveChat`). TIKR uses individual packages only — see `TIKR.Web.csproj` |

---

## Verification checklist

- [ ] Cursor Settings → Tools & MCP shows `sf-blazor-mcp` connected
- [ ] `#sf_blazor_component` returns Syncfusion-accurate Grid/Schedule answers
- [ ] Microsoft Learn MCP returns current .NET 10 / `IChatClient` docs
- [ ] Agent Skills visible in Cursor Rules → Agent Decides
- [ ] `/assistant` streams Ollama responses when Docker Ollama is running
- [ ] “Ask Advanced AI” gated by `USE_GROK` on the API
