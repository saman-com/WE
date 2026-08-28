## Type

AFK

## Parent PRD

`docs/product/SP-001-functional-specification.md`

## What to build

Connect the EI dashboard to intervention creation end-to-end. From the teacher EI insights dashboard (issue 018), a teacher identifies a learning gap and creates an intervention in one flow — pre-populating gap details, target micro-skills, and suggested actions. This completes the diagnostic → gap → intervention workflow.

Includes: UI flow from gap card → create intervention form, API integration, and tests.

## Acceptance criteria

- [ ] Teacher can click a learning gap on EI dashboard and start intervention creation
- [ ] Intervention form pre-populated with gap details, student, and micro-skills
- [ ] Created intervention appears in intervention list (issue 019) and on student SLP
- [ ] Flow works end-to-end without leaving teacher workspace
- [ ] Teacher remains decision-maker — system suggests, teacher confirms
- [ ] Integration test covers gap → intervention creation flow

## Blocked by

- `issues/019-intervention-management.md`
- `issues/018-ei-teacher-insights-dashboard.md`

## User stories addressed

- SP-001 Ch.12 (Intervention Management)
- SP-001 Ch.13 (Teacher Workspace — create interventions)
- SP-001 Ch.3 Workflow Stages 16–17 (Teacher Review, Personalised Intervention)
