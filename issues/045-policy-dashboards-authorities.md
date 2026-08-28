## Type

AFK

## Parent PRD

`SP-001_WE_Platform_Functional_Specification.md`

## What to build

Implement policy dashboards for education authorities end-to-end per SP-001 Ch.16 and Ch.18. Education authority officers see national/regional policy dashboards: system-wide trends, equity analysis, curriculum effectiveness across regions, and intervention impact at scale. Data from EDW national aggregates.

Includes: authority portal pages, national aggregation API, role-based access for Education Authority Officer role, and tests.

## Acceptance criteria

- [ ] Education authority dashboard shows national/regional aggregated trends
- [ ] Equity analysis: mastery distribution across demographics/regions
- [ ] Curriculum effectiveness comparison across regions
- [ ] Only Education Authority Officer role can access policy dashboards
- [ ] All data aggregated and anonymized (no individual student records)
- [ ] Tests verify role access and aggregation correctness

## Blocked by

- `issues/044-national-reporting-ministry-api.md`

## User stories addressed

- SP-001 Ch.16 (School Leadership — authority-level view)
- SP-001 Ch.18 (Reporting — policy dashboards)
- SP-001 §1.11 (Education Authorities — planning and policy)
- EP-001 §18.12 (policy dashboards)
