using System.Diagnostics;
using Maroik.Website.Models.ViewModels;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Maroik.Website.Controllers;

/// <summary>Serves the application error page and the "page not found" page.</summary>
public class ExceptionController : Controller
{
    /// <summary>
    /// Returns the generic error view. Reached through <c>UseExceptionHandler("/Exception/Error")</c>,
    /// which re-executes the pipeline with the <em>original</em> request's HTTP method — a POST that
    /// threw is re-executed as a POST. An action restricted to GET would therefore answer such a
    /// request with an empty 405 instead of the error page, so every method is accepted. The global
    /// <c>AutoValidateAntiforgeryToken</c> is bypassed for the same reason: the handler is re-running
    /// a request whose body is already consumed, and the page it renders is inert (no state change).
    /// <para>
    /// <c>UseStatusCodePagesWithReExecute("/Exception/Error")</c> also lands here, for any error status sent with an empty
    /// body, again with the original method. Only a 404 gets a page (the not-found view, status kept); any other status
    /// leaves with that status and no body, as it did before the re-execute was added. The status is read from
    /// <see cref="IStatusCodeReExecuteFeature"/>, not from the query string: the exception handler keeps the original
    /// request's query, so a query value could turn a crash into a fake 404.
    /// </para>
    /// </summary>
    [AcceptVerbs("GET", "POST", "PUT", "PATCH", "DELETE", "HEAD", "OPTIONS")]
    [IgnoreAntiforgeryToken]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        if (HttpContext.Features.Get<IStatusCodeReExecuteFeature>() is { } reExecute)
        {
            return reExecute.OriginalStatusCode == StatusCodes.Status404NotFound
                ? View("NotFound")
                : StatusCode(reExecute.OriginalStatusCode);
        }

        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
