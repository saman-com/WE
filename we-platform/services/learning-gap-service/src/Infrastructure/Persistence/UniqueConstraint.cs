using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace LearningGapService.Infrastructure.Persistence;

internal static class UniqueConstraint
{
    public static bool IsViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
}
