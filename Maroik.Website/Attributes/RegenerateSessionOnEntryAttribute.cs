namespace Maroik.Website.Attributes;

/// <summary>
/// Marks an action whose incoming request must never be allowed to keep whatever session cookie
/// it arrived with. Read by a middleware placed between <c>UseRouting</c> and <c>UseSession</c> in
/// Program.cs, which strips the session cookie from the request before the session middleware runs,
/// forcing it to establish a brand-new session (and thus issue a brand-new session cookie) 
/// Used on the login action to defend against session fixation: ASP.NET Core's
/// <c>ISession</c> has no supported way to regenerate a session's ID mid-request (its cookie is
/// created and its commit is captured before the request delegate — including the controller — ever
/// runs), so the only reliable way to guarantee a fresh ID is to make the request look session-less
/// before the session middleware sees it.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class RegenerateSessionOnEntryAttribute : Attribute
{
}
