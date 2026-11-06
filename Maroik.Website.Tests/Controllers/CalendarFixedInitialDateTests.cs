using Maroik.Core.Contract.Settings;
using Maroik.Core.Domain.Accounts;
using Maroik.Website.Tests.Fixtures;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Maroik.Website.Tests.Controllers;

/// <summary>
/// The user calendar page hands its script the date to open on: the public demo account's fixed period
/// (<c>ServerSetting.DemoDashboardYear/Month</c>, the one its dashboard shows), and nothing for any other account,
/// whose calendar opens on today. One derived host carries the demo settings.
/// </summary>
[Collection("Website Integration")]
public class CalendarFixedInitialDateTests(MaroikWebApplicationFactory factory)
{
    /// <summary>The demo account this class's host is configured with.</summary>
    private const string DemoEmail = "calendar-demo@test.com";

    /// <summary>The one demo-settings host every test of this class shares (each derived host is a full start-up).</summary>
    private static WebApplicationFactory<Program>? _sharedHost;

    /// <summary>Guards the one-time creation of <see cref="_sharedHost"/>.</summary>
    private static readonly Lock _sharedHostLock = new();

    /// <summary>The host whose settings name <see cref="DemoEmail"/> as the demo account, with June 2025 as its period.</summary>
    private readonly WebApplicationFactory<Program> _host = SharedHost(factory);

    /// <summary>Returns <see cref="_sharedHost"/>, deriving it from <paramref name="factory"/> on first use.</summary>
    private static WebApplicationFactory<Program> SharedHost(MaroikWebApplicationFactory factory)
    {
        lock (_sharedHostLock)
        {
            return _sharedHost ??= factory.WithWebHostBuilder(b => b.ConfigureTestServices(services =>
                services.PostConfigure<ServerSetting>(setting =>
                {
                    setting.DemoAccountEmail = DemoEmail;
                    setting.DemoDashboardYear = "2025";
                    setting.DemoDashboardMonth = "06";
                })));
        }
    }

    /// <summary>Signs <paramref name="email"/> in as a user and returns the calendar page's HTML.</summary>
    private async Task<string> GetCalendarPageAsync(string email)
    {
        using HttpClient client = _host.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://www.localhost/"), AllowAutoRedirect = false, HandleCookies = false
        });
        var session = await AuthenticatedSessionHelper.LoginAsync(_host, client, email, "UserPassword1!", Role.User, TestContext.Current.CancellationToken);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/Calendar/UserIndex");
        request.Headers.Add("Cookie", session.CookieHeader);
        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
    }

    /// <summary>The demo account's page publishes the first day of the fixed period.</summary>
    [Fact]
    public async Task UserIndex_PublishesTheFixedPeriodsFirstDay_ForTheDemoAccount()
    {
        string html = await GetCalendarPageAsync(DemoEmail);

        Assert.Contains("id=\"calendarInitialDate\" name=\"calendarInitialDate\" type=\"hidden\" value=\"2025-06-01\"", html, StringComparison.Ordinal);
    }

    /// <summary>Any other account's page publishes no date, so its calendar opens on today.</summary>
    [Fact]
    public async Task UserIndex_PublishesNoDate_ForAnyOtherAccount()
    {
        string html = await GetCalendarPageAsync($"calendar-other-{Guid.NewGuid():N}@test.com");

        Assert.Contains("id=\"calendarInitialDate\" name=\"calendarInitialDate\" type=\"hidden\" value=\"\"", html, StringComparison.Ordinal);
    }
}
