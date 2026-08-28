## Type

AFK

## Parent PRD

`docs/product/SP-001-functional-specification.md`

Supporting references: `docs/engineering/EP-001-engineering-package.md` (Ch.3, Ch.15)

## What to build

Scaffold the `we-platform/` monorepo per approved ADRs from issue 001. Create the top-level directory structure (`apps/`, `services/`, `shared/`, `databases/`, `infrastructure/`, `docs/`, `scripts/`, `testing/`, `deployment/`, `examples/`), a reference sample service with health endpoint, a minimal Next.js app shell, `docker-compose.yml` for PostgreSQL + Redis + RabbitMQ, and a CI pipeline (GitHub Actions) that runs on PRs: install, compile, unit tests, lint.

End-to-end deliverable: `docker compose up` starts infrastructure; sample API returns `200` on `/health`; CI passes on a clean checkout.

## Acceptance criteria

- [x] Monorepo directory structure matches EP-001 §3.5
- [x] `examples/sample-service` (or equivalent) exposes `/health` and builds via Docker
- [x] `docker-compose.yml` starts Postgres, Redis, and RabbitMQ locally
- [x] Minimal Next.js app in `apps/` builds and runs
- [x] GitHub Actions workflow runs build + test on pull requests
- [x] Root README documents local setup (`docker compose up`, how to run services)

## Blocked by

- `issues/001-engineering-bootstrap-adrs.md`

## User stories addressed

- SP-001 Ch.2 (Overall Platform Architecture)
- EP-001 Ch.3 (Repository Structure)
- EP-001 Ch.15 (CI/CD)
