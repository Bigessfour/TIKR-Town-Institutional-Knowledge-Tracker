# Implementation Plan: Contacts Inventory

**Branch**: `feature/contacts-inventory` | **Spec Kit**: `007-contacts-inventory` | **Date**: 2026-08-14

**Spec**: [spec.md](./spec.md)

## Summary

Add first-class Contact inventory (EF + Minimal API + Vault UI) with many-to-many Requirement links, soft-delete, audit/logging, Election seeds, and proof-of-function tests. Keep denormalized Requirement contact fields and SubmitTo.

## Technical approach

Layered .NET 10: Shared entities/DTOs/interfaces → Infrastructure ContactService + migration + seeder → Api `/api/contacts` + link routes → Web Vault/Requirements/Calendar + TikrApiClient.

## File map

| Layer | Paths |
| --- | --- |
| Shared | `Enums/ContactCategory.cs`, `Entities/Contact.cs`, `Entities/RequirementContact.cs`, `DTOs/ContactDto.cs`, `Interfaces/IContactService.cs` |
| Infra | `Data/TikrDbContext.cs`, migration, `Services/ContactService.cs`, `DbSeeder.cs` / `ContactSeeder.cs`, `DependencyInjection.cs` |
| Api | `Program.cs` contacts + requirement contact endpoints |
| Web | `TikrApiClient.cs`, `Vault.razor`, `Requirements.razor`, `Calendar.razor` |
| Tests | `ContactServiceTests`, `ContactsEndpointTests`, Vault/Requirements bUnit |
| Docs | `incremental-plan.md` Phase 11, `action-items.md`, function inventory, README |

## Auth

`/api` group already applies `RequireAuthorization` when `TIKR_AUTH_ENABLED` — no per-route auth duplication required.
