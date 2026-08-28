## Type

AFK

## Parent PRD

`SP-001_WE_Platform_Functional_Specification.md`

## What to build

Implement parent-teacher messaging end-to-end per SP-001 Ch.17. Parents and teachers can exchange messages about a specific student within the platform. Messages are scoped to the student context, auditable, and respect privacy rules.

Includes: Messaging Service (or module in Communication Service), schema, API, messaging UI in parent and teacher portals, and tests.

## Acceptance criteria

- [ ] Parent can send message to child's teacher; teacher can reply
- [ ] Messages scoped to a specific student context
- [ ] Teacher sees messages from parents of students in their classes only
- [ ] Message history persisted and auditable
- [ ] No messaging between unrelated users
- [ ] Tests verify messaging scope and access control

## Blocked by

- `issues/027-parent-workspace-child-progress.md`

## User stories addressed

- SP-001 Ch.17 (Communication and Collaboration Module)
- SP-001 Ch.15 (Parent Workspace — communicate with schools)
- SP-001 Ch.13 (Teacher Workspace — communicate with parents)
