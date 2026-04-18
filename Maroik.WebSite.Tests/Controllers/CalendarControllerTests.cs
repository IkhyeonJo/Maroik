using Maroik.WebSite.Tests.Infrastructure;

namespace Maroik.WebSite.Tests.Controllers;

/// <summary>
/// Integration smoke tests for <c>CalendarController</c>.
/// The anonymous index uses an anonymous-default <see cref="Maroik.Core.Contract.Dtos.AccountResponse"/>
/// and requires no session; it must respond 200 OK.
/// </summary>
public class CalendarControllerTests : IClassFixture<MaroikWebApplicationFactory>
{
    private readonly HttpClient _client;

    public CalendarControllerTests(MaroikWebApplicationFactory factory)
    {
        _client = factory.CreateTestClient(followRedirects: false);
    }

    /// <summary>Verifies that <c>AnonymousIndex</c> get when returns200.</summary>
    [Fact]
    public async Task AnonymousIndex_Get_Returns200()
    {
        var response = await _client.GetAsync("/Calendar/AnonymousIndex", TestContext.Current.CancellationToken);

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }
}
