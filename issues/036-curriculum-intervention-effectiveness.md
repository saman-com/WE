## Type

AFK

## Parent PRD

`SP-001_WE_Platform_Functional_Specification.md`

## What to build

Implement curriculum and intervention effectiveness dashboards end-to-end per SP-001 Ch.18. School leaders analyse which curriculum areas have lowest mastery, which interventions are most effective, and where teaching strategies need adjustment. Data from EDW aggregated views.

Includes: effectiveness analytics API, leadership dashboard pages, and tests.

## Acceptance criteria

- [ ] Curriculum effectiveness view: mastery rates per subject/unit across school
- [ ] Intervention effectiveness view: outcomes by intervention type
- [ ] Leaders can identify underperforming curriculum areas
- [ ] Data aggregated from EDW (not computed on operational DB)
- [ ] Dashboard accessible to leadership roles only
- [ ] Tests verify aggregation with known seed data

## Blocked by

- `issues/035-longitudinal-learning-analysis.md`
- `issues/019-intervention-management.md`

## User stories addressed

- SP-001 Ch.18 (Reporting and Analytics — effectiveness)
- SP-001 Ch.11 (EI — curriculum implementation monitoring)
- EP-001 §18.10 (curriculum effectiveness, intervention effectiveness)
