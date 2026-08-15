# NAS shared-repository RAG smoke (Town of Wiley Shared Documents)

The **entire** Synology share is TIKR’s document knowledge base — not a council-minutes sample set.

| Share                                     | Container mount        | Role                                                             |
| ----------------------------------------- | ---------------------- | ---------------------------------------------------------------- |
| `/volume1/Town of Wiley Shared Documents` | `/data/town-docs` (RO) | Canonical town filing library → scan → tag/embed → Assistant RAG |

Approx. scale (2026-08-15): **~114** top-level areas, **~8.4k** PDF/Word/text files under the share. Council minutes are one folder among many (budgets, ordinances, elections, CDPHE, personnel, fire, grants, taxes, etc.).

**Do you need to push to the NAS?**

| What                                       | Push?                                                                         |
| ------------------------------------------ | ----------------------------------------------------------------------------- |
| Town documents                             | **No** — already on the shared repository                                     |
| TIKR API/Web image with this branch        | **Yes** — incomplete-import retry, NAS-path folder seed, content-rich Sources |
| Optional smoke fixture `.txt` on the share | Optional — only for a known distinctive phrase                                |

---

## Pre-flight (2026-08-15)

| Check                                  | Result                                                                                 |
| -------------------------------------- | -------------------------------------------------------------------------------------- |
| API health                             | OK (`mr-storage:5050`)                                                                 |
| Ollama                                 | Available                                                                              |
| Semantic search on **existing** corpus | OK — `considered≈2131` docs already chunked; hits return topic + summary + **snippet** |
| `POST /api/library/scan`               | **Blocked** — `Access to the path '/data/town-docs' is denied`                         |

Until the bind-mount permission is fixed, new/changed files on the shared repository cannot be ingested. Assistant can still be tested against the **already embedded** subset. Fix mount ACLs/user before expecting incomplete-import retry to lift the full ~8k corpus.

---

## Knowledge target (whole share)

`TIKR_LIBRARY_SCAN_PATH=/data/town-docs` walks **recursively** — every allowed office file under the share is in scope.

Representative areas (not exhaustive):

| Share area                                                 | Expected TIKR folder (when path seed matches)                         |
| ---------------------------------------------------------- | --------------------------------------------------------------------- |
| `COUNCIL MEETINGS/…` + agenda/minutes in name              | Agenda / Minutes                                                      |
| `BUDGET COMPARISON/…`, mill levy / finance paths           | Budget / Finance                                                      |
| `RESOLUTIONS&ORDINANCE/…`                                  | Ordinances (mapper also accepts `ORDINANCES`, `MUNICIPAL CODE`)       |
| `EMPLOYEE INFO/…`, personnel/HR-named paths                | Personnel / HR                                                        |
| Contracts / agreements style folders                       | Contracts                                                             |
| Correspondence / letters style folders                     | Correspondence                                                        |
| Forms / permit application trees                           | Forms                                                                 |
| Everything else (CDPHE, grants, elections, fire, taxes, …) | Heuristic + LLM / General — still must **embed and retrieve content** |

Path seeding improves classification; it does **not** limit which folders are knowledge. Unmapped trees still import, OCR, chunk, and answer Assistant questions from **excerpts**.

---

## Deploy branch, then smoke

1. Ship `fix/library-scan-corpus-quality` (or successor) to NAS images.
2. `./scripts/deploy-tikr-nas.sh <version>`
3. Fix `/data/town-docs` read permission for the API container; confirm `USE_SYNCFUSION_AGENT_TOOLS=true`, `TIKR_OCR_ENABLED=true`.
4. Optional: recreate `tikr-clerk` from the Wiley Modelfile.
5. Optional distinctive fixture **at share root** (not “council-only”):

```bash
ssh mr-storage 'cat > "/volume1/Town of Wiley Shared Documents/TIKR-RAG-SMOKE.txt" <<EOF
Town of Wiley shared-repository RAG smoke fixture.
Distinctive phrase: aqueduct levy schedule Q3 Wiley smoke test.
EOF'
```

---

## Automated API smoke

```bash
./scripts/nas-library-rag-smoke.sh
```

Default query is broad town language. Cross-domain checks:

```bash
# Shared-repo breadth (run separately; each must return snippet hits when corpus is ready)
TIKR_RAG_SMOKE_QUERY='Board of Trustees minutes' ./scripts/nas-library-rag-smoke.sh
TIKR_RAG_SMOKE_QUERY='mill levy budget' ./scripts/nas-library-rag-smoke.sh
TIKR_RAG_SMOKE_QUERY='ordinance municipal code' ./scripts/nas-library-rag-smoke.sh
TIKR_RAG_SMOKE_QUERY='aqueduct levy schedule Q3 Wiley smoke test' ./scripts/nas-library-rag-smoke.sh
```

**Pass (script):** health OK · Ollama up · corpus-health readable · ≥1 semantic hit **with non-empty snippet**. Scan may warn until mount perms are fixed.

---

## Manual clerk checklist (shared repository)

| #   | Step                                                                                                          | Pass? |
| --- | ------------------------------------------------------------------------------------------------------------- | ----- |
| 1   | Settings → library path is the **town shared documents** mount                                                |       |
| 2   | Scan / poller can read the share (no access-denied)                                                           |       |
| 3   | Corpus health: coverage vs total; needs-attention = OCR/text gaps across **all** areas, not only council      |       |
| 4   | Ask Assistant a **minutes** question → Sources with excerpt                                                   |       |
| 5   | Ask a **budget / mill levy** question → different filing, excerpt present                                     |       |
| 6   | Ask an **ordinance / permit / CDPHE / election** style question from a known share area → substance + Sources |       |
| 7   | Ask something absent from the share → no invented town procedure                                              |       |

Success = Assistant answers from **shared-repository content** with Sources that show passage text. Council minutes are one proof path, not the product boundary.

---

## Interpreting results

| Result                              | Meaning                                 | Action                                                        |
| ----------------------------------- | --------------------------------------- | ------------------------------------------------------------- |
| ~2k considered, share has ~8k files | Large uningestable or sparse remainder  | Fix scan mount; let incomplete retry + OCR run; Corpus health |
| Hits with snippets across domains   | Shared-repo RAG healthy                 | UI Sources check                                              |
| Only one folder family ever hits    | Retrieval/corpus skew or query bias     | Try domain queries above; check folder facets                 |
| Scan access denied                  | Container cannot read shared repository | Fix bind-mount permissions first                              |
| Filename-only Sources               | Pre-branch UI, or empty excerpts        | Deploy content-rich Sources branch; fix sparse OCR            |

---

## What not to do

- Do not treat “a few council minutes” as the knowledge target
- Do not hand-copy the share into TIKR storage
- Do not use Reindex to invent text for unscannable/sparse files
- Do not expect CI alone to prove shared-repository RAG

---

## After smoke

Record: date, image tag, `documentsTotal` / `documentsWithChunks` / sparse count, and that Assistant Sources showed excerpts for **at least two different share domains** (e.g. council + budget).
