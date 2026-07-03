using Microsoft.AspNetCore.StaticFiles;

namespace Maroik.Website.Extensions;
/// <summary>
/// Extension methods for configuring static file serving in the ASP.NET Core pipeline.
/// </summary>
public static class StaticAssetExtensions
{
    /// <summary>
    /// Registers <see cref="Microsoft.AspNetCore.StaticFiles.StaticFileMiddleware"/> with explicit
    /// MIME-type mappings for .js and .css files, and adds the
    /// <c>X-Content-Type-Options: nosniff</c> security header to every static-file response.
    /// </summary>
    public static IApplicationBuilder MapStaticAssets(this IApplicationBuilder app)
    {
        app.UseStaticFiles(new StaticFileOptions
        {
            // Explicitly map common web asset extensions to prevent MIME sniffing issues
            ContentTypeProvider = new FileExtensionContentTypeProvider
            {
                Mappings =
                {
                    [".js"] = "application/javascript",
                    [".css"] = "text/css"
                }
            },
            // Add X-Content-Type-Options: nosniff to prevent browsers from MIME-sniffing responses
            OnPrepareResponse = ctx =>
            {
                ctx.Context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            }
        });

        return app;
    }
}
