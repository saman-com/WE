## Type

AFK

## Parent PRD

`docs/product/SP-001-functional-specification.md`

## What to build

Implement assessment creation end-to-end per SP-001 Ch.8. A teacher creates an assessment for a class, links it to learning objectives and micro-skills, sets due date and instructions, and publishes it for students. Assessment exists in draft until published.

Includes: Assessment Service schema + API, teacher UI for creating/publishing assessments, linkage to LOs/micro-skills from issue 006 and class from issue 004, and tests.

## Acceptance criteria

- [ ] Teacher creates assessment in draft state linked to class, LOs, and micro-skills
- [ ] Teacher publishes assessment; published assessments visible to enrolled students
- [ ] Assessment API enforces teacher-only create/edit; students cannot create
- [ ] UI shows assessment linked LOs/micro-skills and class roster
- [ ] Unpublished assessments not visible to students
- [ ] Tests cover draft → published lifecycle and RBAC

## Blocked by

- `issues/006-learning-objectives-micro-skills.md`
- `issues/007-student-learning-profile.md`

## User stories addressed

- SP-001 Ch.8 (Assessment and Evidence Collection Module)
- SP-001 Ch.3 Workflow Stage 9 (Assessment)
- SP-001 Ch.13 (Teacher Workspace — record assessments)
