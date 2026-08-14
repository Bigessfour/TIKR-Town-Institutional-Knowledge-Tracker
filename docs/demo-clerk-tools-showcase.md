# Clerk tools showcase — presenter script

**Audience:** Deb walkthrough, CML peers, Code Platoon / Syncfusion tech review
**Duration:** 20–25 minutes (or 12 minutes if skipping Word/Excel + Grok)
**In-app hub:** [/demo/clerk-tools](../src/TIKR.Web/Components/Pages/ClerkToolsShowcase.razor) (sidebar **Tools tour**, or Settings → Clerk tools tour)
**Related:** [demo-deb.md](demo-deb.md) (pitch script), [demo-code-platoon.md](demo-code-platoon.md) (API matrix)

---

## Does Syncfusion suggest a demo process?

Yes — but not a municipal product script. From Syncfusion Blazor docs / Sample Browser guidance:

| Syncfusion suggestion              | What it means                                                      | TIKR mapping                                                               |
| ---------------------------------- | ------------------------------------------------------------------ | -------------------------------------------------------------------------- |
| **Sample Browser / live demos**    | Explore default functionalities per control                        | Use real TIKR pages instead of a control zoo                               |
| **Getting-started GitHub samples** | License, themes CSS, package scripts, `AddSyncfusionBlazor`        | Already in `Program.cs` + `App.razor`                                      |
| **Document lifecycle**             | Organize → Open → Work → Save                                      | Browse File Manager → Full Screen → annotate/edit/AssistView → Save to NAS |
| **PDF path**                       | Open/Load → annotate → form fill → toolbar                         | `SfSmartPdfViewer` workspace (+ AssistView on)                             |
| **Smart AI path**                  | `IChatClient` + `InjectOpenAIInference` then enable Smart controls | Ollama → Smart Paste / TextArea / PDF AssistView                           |
| **Blazor Playground**              | Quick experiments outside the app                                  | Optional for IT only                                                       |

TIKR’s showcase follows that lifecycle **inside clerk jobs**, not as a Syncfusion marketing sample.

---

## Before you walk on stage

| Check           | How                                                         |
| --------------- | ----------------------------------------------------------- |
| Web up          | http://localhost:8080 (or NAS)                              |
| Ollama          | Settings → Connected                                        |
| License         | Settings → Forms & documents license check OK               |
| Sample PDF      | Upload `tests/fixtures/agent-scan/minimal-clerk-report.pdf` |
| Optional Office | One `.docx` and one `.xlsx` in the library                  |
| Hub open        | Sidebar → **Tools tour**                                    |

---

## Acts (follow the hub cards)

| #   | Act                      | Say                                                           | Do                                                            |
| --- | ------------------------ | ------------------------------------------------------------- | ------------------------------------------------------------- |
| 1   | Dashboard                | “What is due today without digging binders.”                  | Priorities + grid                                             |
| 2   | Requirements             | “Colorado seeds + election playbook; AI suggests, I approve.” | Election Canvass checklist; Smart Paste/TextArea if Ollama up |
| 3   | Calendar                 | “Same deadlines on a calendar clerks already know.”           | Month/Week/Agenda                                             |
| 4   | Documents / File Manager | “Organize on the NAS.”                                        | Library upload + Browse mode File Manager                     |
| 5   | Smart PDF                | “Annotate, ask the PDF, save to NAS.”                         | Full Screen → AssistView summary → Save to NAS                |
| 6   | Word / Spreadsheet       | “Edit letters and sheets without leaving TIKR.”               | Open `.docx` / `.xlsx` → edit → Save                          |
| 7   | Assistant                | “Day-to-day stays local.”                                     | Ask weekly priorities                                         |
| 8   | Vault                    | “Day-one memory for the next clerk.”                          | Add Election contact                                          |
| 9   | Settings / themes        | “Local AI, license, contrast.”                                | Theme toggle + status                                         |

**Smart Redact / Smart Fill:** intentionally off in UI for municipal safety. Say “available when we turn them on for a controlled form/redact workflow” — do not enable live with real PII.

---

## Live checklist

- [ ] Tools tour page loads (`/demo/clerk-tools`)
- [ ] Each Open button lands on the right route
- [ ] Act 5: AssistView answers with Ollama
- [ ] Act 5: Save to NAS toast
- [ ] Act 2: Playbook checkbox persists
- [ ] Theme swap visible on Grid / AssistView

---

## After demo

Return to [demo-deb.md](demo-deb.md) closing (local-first, NAS, Call Steve).
Catalog source of truth: `ClerkToolsShowcaseCatalog` in `TIKR.Shared` (keeps UI + script aligned).
