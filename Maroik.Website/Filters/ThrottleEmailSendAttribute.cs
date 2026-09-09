using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Distributed;

namespace Maroik.Website.Filters;

/// <summary>
/// Per-target-address cooldown for the account e-mail-sending POST actions (forgot-password and
/// registration / resend-confirmation). Without it, anyone who knows an address can make the app
/// enqueue an unbounded stream of mail to it — and every request also invalidates that address's
/// previous reset / confirmation link.
/// <para>
/// Keyed on the submitted e-mail, <b>not</b> the client IP: the app sits behind Cloudflare with no
/// forwarded-headers handling, so <c>RemoteIpAddress</c> is a Cloudflare edge address shared by all
/// visitors and would throttle everyone at once. Backed by the distributed cache (Valkey); if the
/// cache is unavailable the check fails open, so a real password reset is never blocked by a blip.
/// </para>
/// <para>
/// On a cooldown hit the action does not run; the request is redirected back to the form with a
/// generic "wait a moment" notice. The cooldown key is the address the caller typed, so this
/// discloses nothing about whether that address is registered — the endpoint's e-mail-enumeration
/// resistance is unchanged.
/// </para>
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class ThrottleEmailSendAttribute : Attribute, IAsyncActionFilter
{
    /// <summary>Length of the cooldown window, in seconds. Defaults to 60.</summary>
    public int WindowSeconds { get; init; } = 60;

    /// <inheritdoc />
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        string? email = ExtractEmail(context.ActionArguments.Values);
        var cache = context.HttpContext.RequestServices.GetService<IDistributedCache>();

        if (cache != null && !string.IsNullOrWhiteSpace(email))
        {
            string action = context.RouteData.Values["action"]?.ToString() ?? "";
            string key = $"emailsend:{action}:{email.Trim().ToLowerInvariant()}";
            CancellationToken ct = context.HttpContext.RequestAborted;

            try
            {
                if (await cache.GetAsync(key, ct) is not null)
                {
                    if (context.Controller is Controller controller)
                        controller.TempData["Error"] = "Please wait a moment before requesting another email.";

                    context.Result = new RedirectToActionResult(
                        action,
                        context.RouteData.Values["controller"]?.ToString() ?? "Account",
                        routeValues: null);
                    return;
                }

                await cache.SetAsync(
                    key,
                    [1],
                    new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(WindowSeconds)
                    },
                    ct);
            }
            catch (Exception)
            {
                // Cache unavailable — fail open. Blocking a genuine password reset because Valkey
                // hiccuped is worse than briefly losing the send-rate cap.
            }
        }

        await next();
    }

    /// <summary>
    /// Finds the target e-mail among the bound action arguments: a bare <see cref="string"/> that
    /// looks like an address, or the <c>Email</c> property of a bound view model.
    /// </summary>
    private static string? ExtractEmail(IEnumerable<object?> actionArguments)
    {
        foreach (object? argument in actionArguments)
        {
            switch (argument)
            {
                case null:
                    continue;
                case string s when s.Contains('@', StringComparison.Ordinal):
                    return s;
                default:
                    if (argument.GetType().GetProperty("Email")?.GetValue(argument) is string email
                        && email.Contains('@', StringComparison.Ordinal))
                    {
                        return email;
                    }
                    break;
            }
        }

        return null;
    }
}
