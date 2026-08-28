## Type

AFK

## Parent PRD

`SP-001_WE_Platform_Functional_Specification.md`

## What to build

Implement the EI teacher insights dashboard end-to-end per SP-001 Ch.11. Teachers see an aggregated view of class-level Educational Intelligence: mastery distribution, active learning gaps, recent diagnostic trends, and students needing attention. Insights are explainable with drill-down to individual SLPs.

Includes: EI aggregation API, teacher dashboard UI (new section or page), and tests.

## Acceptance criteria

- [ ] Class-level dashboard shows mastery distribution across micro-skills
- [ ] Active learning gaps listed with severity and student names
- [ ] Drill-down from class view to individual student SLP diagnostics/gaps/mastery
- [ ] Dashboard updates when new evidence is approved
- [ ] All insights include "why" explanations (linked evidence)
- [ ] Tests verify aggregation from sample class data

## Blocked by

- `issues/016-learning-gap-engine.md`
- `issues/017-mastery-calculations.md`

## User stories addressed

- SP-001 Ch.11 (Educational Intelligence Engine)
- SP-001 Ch.3 Workflow Stage 14 (Educational Intelligence)
- SP-001 Ch.13 (Teacher Workspace — analyse Educational Intelligence)
