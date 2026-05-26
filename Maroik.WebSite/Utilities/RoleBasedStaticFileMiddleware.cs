using Maroik.Core.Contract.Utilities;
using Maroik.Core.Contract.Interfaces;

namespace Maroik.WebSite.Utilities;

/// <summary>
/// Middleware that enforces role-based access to static files served under
/// <c>/admin/</c> and <c>/user/</c> path prefixes.
/// Requests to <c>/admin/…</c> are blocked (HTTP 403) for non-Admin sessions,
/// and requests to <c>/user/…</c> are blocked for non-User sessions.
/// </summary>
public class RoleBasedStaticFileMiddleware(RequestDelegate next)
{
    /// <summary>Checks the request path against the session role and either blocks (403) or forwards the request.</summary>
    public async Task InvokeAsync(HttpContext context, ISessionService sessionService)
    {
        var path = context.Request.Path.Value;

        if (!string.IsNullOrEmpty(path))
        {
            var userRole = sessionService.GetAccount()?.Role;

            if ((path.StartsWith("/admin/") && userRole != Role.Admin) || (path.StartsWith("/user/") && userRole != Role.User))
            {
                context.Response.StatusCode = 403;
                return;
            }
        }

        await next(context);
    }
}