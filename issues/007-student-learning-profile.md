## Type

AFK

## Parent PRD

`docs/product/SP-001-functional-specification.md`

## What to build

Implement the Student Learning Profile (SLP) end-to-end per SP-001 Ch.5 — the central domain object of the platform. When a student is enrolled (issue 004), a profile is auto-created. Teachers can view a student's profile showing identity, enrollment, and an empty evidence timeline ready for assessment data.

Includes: Student Learning Service, PostgreSQL schema, REST API (`/api/v1/students/{id}/profile`), teacher UI profile view, and tests.

## Acceptance criteria

- [ ] SLP auto-created when student is enrolled in a class
- [ ] One profile per student (one student, one source of truth)
- [ ] Teacher can view SLP for students in their classes only
- [ ] Student can view their own profile only
- [ ] Profile page shows student info, class enrollment, and evidence timeline (empty initially)
- [ ] Integration tests verify access control and auto-creation on enrollment

## Blocked by

- `issues/004-organisation-classes-enrollment.md`

## User stories addressed

- SP-001 Ch.5 (Student Learning Profile — Core Module)
- SP-001 Ch.3 Workflow Stage 12 (Student Learning Profile Update)
- SP-001 §1.11 (Students — review progress)
