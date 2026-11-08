using System.Collections.Frozen;
using Maroik.Core.Contract.Enums;
using Maroik.Core.Contract.Exceptions;
using Npgsql;

namespace Maroik.Core.Repository.Translators;

/// <summary>
/// Turns a PostgreSQL unique violation into the <see cref="DuplicateKeyException"/> the services answer, so constraint names
/// and SQL states stay in this layer. Only the constraints listed here are translated: a table can carry a second unique
/// constraint that raises the same SQL state (<c>Calendar_pk</c> beside the calendar name's), and that one is no answer to
/// give a user. It stays the database's own exception, a failure someone should look at.
/// </summary>
internal static class DuplicateKeyTranslator
{
    /// <summary>What each translated constraint or unique index means.</summary>
    private static readonly FrozenDictionary<string, DuplicateKey> _keysByConstraint = new Dictionary<string, DuplicateKey>
    {
        ["Account_pk"] = DuplicateKey.AccountEmail,
        ["Account_Nickname_unique"] = DuplicateKey.AccountNickname,
        // The case-insensitive index on lower("Nickname"): "bob" racing "Bob" is a nickname conflict too, not blamed on the email.
        ["Account_unique_index_0"] = DuplicateKey.AccountNickname,
        ["Asset_pk"] = DuplicateKey.AssetProductName,
        ["Calendar_AccountEmail_Name_unique"] = DuplicateKey.CalendarName,
        ["CalendarShared_pk"] = DuplicateKey.CalendarShared,
    }.ToFrozenDictionary(StringComparer.Ordinal);

    /// <summary>
    /// The <see cref="DuplicateKeyException"/> reporting <paramref name="exception"/>, or <see langword="null"/> when nothing in its
    /// chain is a unique violation of a listed constraint. A save wraps the database's exception in a <c>DbUpdateException</c>;
    /// a raw SQL statement or <c>ExecuteUpdate</c> throws it directly.
    /// </summary>
    internal static DuplicateKeyException? Translate(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: { } constraint }
                && _keysByConstraint.TryGetValue(constraint, out DuplicateKey key))
            {
                return new DuplicateKeyException(key, exception);
            }
        }

        return null;
    }
}
