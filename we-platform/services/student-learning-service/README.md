# Student Learning Service

Owns Student Learning Profiles (SLP) — the central educational record per student.

## Endpoints

- `GET /api/v1/students/{id}/profile` — view SLP (student own profile; teacher for students in their classes)
- `POST /api/v1/students/{id}/profile/enrollments` — sync class enrollment to SLP (admin/service)

## Local development

```bash
dotnet run --project services/student-learning-service/src/Api/StudentLearningService.Api.csproj
```

Default port: **8084** (via launch settings or Docker Compose).

## Tests

```bash
dotnet test services/student-learning-service/tests/StudentLearningService.Tests.csproj
```
