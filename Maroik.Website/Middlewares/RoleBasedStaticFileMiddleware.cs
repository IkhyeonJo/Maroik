using Maroik.Core.Domain.Account;
using Maroik.Website.Contracts;

namespace Maroik.Website.Middlewares;

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

            // Leading slashes are dropped before the prefix test: the static-file provider trims them
            // when it resolves the file, so "//admin/x" serves the same file as "/admin/x" and would
            // otherwise slip past a plain "/admin/" StartsWith check.
            //
            // Ordinal (case-sensitive): the deployment target is Linux (case-sensitive filesystem)
            // and the compiled client assets live under the exact-case /admin/ and /user/ folders,
            // so those are the only paths that ever resolve to a file — matching any other casing
            // would gate URLs that 404 anyway.
            var relativePath = path.TrimStart('/');
            if ((relativePath.StartsWith("admin/", StringComparison.Ordinal) && userRole != Role.Admin)
                || (relativePath.StartsWith("user/", StringComparison.Ordinal) && userRole != Role.User))
            {
                context.Response.StatusCode = 403;
                return;
            }
        }

        await next(context);
    }
}
