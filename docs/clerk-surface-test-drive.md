# Clerk surface test drive (runtime evaluation)

Car-lot style walkthrough of every primary clerk surface: **navigate → interact → store/retrieve → verify logs**.

Use when validating a local stack before `/code-review` or a PR. Pair with the inventory canvas (observability + test gaps).

## Prerequisites

| Check  | Expect                                                               |
| ------ | -------------------------------------------------------------------- |
| API    | `http://localhost:5001` listening (`TIKR_DATA_PATH` → `.local-data`) |
| Web    | `http://localhost:8080` with `TIKR_API_URL=http://localhost:5001`    |
| Ollama | `http://127.0.0.1:11434` (Assistant + NL calendar)                   |
| Logs   | `.local-data/logs/tikr-web-YYYYMMDD.log` and `tikr-YYYYMMDD.log`     |
| Tools  | chrome-devtools MCP: `navigate`, `evaluate`, `screenshot`            |

Auth is **off** by default — skip Login/Users unless `AuthEnabled=true`.

### Log audit (after each surface or at end)

```bash
rg "Action UI\." .local-data/logs/tikr-web-*.log | tail -80
```

Pass = `started` then `completed` (or intentional `failed` with clear Error=). Fail = silent UI success with no log, or ERR without clerk-visible message.

### chrome-devtools helpers

```js
// Click by accessible name (partial match)
(() => {
  const name = "ADD_NAME";
  const el = [...document.querySelectorAll("a,button,[role=button]")]
    .find(e => (e.innerText || e.getAttribute("aria-label") || "").includes(name));
  if (!el) return "NOT_FOUND:" + name;
  el.click();
  return "CLICKED:" + name;
})()
```

```js
// Fill first visible textbox / contenteditable matching placeholder or label
(() => {
  const hint = "PLACEHOLDER_OR_LABEL";
  const input = [...document.querySelectorAll("input,textarea,[contenteditable=true]")]
    .find(e => {
      const p = (e.getAttribute("placeholder") || "") + (e.getAttribute("aria-label") || "");
      return p.toLowerCase().includes(hint.toLowerCase()) || e.offsetParent !== null;
    });
  if (!input) return "NOT_FOUND";
  input.focus();
  if ("value" in input) {
    input.value = "VALUE";
    input.dispatchEvent(new Event("input", { bubbles: true }));
  } else {
    input.textContent = "VALUE";
    input.dispatchEvent(new InputEvent("input", { bubbles: true }));
  }
  return "FILLED";
})()
```

Take a `screenshot` after each surface’s critical action for evidence.

---

## Scorecard (fill during run)

| #   | Surface                | Interact | Store/retrieve | Log Pass | Notes |
| --- | ---------------------- | -------- | -------------- | -------- | ----- |
| 0   | Nav smoke              |          | n/a            |          |       |
| 1   | Dashboard              |          |                |          |       |
| 2   | Requirements           |          |                |          |       |
| 3   | Calendar               |          |                |          |       |
| 4   | Documents              |          |                |          |       |
| 5   | Vault                  |          |                |          |       |
| 6   | Assistant              |          |                |          |       |
| 7   | Settings               |          |                |          |       |
| 8   | Cross-cut (theme/help) |          | n/a            | optional |       |

---

## Step 0 — Nav smoke

1. `navigate` → `http://localhost:8080/`
2. For each nav link, `evaluate` click: Dashboard, Calendar, Requirements, Documents, AI Assistant, Knowledge Vault, Settings.
3. Confirm URL and page heading each time.
4. Expected logs: `UI.*.Load started` / `completed` (or `failed` if API down — fix API first).

**Pass:** every primary route loads without blank main content.

---

## Step 1 — Dashboard (`/`)

| Action                               | How                    | Expect UI                                 | Expect log                           |
| ------------------------------------ | ---------------------- | ----------------------------------------- | ------------------------------------ |
| Load                                 | navigate `/`           | Due-out / priority content or empty state | `UI.Dashboard.Load completed`        |
| Reset layout                         | click **Reset layout** | Layout restores                           | `UI.Dashboard.ResetLayout completed` |
| Open workspace (if due row has docs) | open linked workspace  | Dialog/workspace                          | `UI.Dashboard.OpenWorkspace`         |

**Retrieve:** priorities reflect Requirements data (same titles as `/requirements`).

---

## Step 2 — Requirements (`/requirements`) — write path

| Action            | How                                                                  | Expect UI                      | Expect log                                        |
| ----------------- | -------------------------------------------------------------------- | ------------------------------ | ------------------------------------------------- |
| Load              | navigate                                                             | Grid with seeded + custom rows | `UI.Requirements.Load completed Count=`           |
| Create            | **Add requirement** → title `TD-Drive-{timestamp}` → due date → save | Row appears                    | `UI.Requirements.Create completed`                |
| Update            | edit same row (title or done) → save                                 | Grid updates                   | `UI.Requirements.Update completed`                |
| Export            | **Export CSV**                                                       | Download/toast                 | `UI.Requirements.ExportCsv completed`             |
| Packet (optional) | **Council packet**                                                   | Success or SDK message         | `UI.Requirements.GeneratePacket` completed/failed |

**Do not delete seeded Colorado rows.** Delete only the `TD-Drive-*` row if cleaning up → `UI.Requirements.Delete`.

**Store proof:** reload page; `TD-Drive-*` still present → `Load completed` again.

---

## Step 3 — Calendar (`/calendar`) — reflect + create

