## Type

AFK

## Parent PRD

`SP-001_WE_Platform_Functional_Specification.md`

## What to build

Implement the Teacher Workspace class dashboard end-to-end per SP-001 Ch.13. A teacher logs in and sees their classes, roster per class, recent assessments, and a summary view of each student's SLP (evidence count, latest activity). This is the primary teacher interface for day-to-day use.

Includes: teacher portal pages (class list, class detail, student SLP summary), API aggregation endpoints, and tests.

## Acceptance criteria

- [ ] Teacher dashboard lists all assigned classes
- [ ] Class detail shows roster with link to each student's SLP summary
- [ ] Recent assessments for the class displayed with submission/review status
- [ ] Teacher cannot see classes or students outside their assignments
- [ ] UI is navigable and loads within 2 seconds on local stack
- [ ] Integration tests verify teacher scope isolation

## Blocked by

- `issues/010-evidence-immutability-slp-update.md`

## User stories addressed

- SP-001 Ch.13 (Teacher Workspace Module)
- SP-001 §1.11 (Teachers — plan learning, assess, analyse evidence)
