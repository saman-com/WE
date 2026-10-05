# WE Platform NFR report — batch 19

Date: 2026-10-05  
Scope: `we-platform/` non-functional checks (load, dependency vulns, headers/CORS, secrets, backup/restore, health).  
Environment: local machine with `docker compose` stack already up; unit/integration tests via .NET TestHost / Vitest.

---

## 1. Load / latency

### Claim

API responses under concurrent load should meet **p95 / max ≤ 500 ms** (monitoring README alert threshold: p95 > 500 ms). Authority portal page-load SLA: parallel policy dashboard fetches complete in **&lt; 2 s** when each API responds within ~450 ms.

### Compose-wide k6

**Missing.** No `k6` scripts or compose-wide load suite exists under `we-platform/`.

### Unit / TestHost SLA results (executed)

Command:

```bash
dotnet test services/national-reporting-service/tests/NationalReportingService.Tests.csproj \
  --filter "FullyQualifiedName~PlatformSlaLoadTests"
```

| Test | Concurrent requests | min | avg | p95 | max | SLA | Result |
|------|---------------------|-----|-----|-----|-----|------|--------|
| `NationalEnrollmentApi_UnderConcurrentLoad_Meets500msSla` | 25 | 3 ms | 6 ms | **12 ms** | 12 ms | 500 ms | **Passed** |
| `PolicyDashboardTrendsApi_UnderConcurrentLoad_Meets500msSla` | 25 | 1 ms | 1 ms | **1 ms** | 2 ms | 500 ms | **Passed** |

Notes:

- Host is `WebApplicationFactory` + EF InMemory (not Postgres/network). Numbers validate handler latency under concurrency, not production p95.
- Assertion is `max ≤ 500 ms`; p95 is recorded for the report via `ITestOutputHelper`.

### Frontend page-load SLA (executed)

```bash
pnpm exec vitest run src/lib/policy-dashboards.sla.test.ts
```

| Test | Expected | Result |
|------|----------|--------|
| Authority policy dashboard 4-way parallel fetch (mocked API latency 450 ms each) | elapsed &lt; 2000 ms | **Passed** (1 test, ~995 ms file duration) |

### Verdict

**Partial.** Unit SLA comfortably under 500 ms (p95 12 ms / 1 ms). Compose-wide k6 against live endpoints not available.

---

## 2. Dependency vulnerabilities

### .NET (`dotnet list WePlatform.sln package --vulnerable --include-transitive`)

Executed 2026-10-05 against NuGet `https://api.nuget.org/v3/index.json`.

