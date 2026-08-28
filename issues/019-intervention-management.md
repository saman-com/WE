## Type

AFK

## Parent PRD

`SP-001_WE_Platform_Functional_Specification.md`

## What to build

Implement Intervention Management end-to-end per SP-001 Ch.12. Teachers create, track, and close interventions for students. An intervention records the target gap, planned actions, responsible teacher, timeline, and outcome. Interventions link to learning gaps from issue 016.

Includes: Intervention Service, schema, CRUD API, teacher UI for intervention list/detail, status workflow (planned → active → completed → closed), and tests.

## Acceptance criteria

- [ ] Teacher creates intervention linked to a student and learning gap
- [ ] Intervention has status lifecycle: planned → active → completed → closed
- [ ] Teacher can update intervention notes and mark complete
- [ ] Interventions visible on student SLP
- [ ] Only assigned teacher or school leader can modify intervention
- [ ] Tests cover CRUD and status transitions

## Blocked by

- `issues/016-learning-gap-engine.md`

## User stories addressed

- SP-001 Ch.12 (Intervention Management Module)
- SP-001 Ch.3 Workflow Stage 17 (Personalised Intervention)
