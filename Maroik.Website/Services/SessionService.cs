using System.Text;
using System.Text.Json;
using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Enums;
using Maroik.Core.Contract.Helpers;
using Maroik.Website.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Session;
using Microsoft.Extensions.Options;

namespace Maroik.Website.Services;

/// <summary>
/// ASP.NET Core session-based implementation of <see cref="ISessionService"/>.
/// Serializes and deserializes <see cref="AccountResponse"/> to/from the server-side
/// session store using JSON (System.Text.Json) encoded as UTF-8 bytes.
/// The session key name is taken from the <see cref="Session.Account"/> enum description
/// so it is consistent with all other session accesses in the application.
/// </summary>
public class SessionService : ISessionService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ISessionStore _sessionStore;
    private readonly SessionOptions _sessionOptions;

    /// <summary>Initializes a new instance of <see cref="SessionService"/> with the supplied dependencies.</summary>
    public SessionService(IHttpContextAccessor httpContextAccessor, ISessionStore sessionStore, IOptions<SessionOptions> sessionOptions)
    {
        _httpContextAccessor = httpContextAccessor;
        _sessionStore = sessionStore;
        _sessionOptions = sessionOptions.Value;
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

    /// <inheritdoc />
    public void RegenerateSessionId()
    {
        HttpContext? httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null) return;

        // ISession has no "regenerate ID" API, so a genuinely new session (and thus a new session
        // cookie) is created by swapping out the ISessionFeature for one backed by a fresh session
        // key, rather than reusing whatever session (and ID) the request arrived with.
        ISession freshSession = _sessionStore.Create(
            Guid.NewGuid().ToString("N"),
            _sessionOptions.IdleTimeout,
            _sessionOptions.IOTimeout,
            () => true,
            isNewSessionKey: true);

        httpContext.Features.Set<ISessionFeature>(new SessionFeature { Session = freshSession });
    }
}