| Project | Finding |
|---------|---------|
| All service Api/Tests + shared libs (except below) | **No vulnerable packages** |
| `EvidenceApprovalFlow.Tests` | Transitive **SSH.NET 2024.1.0** — **High** ×2 ([GHSA-q939-rpr3-3284](https://github.com/advisories/GHSA-q939-rpr3-3284), [GHSA-mggc-4xg6-vcxf](https://github.com/advisories/GHSA-mggc-4xg6-vcxf)) |

SSH.NET is pulled via Testcontainers (test-only), not production service runtimes.

### Node (`pnpm audit`)

Workspace packages: `apps/web-portal`, `testing/e2e`.

| Location | Command | Result |
|----------|---------|--------|
| `apps/web-portal` | `pnpm audit --prod` | **No known vulnerabilities** (`next@15.5.27`; transitive `postcss` forced to `8.5.28` via `.pnpmfile.cjs`) |
| `apps/web-portal` | `pnpm audit` (incl. dev) | Dev-only findings may remain (eslint toolchain); **0 critical/high in production dependencies** |

### Verdict

**Covered** (scans run). Runtime .NET clean except test-only SSH.NET; Node/portal production audit has **0 critical/high**.

---

## 3. OWASP headers / CORS

### Security headers

Source scan of `services/*/src/Api/Program.cs` and related middleware: **no** `UseHsts`, `X-Content-Type-Options`, `X-Frame-Options`, `Content-Security-Policy`, `Referrer-Policy`, or `Permissions-Policy` middleware.

| Header / control | Status |
|------------------|--------|
| HSTS | Missing |
| X-Content-Type-Options | Missing |
| X-Frame-Options / CSP frame-ancestors | Missing |
| Content-Security-Policy | Missing |
| Referrer-Policy | Missing |
| Permissions-Policy | Missing |

No product header middleware was added in this batch (not already patterned).

### CORS

Present on portal-facing APIs:

- Named policy `"WebPortal"` (most services) or default policy (`ai-gateway-service`).
- Origin allow-list: **`http://localhost:3000` only**.
- `AllowAnyHeader()` + `AllowAnyMethod()`.
- No `AllowCredentials()` / `AllowAnyOrigin()`.

**No CORS** on worker-style hosts: `edw-ingest-service`, `event-subscriber-service` (health only / no portal CORS).

### Verdict

**Covered** (inspection). CORS is origin-scoped for local portal; OWASP response headers are absent across APIs.

---

## 4. Secrets

### Scan (rg / find)

| Check | Result |
|-------|--------|
| Committed `.env` / `.env.local` | **None tracked.** `apps/web-portal/.env.local` exists locally but is **gitignored** (`.gitignore: .env*`). Tracked examples only: `.env.example`, `apps/web-portal/.env.local.example`. |
| `credentials.json` | None |
| PEM / private key material (`BEGIN … PRIVATE KEY`) | None |
| Hardcoded connection strings with passwords | **Dev-only** `Password=we_dev` in `docker-compose.yml` and many `appsettings.json` / `appsettings.Development.json` (local Postgres). |
| JWT signing keys in repo | Dev/test placeholders e.g. `local-dev-only-change-in-production-32chars`, `test-signing-key-at-least-32-chars-long`; curriculum production `appsettings.json` uses `SET_VIA_ENVIRONMENT_VARIABLE`. |

### Verdict

**Covered.** No committed production secrets / private keys. Local-dev Postgres password and JWT placeholders are intentional for compose; production must override via env.

---

## 5. Backup / restore

### Procedure (manual checklist)

1. Ensure compose Postgres is healthy: `docker compose ps postgres`.
2. Create dump directory: `mkdir -p backups/$(date +%Y%m%d)`.
3. Dump every service database (custom format):

```bash
DBS="we_identity we_organisation we_curriculum we_learning we_assessment \
we_evidence we_diagnostic we_gaps we_mastery we_interventions we_ei \
we_ai_gateway we_communication we_notifications we_reporting we_edw \
we_configuration we_federation we_national_reporting"

for db in $DBS; do
  docker exec we-platform-postgres-1 \
    pg_dump -U we -d "$db" -Fc -f "/tmp/${db}.dump"
  docker cp "we-platform-postgres-1:/tmp/${db}.dump" "backups/$(date +%Y%m%d)/${db}.dump"
done
```

4. Restore (disruptive — stop APIs first or restore into a fresh Postgres volume):

```bash
# Example per DB (drop/recreate or --clean as appropriate for the environment)
docker exec -i we-platform-postgres-1 \
  pg_restore -U we -d "$db" --clean --if-exists < "backups/<date>/${db}.dump"
```

5. Restart services: `docker compose up -d`.
6. Smoke: curl each `/health`, then `pnpm e2e` from `we-platform/testing/e2e`.

### Execution this pass

| Step | Status |
|------|--------|
| `pg_dump` all 19 service DBs | **Executed** — 19/19 OK (sizes ~2.5–20 KB each into `/tmp/we-platform-nfr-backup/`) |
| `pg_restore` into running cluster | **Not executed** (would disrupt live compose used by other work) |
| Post-restore `pnpm e2e` | **Not executed** |

### Verdict

**Partial.** Dump path validated; restore + e2e remain a manual ops checklist item.

---

## 6. Health

### Endpoints

Every compose API maps `GET /health` → `{ "status": "healthy" }` (see `deployment/runbook.md`).

| Service | Host port | Sample (this pass) |
|---------|-----------|--------------------|
| sample-service | 8080 | 200 / healthy (~31 ms) |
| identity-service | 8081 | 200 / healthy |
| organisation-service | 8082 | 200 / healthy |
| curriculum-service | 8083 | 200 / healthy |
| student-learning-service | 8084 | 200 / healthy |
| assessment-service | 8085 | 200 / healthy |
| evidence-service | 8086 | 200 / healthy |
| event-subscriber-service | 8087 | 200 / healthy |
| diagnostic-service | 8088 | 200 / healthy |
| learning-gap-service | 8089 | 200 / healthy |
| mastery-service | 8090 | 200 / healthy |
| ei-service | 8091 | 200 / healthy |
| intervention-service | 8092 | 200 / healthy |
| ai-gateway-service | 8093 | 200 / healthy |
| communication-service | 8094 | 200 / healthy |
| notification-service | 8095 | 200 / healthy |
| reporting-service | 8096 | 200 / healthy |
| edw-ingest-service | 8097 | 200 / healthy |
| configuration-service | 8098 | 200 / healthy |
| federation-service | 8099 | 200 / healthy |
| national-reporting-service | 8100 | 200 / healthy |

**21/21** live `/health` probes returned HTTP 200.

### Automated tests / scripts

| Artifact | Role |
|----------|------|
| `examples/sample-service/tests/HealthEndpointTests.cs` | Unit: `GET /health` → 200 |
| Compose `healthcheck` | Only on **postgres**, **redis**, **rabbitmq** — **no** app-service healthchecks in `docker-compose.yml` |
| Compose-wide health script | **Missing** (manual curl loop used for this report) |

### Verdict

**Partial → covered for live sample; automation gap remains.** All running services healthy; no compose-wide health script or per-service Docker healthchecks.

---

## Summary

| Area | Status | Key number / note |
|------|--------|-------------------|
| Load / latency | partial | p95 **12 ms** / **1 ms** vs 500 ms claim (TestHost); **no k6** |
| Dependency vulns | covered | .NET: SSH.NET High (tests only); Node prod: **0 critical/high** (`next@15.5.27`) |
| Headers / CORS | covered | CORS localhost:3000; **OWASP headers missing** |
| Secrets | covered | No committed private keys / `.env`; local `we_dev` placeholders only |
| Backup / restore | partial | **19/19 dumps OK**; restore + e2e not run |
| Health | covered* | **21/21** `/health` OK; *no compose-wide script / app healthchecks |

Report path: `docs/validation/nfr-report.md`.
