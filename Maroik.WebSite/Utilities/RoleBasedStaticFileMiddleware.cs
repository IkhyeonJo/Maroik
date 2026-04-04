using Maroik.Core.Contract.Dtos;
using Maroik.WebSite.Utilities;
using Newtonsoft.Json;
using System.Text;

namespace Maroik.WebSite.Utilities;

public class RoleBasedStaticFileMiddleware
{
    private readonly RequestDelegate _next;

    public RoleBasedStaticFileMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value;

        if (!string.IsNullOrEmpty(path))
        {
            _ = context.Session.TryGetValue(Maroik.WebSite.Utilities.EnumHelper.GetDescription(Maroik.WebSite.Utilities.Session.Account), out byte[] resultByte);
            string userRole = null;

            if (resultByte != null)
            {
                AccountResponse loginedAccount = JsonConvert.DeserializeObject<AccountResponse>(Encoding.Default.GetString(resultByte));
                userRole = loginedAccount.Role;
            }

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