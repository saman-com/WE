## Type

AFK

## Parent PRD

`docs/product/SP-001-functional-specification.md`

## What to build

Implement student assessment submission end-to-end per SP-001 Ch.14. A student views published assessments for their class, completes and submits responses, and sees submission confirmation. Submissions are stored but not yet official evidence (teacher review comes in issue 010).

Includes: submission schema in Assessment Service, student API endpoints, student portal UI for viewing and submitting assessments, and tests.

## Acceptance criteria

- [ ] Student sees only published assessments for their enrolled classes
- [ ] Student can submit responses before due date; late submission behavior documented
- [ ] Submission stored with timestamp and student ID; status = `submitted` (pending review)
- [ ] Student cannot edit submission after submit
- [ ] Student portal UI shows assessment list and submission form
- [ ] Tests verify student cannot access other students' submissions

## Blocked by

- `issues/008-teacher-creates-assessment.md`

## User stories addressed

- SP-001 Ch.14 (Student Workspace — submit assessments)
- SP-001 Ch.8 (Assessment and Evidence Collection)
- SP-001 Ch.3 Workflow Stage 8 (Student Participation)
