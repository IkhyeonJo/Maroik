using Maroik.Core.Contract.Utilities;
using Maroik.Core.Contract.Interfaces;

namespace Maroik.WebSite.Utilities;

public class RoleBasedStaticFileMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ISessionService _sessionService;

    public RoleBasedStaticFileMiddleware(RequestDelegate next, ISessionService sessionService)
    {
        _next = next;
        _sessionService = sessionService;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value;

        if (!string.IsNullOrEmpty(path))
        {
            string userRole = _sessionService.GetAccount()?.Role;

            if (path.StartsWith("/admin/") && userRole != Role.Admin)
            {
                context.Response.StatusCode = 403;
                return;
            }

            if (path.StartsWith("/user/") && userRole != Role.User)
            {
                context.Response.StatusCode = 403;
                return;
            }
        }

        await _next(context);
    }
}