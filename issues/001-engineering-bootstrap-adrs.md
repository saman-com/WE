## Type

HITL — **Completed**

## Parent PRD

`docs/product/SP-001-functional-specification.md`

Supporting references: `docs/engineering/EP-001-engineering-package.md` (Ch.1–3), `docs/architecture/TD-001-technical-architecture.md` (Ch.2)

## What to build

Establish the engineering foundation before any application code. Create `docs/adr/` and write Architecture Decision Records (ADRs) that lock the technology stack, monorepo tooling, and local development approach per EP-001 Ch.3 and TD-001 Ch.2.

ADRs must cover at minimum: backend (.NET 9 / ASP.NET Core), frontend (React + Next.js + TypeScript + Tailwind), database (PostgreSQL), cache (Redis), message broker (RabbitMQ), API style (REST `/api/v1/`), auth approach (OAuth 2.0 / OIDC / JWT), and monorepo directory layout (`we-platform/` tree from EP-001 §3.5).

This slice produces documentation only — no application services yet. Human review and approval of each ADR is required before issue 002 begins.

## Acceptance criteria

- [x] `docs/adr/` directory exists with at least 5 ADRs (stack, monorepo, database, auth, messaging)
- [x] Each ADR follows a consistent format: context, decision, consequences
- [x] ADRs align with TD-001 technology recommendations and EP-001 engineering standards
- [x] A root `README.md` section links to ADRs and describes how to start development
- [x] Human has reviewed and approved all ADRs (record approval date in each ADR) — Saman, 2026-08-28

## Blocked by

None — can start immediately.

## User stories addressed

- SP-001 Ch.2 (Overall Platform Architecture)
- SP-001 Ch.20 (Security, Privacy and Identity — auth approach)
- EP-001 Ch.3 (Repository Structure)
