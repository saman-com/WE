# Sample Service

Reference ASP.NET Core service demonstrating the WE Platform service pattern.

## Endpoints

| Method | Path | Description |
|--------|------|-------------|
| GET | `/health` | Health check — returns `200 OK` |

## Local development

```bash
cd we-platform/examples/sample-service
dotnet run --project src/Api
curl http://localhost:5000/health
```

## Docker

```bash
docker build -t we-sample-service .
docker run -p 8080:8080 we-sample-service
curl http://localhost:8080/health
```

## Tests

```bash
dotnet test tests/SampleService.Tests.csproj
```
