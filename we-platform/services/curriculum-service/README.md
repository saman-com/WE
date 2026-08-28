# Curriculum Service

Curriculum hierarchy (subject → unit → topic) with learning objectives and micro-skills scoped to an organisation for the WE Platform (issues 005–006).

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
| GET | `/api/v1/curriculum/{id}/tree` | Teacher, SystemAdministrator | Navigable subject → unit → topic → learning objective → micro-skill tree |
| POST/GET/PUT/DELETE | `/api/v1/curriculum/{id}/subjects` | Teacher, SystemAdministrator | Subject CRUD |
| POST/GET/PUT/DELETE | `/api/v1/curriculum/{id}/subjects/{subjectId}/units` | Teacher, SystemAdministrator | Unit CRUD (units cannot exist without a subject) |
| POST/GET/PUT/DELETE | `/api/v1/curriculum/{id}/subjects/{subjectId}/units/{unitId}/topics` | Teacher, SystemAdministrator | Topic CRUD |
| POST/GET/PUT/DELETE | `/api/v1/curriculum/{id}/subjects/{subjectId}/units/{unitId}/learning-objectives` | Teacher, SystemAdministrator | Learning objective CRUD (linked to unit) |
| POST/GET/PUT/DELETE | `/api/v1/curriculum/{id}/subjects/{subjectId}/units/{unitId}/learning-objectives/{loId}/micro-skills` | Teacher, SystemAdministrator | Micro-skill CRUD (linked to learning objective) |
| GET | `/health` | No | Health check |

Units always reference a subject in the same curriculum. Deleting a subject removes its units so none are left orphaned.

**Unit delete with learning objectives:** Deleting a unit cascades to its learning objectives and their micro-skills. Micro-skills are the smallest measurable unit and are referenced by stable IDs in downstream assessment and diagnostic slices.

Deleting a learning objective cascades to its micro-skills.

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
