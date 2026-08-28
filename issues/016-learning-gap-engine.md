## Type

AFK

## Parent PRD

`SP-001_WE_Platform_Functional_Specification.md`

## What to build

Implement the Learning Gap Engine end-to-end per SP-001 Ch.10. Using diagnostics from issue 015, identify gaps between expected and demonstrated learning per micro-skill. Each gap has severity (low/medium/high) and urgency. Gaps feed intervention planning.

Includes: Learning Gap Service, schema, gap calculation rules, API, teacher UI showing gaps per student, and tests.

## Acceptance criteria

- [ ] Gaps calculated from diagnostics: expected vs actual mastery per micro-skill
- [ ] Each gap has severity, urgency, and linked micro-skill/LO
- [ ] Teacher can view learning gaps for students in their classes
- [ ] Gaps update when new evidence is approved (event-driven)
- [ ] Gap explanations are human-readable (not black-box)
- [ ] Tests verify gap calculation from known diagnostic inputs

## Blocked by

- `issues/015-diagnostic-engine.md`

## User stories addressed

- SP-001 Ch.10 (Learning Gap Engine Module)
- SP-001 Ch.3 Workflow Stage 13 (Learning Gap Analysis)
