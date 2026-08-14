# Contract: Contacts API

**Feature**: `007-contacts-inventory`

Base path: `/api` (auth-gated when `TIKR_AUTH_ENABLED`).

## Contacts

| Method | Route | Body / query | Response |
| --- | --- | --- | --- |
| GET | `/contacts` | `q?`, `category?` (int flags), `deleted?` bool | `ContactDto[]` |
| GET | `/contacts/{id}` | | `ContactDto` or 404 |
| POST | `/contacts` | `CreateContactRequest` | 201 `ContactDto` |
| PUT | `/contacts/{id}` | `UpdateContactRequest` | 200 `ContactDto` |
| DELETE | `/contacts/{id}` | | 204 soft-delete |
| POST | `/contacts/{id}/restore` | | 200 `ContactDto` |

## Requirement links

| Method | Route | Query / body | Response |
| --- | --- | --- | --- |
| GET | `/requirements/{id}/contacts` | | `ContactDto[]` (with IsPrimary on link DTO if needed) |
| POST | `/requirements/{id}/contacts/{contactId}` | `primary?=true` | 204 |
| DELETE | `/requirements/{id}/contacts/{contactId}` | | 204 |

Link as primary copies Contact Name/Email/Phone onto Requirement denormalized fields.

## DTOs

```csharp
ContactDto(Id, Name, Role, Organization, Address, Office, Email, Phone, Notes, Categories, CreatedAt, UpdatedAt, CreatedBy, DeletedAt, IsPrimary?);
CreateContactRequest(Name, Role?, Organization?, Address?, Office?, Email?, Phone?, Notes?, Categories);
UpdateContactRequest(...same...);
```
