using Maroik.Core.Contract.Constants;
using Maroik.Website.Tests.Infrastructure;

namespace Maroik.Website.Tests.Controllers;

/// <summary>
/// Integration tests for the remaining read-oriented actions of <c>CalendarController</c>
/// that don't touch <c>CalendarService</c>'s transaction-wrapped write paths
/// (CalendarEvent/OtherCalendar/CalendarShared create/update/delete — see
/// <c>CalendarControllerCalendarTests</c> for why those are out of scope under the EF Core
/// InMemory provider used by this test factory). <c>GetCalendarEventAsync</c> and
/// <c>GetCalendarsAsync</c> are plain reads, so the not-found branches of
/// <c>IsCalendarEventExists</c>/<c>IsOtherCalendarEventExists</c> and the empty-result path of
/// <c>GetCalendarShareds</c>/<c>GetBrowseCalendarsOfInterest</c> are safe to exercise here.
/// </summary>
[Collection("Website Integration")]
public class CalendarControllerReadOnlyTests
{
    private readonly MaroikWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public CalendarControllerReadOnlyTests(MaroikWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateTestClient(followRedirects: false);
    }

    private Task<AuthenticatedSession> LoginAsUserAsync(string email = "calendar-readonly-user@test.com") =>
        AuthenticatedSessionHelper.LoginAsync(_factory, _client, email, "UserPassword1!", Role.User, TestContext.Current.CancellationToken);

    private Task<AuthenticatedSession> LoginAsAdminAsync(string email = "calendar-readonly-admin@test.com") =>
        AuthenticatedSessionHelper.LoginAsync(_factory, _client, email, "AdminPassword1!", Role.Admin, TestContext.Current.CancellationToken);

    [Fact]
    public async Task IsCalendarEventExists_UnknownId_ReturnsFailureResult()
    {
        var session = await LoginAsUserAsync();
        using var request = session.BuildJsonPostRequest("/Calendar/IsCalendarEventExists?id=999999");

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);
        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Contains("\"result\":false", json);
    }

    [Fact]
    public async Task IsOtherCalendarEventExists_UnknownId_ReturnsFailureResult()
    {
        var session = await LoginAsUserAsync();
        using var request = session.BuildJsonPostRequest("/Calendar/IsOtherCalendarEventExists?id=999999");

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);
        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Contains("\"result\":false", json);
    }

    [Fact]
    public async Task GetCalendarShareds_UserSession_IsForbidden()
    {
        var session = await LoginAsUserAsync();
        using var request = session.BuildJsonPostRequest("/Calendar/GetCalendarShareds");

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.NotEqual(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetCalendarShareds_AdminSession_ReturnsSuccessResult()
    {
        var session = await LoginAsAdminAsync();
        using var request = session.BuildJsonPostRequest("/Calendar/GetCalendarShareds");

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);
        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Contains("\"result\":true", json);
    }

    [Fact]
    public async Task GetBrowseCalendarsOfInterest_UserSession_ReturnsSuccessResult()
    {
        var session = await LoginAsUserAsync();
        using var request = session.BuildJsonPostRequest("/Calendar/GetBrowseCalendarsOfInterest");

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);
        string json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Contains("\"result\":true", json);
    }
}
