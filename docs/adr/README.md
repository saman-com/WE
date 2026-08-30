# Architecture Decision Records (WE Platform)

This directory contains Architecture Decision Records (ADRs) for the WE Platform engineering implementation. ADRs document significant technical decisions aligned with **TD-001** and **EP-001**.

## Index

| ADR | Title | Status |
|-----|-------|--------|
| [0001](0001-technology-stack.md) | Technology Stack | Accepted |
| [0002](0002-monorepo-structure.md) | Monorepo Structure and Tooling | Accepted |
| [0003](0003-database-and-cache.md) | Database and Cache | Accepted |
| [0004](0004-authentication.md) | Authentication and Authorisation | Accepted |
| [0005](0005-messaging-and-events.md) | Messaging and Domain Events | Accepted |
| [0006](0006-api-design.md) | API Design (REST) | Accepted |
| [0007](0007-ai-provider.md) | AI Provider and Gateway | Proposed |

**Approved by:** Saman, 2026-08-28 (0001–0006); ADR-0007 pending human approval

## ADR format

Each ADR includes:

- **Status** — Proposed → Accepted (after human review) → Superseded
- **Context** — Problem and constraints
- **Decision** — What we chose
- **Consequences** — Positive, negative, and neutral outcomes

## Related documentation

- [`../product/SP-001-functional-specification.md`](../product/SP-001-functional-specification.md) — Product functional spec
- [`../architecture/TD-001-technical-architecture.md`](../architecture/TD-001-technical-architecture.md) — TD-001 Technical Architecture
- [`../engineering/EP-001-engineering-package.md`](../engineering/EP-001-engineering-package.md) — EP-001 Engineering Implementation Package
- [`../../issues/001-engineering-bootstrap-adrs.md`](../../issues/001-engineering-bootstrap-adrs.md) — Parent issue
