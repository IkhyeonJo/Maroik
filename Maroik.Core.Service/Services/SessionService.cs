#nullable enable
using Maroik.Core.Contract.Utilities;
using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Text;
using System.Text.Json;

namespace Maroik.Core.Service.Services;

/// <summary>
/// ASP.NET Core session-based implementation of <see cref="ISessionService"/>.
/// Serialises and deserialises <see cref="AccountResponse"/> to/from the server-side
/// session store using JSON (System.Text.Json) encoded as UTF-8 bytes.
/// The session key name is taken from the <see cref="Session.Account"/> enum description
/// so it is consistent with all other session accesses in the application.
/// </summary>
public class SessionService : ISessionService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public SessionService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc />
    public AccountResponse? GetAccount()
    {
        var session = _httpContextAccessor.HttpContext?.Session;
        if (session == null) return null;

        bool exists = session.TryGetValue(EnumHelper.GetDescription(Session.Account), out byte[]? bytes);
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
        var session = _httpContextAccessor.HttpContext?.Session;
        session?.Set(
            EnumHelper.GetDescription(Session.Account),
            Encoding.UTF8.GetBytes(JsonSerializer.Serialize(account)));
    }

    /// <inheritdoc />
    public void RemoveAccount()
    {
        var session = _httpContextAccessor.HttpContext?.Session;
        session?.Remove(EnumHelper.GetDescription(Session.Account));
    }
}
