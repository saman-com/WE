extern alias evidence;
extern alias learning;
extern alias diagnostic;
extern alias gaps;
extern alias mastery;

using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Xunit;

namespace EvidenceApprovalFlow.Tests;

public sealed class FlowFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithUsername("we")
        .WithPassword("we_dev")
        .Build();

    private readonly RabbitMqContainer _rabbitMq = new RabbitMqBuilder()
        .WithImage("heidiks/rabbitmq-delayed-message-exchange:3.13.3-management")
        .WithUsername("we")
        .WithPassword("we_dev")
        .Build();

    public string PostgresHostConnectionString { get; private set; } = string.Empty;
    public string RabbitHost { get; private set; } = "localhost";
    public int RabbitPort { get; private set; }
    public string RabbitUsername => "we";
    public string RabbitPassword => "we_dev";

    public HttpClient EvidenceClient { get; private set; } = null!;
    public HttpClient LearningClient { get; private set; } = null!;

    private WebApplicationFactory<evidence::Program> _evidenceFactory = null!;
    private WebApplicationFactory<learning::Program> _learningFactory = null!;
    private WebApplicationFactory<diagnostic::Program> _diagnosticFactory = null!;
    private WebApplicationFactory<gaps::Program> _gapFactory = null!;
    private WebApplicationFactory<mastery::Program> _masteryFactory = null!;

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_postgres.StartAsync(), _rabbitMq.StartAsync());

        PostgresHostConnectionString = _postgres.GetConnectionString();
        CaptureRabbitEndpoint();

        await CreateDatabasesAsync(
            "we_evidence_it",
            "we_learning_it",
            "we_diagnostic_it",
            "we_gaps_it",
            "we_mastery_it");

        await StartHostsAsync();
        await Task.Delay(TimeSpan.FromSeconds(2));
    }

    public async Task DisposeAsync()
    {
        await DisposeHostsAsync();
        await _rabbitMq.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    public string Db(string database) =>
        new NpgsqlConnectionStringBuilder(PostgresHostConnectionString) { Database = database }.ConnectionString;

    public async Task StopRabbitAsync() => await _rabbitMq.StopAsync();

    public async Task RecoverRabbitAndRestartHostsAsync()
    {
        await DisposeHostsAsync();
        await _rabbitMq.StartAsync();
        CaptureRabbitEndpoint();
        await StartHostsAsync();
        await Task.Delay(TimeSpan.FromSeconds(2));
    }

    public async Task SeedStudentProfileAsync(Guid organisationId, string studentUserId)
    {
        var adminId = Guid.NewGuid().ToString();
        var classId = Guid.CreateVersion7();
        using var request = TestJwt.Authorized(
            HttpMethod.Post,
            $"/api/v1/students/{studentUserId}/profile/enrollments",
            adminId,
            organisationId,
            TestJwt.AdminRole);
        request.Content = JsonContent.Create(new
        {
            organisationId,
            classId,
            className = "Year 11 Mathematics",
            classCode = "11MAT"
        });

        var response = await LearningClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    public async Task<Guid> ApproveEvidenceAsync(
        Guid organisationId,
        string teacherId,
        string studentId,
        Guid classId,
        Guid assessmentId,
        Guid submissionId,
        string title,
        IReadOnlyList<(Guid MicroSkillId, decimal Mark, string Feedback)> marks)
    {
        using var request = TestJwt.Authorized(
            HttpMethod.Post,
            "/api/v1/evidence",
            teacherId,
            organisationId,
            TestJwt.TeacherRole);
        request.Content = JsonContent.Create(new
        {
            organisationId,
            classId,
            assessmentId,
            submissionId,
            studentUserId = studentId,
            title,
            microSkillMarks = marks.Select(m => new
            {
                microSkillId = m.MicroSkillId,
                mark = m.Mark,
                feedback = m.Feedback
            }).ToList()
        });

        var response = await EvidenceClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<EvidenceCreatedResponse>();
        return payload?.Id ?? throw new InvalidOperationException("Missing evidence id.");
    }

    public async Task WaitForAsync(Func<Task<bool>> condition, TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(45));
        Exception? lastError = null;
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                if (await condition())
                {
                    return;
                }
            }
            catch (Exception ex)
            {
                lastError = ex;
            }

            await Task.Delay(250);
        }

        throw new TimeoutException(
            lastError is null
                ? "Condition was not met before timeout."
                : $"Condition was not met before timeout. Last error: {lastError.Message}");
    }

    public async Task<long> ScalarAsync(string database, string sql)
    {
        await using var connection = new NpgsqlConnection(Db(database));
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        var result = await command.ExecuteScalarAsync();
        return result is null or DBNull ? 0 : Convert.ToInt64(result);
    }

    private void CaptureRabbitEndpoint()
    {
        RabbitHost = _rabbitMq.Hostname;
        RabbitPort = _rabbitMq.GetMappedPublicPort(5672);
    }

    private async Task StartHostsAsync()
    {
        _evidenceFactory = new EvidenceFactory(this);
        _learningFactory = new LearningFactory(this);
        _diagnosticFactory = new DiagnosticFactory(this);
        _gapFactory = new GapFactory(this);
        _masteryFactory = new MasteryFactory(this);

        EvidenceClient = _evidenceFactory.CreateClient();
        LearningClient = _learningFactory.CreateClient();
        _ = _diagnosticFactory.CreateClient();
        _ = _gapFactory.CreateClient();
        _ = _masteryFactory.CreateClient();
        await Task.CompletedTask;
    }

    private async Task DisposeHostsAsync()
    {
        EvidenceClient?.Dispose();
        LearningClient?.Dispose();
        if (_evidenceFactory is not null)
        {
            await _evidenceFactory.DisposeAsync();
        }

        if (_learningFactory is not null)
        {
            await _learningFactory.DisposeAsync();
        }

        if (_diagnosticFactory is not null)
        {
            await _diagnosticFactory.DisposeAsync();
        }

        if (_gapFactory is not null)
        {
            await _gapFactory.DisposeAsync();
        }

        if (_masteryFactory is not null)
        {
            await _masteryFactory.DisposeAsync();
        }
    }

    private async Task CreateDatabasesAsync(params string[] names)
    {
        await using var connection = new NpgsqlConnection(PostgresHostConnectionString);
        await connection.OpenAsync();
        foreach (var name in names)
        {
            await using var command = new NpgsqlCommand($"CREATE DATABASE \"{name}\"", connection);
            await command.ExecuteNonQueryAsync();
        }
    }

    private void ApplyCommonConfig(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:EvidenceDb", Db("we_evidence_it"));
        builder.UseSetting("ConnectionStrings:StudentLearningDb", Db("we_learning_it"));
        builder.UseSetting("ConnectionStrings:DiagnosticDb", Db("we_diagnostic_it"));
        builder.UseSetting("ConnectionStrings:GapDb", Db("we_gaps_it"));
        builder.UseSetting("ConnectionStrings:MasteryDb", Db("we_mastery_it"));
        builder.UseSetting("RabbitMQ:Host", RabbitHost);
        builder.UseSetting("RabbitMQ:Port", RabbitPort.ToString());
        builder.UseSetting("RabbitMQ:Username", RabbitUsername);
        builder.UseSetting("RabbitMQ:Password", RabbitPassword);
        builder.UseSetting("RabbitMQ:DelayedRedeliveryIntervalsSeconds", "2,5,10");
        builder.UseSetting("Jwt:Issuer", TestJwt.Issuer);
        builder.UseSetting("Jwt:Audience", TestJwt.Audience);
        builder.UseSetting("Jwt:Key", TestJwt.Key);
    }

    private sealed class EvidenceFactory(FlowFixture fixture)
        : WebApplicationFactory<evidence::Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            fixture.ApplyCommonConfig(builder);
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<evidence::EvidenceService.Application.IClassAccessChecker>();
                services.AddSingleton<evidence::EvidenceService.Application.IClassAccessChecker>(
                    new AllowAllClassAccess());
            });
        }
    }

    private sealed class LearningFactory(FlowFixture fixture)
        : WebApplicationFactory<learning::Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            fixture.ApplyCommonConfig(builder);
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<learning::StudentLearningService.Application.IOrganisationAccessChecker>();
                services.AddSingleton<learning::StudentLearningService.Application.IOrganisationAccessChecker>(
                    new AllowAllLearningOrgAccess());
            });
        }
    }

    private sealed class DiagnosticFactory(FlowFixture fixture)
        : WebApplicationFactory<diagnostic::Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            fixture.ApplyCommonConfig(builder);
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<diagnostic::DiagnosticService.Application.IOrganisationAccessChecker>();
                services.AddSingleton<diagnostic::DiagnosticService.Application.IOrganisationAccessChecker>(
                    new AllowAllDiagnosticOrgAccess());
            });
        }
    }

    private sealed class GapFactory(FlowFixture fixture)
        : WebApplicationFactory<gaps::Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            fixture.ApplyCommonConfig(builder);
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<gaps::LearningGapService.Application.IOrganisationAccessChecker>();
                services.AddSingleton<gaps::LearningGapService.Application.IOrganisationAccessChecker>(
                    new AllowAllGapOrgAccess());
            });
        }
    }

    private sealed class MasteryFactory(FlowFixture fixture)
        : WebApplicationFactory<mastery::Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            fixture.ApplyCommonConfig(builder);
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<mastery::MasteryService.Application.IOrganisationAccessChecker>();
                services.AddSingleton<mastery::MasteryService.Application.IOrganisationAccessChecker>(
                    new AllowAllMasteryOrgAccess());
            });
        }
    }

    private sealed record EvidenceCreatedResponse(Guid Id);
}

[CollectionDefinition(FlowCollection.Name)]
public sealed class FlowCollection : ICollectionFixture<FlowFixture>
{
    public const string Name = "evidence-approval-flow";
}
