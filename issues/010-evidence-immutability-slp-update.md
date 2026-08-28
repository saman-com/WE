## Type

AFK

## Parent PRD

`SP-001_WE_Platform_Functional_Specification.md`

## What to build

Implement the evidence approval workflow end-to-end per SP-001 Ch.8 and Ch.5. A teacher reviews a student submission, records marks/feedback per micro-skill, and approves it — creating immutable educational evidence that updates the Student Learning Profile. Approved evidence cannot be modified or deleted.

Includes: Evidence Service (or module), approval API, teacher review UI, SLP timeline update, immutability enforcement at DB/API layer, and tests.

## Acceptance criteria

- [ ] Teacher reviews submission and approves with marks/feedback per micro-skill
- [ ] Approval creates an evidence record linked to SLP, assessment, and micro-skills
- [ ] Approved evidence is immutable — PUT/DELETE on approved evidence returns 403
- [ ] SLP timeline shows new evidence entry after approval
- [ ] Only teachers (not students or AI) can approve evidence
- [ ] Tests verify immutability and SLP update on approval

## Blocked by

- `issues/009-student-submits-assessment.md`

## User stories addressed

- SP-001 Ch.8 (Educational Evidence Collection)
- SP-001 Ch.5 (Student Learning Profile Update)
- SP-001 Ch.3 Workflow Stages 10, 12 (Evidence Collection, SLP Update)
