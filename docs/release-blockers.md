# TIKR release blockers (E2E-known failures)

Tracked by `tests/e2e/functions-inventory.spec.ts` with `test.fail()` and `scripts/check-functions-inventory-coverage.py`.

| ID | Control | Evidence | Proposed fix |
|----|---------|----------|--------------|
| **RB-001** | Dashboard **AI Scan** | `Home.razor:88` — same `NavigateTo("/requirements")` as Add requirement; E2E expects scan UI after click | Navigate to `/requirements` with query to open AI Scan uploader, or rename button to “Open Requirements scan” |
| **RB-002** | Settings **Call Steve** (×14) | `Settings.razor:688` — `CallSteve() => Toast.Show` only; Playwright finds no `tel:`/`mailto:` link after click | Add `SteveSupportContact` in Shared config: phone + mailto button, or change label to “Show IT toast” |
| **RB-003** | Assistant chat streaming | `Assistant.razor:90` — `EnableStreaming="false"`; UX audit: RAG round-trips before progress | Set `EnableStreaming="true"` and stream tokens; show status before semantic search |
| **RB-004** | MainLayout **Try again** | `MainLayout.razor:92` — `StateHasChanged()` only on error boundary | Reload route or reset error boundary child state |
| **RB-005** | Vault **Add how-to** | No primary “New entry” on How-To tab (voice/import only); `inv-vault-add-howto` | Add “New how-to entry” opening editor with `CreateKnowledgeEntryAsync` |

**Baseline audit:** [tikr-ux-evaluation.md](./tikr-ux-evaluation.md)

Regenerate inventory after Razor changes:

```bash
python3 scripts/generate-functions-inventory.py
```