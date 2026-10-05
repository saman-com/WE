using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;
using WePlatform.Messaging;
using Xunit;

namespace EvidenceApprovalFlow.Tests;

public sealed class MassTransitOutboxInboxSchemaTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithUsername("we")
        .WithPassword("we_dev")
        .Build();

    public async Task InitializeAsync() => await _postgres.StartAsync();

    public async Task DisposeAsync() => await _postgres.DisposeAsync();

    [Fact]
    public async Task EnsureTablesAsync_AddsOutboxInboxToPreExistingDatabase()
    {
        var database = "we_evidence_legacy";
        await using (var admin = new NpgsqlConnection(_postgres.GetConnectionString()))
        {
            await admin.OpenAsync();
            await using var createDb = new NpgsqlCommand($"CREATE DATABASE \"{database}\"", admin);
            await createDb.ExecuteNonQueryAsync();
        }

        var connectionString = new NpgsqlConnectionStringBuilder(_postgres.GetConnectionString())
        {
            Database = database
        }.ConnectionString;

        // Simulate a database created before MassTransit outbox entities existed.
        await using (var connection = new NpgsqlConnection(connectionString))
        {
            await connection.OpenAsync();
            await using var createLegacy = new NpgsqlCommand(
                """
                CREATE TABLE educational_evidence (
                    id uuid PRIMARY KEY,
                    organisation_id uuid NOT NULL,
                    title text NOT NULL
                );
                """,
                connection);
            await createLegacy.ExecuteNonQueryAsync();
        }

        Assert.Equal(0, await CountOutboxTablesAsync(connectionString));

        await using (var db = new ProbeDbContext(connectionString))
        {
            // EnsureCreated is a no-op when any tables already exist.
            await db.Database.EnsureCreatedAsync();
            Assert.Equal(0, await CountOutboxTablesAsync(connectionString));

            await MassTransitOutboxInboxSchema.EnsureTablesAsync(db);
            Assert.Equal(3, await CountOutboxTablesAsync(connectionString));

            // Idempotent on second run.
            await MassTransitOutboxInboxSchema.EnsureTablesAsync(db);
            Assert.Equal(3, await CountOutboxTablesAsync(connectionString));
        }
    }

    private static async Task<long> CountOutboxTablesAsync(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            "select count(*) from information_schema.tables where table_schema = 'public' and table_name in ('InboxState','OutboxState','OutboxMessage')",
            connection);
        return Convert.ToInt64(await command.ExecuteScalarAsync());
    }

    private sealed class ProbeDbContext(string connectionString) : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) =>
            optionsBuilder.UseNpgsql(connectionString);
    }
}
