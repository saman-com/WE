## Type

AFK

## Parent PRD

`SP-001_WE_Platform_Functional_Specification.md`

## What to build

Implement the standard reporting module end-to-end per SP-001 Ch.18. Teachers generate class progress reports; school leaders generate school summary reports. Reports pull from EI data (mastery, gaps, interventions) and can be exported as PDF. Reports are generated on demand, not pre-cached.

Includes: Reporting Service, report templates, generation API, teacher/leader UI for report requests, PDF export, and tests.

## Acceptance criteria

- [ ] Teacher can generate class progress report (mastery, gaps, assessment summary)
- [ ] School leader can generate school summary report
- [ ] Reports exportable as PDF
- [ ] Report data sourced from EI services (not duplicated)
- [ ] Reports scoped by role (teacher sees their classes; leader sees school)
- [ ] Tests verify report generation with seed data

## Blocked by

- `issues/018-ei-teacher-insights-dashboard.md`

## User stories addressed

- SP-001 Ch.18 (Reporting and Analytics Module)
- SP-001 Ch.13 (Teacher Workspace — generate reports)
- SP-001 Ch.16 (School Leadership — reporting)
