namespace Maroik.Core.Service.Extensions;

/// <summary>Helpers for classifying database exceptions without a hard dependency on Npgsql.</summary>
public static class DbExceptionExtensions
{
    /// <summary>
    /// Returns <see langword="true"/> when <paramref name="e"/> represents a PostgreSQL
    /// unique-constraint violation (error code 23505). Checks the exception's type name and
    /// message by string rather than an <c>is NpgsqlException</c> cast, so callers do not need
    /// a package reference to Npgsql just to classify the failure.
    /// </summary>
    public static bool IsPostgresUniqueViolation(this Exception e) =>
        (e.InnerException?.Message.Contains("23505") ?? false) ||
        ((e.InnerException?.GetType().Name.Contains("NpgsqlException") ?? false) &&
         (e.InnerException?.Message.Contains("unique", StringComparison.OrdinalIgnoreCase) ?? false));
}
