using Maroik.Core.Contract.Utilities;
using Maroik.Core.Contract.Interfaces;

namespace Maroik.WebSite.Utilities;

public class RoleBasedStaticFileMiddleware(RequestDelegate next, ISessionService sessionService)
{
    public async Task InvokeAsync(HttpContext context)
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