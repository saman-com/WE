# Curriculum Service

Curriculum hierarchy (subject → unit → topic) scoped to an organisation for the WE Platform (issue 005).

Teachers and system administrators have curriculum-management permission. Other authenticated roles receive 403.

## Endpoints

All `/api/v1/curriculum/*` routes require a Bearer JWT from Identity Service.

| Method | Path | Roles | Description |
|--------|------|-------|-------------|
| POST | `/api/v1/curriculum` | Teacher, SystemAdministrator | Create curriculum linked to an organisation |
| GET | `/api/v1/curriculum?organisationId={uuid}` | Teacher, SystemAdministrator | List curricula for a school |
| GET | `/api/v1/curriculum/{id}` | Teacher, SystemAdministrator | Get curriculum |
| PUT | `/api/v1/curriculum/{id}` | Teacher, SystemAdministrator | Update curriculum |
| DELETE | `/api/v1/curriculum/{id}` | Teacher, SystemAdministrator | Delete curriculum (cascades subjects/units/topics) |
| GET | `/api/v1/curriculum/{id}/tree` | Teacher, SystemAdministrator | Navigable subject → unit → topic tree |
| POST/GET/PUT/DELETE | `/api/v1/curriculum/{id}/subjects` | Teacher, SystemAdministrator | Subject CRUD |
| POST/GET/PUT/DELETE | `/api/v1/curriculum/{id}/subjects/{subjectId}/units` | Teacher, SystemAdministrator | Unit CRUD (units cannot exist without a subject) |
| POST/GET/PUT/DELETE | `/api/v1/curriculum/{id}/subjects/{subjectId}/units/{unitId}/topics` | Teacher, SystemAdministrator | Topic CRUD |
| GET | `/health` | No | Health check |

Units always reference a subject in the same curriculum. Deleting a subject removes its units so none are left orphaned.

## Configuration

| Variable | Description |
|----------|-------------|
| `ConnectionStrings__CurriculumDb` | PostgreSQL connection string |
| `Jwt__Key` | Same signing key as Identity Service |
| `Jwt__Issuer` | Token issuer (must match Identity) |
| `Jwt__Audience` | Token audience (must match Identity) |

## Local development

```bash
cd we-platform
docker compose up -d
curl http://localhost:8083/health
```

Schema SQL is versioned in `databases/curriculum/`.

## Tests

```bash
dotnet test services/curriculum-service/tests/CurriculumService.Tests.csproj
```
