## Type

AFK

## Parent PRD

`docs/product/SP-001-functional-specification.md`

## What to build

Implement national reporting and ministry API integrations end-to-end per SP-001 Ch.22. Education authorities can pull aggregated, anonymized national-level reports via a secured Open API. Ministry systems can query enrollment statistics, national mastery benchmarks, and curriculum coverage metrics. No individual student PII exposed via national API.

Includes: Open API endpoints for national reporting, API key authentication for ministry clients, aggregated report generation, API documentation (OpenAPI), and tests.

## Acceptance criteria

- [x] National reporting API exposes aggregated metrics (enrollment, mastery benchmarks, coverage)
- [x] No individual student PII in national API responses
- [x] Ministry clients authenticate via API keys with scoped permissions
- [x] OpenAPI documentation published at `/api/v1/docs`
- [x] Rate limiting on national API endpoints
- [x] Tests verify aggregation correctness and PII exclusion

## Blocked by

- `issues/037-phase6-validation-gate.md`
- `issues/042-curriculum-variants-region.md`

## User stories addressed

- SP-001 Ch.22 (Platform Integration, APIs and Future Extensibility)
- SP-001 §1.11 (Education Authorities)
- EP-001 §18.12 (national reporting, ministry integrations)
