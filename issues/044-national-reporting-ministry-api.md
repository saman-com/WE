## Type

AFK

## Parent PRD

`SP-001_WE_Platform_Functional_Specification.md`

## What to build

Implement national reporting and ministry API integrations end-to-end per SP-001 Ch.22. Education authorities can pull aggregated, anonymized national-level reports via a secured Open API. Ministry systems can query enrollment statistics, national mastery benchmarks, and curriculum coverage metrics. No individual student PII exposed via national API.

Includes: Open API endpoints for national reporting, API key authentication for ministry clients, aggregated report generation, API documentation (OpenAPI), and tests.

## Acceptance criteria

- [ ] National reporting API exposes aggregated metrics (enrollment, mastery benchmarks, coverage)
- [ ] No individual student PII in national API responses
- [ ] Ministry clients authenticate via API keys with scoped permissions
- [ ] OpenAPI documentation published at `/api/v1/docs`
- [ ] Rate limiting on national API endpoints
- [ ] Tests verify aggregation correctness and PII exclusion

## Blocked by

- `issues/037-phase6-validation-gate.md`
- `issues/042-curriculum-variants-region.md`

## User stories addressed

- SP-001 Ch.22 (Platform Integration, APIs and Future Extensibility)
- SP-001 §1.11 (Education Authorities)
- EP-001 §18.12 (national reporting, ministry integrations)
