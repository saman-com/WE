# WE Platform Deployment Runbook

| Field | Value |
|-------|-------|
| **Audience** | Platform Operations |
| **Scope** | Local/compose deployment and CI release path |
| **Related gate** | Issue 046 / EP-001 §18.16 Operational Validation |

## Service overview

WE Platform is a multi-service Educational Intelligence stack:

- **Apps**: `web-portal` (Next.js) on port 3000
- **Services**: ASP.NET Core APIs under `we-platform/services/` (identity, organisation, curriculum, assessment, evidence, diagnostics, gaps, mastery, interventions, EI, AI gateway, communication, notifications, reporting, EDW ingest, configuration, federation, national-reporting, event-subscriber)
- **Infrastructure**: PostgreSQL 16, Redis 7, RabbitMQ 3.13

Every API exposes `GET /health` returning `{ "status": "healthy" }`.

## Dependencies

| Dependency | Port | Required for |
|------------|------|--------------|
| PostgreSQL | 5432 | All persistence-backed services |
| Redis | 6379 | Cache / session support |
| RabbitMQ | 5672 / 15672 | Domain events and async consumers |
| JWT signing key | env `JWT_KEY` | Authenticated APIs |

## Deploy (local / compose)

```bash
cd we-platform
docker compose up -d --build
curl http://localhost:8080/health   # sample-service
curl http://localhost:8081/health   # identity-service
# Frontend
cp apps/web-portal/.env.local.example apps/web-portal/.env.local
pnpm install
pnpm build && pnpm --filter web-portal start
```

## CI release path

GitHub Actions workflow `.github/workflows/ci.yml`:

1. `dotnet restore` / `dotnet build` / `dotnet test` on `WePlatform.sln`
2. `pnpm install --frozen-lockfile` / `pnpm lint` / `pnpm build`

A release is deployable only when both jobs are green.

## Restart procedures

1. Restart a single service: `docker compose restart <service-name>`
2. Recreate after config change: `docker compose up -d --build <service-name>`
3. Full stack bounce: `docker compose down && docker compose up -d --build`
4. Portal only: restart the `pnpm`/`next start` process

## Common failures and recovery

| Symptom | Likely cause | Recovery |
|---------|--------------|----------|
| Service unhealthy / 500 on start | Postgres not ready | Wait for `postgres` healthcheck; restart dependent service |
| Auth 401/403 after deploy | JWT key mismatch | Align `Jwt__Key` / `JWT_KEY` across identity and APIs |
| Events not flowing | RabbitMQ down | `docker compose restart rabbitmq` then restart publishers/consumers |
| Evidence approved but profile/EI not updated on upgraded volume | Missing MassTransit outbox/inbox tables (`EnsureCreated` is a no-op on existing DBs) | Apply `databases/<service>/V00*__masstransit_outbox_inbox.sql` (or restart the service — startup runs the same SQL idempotently), then republish/retry |
| National API unauthorized | Missing/invalid API key | Provision active ministry key with required scopes |
| Portal blank after login | API URL env misconfigured | Verify `NEXT_PUBLIC_*` URLs in `.env.local` |

## Schema upgrades (existing Postgres volumes)

`EnsureCreated` does **not** add new tables to databases that already exist. For evidence, diagnostic, gaps, mastery, and student-learning services, MassTransit requires `InboxState`, `OutboxState`, and `OutboxMessage`.

On startup those services run the idempotent SQL in `WePlatform.Messaging.MassTransitOutboxInboxSchema` (mirrors `databases/<service>/V00*__masstransit_outbox_inbox.sql`). Operators can also apply the scripts manually:

```bash
psql "$EVIDENCE_DB_URL" -f databases/evidence/V002__masstransit_outbox_inbox.sql
psql "$DIAGNOSTIC_DB_URL" -f databases/diagnostic/V002__masstransit_outbox_inbox.sql
psql "$GAPS_DB_URL" -f databases/gaps/V002__masstransit_outbox_inbox.sql
psql "$MASTERY_DB_URL" -f databases/mastery/V002__masstransit_outbox_inbox.sql
psql "$LEARNING_DB_URL" -f databases/learning/V003__masstransit_outbox_inbox.sql
```

RabbitMQ must include the `rabbitmq_delayed_message_exchange` plugin (compose uses `heidiks/rabbitmq-delayed-message-exchange:3.13.3-management`) so consumer delayed redelivery works.

## Escalation

1. Platform Operations (on-call) — service restart, compose logs (`docker compose logs <service>`)
2. Platform Engineering — schema/migration or contract defects
3. Security — suspected credential leak or cross-tenant exposure (see `incident-response.md`)
