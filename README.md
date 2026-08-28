# WE Platform

An **Educational Intelligence Platform** that connects curriculum, assessment, evidence, student learning profiles, diagnostics, learning gaps, interventions, and AI-assisted teaching into one continuous educational workflow.

## Documentation

All specifications and engineering docs are in [`docs/`](docs/):

| Code | Document | Description |
|------|----------|-------------|
| BP-001 | [`docs/product/BP-001-business-package.md`](docs/product/BP-001-business-package.md) | Vision, benefits, phased rollout |
| SP-001 | [`docs/product/SP-001-functional-specification.md`](docs/product/SP-001-functional-specification.md) | Functional spec (parent PRD) |
| TD-001 | [`docs/architecture/TD-001-technical-architecture.md`](docs/architecture/TD-001-technical-architecture.md) | Technical architecture |
| EP-001 | [`docs/engineering/EP-001-engineering-package.md`](docs/engineering/EP-001-engineering-package.md) | Engineering implementation package |

See [`docs/README.md`](docs/README.md) for the full documentation index.

## Architecture Decision Records

Engineering decisions are documented in [`docs/adr/`](docs/adr/) — **all accepted** as of 2026-08-28:

| ADR | Decision |
|-----|----------|
| [0001](docs/adr/0001-technology-stack.md) | .NET 9 backend, Next.js frontend, TypeScript, Tailwind |
| [0002](docs/adr/0002-monorepo-structure.md) | `we-platform/` monorepo layout and tooling |
| [0003](docs/adr/0003-database-and-cache.md) | PostgreSQL 16 + Redis 7, DB-per-service |
| [0004](docs/adr/0004-authentication.md) | OAuth 2.0 / OIDC / JWT, RBAC |
| [0005](docs/adr/0005-messaging-and-events.md) | RabbitMQ + MassTransit, domain events |
| [0006](docs/adr/0006-api-design.md) | REST `/api/v1/`, OpenAPI, YARP gateway |

## Implementation backlog

The platform is built as **46 vertical slices** in [`issues/`](issues/), ordered by dependency:

```
001 Engineering ADRs ✅ → 002 Monorepo scaffold ✅ → 003 Auth → ... → 046 Platform gate
```

Next: [`issues/003-identity-auth.md`](issues/003-identity-auth.md)

### How to implement an issue

1. Open the issue file in Cursor agent chat
2. Prompt: *"Implement this issue completely. Follow EP-001 standards and TD-001 architecture. Run tests before finishing."*
3. Verify acceptance criteria
4. Commit and proceed to the next unblocked issue

## Getting started (development)

### Prerequisites

| Tool | Version |
|------|---------|
| .NET SDK | 9.0+ |
| Node.js | 20+ |
| pnpm | 9+ |
| Docker | 24+ |

### Local setup

```bash
cd we-platform

# Start PostgreSQL, Redis, RabbitMQ, and the sample API
docker compose up -d

# Verify sample service
curl http://localhost:8080/health

# Backend: build and test
dotnet build WePlatform.sln
dotnet test WePlatform.sln

# Frontend: install and run
pnpm install
pnpm dev
```

The web portal runs at http://localhost:3000. See [`we-platform/README.md`](we-platform/README.md) for full details.

## Core principles

- **Learning is the centre** — every feature must improve educational outcomes
- **Teachers remain decision-makers** — AI assists, never replaces professional judgement
- **Evidence is immutable** after teacher approval
- **EI ≠ AI** — Educational Intelligence is deterministic; AI handles communication
- **Least privilege** — RBAC on every endpoint

## Repository structure

```
/
├── docs/
│   ├── adr/              # Architecture Decision Records
│   ├── product/          # BP-001, SP-001
│   ├── architecture/     # TD-001
│   ├── engineering/      # EP-001
│   └── validation/       # Phase gate reports (created during build)
├── issues/               # Vertical-slice implementation backlog
├── we-platform/          # Application code (created in issue 002)
│   ├── apps/             # Web portals
│   ├── services/         # Backend microservices
│   └── shared/           # Contracts, auth helpers
└── README.md
```

## License

TBD
