namespace Maroik.Core.Service.Extensions;

/// <summary>Helpers for classifying database exceptions without a hard dependency on Npgsql.</summary>
public static class DbExceptionExtensions
{
    /// <summary>PostgreSQL SQLSTATE for a unique / primary-key constraint violation.</summary>
    private const string UniqueViolationSqlState = "23505";

    extension(Exception e)
    {
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
        public bool IsPostgresUniqueViolation()
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

        /// <summary>
        /// Returns <see langword="true"/> when <paramref name="e"/> — or anything in its
        /// <see cref="Exception.InnerException"/> chain — is a PostgreSQL unique-constraint violation
        /// (SQLSTATE <c>23505</c>) specifically on the constraint named <paramref name="constraintName"/>.
        /// <para>
        /// A table can carry more than one unique constraint that all raise the same SQLSTATE (e.g. a
        /// surrogate-key primary key alongside a separate business-key uniqueness constraint), so
        /// <see cref="IsPostgresUniqueViolation"/> alone cannot tell them apart. This checks Npgsql's
        /// <c>PostgresException.ConstraintName</c> (read reflectively, for the same no-hard-dependency
        /// reason as above), falling back to matching the constraint name as a substring of the
        /// exception message for exceptions that no longer carry the typed property.
        /// </para>
        /// </summary>
        public bool IsPostgresUniqueViolationOn(string constraintName)
        {
            for (Exception? current = e; current is not null; current = current.InnerException)
            {
                var type = current.GetType();
                var sqlState =
                    type.GetProperty("SqlState")?.GetValue(current) as string
                    ?? type.GetProperty("Code")?.GetValue(current) as string;

                bool isUniqueViolation = sqlState == UniqueViolationSqlState
                                         || current.Message.Contains(UniqueViolationSqlState, StringComparison.Ordinal);
                if (!isUniqueViolation)
                    continue;

                var constraint = type.GetProperty("ConstraintName")?.GetValue(current) as string;
                if (constraint == constraintName)
                    return true;
                if (constraint is null && current.Message.Contains(constraintName, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }
    }
}
