namespace Maroik.Website.Tests.Fixtures;

/// <summary>
/// Shares a single <see cref="MaroikWebApplicationFactory"/> instance across all
/// controller integration-test classes.  A shared factory means Program.cs runs
/// once per test session (and one PostgreSQL container serves them all).
/// </summary>
[CollectionDefinition("Website Integration")]
public class WebsiteIntegrationCollection : ICollectionFixture<MaroikWebApplicationFactory>;
