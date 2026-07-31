<!-- markdownlint-disable MD033 MD047 -->
# TIKR Function Tree (Visual Overview — Maintained Layer)

**Auto-generated raw data:** [function-inventory.generated.md](./function-inventory.generated.md)
**Human overlay + status:** [action-items.md](./action-items.md)
**Update when structure changes.** Seed from script output + existing `docs/diagrams/03-clerk-feature-map.mmd` and `04-api-surface.mmd`.

> This is the **maintained visual** complement to the hybrid inventory. Use Mermaid for clerk + agent comprehension.

## Clerk Workflows (Primary Surfaces)

```mermaid
flowchart TB
    Clerk((Town Clerk))

    subgraph ClerkWorkflows["Clerk Workflows"]
        Dashboard["/ (Home)\nUrgency pills • AI summary • Quick actions • Due grid"]
        Requirements["/requirements\nGrid CRUD • CSV • AI Scan • Packet export • Print"]
        Documents["/documents\nUpload • Folders/Tree • Semantic search • Download/Preview • Convert"]
        Assistant["/assistant\nSfAIAssistView + per-user DB history\n+ memory facts + RAG"]
        Vault["/vault\nHow-To • Contacts • Tribal • RTE • Copy for new clerk • Voice"]
        Calendar["/calendar\nSfSchedule timeline"]
        Settings["/settings + /users\nAudit • Health (NAS/Ollama/SDK) • Theme • Users (admin)"]
        Account["/account • /login\nPassword • JWT auth (optional)"]
    end

    Clerk --> Dashboard
    Clerk --> Requirements
    Clerk --> Documents
    Clerk --> Assistant
    Clerk --> Vault
    Clerk --> Calendar
    Clerk --> Settings
    Clerk --> Account

    classDef done fill:#d4edda,stroke:#28a745,color:#155724
    class Dashboard,Requirements,Documents,Assistant,Vault,Calendar,Settings,Account done
```

## Cross-cutting & System

```mermaid
flowchart LR
    subgraph Cross["Cross-cutting UX"]
        Offline[Offline banner]
        Footer[NAS status footer + SDK status]
        Help[PageHelp on main pages]
        Delete[Confirm delete + undo toast]
        Keys[? shortcuts + g-nav]
        Theme[Theme (light/dark/high-contrast)]
    end

    subgraph System["System / AI / Data"]
        Health["/health + /api/system/*"]
        AI["HybridAiService (Ollama + gated Grok)"]
        RAG["Semantic + embeddings (nomic)"]
        Agent["DocumentAgentService + Syncfusion Orchestrator + ToolRegistry"]
        Gen["Document generation (Syncfusion) + council packet"]
        Auth["Optional ASP.NET Identity + JWT + policies"]
        Audit["Audit log"]
    end

    Cross --> ClerkWorkflows
    System --> ClerkWorkflows
```

## AI / Agent Layer Detail

```mermaid
flowchart TB
    subgraph AILayer["AI / RAG / Agent Layer"]
        Tag["TagDocument (auto on upload)"]
        Embed["Embed* (auto on knowledge write + explicit)"]
        Search["SemanticSearch (docs + knowledge)"]
        Advanced["AskAdvanced (Grok when enabled)"]
        Priorities["DashboardPriorities"]
        AgentScan["agent-scan (stub ↔ Syncfusion tools via Ollama loop)"]
        Orchestrator["SyncfusionDocumentAgentOrchestrator (A3)"]
        Registry["ToolRegistry (PDF/Word/Excel/PPT/Office→PDF/Data)"]
    end

    Tag & Embed & Search & Advanced & Priorities & AgentScan --> Hybrid["HybridAiService"]
    AgentScan --> Orchestrator
    Orchestrator --> Registry
```

## Key Workflows (High-Level)

- **AI Scan flow**: Upload (Requirements) → agent ProcessUpload → pre-fill dialog (see 05b-requirements-agent-scan.mmd + action-items).
- **Semantic + RAG**: Write triggers embed → search prepends context in Assistant + Documents toggle (05c, 05d).
- **Generation + persist**: Packet / agenda / minutes / memo / compliance → NAS store + audit.
- **Health everywhere**: Local status footer + SDK badge on all pages.

**Status legend (aligns with diagrams):**
- Green/done: shipped on main
- Gray/defer: vNext (Phase 6 smart components, Phase 9 previews, etc.)

See `docs/diagrams/README.md` for sequence diagrams (05a–05e) and architecture.md for C4 views.

---

*Keep this file in sync with structural changes detected by the inventory script.*
