# Implementation Plan: Max AI Assistant Capabilities

**Feature**: `005-ai-assistant-capabilities`  
**Date**: 2026-07-31  
**Stack**: .NET 10, Blazor Interactive Server, HybridAiService/Ollama, existing Assistant + chat history

## Architecture

```
Assistant.razor
  → ProductHelpCatalog (static ops + Syncfusion coach pack)
  → TikrApiClient semantic-search + semantic-search-knowledge + dashboard
  → AssistantPromptBuilder (energetic system + triple RAG pack + next steps)
  → IChatClient (Ollama)
  → ChatHistoryService (existing)
```

## Workstreams

| ID | Workstream | Deliverables |
|----|------------|--------------|
| W1 | Product help catalog | `ProductHelpCatalog`, Syncfusion + TIKR ops entries, search API |
| W2 | Prompt + packing | Energetic system prompt; product block in user message; next-step instruction |
| W3 | Assistant UX | Suggestion chips; proactive brief; identity banner (exists) |
| W4 | API surface | Optional `GET/POST` product-help search or pure Web catalog |
| W5 | Proof | Unit tests for catalog/search/prompt; Assistant bUnit smoke |

## Layer rules

- Business AI packaging helpers may live in `TIKR.Shared` / `TIKR.Web.Helpers` for UX packing.
- Product help catalog: `TIKR.Shared` (pure data + search) so tests don’t need Blazor.
- No secrets; local-first only for everyday path.

## Implementation order

1. Shared product help catalog + keyword search  
2. Prompt builder updates  
3. Assistant UI (chips, brief, product pack in turn)  
4. Docs (`docs/ai-tooling.md`)  
5. Tests  

## Risks

| Risk | Mitigation |
|------|------------|
| 3b model ignores help pack | Explicit “prefer product help for how-to” + short entries |
| Latency from extra work | Product search is in-memory keyword (no extra Ollama call) |
| Prompt bloat | Cap product hits to top 3, short bodies |

## Acceptance mapping

- FR-001/002: existing + regression tests  
- FR-003/004: ProductHelpCatalog content  
- FR-005–007: prompt + Assistant UI  
- FR-008: BuildUserMessageWithRag product section  
- SC-008: tests listed in tasks.md  
