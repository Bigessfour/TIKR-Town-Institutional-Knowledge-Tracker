# Clerk correctness surface passes

Living register of **surfaces clerks touch** (nav + write paths + backing API/services). Each row must have automated proof before ship.

**Inventory source:** `.function-inventory.json` → `./scripts/update-function-inventory.sh`
**Gate:** Summary in `docs/function-inventory.generated.md` must show **0 without proof**.

| Surface                   | Route / role              | Proof                                                                                      | E2E (optional)       | Pass date  |
| ------------------------- | ------------------------- | ------------------------------------------------------------------------------------------ | -------------------- | ---------- |
| Login.razor               | `/login`                  | `LoginPageTests.cs`                                                                        | —                    | 2026-08-12 |
| Home.razor                | `/` dashboard             | `HomePageTests.cs`, `clerk-smoke.spec.ts`                                                  | dashboard + layout   | 2026-08-12 |
| Calendar.razor            | `/calendar`               | `CalendarPageTests.cs`, `page-readiness.spec.ts`                                           | schedule/grid        | 2026-08-12 |
| Requirements.razor        | `/requirements`           | `RequirementsPageTests.cs`, `RequirementsEndpointTests.cs`, `clerk-smoke.spec.ts`          | toolbar + dialog     | 2026-08-12 |
| Documents.razor           | `/documents`              | `DocumentsPageTests.cs`, `KnowledgeAndDocumentsEndpointTests.cs`, `page-readiness.spec.ts` | upload + selection   | 2026-08-12 |
| Assistant.razor           | `/assistant`              | `AssistantPageTests.cs`, `ChatHistoryEndpointTests.cs`, `page-readiness.spec.ts`           | prompt + clear       | 2026-08-12 |
| Vault.razor               | `/vault`                  | `VaultPageTests.cs`, `KnowledgeEndpointTests.cs`, `page-readiness.spec.ts`                 | tabs + copy          | 2026-08-12 |
| Settings.razor            | `/settings`               | `SettingsPageTests.cs`, `FeatureSettingsServiceTests.cs`, `page-readiness.spec.ts`         | status cards         | 2026-08-12 |
| Account.razor             | `/account`                | `AccountPageTests.cs`                                                                      | auth-gated           | 2026-08-12 |
| Users.razor               | `/settings/users` (admin) | `UsersPageTests.cs`, `AuthEndpointTests.cs`                                                | auth-gated           | 2026-08-12 |
| AuthEndpoints.cs          | `/api/auth/*`             | `AuthEndpointTests.cs`                                                                     | —                    | 2026-08-12 |
| Program.cs                | `/api/*` clerk routes     | `RequirementsEndpointTests.cs`, `KnowledgeAndDocumentsEndpointTests.cs`, …                 | —                    | 2026-08-12 |
| ChatHistoryEndpoints.cs   | `/api/assistant/*`        | `ChatHistoryEndpointTests.cs`                                                              | —                    | 2026-08-12 |
| CouncilPacketEndpoints.cs | packet/agenda helpers     | `CouncilPacketEndpointTests.cs`                                                            | —                    | 2026-08-12 |
| JwtTokenService           | auth tokens               | `JwtTokenServiceTests.cs`                                                                  | —                    | 2026-08-12 |
| DashboardService          | dashboard summary         | `DashboardServiceTests.cs`                                                                 | —                    | 2026-08-12 |
| FeatureSettingsService    | town settings             | `FeatureSettingsServiceTests.cs`                                                           | —                    | 2026-08-12 |
| HybridAiService           | Ollama/Grok/RAG           | `HybridAiServiceTests.cs`                                                                  | —                    | 2026-08-12 |
| RuntimeSecretsStore       | runtime keys              | `RuntimeSecretsStoreTests.cs`                                                              | —                    | 2026-08-12 |
| ChatClerkIdentityService  | assistant memory identity | `WebServicesProofTests.cs`                                                                 | —                    | 2026-08-12 |
| ClerkUserGuideService     | in-app guide              | `WebServicesProofTests.cs`                                                                 | —                    | 2026-08-12 |
| ClerkTourService          | walkthrough               | `ClerkTourServiceTests.cs`, `ClerkTourCatalogTests.cs`                                     | tour disabled in e2e | 2026-08-12 |

## Not clerk surfaces (demoted)

- `Knowledge.razor` — redirect to `/vault` only
- `Error.razor`, `NotFound.razor` — framework shells
- Syncfusion control attribute audit — see `syncfusion-control-audit.md` (separate from correctness proof)

## Workflow per surface

1. Read clerk guide / expected behavior for the route.
2. Add or extend bUnit or API integration test that exercises the write or read path.
3. Optionally add Playwright assertion in `page-readiness.spec.ts` or `clerk-smoke.spec.ts`.
4. Re-run `./scripts/update-function-inventory.sh` — proof column must populate.
5. Run `./scripts/done-detector.sh`.
