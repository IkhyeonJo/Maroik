using Maroik.Core.Contract.Constants;
using Maroik.Core.Contract.Dtos;

namespace Maroik.Core.Contract.Extensions;

/// <summary>
/// Extension methods encoding the "owner" and "owner-or-admin" authorization rules
/// used to gate edit/delete UI affordances for board posts, comments, and private notes.
/// </summary>
public static class AccountResponseExtensions
{
    extension(AccountResponse viewer)
    {
        /// <summary>True when <paramref name="contentWriter"/> is this viewer's own nickname.</summary>
        public bool IsOwner(string? contentWriter) => viewer.Nickname == contentWriter;

        /// <summary>True when the viewer wrote the content, or the viewer is an Admin.</summary>
        public bool IsOwnerOrAdmin(string? contentWriter) => viewer.Nickname == contentWriter || viewer.Role == Role.Admin;
    }

    extension(IEnumerable<AccountResponse> accounts)
    {
        /// <summary>Nicknames of every Admin account in the collection, for O(1) "is this writer an admin?" lookups.</summary>
        public HashSet<string> ToAdminNicknameSet() =>
            accounts.Where(account => account.Role == Role.Admin && !string.IsNullOrEmpty(account.Nickname))
                .Select(account => account.Nickname!)
                .ToHashSet();
    }
}
