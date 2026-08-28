# WE Platform Monorepo

Application code for the Educational Intelligence Platform.

## Prerequisites

| Tool | Version |
|------|---------|
| .NET SDK | 9.0+ |
| Node.js | 20+ |
| pnpm | 9+ |
| Docker | 24+ |

## Quick start

```bash
# 1. Start infrastructure (PostgreSQL, Redis, RabbitMQ) and sample API
cd we-platform
docker compose up -d

# 2. Verify sample service health
curl http://localhost:8080/health

# 3. Run backend tests
dotnet test WePlatform.sln

# 4. Run frontend (copy .env.local.example to .env.local first)
cp apps/web-portal/.env.local.example apps/web-portal/.env.local
pnpm install
pnpm dev
```

The web portal runs at http://localhost:3000.

## Directory layout

```
we-platform/
├── apps/              # Next.js portals
├── services/          # ASP.NET Core microservices
├── shared/            # Cross-cutting libraries (contracts, auth)
├── infrastructure/    # Terraform, Kubernetes, Helm
├── databases/         # SQL migrations and seeds
├── ai/                # Python AI adapters and prompts
├── scripts/           # Automation scripts
├── tools/             # Code generators
├── testing/           # Integration and E2E tests
├── deployment/        # Release configs
├── monitoring/        # Dashboards and alerts
├── security/          # Security policies
├── examples/          # Reference patterns (sample-service)
└── docker-compose.yml # Local dev stack
```

## Services

| Service | Port | Description |
|---------|------|-------------|
| PostgreSQL | 5432 | Primary database |
| Redis | 6379 | Cache |
| RabbitMQ | 5672 / 15672 | Message broker (management UI) |
| sample-service | 8080 | Reference API with `/health` |
| identity-service | 8081 | Authentication (`/api/v1/auth/login`, `/api/v1/auth/me`) |

## Development

```bash
# Backend
dotnet build WePlatform.sln
dotnet test WePlatform.sln

# Frontend
pnpm install
pnpm dev      # Start web-portal
pnpm build    # Production build
pnpm lint     # ESLint
```

See [`examples/sample-service/README.md`](examples/sample-service/README.md) for the reference service pattern.
