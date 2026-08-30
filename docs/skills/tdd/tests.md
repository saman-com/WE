# Testing Guidelines for WE Platform (.NET / xUnit)

Per **SP-001**, **TD-001**, and **EP-001**, deterministic testing, strong typing, and validation of service boundaries are mandatory for backend implementation tasks. Tests must prove correct behaviour, authorization, and persistence—not just happy-path HTTP status codes.

## 1. Core Testing Stack

| Concern | Choice |
|---------|--------|
| Runtime | .NET 9 |
| Language | C# 13 (`<Nullable>enable</Nullable>`) |
| Test framework | xUnit 2.x |
| API integration tests | `Microsoft.AspNetCore.Mvc.Testing` (`WebApplicationFactory<TProgram>`) |
| Persistence (tests) | EF Core InMemory (`UseInMemoryDatabase`) in `Testing` environment |
| Messaging (tests) | MassTransit `InMemoryTestHarness` |
| Coverage | `coverlet.collector` (via `dotnet test --collect:"XPlat Code Coverage"`) |

Standard test project packages (see `examples/sample-service/tests/SampleService.Tests.csproj`):

- `Microsoft.NET.Test.Sdk`
- `xunit` / `xunit.runner.visualstudio`
- `coverlet.collector`
- `Microsoft.AspNetCore.Mvc.Testing` (for API services)

## 2. Structural Conventions

Each microservice under `we-platform/services/{service-name}/` follows a layered layout. Tests live in a sibling `tests/` project that references the service `Api` project.

```text
we-platform/services/intervention-service/
  src/
    Api/
    Application/
    Domain/
    Infrastructure/
  tests/
    InterventionService.Tests.csproj
    InterventionEndpointTests.cs
    InterventionWebApplicationFactory.cs
    TestJwt.cs
    FakeOrganisationAccessChecker.cs
```

**Naming**

- Test project: `{ServiceName}.Tests.csproj`
- Integration tests: `{Feature}EndpointTests.cs`
- Domain/application unit tests: `{Component}Tests.cs` or `{Engine}Tests.cs`
- Test factory: `{Service}WebApplicationFactory.cs`
- Test methods: descriptive `PascalCase` names (e.g. `Teacher_CreatesInterventionLinkedToStudentAndGap`)

**Test types**

1. **Endpoint / integration tests** — HTTP calls through `HttpClient`, JWT auth, fake external dependencies wired via `WebApplicationFactory`.
2. **Application / domain unit tests** — pure logic classes with no HTTP or database (e.g. calculation engines, status transition rules).
3. **Event consumer tests** — MassTransit in-memory harness verifying message handling.

## 3. WE Platform Testing Invariants

When practicing TDD for platform issues, enforce these constraints during test authoring:

### A. Persistence as Source of Truth

PostgreSQL is the production database; tests use EF Core InMemory in the `Testing` environment.

- **Rule**: Tests verifying create/update/delete operations should assert persisted entity state (via repository, `DbContext`, or follow-up GET), not only the response body.
- **Rule**: Register InMemory databases in `Infrastructure/DependencyInjection.cs` when `IHostEnvironment.IsEnvironment("Testing")`.

### B. Determinism

Business logic and aggregation must be replayable and stable.

- **Rule**: Pin non-deterministic inputs in tests (`DateTimeOffset.UtcNow`, `Guid.CreateVersion7()` used as fixed values in arrange steps).
- **Rule**: Pure calculation engines must produce identical output for identical input (see `GapCalculationEngineTests.CalculateFromDiagnostics_ProducesDeterministicGaps`).

### C. Authorization Boundaries

Access control is enforced per role and organisation scope.

- **Rule**: Integration tests must cover allowed and denied actors (teacher, school leader, student, unassigned teacher).
- **Rule**: Replace `IOrganisationAccessChecker` (and similar ports) with fake implementations in `WebApplicationFactory.ConfigureWebHost`.
- **Rule**: Issue JWTs via a `TestJwt` helper; never disable auth middleware in tests.

### D. Domain Events

Cross-service communication uses versioned contracts in `we-platform/shared/events/`.

- **Rule**: Consumer tests publish contract types through `InMemoryTestHarness` and assert on consumed message content.
- **Rule**: Do not require a running RabbitMQ broker for unit or service tests.

## 4. The TDD Workflow (Red, Green, Refactor, Verify)

