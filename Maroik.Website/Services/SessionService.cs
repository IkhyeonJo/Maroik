using System.Text;
using System.Text.Json;
using Maroik.Core.Contract.Dtos;
using Maroik.Website.Constants;
using Maroik.Website.Contracts;

namespace Maroik.Website.Services;

/// <summary>
/// ASP.NET Core session-based implementation of <see cref="ISessionService"/>.
/// Serializes and deserializes <see cref="AccountResponse"/> to/from the server-side
/// session store using JSON (System.Text.Json) encoded as UTF-8 bytes.
/// The session key name comes from <see cref="SessionKeys.Account"/>
/// so it is consistent with all other session accesses in the application.
/// </summary>
public class SessionService(IHttpContextAccessor httpContextAccessor) : ISessionService
{
    /// <inheritdoc />
    public AccountResponse? GetAccount()
    {
        var session = httpContextAccessor.HttpContext?.Session;
        if (session == null) return null;

        bool exists = session.TryGetValue(SessionKeys.Account, out byte[]? bytes);
        if (!exists || bytes == null || bytes.Length == 0) return null;

        try
        {
            return JsonSerializer.Deserialize<AccountResponse>(Encoding.UTF8.GetString(bytes));
        }
        catch
        {
            return null;
        }
    }

    /// <inheritdoc />
    public void SetAccount(AccountResponse account)
    {
        var session = httpContextAccessor.HttpContext?.Session;
        session?.Set(
            SessionKeys.Account,
            Encoding.UTF8.GetBytes(JsonSerializer.Serialize(account)));
    }

    /// <inheritdoc />
    public void RemoveAccount()
    {
        var session = httpContextAccessor.HttpContext?.Session;
        session?.Remove(SessionKeys.Account);
    }
}
