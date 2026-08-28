## Type

AFK

## Parent PRD

`SP-001_WE_Platform_Functional_Specification.md`

## What to build

Implement AI governance, audit logging, and safety filters end-to-end per SP-001 Ch.19 and Ch.20. Every AI interaction is logged immutably. Safety filters block harmful content, PII leakage, and attempts to use AI for prohibited actions (grading, discipline, overriding teacher decisions). Admin can review AI audit logs.

Includes: audit log schema, safety filter rules, admin audit log UI, and tests for blocked requests.

## Acceptance criteria

- [ ] Every AI request/response logged with user, timestamp, prompt version, and outcome
- [ ] Safety filter blocks requests containing PII in prompts or harmful content patterns
- [ ] AI cannot be used to approve evidence, assign grades, or make discipline decisions (blocked at gateway)
- [ ] Admin can view AI audit log with search/filter
- [ ] Audit logs are append-only (immutable)
- [ ] Tests verify safety filter blocks prohibited actions

## Blocked by

- `issues/023-ai-teacher-assistant-feedback.md`

## User stories addressed

- SP-001 Ch.19 (AI governance)
- SP-001 Ch.20 (Security, Privacy — audit and accountability)
- EP-001 Ch.12 (AI governance requirements)
