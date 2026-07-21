using Maroik.Website.Attributes;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace Maroik.Website.Middlewares;

/// <summary>
/// Strips the session cookie from the incoming request when the resolved endpoint carries
/// <see cref="RegenerateSessionOnEntryAttribute"/>, before the session middleware (<c>UseSession</c>)
/// gets a chance to read it. Must run after <c>UseRouting</c>
/// (so HttpContext.GetEndpoint is populated) and before <c>UseSession</c>.
/// With no valid session cookie on the request, ASP.NET Core's session middleware always establishes
/// a brand-new session — new ID, freshly wired cookie-issuing — for the response. That is the only
/// reliable way to guarantee a fresh session ID for these endpoints: <c>ISession</c> has no supported
/// "regenerate" API, and the session middleware decides the ID and captures the object it will commit
/// before the request delegate (and therefore the controller) ever runs, so nothing a controller does
/// mid-request can change what gets committed or which cookie gets sent back.
/// </summary>
public class RegenerateSessionOnEntryMiddleware(RequestDelegate next)
{
    /// <summary>Removes the configured session cookie from the request when the endpoint opts in, then forwards the request.</summary>
    public async Task InvokeAsync(HttpContext context, IOptions<SessionOptions> sessionOptions)
    {
        var endpoint = context.GetEndpoint();
        if (endpoint?.Metadata.GetMetadata<RegenerateSessionOnEntryAttribute>() != null)
        {
            string sessionCookieName = sessionOptions.Value.Cookie.Name!;
            if (context.Request.Cookies.ContainsKey(sessionCookieName))
            {
                var remaining = context.Request.Cookies
                    .Where(c => c.Key != sessionCookieName)
                    .Select(c => new CookieHeaderValue(c.Key, c.Value).ToString())
                    .ToList();
                if (remaining.Count == 0)
                {
                    context.Request.Headers.Remove(HeaderNames.Cookie);
                }
                else
                {
                    context.Request.Headers.Cookie = string.Join("; ", remaining);
                }
            }
        }

        await next(context);
    }
}
