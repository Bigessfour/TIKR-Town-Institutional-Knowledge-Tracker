# Implementation Plan: Requirement Checklists

**Branch**: `feature/requirement-checklists` | **Spec Kit**: `009-requirement-checklists`

**Status**: Done (code + tests + docs on branch)

## Summary

Add nested playbook/checklist items under Requirements so Deb can track multi-step Election (and other) due-outs beyond Description + linked docs.

## File map

| Layer  | Paths                                                                             |
| ------ | --------------------------------------------------------------------------------- |
| Shared | Entity, DTOs, `IRequirementService` checklist methods                             |
| Infra  | EF config + migration, `RequirementService` methods, `RequirementChecklistSeeder` |
| Api    | `/api/requirements/{id}/checklist`                                                |
| Web    | Requirements.razor checklist panel + progress column; TikrApiClient               |
| Tests  | Infra/Api/Web + seeder + Playwright page-readiness Election Canvass               |

## Due guidance

`DueOffsetDays` = days **before** parent Requirement.DueDate. Optional absolute `DueDate` overrides.

## Research

See [research.md](./research.md).
