## Type

AFK

## Parent PRD

`docs/product/SP-001-functional-specification.md`

## What to build

Implement the Student Workspace progress view end-to-end per SP-001 Ch.14. A student logs in and sees their learning progress: pending assessments, submitted work, teacher feedback on approved evidence, and SLP timeline. Learning should be visible and motivating.

Includes: student portal pages (dashboard, assessment list, feedback view, progress timeline), API endpoints, and tests.

## Acceptance criteria

- [ ] Student dashboard shows pending and completed assessments
- [ ] Student can view teacher feedback on approved evidence
- [ ] SLP progress timeline visible to student (their data only)
- [ ] Student cannot see other students' data or teacher-only views
- [ ] UI shows what they are learning and what success looks like (linked LOs)
- [ ] Integration tests verify student-only data access

## Blocked by

- `issues/010-evidence-immutability-slp-update.md`

## User stories addressed

- SP-001 Ch.14 (Student Workspace Module)
- SP-001 §1.11 (Students — review progress, receive feedback)
