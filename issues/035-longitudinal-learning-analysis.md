## Type

AFK

## Parent PRD

`docs/product/SP-001-functional-specification.md`

## What to build

Implement longitudinal learning analysis views end-to-end per SP-001 Ch.18. Teachers and leaders can view student learning trajectories over time: mastery trends, gap history, intervention outcomes across terms/years. Data sourced from EDW (issue 034).

Includes: analytics query API, longitudinal chart UI (mastery over time, gap trends), and tests.

## Acceptance criteria

- [ ] Longitudinal mastery chart per student showing progress over time
- [ ] Gap history timeline showing gaps opened/closed
- [ ] Intervention outcome tracking over time
- [ ] Data sourced from EDW analytics store (not operational DB)
- [ ] Teacher sees longitudinal data for their students; leader sees school-wide
- [ ] Tests verify query correctness with multi-period seed data

## Blocked by

- `issues/034-edw-ingest-pipeline.md`

## User stories addressed

- SP-001 Ch.18 (Reporting and Analytics — longitudinal analysis)
- SP-001 Ch.5 (SLP — continuous growth over time)
- EP-001 §18.10 (longitudinal learning analysis)