| Action               | How                                                  | Expect UI                | Expect log                                    |
| -------------------- | ---------------------------------------------------- | ------------------------ | --------------------------------------------- |
| Load                 | navigate                                             | Schedule shows deadlines | `UI.Calendar.Load completed`                  |
| Spot prior create    | find `TD-Drive-*` on calendar                        | Event visible            | (from Load counts)                            |
| Create (optional)    | double-click day or editor → new subject             | Event saved              | `UI.Calendar.Create completed`                |
| NL create (optional) | NL prompt e.g. “file mill levy reminder next Friday” | New deadline             | `UI.Calendar.NaturalLanguageCreate completed` |

**Retrieve:** open `/requirements` — calendar-created item listed.

---

## Step 4 — Documents (`/documents`) — storage + retrieval

| Action                     | How                                              | Expect UI            | Expect log                                               |
| -------------------------- | ------------------------------------------------ | -------------------- | -------------------------------------------------------- |
| Load                       | navigate                                         | Library list / tree  | `UI.Documents.Load completed Count=`                     |
| Upload                     | upload a small PDF/TXT named `td-drive-sample.*` | Row appears          | `UI.Documents.Upload completed`                          |
| Preview                    | select row / open preview                        | Side or main preview | `UI.Documents.Preview` + `PdfPreview` / `SidePdfPreview` |
| Download                   | context **Download**                             | Browser download     | `UI.Documents.Download completed`                        |
| Semantic search (optional) | toggle semantic → query                          | Hits or empty        | Load/search path; no ERR flood                           |

**Store proof:** hard refresh; document still listed. Prefer soft-delete cleanup over purge unless intentional.

---

## Step 5 — Vault (`/vault`) — succession path (highest UI-test gap)

| Action              | How                                          | Expect UI            | Expect log                           |
| ------------------- | -------------------------------------------- | -------------------- | ------------------------------------ |
| Load                | navigate                                     | Banner + entries     | `UI.Vault.Load completed`            |
| Save entry          | add/edit how-to titled `TD-Drive vault note` | Saved toast / list   | `UI.Vault.SaveEntry completed`       |
| Copy for new clerk  | **Copy Everything for New Clerk**            | Clipboard / feedback | `UI.Vault.CopyForNewClerk completed` |
| Handover (optional) | **Generate Complete Handover Package**       | PDF download         | `UI.Vault.HandoverPackage completed` |

**Retrieve:** reload; entry still there. Paste clipboard contains note title.

---

## Step 6 — Assistant (`/assistant`)

| Action           | How                                                | Expect UI           | Expect log                                            |
| ---------------- | -------------------------------------------------- | ------------------- | ----------------------------------------------------- |
| Session          | navigate                                           | Chat UI             | `UI.Assistant.Session`                                |
| Prompt           | ask: “What is on my deadline calendar this month?” | Streamed answer     | `UI.Assistant.Prompt started` → `Route` → `completed` |
| Clear (optional) | clear conversation                                 | Empty thread + note | `UI.Assistant.ClearConversation`                      |

**Pass:** `Path=` / `Route=` present; no unexplained Failed after Completed.

---

## Step 7 — Settings (`/settings`)

| Action                    | How                     | Expect UI             | Expect log                                |
| ------------------------- | ----------------------- | --------------------- | ----------------------------------------- |
| Load                      | navigate                | Ollama/SDK/NAS health | `UI.Settings.Load completed`              |
| Clerk override (if shown) | set display name → save | Banner updates        | `UI.Settings.SaveClerkOverride completed` |
| Scan library (optional)   | **Scan** / library scan | Progress + result     | `UI.Settings.ScanLibrary completed`       |
| Reindex (optional, slow)  | reindex embeddings      | Counts                | `UI.Settings.ReindexEmbeddings completed` |

Avoid toggling production-breaking feature flags on a shared NAS DB without intent.

---

## Step 8 — Cross-cutting (quick)

1. Theme: Light → Dark → Light (sidebar). Persists after navigate.
2. Page help (?) on one page — tooltip/panel opens.
3. Footer shows local NAS / API status (not stuck offline).

No required `UI.*` lines; note any circuit errors in web log.

---

## Final log gate

```bash
# Per-surface completed loads
rg "Action UI\.(Dashboard|Calendar|Requirements|Documents|Assistant|Vault|Settings)\.Load completed" \
  .local-data/logs/tikr-web-*.log

# Write actions from this drive
rg "Action UI\.(Requirements\.(Create|Update)|Documents\.(Upload|Preview|Download)|Vault\.(SaveEntry|CopyForNewClerk)|Assistant\.Prompt completed|Calendar\.Create)" \
  .local-data/logs/tikr-web-*.log
```

**Ship confidence for Layer 3:** every primary surface has a Load completed; Requirements create + Documents preview + Vault save + Assistant prompt each have a completed line from this session.

---

## Agent execution notes

1. Prefer `navigate` over brittle SPA clicks when Syncfusion overlays steal focus.
2. After write actions, wait 1–2s before grepping logs (Serilog flush).
3. If `Connection refused (localhost:5001)` appears in Failed logs, restart API before continuing.
4. File uploads may need OS file picker — if chrome-devtools cannot attach files, mark Upload as **manual** and still prove Preview/Download on an existing document.
5. Do not commit secrets, screenshots with PII, or `.local-data` into git.

## Related

- [ai-tooling.md](ai-tooling.md) — `TikrActionLog` surface table
- [function-tree.md](function-tree.md) — clerk surface map
- [action-items.md](action-items.md) — curated proof checklist
- Automated: `dotnet test TIKR.sln --configuration Release` (Layers 1–2)
