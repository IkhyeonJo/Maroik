using Maroik.Website.Constants;
using Maroik.Website.Extensions;
using Maroik.Website.Tests.Fixtures;

namespace Maroik.Website.Tests.Extensions;

/// <summary>
/// Unit tests for <see cref="NavigationCacheExtensions.InvalidateNavigationMenuCacheAsync"/> --
/// verifies every one of the six role-partitioned navigation cache keys is removed, so a stale menu
/// can never survive under a key this method forgot to include.
/// </summary>
public class NavigationCacheExtensionsTests
{
    /// <summary>All six Admin/User/Anonymous category and sub-category cache keys are removed.</summary>
    [Fact]
    public async Task InvalidateNavigationMenuCacheAsync_RemovesAllSixNavigationCacheKeys()
    {
        var cache = new FakeDistributedCache();

        await cache.InvalidateNavigationMenuCacheAsync();
        var removedKeys = cache.RemoveAsyncCalls.Select(c => c.Key).ToList();

        Assert.Equal(
            new[]
            {
                NavigationCacheKeys.AdminCategories,
                NavigationCacheKeys.AdminSubCategories,
                NavigationCacheKeys.UserCategories,
                NavigationCacheKeys.UserSubCategories,
                NavigationCacheKeys.AnonymousCategories,
                NavigationCacheKeys.AnonymousSubCategories,
            }.OrderBy(x => x),
            removedKeys.OrderBy(x => x));
    }

    /// <summary>A failure removing one key still surfaces (via the awaited Task.WhenAll) rather than being silently swallowed.</summary>
    [Fact]
    public async Task InvalidateNavigationMenuCacheAsync_PropagatesFailure_WhenAnyRemoveFails()
    {
        var cache = new FakeDistributedCache
        {
            OnRemoveAsync = (key, _) => key == NavigationCacheKeys.UserCategories
                ? Task.FromException(new InvalidOperationException("cache unavailable"))
                : Task.CompletedTask,
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => cache.InvalidateNavigationMenuCacheAsync());
    }
}
