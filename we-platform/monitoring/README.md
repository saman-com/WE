# WE Platform Monitoring

| Field | Value |
|-------|-------|
| **Audience** | Platform Operations |
| **Related gate** | Issue 046 / EP-001 §18.16 Operational Validation |

## Health checks

Every microservice exposes:

```http
GET /health
```

Expected response: HTTP 200 with `{ "status": "healthy" }`.

Compose healthchecks already cover PostgreSQL (`pg_isready`), Redis (`PING`), and RabbitMQ (`rabbitmq-diagnostics ping`).

## What to monitor

| Signal | Source | Alert when |
|--------|--------|------------|
| Service health | `/health` on each API | Consecutive failures ≥ 3 |
| API latency | Request duration (national + core APIs) | p95 &gt; 500ms |
| Portal page load | Browser / RUM or synthetic check | &gt; 2s for primary dashboards |
| Auth failures | Identity + API 401/403 rates | Sudden spike vs baseline |
| Queue depth | RabbitMQ management (15672) | Growing without consumers |
| DB connectivity | Postgres healthcheck / service errors | Unhealthy or connection storms |
| National API rate limits | HTTP 429 on `/api/v1/national/*` | Sustained 429 for valid clients |

## Synthetic checks (minimum)

1. `GET /health` for identity, curriculum, assessment, evidence, intervention, reporting, national-reporting
2. Ministry enrollment report with valid API key
3. Education Authority Officer policy trends with valid JWT
4. Portal `/login` reachable and language switcher loads

## Dashboards (baseline)

Until a hosted APM is wired, use:

- `docker compose ps` for container health
- RabbitMQ management UI at `http://localhost:15672`
- CI status on `main` as the release-health signal

Place vendor-specific dashboard JSON / alert rules in this directory as they are adopted.
