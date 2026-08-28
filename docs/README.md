# WE Platform Documentation

All product, architecture, and engineering documentation for the WE Platform.

## Product specifications

| Code | Document | Description |
|------|----------|-------------|
| BP-001 | [`product/BP-001-business-package.md`](product/BP-001-business-package.md) | Vision, benefits, phased rollout |
| SP-001 | [`product/SP-001-functional-specification.md`](product/SP-001-functional-specification.md) | Functional spec (parent PRD for implementation) |

## Architecture & engineering

| Code | Document | Description |
|------|----------|-------------|
| TD-001 | [`architecture/TD-001-technical-architecture.md`](architecture/TD-001-technical-architecture.md) | Technical architecture and system design |
| EP-001 | [`engineering/EP-001-engineering-package.md`](engineering/EP-001-engineering-package.md) | Engineering implementation package |

## Architecture Decision Records

Accepted engineering decisions live in [`adr/`](adr/):

| ADR | Title | Status |
|-----|-------|--------|
| [0001](adr/0001-technology-stack.md) | Technology Stack | Accepted |
| [0002](adr/0002-monorepo-structure.md) | Monorepo Structure and Tooling | Accepted |
| [0003](adr/0003-database-and-cache.md) | Database and Cache | Accepted |
| [0004](adr/0004-authentication.md) | Authentication and Authorisation | Accepted |
| [0005](adr/0005-messaging-and-events.md) | Messaging and Domain Events | Accepted |
| [0006](adr/0006-api-design.md) | API Design (REST) | Accepted |

## Validation reports

Created during HITL validation gate issues (013, 021, 026, 032, 037, 041, 046):

```
docs/validation/   # Created as issues are completed
```

## Document roadmap

| Code | Document | Status |
|------|----------|--------|
| BP-001 | Business Package | Complete |
| SP-001 | Functional Specification | Complete |
| TD-001 | Technical Architecture | Complete |
| EP-001 | Engineering Package | Complete |
| UX-001 | UI/UX Design System | Future |
| PM-001 | Product Management Guide | Future |
| OP-001 | Operations Manual | Future |

## Implementation backlog

Vertical-slice issues are in [`../issues/`](../issues/). Start with [`../issues/001-engineering-bootstrap-adrs.md`](../issues/001-engineering-bootstrap-adrs.md).
