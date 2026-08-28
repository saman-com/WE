## Type

AFK

## Parent PRD

`SP-001_WE_Platform_Functional_Specification.md`

Supporting references: `ABC.md` (Ch.14)

## What to build

Implement the Educational Data Warehouse (EDW) ingest pipeline end-to-end per SP-001 Ch.18 and TD-001 Ch.14. Domain events from the event bus are consumed and written to an analytics store (separate from operational DBs). This enables longitudinal analysis without impacting transactional performance.

Includes: EDW ingest service, event subscribers, analytics schema (star/snowflake), batch and streaming ingest, and tests.

## Acceptance criteria

- [ ] EDW ingest service subscribes to key domain events (EvidenceCreated, AssessmentApproved, InterventionCreated)
- [ ] Events written to analytics store (separate PostgreSQL schema or dedicated DB)
- [ ] Operational services unaffected by EDW ingest (async, non-blocking)
- [ ] Analytics schema supports fact tables for evidence, assessments, interventions
- [ ] Ingest is idempotent (re-processing same event does not duplicate)
- [ ] Tests verify event → analytics record pipeline

## Blocked by

- `issues/014-domain-event-bus.md`

## User stories addressed

- SP-001 Ch.18 (Reporting and Analytics — data warehouse)
- ABC.md Ch.14 (Learning Analytics, Reporting & Data Warehouse)
- EP-001 §18.10 (Phase 6 — Advanced Analytics)
