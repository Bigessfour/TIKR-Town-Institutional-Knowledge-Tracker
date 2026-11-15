# TIKR release blockers (E2E-known failures)

Tracked by `tests/e2e/functions-inventory.spec.ts` with `test.fail()` and `scripts/check-functions-inventory-coverage.py`.

**Production walkthrough reference:** `main` @ `e1c5399` (live NAS). See also [tikr-ux-evaluation.md](./tikr-ux-evaluation.md).

| ID | Area | Control / symptom | Evidence | Proposed fix |
|----|------|-------------------|----------|--------------|
| **RB-001** | Dashboard | **AI Scan** | `Home.razor:88` — `NavigateTo("/requirements")` only | Open scan uploader via query or rename control |
| **RB-002** | Settings | **Call Steve** (~10–14) | `Settings.razor:688` — toast only | `tel:`/`mailto:` or honest label |
| **RB-003** | Assistant | No immediate progress; buffered chat | `Assistant.razor:90` `EnableStreaming="false"`; RAG before status | Stream tokens; status before semantic search |
| **RB-004** | Layout | **Try again** on error boundary | `MainLayout.razor:92` — `StateHasChanged()` | Reload route / reset child |
| **RB-005** | Vault | No **New how-to / tribal** entry | Editor only on edit; empty state dead-end | Add create button + API |
| **RB-006** | Assistant | **~140s → “Response stopped.”** | `HttpClient` default **100s**; status + **2× semantic search** before Ollama; `OperationCanceledException` → stopped HTML; Grok only on **empty** reply | Raise timeout; progress first; retry copy; Grok on timeout/cancel |
| **RB-007** | Assistant | Prompt resets to default | `Prompt="What should I be working on this week?"` on `SfAIAssistView` | Clear `Prompt` after bind or use placeholder-only |
| **RB-008** | Guide / tour | **Ask Advanced** documented | `clerk-user-guide.md:41`, `ClerkTourCatalog.cs:84` | Remove references; match auto-Grok |
| **RB-009** | Calendar | **Create deadline** looks enabled | `Disabled` when empty but weak affordance | Visible disabled styling + hint |
| **RB-010** | Calendar | Deadlines **midnight–1AM** not all-day | `CalendarEvent` start/end mapping | All-day `IsAllDay` + timezone |
| **RB-011** | Calendar | Recurring deadlines don’t roll | Recurrence update in Infrastructure | Advance next occurrence after due |
| **RB-012** | Documents | **Row click** no-op | `CheckboxOnly="true"` on grid selection | Row click selects + preview |
| **RB-013** | Documents | Upload time **UTC as local** in grid | `UploadedAt` column `Format="g"` without `ToLocalTime()` | Localize grid column like preview pane |
| **RB-014** | Guide | **User guide** won’t reopen | `ClerkUserGuideDialog` / `SfDialog` state | Fix second `OpenRequested` |
| **RB-015** | All pages | **Page help (i)** click no-op | `PageHelp.razor` — tooltip only | Drawer/dialog on click |
| **RB-016** | Dashboard | **Upload document** no picker | `Home.razor:89` navigates with query only | Focus file input / open chooser |
| **RB-017** | Settings | **Reindex** missing on prod | Code has `Settings.razor:425`; prod walkthrough gap | Verify deployment; surface when API up |
| **RB-018** | Requirements | **Print council packet** prints page | `PrintPacketAsync` → `JS.InvokeVoidAsync("print")` | Print packet template/PDF |
| **RB-019** | Settings | Two tour buttons → same tour | `PageTourButton` + `ReplayTourAsync` overlap | Distinct tours or single entry |
| **RB-020** | **Security** | **`/account` HTTP 500**; **no auth** on LAN; Keys open | `Account.razor` `[Authorize]` when auth off; `Routes.razor` | Enable auth in prod; fix account route |
| **RB-021** | Settings | **Ready** + **Ollama offline** together | Status check server ping only | Single consolidated health |
| **RB-022** | A11y | **Overdue** contrast **3.06:1** | `.priority-overdue` on dashboard strip | Bump contrast ≥ 4.5:1 |
| **RB-023** | A11y | Grid filters labelled **`_Filter`** | Syncfusion filter bar defaults | Set `aria-label` per column |
| **RB-024** | Dashboard | **Add requirement** navigate only | `Home.razor:87` | Open create dialog on arrival |

## Ops notes (prod walkthrough)

- Model config `llama3.2:3b` resolves to **qwen2.5:7b** on CPU; single Ollama request → chat queues behind indexing.
- E2E uses **Docker stub** for deterministic assistant; production failures above stay `test.fail` until fixed.

Regenerate inventory:

```bash
python3 scripts/generate-functions-inventory.py
```
