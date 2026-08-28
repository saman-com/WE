## Type

AFK

## Parent PRD

`docs/product/SP-001-functional-specification.md`

## What to build

Implement leadership intervention monitoring end-to-end per SP-001 Ch.12 and Ch.16. School leaders see all active interventions across the school: status, responsible teacher, target gaps, and outcomes. Leaders can filter by department, class, or severity. This extends the leadership dashboard with intervention oversight.

Includes: intervention aggregation API, leadership dashboard intervention section, and tests.

## Acceptance criteria

- [ ] Leadership dashboard shows all active interventions school-wide
- [ ] Filter by department, class, severity, status
- [ ] Each intervention shows: student, gap, teacher, status, timeline
- [ ] Leaders can view but not modify interventions (oversight only)
- [ ] Data sourced from Intervention Service (issue 019)
- [ ] Tests verify leadership access and aggregation

## Blocked by

- `issues/019-intervention-management.md`
- `issues/029-school-leadership-dashboard.md`

## User stories addressed

- SP-001 Ch.12 (Intervention Management — monitoring)
- SP-001 Ch.16 (School Leadership Dashboard)
- SP-001 §1.11 (School Leaders — support teachers)
