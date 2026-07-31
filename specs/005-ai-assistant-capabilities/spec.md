# Feature Specification: Max AI Assistant Capabilities

**Feature Branch**: `005-ai-assistant-capabilities`  
**Created**: 2026-07-31  
**Status**: Implemented (essential AI capabilities shipped)  
**Input**: Max out essential AI capabilities for Deb/Paige — RAG, chat history, energetic interactive chat, product/Syncfusion help, suggestions, proactive usefulness; Spec Kit implementation.

## North star

The AI Assistant is Deb Dillon and Paige Lindo’s energetic deputy clerk: it answers from town documents and vault, remembers who they are, teaches how TIKR and Syncfusion document tools work, suggests next steps, and never invents statute or fees.

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Grounded town Q&A with history (Priority: P1)

Deb asks follow-up questions about due-outs and packets. Answers use document/vault retrieval, cite sources, and continue from her machine-locked chat history and memory facts.

**Why this priority**: Core daily value; already partially shipped — must remain solid while expanding capabilities.

**Independent Test**: Ask about a known document; get cited answer; reload; history and preferred name fact still apply.

**Acceptance Scenarios**:

1. **Given** embeddings available and a matching document, **When** Deb asks a content question, **Then** the answer uses retrieved excerpts and lists Sources.
2. **Given** prior turns on this computer’s clerk profile, **When** she asks a follow-up, **Then** retrieval and reply respect conversation context.
3. **Given** search offline, **When** she asks a content question, **Then** the assistant says search is unavailable rather than inventing document content.

---

### User Story 2 — TIKR operations help (Priority: P1)

Paige asks “How do I save a PDF back to the NAS?” or “How do I link a packet?” The assistant answers from a **product knowledge base**, not general model guesswork.

**Why this priority**: Unlocks interactive QA on how TIKR works; currently missing.

**Independent Test**: Ask three product how-to questions; answers match help content and point to the right screen.

**Acceptance Scenarios**:

1. **Given** the product help pack is loaded, **When** the clerk asks how a TIKR feature works, **Then** the reply is based on help content and names the relevant page or control.
2. **Given** no matching help, **When** asked, **Then** the assistant admits it and offers the user guide / Call Steve path rather than inventing UI.

---

### User Story 3 — Syncfusion document workspace coaching (Priority: P1)

Deb asks how to redact, convert to PDF, or use Full Screen tools. The assistant coaches Syncfusion Document SDK workflows available in TIKR (Full Screen workspace, Smart PDF, Save to NAS).

**Why this priority**: Explicit product goal; high clerk value during packet work.

**Independent Test**: Ask about Smart Redact / Full Screen / Save to NAS; answer references TIKR’s workspace behavior.

**Acceptance Scenarios**:

1. **Given** Syncfusion help entries, **When** Deb asks about redaction or form fill, **Then** she gets step-oriented guidance aligned with Document Library Full Screen.
2. **Given** a license-limited feature, **When** relevant, **Then** the assistant notes Settings / license when help content says so.

---

### User Story 4 — Energetic interactive experience (Priority: P2)

The assistant is warm, concise, and useful: greeting/context on open, suggestion chips, and next-step offers after answers.

**Why this priority**: Turns safe Q&A into a partner without breaking grounding.

**Independent Test**: Open Assistant; see suggestions and identity-aware brief; complete a turn that ends with clear next steps.

**Acceptance Scenarios**:

1. **Given** the Assistant page loads, **When** Deb arrives, **Then** she sees who chat memory is for and suggested starter questions.
2. **Given** due-outs exist, **When** the page loads, **Then** a short proactive brief summarizes urgency without inventing items.
3. **Given** an answer is returned, **When** Deb reads it, **Then** useful next steps or related suggestions appear when appropriate.

---

### User Story 5 — Smarter retrieval and optional tools (Priority: P2)

Retrieval covers documents, vault, and product help; follow-ups refine search; optional tools can re-search when the first pack is thin.

**Why this priority**: Extends RAG quality without requiring cloud.

**Independent Test**: Ask a vague follow-up; retrieval query uses prior turns; product + town packs both considered.

**Acceptance Scenarios**:

1. **Given** a short follow-up, **When** sent, **Then** retrieval uses recent user questions to form the search query.
2. **Given** product and town hits, **When** both apply, **Then** the model is instructed which sources to prefer for how-to vs substance.

---

### Edge Cases

- Ollama offline → clear offline messaging; no fake citations.
- Unmapped machine (no Deb/Paige profile) → banner + Settings; chat may still run with ephemeral isolation.
- Empty corpus → honest “no matching documents.”
- Legal/statutory binding questions → recommend attorney / official sources.
- Large model latency → preparing state remains; no think-token flash.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Assistant MUST retrieve relevant town document and vault passages for content questions when embeddings are available.
- **FR-002**: Assistant MUST persist multi-turn chat and durable memory per clerk identity (machine-mapped Deb Dillon / Paige Lindo).
- **FR-003**: System MUST provide a TIKR operations knowledge pack (product help) searchable by the Assistant for how-to questions.
- **FR-004**: System MUST include Syncfusion Document workspace coaching content aligned with TIKR’s Full Screen / Smart PDF / convert / save flows.
- **FR-005**: System prompt MUST use an energetic, clerk-friendly voice while remaining grounded (no invented statute/fees).
- **FR-006**: Assistant UI MUST show starter suggestion chips and a proactive brief when deadline data is available.
- **FR-007**: Answers SHOULD offer 1–3 concrete next steps when helpful (open a page, attach a packet, re-ask with more detail).
- **FR-008**: When product help and town RAG both match, the prompt MUST distinguish product how-to vs town substance.
- **FR-009**: Sanitized, clerk-safe output (no chain-of-thought / tool JSON) MUST remain.
- **FR-010**: Local-first: everyday chat MUST work with Ollama only; Grok remains optional advanced path.

### Key Entities

- **ProductHelpEntry**: Title, body, keywords/tags, optional route hint.
- **AssistantContextPack**: Deadlines + document hits + vault hits + product hits for one turn.
- **ClerkIdentity**: Deb Dillon / Paige Lindo (existing machine map).

## Success Criteria *(mandatory)*

- **SC-001**: Clerks can get a cited answer from town documents in one turn when content exists.
- **SC-002**: At least 5 product how-to questions answer from help pack content (not generic hallucination).
- **SC-003**: At least 3 Syncfusion/workspace questions answer with TIKR-aligned steps.
- **SC-004**: Opening Assistant shows identity banner + ≥4 suggestion chips.
- **SC-005**: Proactive brief appears when ≥1 open due-out exists.
- **SC-006**: Follow-up questions reuse recent user text for retrieval.
- **SC-007**: Chat history and memory facts still isolate Deb vs Paige by machine.
- **SC-008**: Automated tests cover product help search, prompt packaging, and Assistant UI smoke.

## Assumptions

- Chat history / machine identity from `feature/assistant-chat-history-memory` remains the identity backbone.
- Product help may start as curated static catalog (keyword + optional embed later); full reindex of help into SQLite embeddings is a follow-on if needed.
- Tool-calling multi-hop agent loop is optional P2; first ship packs all three RAG channels into one turn.
- `llama3.2:3b` remains default; larger Assistant-only model is config-optional, not required for this feature.

## Out of scope

- Replacing Ollama with cloud-only chat.
- Full autonomous document editing without clerk confirmation.
- Live NAS backup scanning for identity (already replaced by machine map).
- Training custom weights beyond optional Modelfile.
