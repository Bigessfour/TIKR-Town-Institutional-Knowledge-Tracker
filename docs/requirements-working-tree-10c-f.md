# Requirements Manager — Working Tree (Phase 10C excerpt)

**Branch:** `feature/phase10c-document-tool-coverage` — Syncfusion Document SDK A3 + clerk document types (PDF/Word/Excel/PPT).

Full checklist: see git history on `main`; this excerpt tracks **10C-F** only.

## Phase 10C status (this PR)

| Group | Scope | Status |
|-------|--------|--------|
| **A3** | Ollama + `Microsoft.Extensions.AI` over Storage Mode tools | **done** — `SyncfusionDocumentAgentOrchestrator` |
| **F** | PDF ops + Word + Excel + PPT + Office→PDF registry; deterministic Excel/PPT paths | **done** — `SyncfusionDocumentAgentToolRegistry` |
| **B** | NAS Storage Mode (`NasSyncfusionDocumentStorage`) | **done** on `main` |
| **C** | Extraction source UI badge | **PR #37** — separate branch |
| **NAS smoke** | Licensed PDF/DOCX on Synology | **blocked** — [nas-agent-tools-setup.md](nas-agent-tools-setup.md) |

## Env (docker/.env)

```bash
USE_SYNCFUSION_AGENT_TOOLS=true
USE_SYNCFUSION_AGENT_ORCHESTRATION=true   # optional Ollama tool loop
SYNCFUSION_LICENSE_KEY=<Document SDK key>
OLLAMA_HOST=http://ollama:11434
```

## References

- [sf-document-agent-tools.md](sf-document-agent-tools.md)
- [nas-agent-tools-setup.md](nas-agent-tools-setup.md)
- [Syncfusion product overview](https://www.syncfusion.com/explore/ai-agent-tools-for-document-sdk/)
