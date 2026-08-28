# Organisation Service

School structure, classes, and enrollment for the WE Platform (issue 004).

## Endpoints

All `/api/v1/organisations/*` routes require a Bearer JWT from Identity Service.

| Method | Path | Roles | Description |
|--------|------|-------|-------------|
| POST | `/api/v1/organisations` | SystemAdministrator | Create school |
| GET | `/api/v1/organisations` | Admin: all; Teacher/Student: scoped | List schools |
| GET | `/api/v1/organisations/{id}` | Scoped | Get school |
| PUT | `/api/v1/organisations/{id}` | SystemAdministrator | Update school |
| DELETE | `/api/v1/organisations/{id}` | SystemAdministrator | Delete school |
| POST/GET/PUT/DELETE | `/api/v1/organisations/{id}/year-levels` | Admin write; scoped read | Year level CRUD |
| POST/GET/PUT/DELETE | `/api/v1/organisations/{id}/classes` | Admin write; teacher sees assigned classes; student sees enrollments | Class CRUD |
| POST/GET/DELETE | `/api/v1/organisations/{id}/classes/{classId}/teachers` | Admin assign; scoped read | Teacher assignment |
| POST/GET/DELETE | `/api/v1/organisations/{id}/classes/{classId}/enrollments` | Admin enroll; student sees own row | Student enrollment |
| GET | `/health` | No | Health check |

Teachers are denied access to classes they are not assigned to. Students are denied access to classes they are not enrolled in.

## Configuration

| Variable | Description |
|----------|-------------|
| `ConnectionStrings__OrganisationDb` | PostgreSQL connection string |
| `Jwt__Key` | Same signing key as Identity Service |
| `Jwt__Issuer` | Token issuer (must match Identity) |
| `Jwt__Audience` | Token audience (must match Identity) |

## Local development

```bash
cd we-platform
docker compose up -d
curl http://localhost:8082/health
```

Schema SQL is versioned in `databases/organisation/`.

## Tests

```bash
dotnet test services/organisation-service/tests/OrganisationService.Tests.csproj
```
