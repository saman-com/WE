# ADR-0003: Database and Cache

## Status

Accepted

## Date

2026-08-28

## Approval

| Reviewer | Date | Decision |
|----------|------|----------|
| Saman | 2026-08-28 | Accepted |

## Context

The WE Platform stores relational educational data (curriculum, assessments, evidence, SLPs, interventions), requires fast session/token caching, and must support high read throughput for dashboards. TD-001 §2.9 recommends PostgreSQL as the primary database and Redis for caching. EP-001 mandates database-per-service with no shared database access between services.

## Decision

### Primary database: PostgreSQL 16

- One PostgreSQL **instance** in local/dev (Docker Compose)
- One **database per service** in production (e.g. `we_identity`, `we_curriculum`, `we_assessment`)
- Migrations managed via **EF Core migrations** stored in `we-platform/databases/<service>/`
- Connection strings via environment variables / secrets manager (never committed)

### Cache: Redis 7

- Used for: session data, JWT blocklist (logout), frequently-read reference data (curriculum trees), API rate limiting
- Redis is **shared infrastructure** but each service uses namespaced keys (`identity:`, `curriculum:`, etc.)
- Not a source of truth — cache miss falls back to PostgreSQL

### Data rules (EP-001 + TD-001)

| Rule | Implementation |
|------|----------------|
| DB-per-service | No cross-service SQL joins; use APIs or events |
| Evidence immutability | Approved evidence rows are append-only; no UPDATE/DELETE |
| Audit trail | Separate audit log table per service for significant actions |
| Migrations only | No manual schema changes in production |
| Seed data | Fake school data in `databases/seeds/` for local dev (no real PII) |

### Local development (Docker Compose)

```yaml
# we-platform/docker-compose.yml (to be created in issue 002)
services:
  postgres:
    image: postgres:16-alpine
    ports: ["5432:5432"]
  redis:
    image: redis:7-alpine
    ports: ["6379:6379"]
```

### Future storage (not V1)

| Store | When | Purpose |
|-------|------|---------|
| OpenSearch | Phase 6+ | Full-text search across curriculum, messages |
| Object storage (S3/Blob) | Phase 5+ | File attachments, report PDFs |
| Analytics warehouse | Issue 034 | EDW for longitudinal analysis (separate schema) |

## Consequences

### Positive

- PostgreSQL handles relational educational data with ACID guarantees and JSONB for flexible metadata
- Redis reduces load on evidence/SLP read paths
- DB-per-service enforces bounded context boundaries

### Negative

- Cross-service reporting requires event-driven aggregation or EDW (issue 034), not SQL joins
- Multiple databases increase operational overhead in production

### Neutral

- SQLite considered for unit tests only; integration tests use PostgreSQL via Docker

## References

- TD-001 `docs/architecture/TD-001-technical-architecture.md` §2.9, Ch.6 (Data Architecture), Ch.7 (Database Schema)
- EP-001 `docs/engineering/EP-001-engineering-package.md` Ch.7 (Database Implementation Standards)
- SP-001 Ch.5 (Student Learning Profile — one source of truth)
