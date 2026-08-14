# Data Model: Contacts Inventory

**Feature**: `007-contacts-inventory` | **Date**: 2026-08-14

## ContactCategory (flags enum)

| Flag | Value |
| --- | --- |
| None | 0 |
| Budget | 1 |
| Audit | 2 |
| MillLevy | 4 |
| Compliance | 8 |
| Election | 16 |
| Custom | 32 |

Stored as integer on `Contact.Categories`.

## Contact

| Field | Type | Rules |
| --- | --- | --- |
| Id | Guid | PK |
| Name | string | Required, max 200 |
| Role | string? | Title, max 200 |
| Organization | string? | max 300 |
| Address | string? | max 500 |
| Office | string? | max 200 |
| Email | string? | max 200 |
| Phone | string? | max 50 |
| Notes | string? | max 2000 |
| Categories | ContactCategory | flags |
| CreatedAt / UpdatedAt | DateTime | UTC |
| CreatedBy | string? | user id when auth |
| DeletedAt | DateTime? | soft-delete |

## RequirementContact

| Field | Type | Rules |
| --- | --- | --- |
| RequirementId | Guid | PK part, FK |
| ContactId | Guid | PK part, FK |
| LinkedAt | DateTime | UTC |
| IsPrimary | bool | primary syncs Requirement ContactName/Email/Phone |

## Relationships

```text
Requirement 1──* RequirementContact *──1 Contact
Requirement still owns SubmitTo + denormalized ContactName/Email/Phone
```

## Unchanged

- `Requirement.SubmitTo`, `ContactName`, `ContactEmail`, `ContactPhone`
- `KnowledgeEntry` with `KnowledgeCategory.Contact` (legacy Vault notes)
