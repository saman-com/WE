## Type

HITL

## Parent PRD

`SP-001_WE_Platform_Functional_Specification.md`

Supporting references: `ABC_EP001.md` (Ch.1–3), `ABC.md` (Ch.2)

## What to build

Establish the engineering foundation before any application code. Create `docs/adr/` and write Architecture Decision Records (ADRs) that lock the technology stack, monorepo tooling, and local development approach per EP-001 Ch.3 and TD-001 Ch.2.

ADRs must cover at minimum: backend (.NET 9 / ASP.NET Core), frontend (React + Next.js + TypeScript + Tailwind), database (PostgreSQL), cache (Redis), message broker (RabbitMQ), API style (REST `/api/v1/`), auth approach (OAuth 2.0 / OIDC / JWT), and monorepo directory layout (`we-platform/` tree from EP-001 §3.5).

This slice produces documentation only — no application services yet. Human review and approval of each ADR is required before issue 002 begins.

## Acceptance criteria

- [ ] `docs/adr/` directory exists with at least 5 ADRs (stack, monorepo, database, auth, messaging)
- [ ] Each ADR follows a consistent format: context, decision, consequences
- [ ] ADRs align with TD-001 technology recommendations and EP-001 engineering standards
- [ ] A root `README.md` section links to ADRs and describes how to start development
- [ ] Human has reviewed and approved all ADRs (record approval date in each ADR)

## Blocked by

None — can start immediately.

## User stories addressed

- SP-001 Ch.2 (Overall Platform Architecture)
- SP-001 Ch.20 (Security, Privacy and Identity — auth approach)
- EP-001 Ch.3 (Repository Structure)