1. **Write the Test (RED)**: Add an xUnit `[Fact]` or `[Theory]` describing expected behaviour. Run `dotnet test` and confirm failure.
2. **Write the Code (GREEN)**: Implement the minimal C# code in the appropriate layer to satisfy the test.
3. **Refactor**: Extract shared arrange helpers; keep tests readable with Arrange / Act / Assert structure.
4. **Verify (DONE WHEN)**:
   - `dotnet test services/{service}/tests/{Service}.Tests.csproj` passes.
   - `dotnet build WePlatform.sln` succeeds.
   - Issue acceptance criteria (e.g. "Tests cover CRUD and status transitions") are met.

## 5. Syntax and Examples

### API Integration Test with JWT and WebApplicationFactory

```csharp
using System.Net;
using System.Net.Http.Json;
using InterventionService.Application;

namespace InterventionService.Tests;

public class InterventionEndpointTests : IClassFixture<InterventionWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly FakeOrganisationAccessChecker _accessChecker;

    public InterventionEndpointTests(InterventionWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        _accessChecker = factory.AccessChecker;
    }

    [Fact]
    public async Task Teacher_CreatesInterventionLinkedToStudentAndGap()
    {
        var teacherId = Guid.NewGuid().ToString();
        var studentId = Guid.NewGuid().ToString();
        _accessChecker.AllowTeacher(teacherId, studentId);

        using var request = TestJwt.Authorized(
            HttpMethod.Post,
            "/api/v1/interventions",
            teacherId,
            TestJwt.TeacherRole);
        request.Content = JsonContent.Create(new CreateInterventionRequest(
            Guid.CreateVersion7(),
            studentId,
            Guid.CreateVersion7(),
            "Guided practice.",
            null,
            null,
            null,
            null));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<InterventionResponse>();
        Assert.Equal(studentId, created!.StudentUserId);
    }
}
```

### Custom WebApplicationFactory

```csharp
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace InterventionService.Tests;

public sealed class InterventionWebApplicationFactory : WebApplicationFactory<Program>
{
    public FakeOrganisationAccessChecker AccessChecker { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IOrganisationAccessChecker>();
            services.AddSingleton<IOrganisationAccessChecker>(AccessChecker);
        });
    }
}
```

### Parametrized Domain Logic Test

Use `[Theory]` and `[InlineData]` for matrix conditions (status transitions, severity rules, scoring thresholds).

```csharp
namespace InterventionService.Tests;

public class InterventionStatusTransitionTests
{
    [Theory]
    [InlineData(InterventionStatuses.Planned, InterventionStatuses.Active, true)]
    [InlineData(InterventionStatuses.Planned, InterventionStatuses.Closed, false)]
    public void CanTransition_RespectsLifecycle(string current, string next, bool expected) =>
        Assert.Equal(expected, InterventionStatusTransitions.CanTransition(current, next));
}
```

### Domain Event Consumer Test

```csharp
using EventSubscriberService.Api.Consumers;
using MassTransit;
using MassTransit.Testing;
using WePlatform.Events;

namespace EventSubscriberService.Tests;

public class DomainEventConsumerTests
{
    [Fact]
    public async Task EvidenceCreatedConsumer_ReceivesPublishedEvent()
    {
        var harness = new InMemoryTestHarness();
        var consumerHarness = harness.Consumer(() =>
            new EvidenceCreatedConsumer(
                Microsoft.Extensions.Logging.Abstractions.NullLogger<EvidenceCreatedConsumer>.Instance));

        await harness.Start();
        try
        {
            var domainEvent = CreateEvidenceCreated();
            await harness.Bus.Publish(domainEvent);

            Assert.True(await consumerHarness.Consumed.Any<EvidenceCreated>());
            var context = consumerHarness.Consumed.Select<EvidenceCreated>().First().Context;
            Assert.Equal(domainEvent.EventId, context.Message.EventId);
        }
        finally
        {
            await harness.Stop();
        }
    }
}
```

## 6. Running Tests

From `we-platform/`:

```bash
# Single service
dotnet test services/intervention-service/tests/InterventionService.Tests.csproj

# Filter to one test
dotnet test services/intervention-service/tests/InterventionService.Tests.csproj \
  --filter "FullyQualifiedName~Teacher_CreatesInterventionLinkedToStudentAndGap"

# Full backend suite
dotnet test WePlatform.sln

# With coverage collection
dotnet test WePlatform.sln --collect:"XPlat Code Coverage"
```

## Summary for the AI Coding Partner

When instructed to use TDD:

1. Read the issue file under `issues/` for acceptance criteria only—do not invent requirements.
2. Scaffold or extend `tests/*.cs` in the target service **first**.
3. Write xUnit tests that cover authorization, persistence, and domain rules as applicable.
4. Implement the minimal C# code across `Domain` / `Application` / `Infrastructure` / `Api`.
5. Do not mark the task complete until `dotnet test` passes for the affected projects and the issue acceptance criteria are satisfied.
