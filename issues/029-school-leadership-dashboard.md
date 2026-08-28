## Type

AFK

## Parent PRD

`docs/product/SP-001-functional-specification.md`

## What to build

Implement the School Leadership Dashboard end-to-end per SP-001 Ch.16. School leaders (Principal, Deputy Principal, Head of Department) see whole-school KPIs: student progress trends, mastery distribution, active interventions, assessment completion rates, and class-level comparisons. Data aggregated from EI services.

Includes: leadership portal pages, aggregation API, role-based access for leadership roles, and tests.

## Acceptance criteria

- [ ] Leadership dashboard shows school-wide KPIs: progress, mastery, interventions, assessments
- [ ] Drill-down from school → department → class → student (with appropriate access)
- [ ] Only leadership roles (Principal, Deputy, HoD) can access dashboard
- [ ] Data aggregated from EI services (not duplicated business logic)
- [ ] Dashboard loads within 2 seconds for a school with seed data
- [ ] Tests verify leadership role access and data aggregation

## Blocked by

- `issues/018-ei-teacher-insights-dashboard.md`

## User stories addressed

- SP-001 Ch.16 (School Leadership Dashboard Module)
- SP-001 §1.11 (School Leaders — monitor improvement, review EI)
