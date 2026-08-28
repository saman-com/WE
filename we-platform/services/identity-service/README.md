# Identity Service

Built-in authentication for the WE Platform (issue 003).

## Endpoints

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| POST | `/api/v1/auth/login` | No | Login with email/password, returns JWT |
| GET | `/api/v1/auth/me` | Bearer | Returns current user and roles |
| GET | `/api/v1/auth/admin` | Bearer (SystemAdministrator) | RBAC-protected admin endpoint |
| GET | `/health` | No | Health check |

## Seed users (local dev)

| Email | Password | Role |
|-------|----------|------|
| `teacher@school.local` | `Password123!` | Teacher |
| `admin@school.local` | `Password123!` | SystemAdministrator |
| `student@school.local` | `Password123!` | Student (id `22222222-2222-2222-2222-222222222222`) |

## Configuration

Set via environment variables (never commit secrets):

| Variable | Description |
|----------|-------------|
| `ConnectionStrings__IdentityDb` | PostgreSQL connection string |
| `Jwt__Key` | JWT signing key (min 32 characters) |
| `Jwt__Issuer` | Token issuer |
| `Jwt__Audience` | Token audience |

## Local development

```bash
cd we-platform
docker compose up -d
curl -X POST http://localhost:8081/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"teacher@school.local","password":"Password123!"}'
```

## Tests

```bash
dotnet test services/identity-service/tests/IdentityService.Tests.csproj
```
