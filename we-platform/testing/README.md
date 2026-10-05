# Platform integration and end-to-end tests

## EvidenceApprovalFlow.Tests

End-to-end and reliability tests for teacher evidence approval using Testcontainers (PostgreSQL + RabbitMQ).

Covered scenarios:

- Happy path: profile, diagnostics, gaps, and mastery updated from seed-demo micro-skill marks 5/4/3/2
- RabbitMQ down during approval: evidence committed via outbox, delivered after recovery
- Duplicate `EvidenceCreated` delivery: no duplicate diagnostic rows
- Consumer exception then retry then success
- Permanent consumer failure after retries (Fault / error-queue path)

Requires Docker. Included in `dotnet test WePlatform.sln` / CI.

## Browser e2e (`testing/e2e`)

Playwright + TypeScript against the real Next.js portal and docker-compose backend stack.

### Portal hosting choice

The web portal is **not** added to `docker-compose.yml`. Playwright starts it via `webServer` in `testing/e2e/playwright.config.ts` (`next dev` on `http://localhost:3000`). The `pnpm e2e` script brings up compose services, waits for every `/health` endpoint, runs `scripts/seed-demo.sh`, then runs Playwright (which starts the portal).

Use `localhost` (not `127.0.0.1`) so identity CORS (`http://localhost:3000`) allows browser login.

### One-command run

From `we-platform/`:

```bash
pnpm install
pnpm --filter e2e exec playwright install chromium
pnpm e2e
```

This must pass twice in a row (unique titles + idempotent seed data).

### Run against an already-running stack

```bash
# terminal 1: docker compose up -d && ./scripts/seed-demo.sh && pnpm dev
pnpm e2e:test
# or UI mode
pnpm e2e:ui
```

### Auth / storageState

`tests/auth.setup.ts` signs in once per demo role through `/login` and writes `.auth/*.json` (includes `localStorage.we_access_token`). Specs load the role they need via `browser.newContext({ storageState })`.

Demo password: `Password123!`

| Email | Role |
|-------|------|
| teacher@school.local | Teacher |
| student@school.local | Student |
| parent@school.local | Parent |
| admin@school.local | SystemAdministrator |
| leader@school.local | SchoolLeader |
| authority@ministry.local | EducationAuthorityOfficer |
| federation@ministry.local | FederationAdmin |
| teacher-b@schoolb.local | Teacher (School B tenant) |
| student-b@schoolb.local | Student (School B tenant) |

### Debug

- UI mode: `pnpm e2e:ui`
- Headed: `pnpm --filter e2e e2e:headed`
- HTML report: `pnpm --filter e2e e2e:report`
- Traces / screenshots / video: on failure under `testing/e2e/test-results/`
- Open a trace: `pnpm --filter e2e exec playwright show-trace test-results/.../trace.zip`

### Adding a test

1. Put specs in `testing/e2e/tests/`.
2. Prefer `getByRole` / `getByLabel`.
3. Use unique titles via `helpers/unique.ts`.
4. For event-driven UI, poll with `helpers/poll.ts`.
5. Load the role with `storagePath("teacher")` (etc.).
6. Prefer `test.fixme(true, "reason")` inside a test body — bare `test.fixme(title)` without a body can skip sibling tests.

### CI

The `e2e` job in `.github/workflows/ci.yml` runs after backend and frontend, starts the stack, runs Playwright, and uploads the HTML report and traces as artifacts.

### Journey status

| Journey | Status | Notes |
|---------|--------|-------|
| a. Core learning loop | pass | Create/publish/submit/approve + progress polling |
| b. Interventions | pass | From class gap → Active; parent sees it |
| c. Parent/teacher messages | fixme | Parent send 403: TeacherCanViewStudent checked with parent JWT |
| d. AI feedback draft | pass | Draft/edit/approve + admin AI audit |
| e. Leadership | pass* / fixme | Admin generate+PDF passes; SchoolLeader dashboard fixme (assessment 403) |
| f. Authority | pass | Dashboards load |
| g. Access control | pass | Role blocks, login redirect, school A/B isolation |
| h. Student wording | pass | No gap/failure/weakness/severity; Strong/Solid/Getting there/Not yet |
| i. Arabic | pass | `dir=rtl`, Arabic chrome, persists after reload |
| j. Smoke | pass | Intended-role page loads (leader/federation routes omitted — product bugs below) |

Local verification: `pnpm e2e` passed twice in a row (29 passed / 2 skipped).

### App / seed changes (test hooks and unblocking intended flows)

| Change | Why |
|--------|-----|
| `IdentityDataSeeder` + identity README | SchoolLeader, FederationAdmin, School B teacher/student |
| `scripts/seed-demo.sh` | Parent–student link, leader org membership, School B org/curriculum |
| `apps/web-portal/.env.local.example` | `NEXT_PUBLIC_FEDERATION_API_URL`, `NEXT_PUBLIC_NATIONAL_REPORTING_API_URL` |
| `ServiceJwtIssuer` tenant claim | Service-to-AI-gateway calls need tenant (journey d) |
| AI gateway CORS `localhost:3000` | Browser calls from Playwright portal |
| `GovernanceSafetyFilter` GUID/timestamp phone false positives | Assessment feedback payloads were blocked as PII |

No `data-testid` attributes were required; specs use role/text locators.

### Product bugs found (left failing / fixme)

1. **Parent messaging 403** — send path evaluates `TeacherCanViewStudent` against the parent JWT, so organisation assignments are empty.
2. **SchoolLeader `/leadership`** — forwarded assessment calls return 403 during dashboard aggregation.
3. **FederationAdmin `/admin/federation`** — not smoke-tested; same class of role/API gaps as leadership (avoided hardening product for green CI).
4. **SystemAdministrator** — already covered by `admin@school.local` (no separate demo user added).
