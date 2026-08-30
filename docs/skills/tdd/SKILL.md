# Skill: Test-Driven Development (TDD) for WE Platform

## Context

You are implementing one of the platform issues under `issues/`. You must use Test-Driven Development (TDD) so all backend code is deterministic, strongly typed, and respects service boundaries defined in SP-001, TD-001, and EP-001.

Backend services are ASP.NET Core (.NET 9 / C# 13) under `we-platform/services/`.

## Execution Rules

When instructed to implement a feature or task using TDD, you MUST adhere to the following sequence:

1. **Review Standards**: Silently read `docs/skills/tdd/tests.md` to internalize WE Platform testing conventions (xUnit, `WebApplicationFactory`, EF Core InMemory, JWT test helpers, MassTransit in-memory harness).
2. **Write the Test (RED)**:
   - Create or update the corresponding test file in the service's `tests/` project (e.g. `we-platform/services/intervention-service/tests/InterventionEndpointTests.cs`).
   - Write the failing xUnit test method (`[Fact]` or `[Theory]`).
   - Enable nullable reference types; test code must compile cleanly with `<Nullable>enable</Nullable>`.
3. **Verify Failure**:
   - Run the test from `we-platform/`:
     ```bash
     dotnet test services/{service-name}/tests/{ServiceName}.Tests.csproj --filter "FullyQualifiedName~{TestMethodName}"
     ```
   - Confirm the test fails. Do not write the implementation until you have confirmed the test fails.
4. **Implement Code (GREEN)**:
   - Write the minimal C# implementation in the service's `src/` projects (`Domain`, `Application`, `Infrastructure`, `Api`) to make the test pass.
   - Respect service boundaries: each bounded context owns its schema and API; cross-service communication uses domain events or HTTP contracts in `we-platform/shared/`.
5. **Verify Success (DONE WHEN Gate)**:
   - Run the test again:
     ```bash
     dotnet test services/{service-name}/tests/{ServiceName}.Tests.csproj
     ```
   - Verify the solution builds without warnings treated as errors:
     ```bash
     dotnet build WePlatform.sln
     ```
   - Run the full backend test suite when the change could affect shared contracts:
     ```bash
     dotnet test WePlatform.sln
     ```
6. **Refactor**: Clean up the implementation. Keep domain logic in `Application`/`Domain`; keep HTTP and persistence in `Api`/`Infrastructure`.

## Invariants & Guardrails

- **Backend stack**: .NET 9, ASP.NET Core, EF Core 9, xUnit. Do not scaffold Python or Node test runners for backend services.
- **Strict typing**: Code is not complete if `dotnet build` fails or nullable reference warnings are unresolved.
- **Persistence truth**: Assert against persisted state (EF Core InMemory or queried entities), not just HTTP response DTOs, when verifying write operations.
- **Authorization**: Integration tests must exercise role-based access using JWT test helpers and fake access-checker implementations registered in a custom `WebApplicationFactory`.
- **Events**: Consumer tests use MassTransit `InMemoryTestHarness`; do not require RabbitMQ for unit/integration tests.
- **Frontend**: Portal UI work uses separate frontend test tooling (`pnpm`); this skill governs backend C# TDD unless the issue explicitly targets the frontend.
