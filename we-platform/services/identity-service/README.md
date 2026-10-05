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
| `parent@school.local` | `Password123!` | Parent (id `33333333-3333-3333-3333-333333333333`) |
| `authority@ministry.local` | `Password123!` | EducationAuthorityOfficer |
| `leader@school.local` | `Password123!` | SchoolLeader |
| `federation@ministry.local` | `Password123!` | FederationAdmin |
| `teacher-b@schoolb.local` | `Password123!` | Teacher (School B tenant) |
| `student-b@schoolb.local` | `Password123!` | Student (School B tenant) |

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
