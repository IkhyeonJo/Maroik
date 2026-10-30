using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Maroik.Website.Tests.Fixtures;

/// <summary>A hand-written stand-in for <see cref="ITempDataProvider"/>: each member records its calls and runs the behaviour a test sets (<c>On…</c>); without one it returns an empty / default value.</summary>
public sealed class FakeTempDataProvider : ITempDataProvider
{
    /// <summary>What <c>LoadTempData</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<HttpContext, IDictionary<string, object>>? OnLoadTempData { get; set; }

    /// <summary>The arguments of every <c>LoadTempData</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<HttpContext> LoadTempDataCalls { get; } = [];

    /// <inheritdoc/>
    public IDictionary<string, object> LoadTempData(HttpContext context)
    {
        lock (LoadTempDataCalls) LoadTempDataCalls.Add(context);
        if (OnLoadTempData is { } on)
        {
            return on(context);
        }

        return new Dictionary<string, object>();
    }

    /// <summary>What <c>SaveTempData</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Action<HttpContext, IDictionary<string, object>>? OnSaveTempData { get; set; }

    /// <summary>The arguments of every <c>SaveTempData</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(HttpContext Context, IDictionary<string, object> Values)> SaveTempDataCalls { get; } = [];

    /// <inheritdoc/>
    public void SaveTempData(HttpContext context, IDictionary<string, object> values)
    {
        lock (SaveTempDataCalls) SaveTempDataCalls.Add((context, values));
        OnSaveTempData?.Invoke(context, values);
    }
}
