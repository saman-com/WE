# WE Platform load tests (k6)

Delivery-readiness scripts against the local docker-compose stack. Thresholds record **p95 < 500ms**; misses are exported for bug reports and do **not** hard-fail the suite (`run-load.sh` treats k6 exit 99 as soft).

## Prerequisites

1. Install [k6](https://grafana.com/docs/k6/latest/set-up/install-k6/) (`brew install k6`).
2. Start the compose stack from `we-platform/`:

   ```bash
   docker compose up -d --build
   ./scripts/seed-demo.sh
   ```

3. Demo users (password `Password123!`) come from `IdentityDataSeeder` / e2e `helpers/users.ts`.

## Scenarios

| Script | Role | What it hits |
|--------|------|----------------|
| `scenarios/login.js` | student / teacher | `POST /api/v1/auth/login` |
| `scenarios/student-home.js` | student | `auth/me`, student workspace, learning profile, assessments list, mastery |
| `scenarios/teacher-class.js` | teacher | class dashboard, EI insights, orgs/classes |
| `scenarios/approve-evidence.js` | teacher + student | create → publish → submit → `POST /api/v1/evidence` (skips gracefully as N/A if write path fails) |
| `scenarios/leadership.js` | SchoolLeader | leadership dashboard / interventions / year-level / class summary |
| `scenarios/authority.js` | EducationAuthorityOfficer | national-reporting policy dashboards |

### Evidence approve setup

Seed alone leaves **Algebra sheet** already approved (duplicate submission → 409). The load script therefore creates a **unique assessment per iteration**, has the demo student submit, then approves. Custom counters:

- `evidence_approve_ok` — successful `201`
- `evidence_approve_na` — skipped / conflict / upstream failure

Optional overrides: `ORG_ID`, `CLASS_ID`, `STUDENT_USER_ID`, micro-skill IDs via `lib/config.js` defaults matching `seed-demo.sh`.

## Environment variables (base URLs)

Defaults match portal `.env.local.example` / compose host ports:

| Variable | Default |
|----------|---------|
| `IDENTITY_API_URL` | `http://localhost:8081` |
| `ORGANISATION_API_URL` | `http://localhost:8082` |
| `CURRICULUM_API_URL` | `http://localhost:8083` |
| `STUDENT_LEARNING_API_URL` | `http://localhost:8084` |
| `ASSESSMENT_API_URL` | `http://localhost:8085` |
| `EVIDENCE_API_URL` | `http://localhost:8086` |
| `MASTERY_API_URL` | `http://localhost:8090` |
| `EI_API_URL` | `http://localhost:8091` |
| `NATIONAL_REPORTING_API_URL` | `http://localhost:8100` |
| `DEMO_PASSWORD` | `Password123!` |
| `STUDENT_EMAIL` / `TEACHER_EMAIL` / `LEADER_EMAIL` / `AUTHORITY_EMAIL` | demo emails |
| `ORG_ID` / `CLASS_ID` / `YEAR_LEVEL_ID` / `STUDENT_USER_ID` | seed-demo IDs |

## Run

### Full matrix (25 and 100 VUs)

```bash
cd we-platform/testing/load
./run-load.sh
```

Writes JSON under `results/` (`--summary-export`) plus `run-summary-*.txt`. Threshold misses print as `MISS` / exit 99 but the wrapper exits 0 unless k6 hard-fails.

### Custom VUs / duration / subset

```bash
DURATION=1m VUS_LIST="25 100" ./run-load.sh
./run-load.sh login student-home          # subset of scenarios
DURATION=15s VUS_LIST="10" ./run-load.sh authority
```

### Single scenario (manual)

```bash
k6 run --vus 25 --duration 30s --summary-export results/login-25vu.json scenarios/login.js
k6 run --vus 100 --duration 30s --summary-export results/login-100vu.json scenarios/login.js
```

Inspect p95:

```bash
jq '.metrics.http_req_duration.values["p(95)"]' results/login-25vu-*.json
```

If p95 ≥ 500ms, treat as a delivery bug candidate (do not rely on k6 non-zero exit alone — the wrapper soft-fails thresholds on purpose).

## Thresholds

Each script sets:

```js
thresholds: { http_req_duration: ['p(95)<500'] }
```

`run-load.sh` continues after exit code `99` so the full VU matrix completes; report misses from `results/run-summary-*.txt`.
