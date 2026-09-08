namespace Maroik.Core.Service.Extensions;

/// <summary>Helpers for classifying database exceptions without a hard dependency on Npgsql.</summary>
public static class DbExceptionExtensions
{
    /// <summary>PostgreSQL SQLSTATE for a unique / primary-key constraint violation.</summary>
    private const string UniqueViolationSqlState = "23505";

    /// <summary>
    /// Returns <see langword="true"/> when <paramref name="e"/> — or anything in its
    /// <see cref="Exception.InnerException"/> chain — is a PostgreSQL unique-constraint violation
    /// (SQLSTATE <c>23505</c>).
    /// <para>
    /// Primary check: Npgsql's <c>PostgresException</c> exposes the SQLSTATE as a <c>SqlState</c>
    /// (and <c>Code</c>) string property, read here reflectively so <c>Maroik.Core.Service</c> needs
    /// no Npgsql package reference. This is the structured code, not the culture- and
    /// version-dependent message text. As a fallback (wrapped / rethrown / logged exceptions that no
    /// longer carry the typed property) the SQLSTATE is also matched as a substring of any message
    /// in the chain.
    /// </para>
    /// </summary>
    public static bool IsPostgresUniqueViolation(this Exception e)
    {
        for (Exception? current = e; current is not null; current = current.InnerException)
        {
            var type = current.GetType();
            var sqlState =
                type.GetProperty("SqlState")?.GetValue(current) as string
                ?? type.GetProperty("Code")?.GetValue(current) as string;

            if (sqlState == UniqueViolationSqlState)
                return true;

            if (current.Message.Contains(UniqueViolationSqlState, StringComparison.Ordinal))
                return true;
        }

        return false;
    }
}
