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
            // OrdinalIgnoreCase, not Ordinal: the assets are served through endpoint routing
            // (MapStaticAssets), which matches route templates case-insensitively even though the
            // Linux filesystem is case-sensitive. "/Admin/x" therefore resolves to the same asset as
            // "/admin/x", and a case-sensitive test here would let it straight past the gate.
            var relativePath = path.TrimStart('/');
            if ((relativePath.StartsWith("admin/", StringComparison.OrdinalIgnoreCase) && userRole != Role.Admin)
                || (relativePath.StartsWith("user/", StringComparison.OrdinalIgnoreCase) && userRole != Role.User))
            {
                context.Response.StatusCode = 403;
                return;
            }
        }

        await next(context);
    }
}
