# ADR-0002: Monorepo Structure and Tooling

## Status

Accepted

## Date

2026-08-28

## Approval

| Reviewer | Date | Decision |
|----------|------|----------|
| Saman | 2026-08-28 | Accepted |

## Context

EP-001 §3.5 defines a modular monorepo where each service is independently buildable and deployable. The platform spans multiple portals, backend services, shared libraries, infrastructure, and AI assets. We need a single repository layout and tooling choices before issue 002 scaffolds the codebase.

## Decision

### Repository layout

Use the EP-001 `we-platform/` tree at the repository root:

```
we-platform/
├── apps/                 # Next.js portals (teacher, student, parent, leadership, admin)
├── services/             # ASP.NET Core microservices (one capability each)
├── shared/               # Cross-cutting libraries (no business logic)
│   ├── contracts/        # Event contracts, DTOs shared across services
│   ├── auth/             # JWT validation helpers
│   └── ui/               # Shared React component library (optional, later)
├── infrastructure/       # Terraform, Kubernetes manifests, Helm charts
├── databases/            # SQL migrations, seeds per service
├── ai/                   # Prompts, eval datasets, Python AI adapters
├── scripts/              # Setup, migration, automation scripts
├── tools/                # Code generators, analysers
├── testing/              # Integration/E2E test suites, test data
├── deployment/           # CI/CD pipeline definitions, release configs
├── monitoring/           # Dashboards, alert rules, tracing config
├── security/             # Security policies, scanning configs
├── examples/             # Reference service patterns
└── README.md             # Monorepo developer guide
```

Repository root (outside `we-platform/`):

```
/
├── docs/
│   ├── adr/              # Architecture Decision Records
│   ├── product/          # BP-001, SP-001
│   ├── architecture/     # TD-001
│   ├── engineering/      # EP-001
│   └── validation/       # Phase gate reports
├── issues/               # Vertical-slice implementation backlog
└── README.md             # Project overview
```

### Service internal layout (per EP-001 §4)

Each service under `services/<name>/`:

```
services/<name>/
├── src/
│   ├── Api/              # HTTP endpoints, middleware
│   ├── Application/      # Use cases, commands, queries
│   ├── Domain/           # Entities, value objects, domain events
│   └── Infrastructure/   # EF Core, messaging, external adapters
├── tests/
├── Dockerfile
└── README.md
```

### Tooling

| Concern | Tool |
|---------|------|
| .NET solution | Single `WePlatform.sln` at `we-platform/` referencing all services |
| Frontend workspaces | pnpm workspaces (`pnpm-workspace.yaml` in `we-platform/`) |
| Local infrastructure | `docker-compose.yml` at `we-platform/` (Postgres, Redis, RabbitMQ) |
| CI/CD | GitHub Actions (`.github/workflows/` at repo root) |
| Code style | EditorConfig, dotnet format, ESLint + Prettier |
| Git branching | `main` (protected), `feature/*`, `bugfix/*` per EP-001 Ch.15 |

### Naming conventions (EP-001 §3.7)

- Directories and repos: `lowercase-with-hyphens`
- C# types: PascalCase
- API routes: `kebab-case` segments under `/api/v1/`
- Event names: PascalCase past tense (`EvidenceCreated`, `AssessmentApproved`)

## Consequences

### Positive

- Matches EP-001 handbook exactly; new engineers have a clear map
- Shared contracts in `shared/` prevent event schema drift
- Single CI pipeline can build and test the full platform
- `examples/` enables copy-paste reference patterns for AI agent implementation

### Negative

- Large monorepo clone size grows over time; mitigated by sparse checkout if needed
- pnpm + .NET dual toolchain requires both SDKs in dev environment

### Neutral

- Microservices are logically separate but colocated; extraction to separate repos is possible later per TD-001 Phase 2 evolution

## References

- EP-001 `docs/engineering/EP-001-engineering-package.md` §3.5–3.7, Ch.4 (Backend layer structure)
- TD-001 `docs/architecture/TD-001-technical-architecture.md` §2.6 Modular Monolith First, Microservices Ready
- SP-001 Ch.2 (Overall Platform Architecture)
