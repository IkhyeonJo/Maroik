using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.Extensions.Localization;

namespace Maroik.Website.Tests.Fixtures;

/// <summary>A hand-written stand-in for <see cref="IViewLocalizer"/>: each member records its calls and runs the behaviour a test sets (<c>On…</c>); without one it returns an empty / default value.</summary>
public sealed class FakeViewLocalizer : IViewLocalizer
{
    /// <summary>What a read of this indexer answers; <see langword="null"/> answers an empty / default value.</summary>
    public Func<string, LocalizedHtmlString>? OnItem { get; set; }

    /// <summary>The arguments of every read of this indexer, in order (appended under a lock on the list).</summary>
    public List<string> ItemCalls { get; } = [];

    /// <inheritdoc/>
    public LocalizedHtmlString this[string name]
    {
        get
        {
            lock (ItemCalls) ItemCalls.Add(name);
            return OnItem is { } on ? on(name) : default!;
        }
    }

    /// <summary>What a read of this indexer answers; <see langword="null"/> answers an empty / default value.</summary>
    public Func<string, object[], LocalizedHtmlString>? OnItemWithArguments { get; set; }

    /// <summary>The arguments of every read of this indexer, in order (appended under a lock on the list).</summary>
    public List<(string Name, object[] Arguments)> ItemWithArgumentsCalls { get; } = [];

    /// <inheritdoc/>
    public LocalizedHtmlString this[string name, object[] arguments]
    {
        get
        {
            lock (ItemWithArgumentsCalls) ItemWithArgumentsCalls.Add((name, arguments));
            return OnItemWithArguments is { } on ? on(name, arguments) : default!;
        }
    }

    /// <summary>What <c>GetString</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, LocalizedString>? OnGetString { get; set; }

    /// <summary>The arguments of every <c>GetString</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<string> GetStringCalls { get; } = [];

    /// <inheritdoc/>
    public LocalizedString GetString(string name)
    {
        lock (GetStringCalls) GetStringCalls.Add(name);
        if (OnGetString is { } on)
        {
            return on(name);
        }

        return default!;
    }

    /// <summary>What <c>GetString</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, object[], LocalizedString>? OnGetStringWithArguments { get; set; }

    /// <summary>The arguments of every <c>GetString</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string Name, object[] Arguments)> GetStringWithArgumentsCalls { get; } = [];

    /// <inheritdoc/>
    public LocalizedString GetString(string name, object[] arguments)
    {
        lock (GetStringWithArgumentsCalls) GetStringWithArgumentsCalls.Add((name, arguments));
        if (OnGetStringWithArguments is { } on)
        {
            return on(name, arguments);
        }

        return default!;
    }

    /// <summary>What <c>GetAllStrings</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<bool, IEnumerable<LocalizedString>>? OnGetAllStrings { get; set; }

    /// <summary>The arguments of every <c>GetAllStrings</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<bool> GetAllStringsCalls { get; } = [];

    /// <inheritdoc/>
    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
    {
        lock (GetAllStringsCalls) GetAllStringsCalls.Add(includeParentCultures);
        if (OnGetAllStrings is { } on)
        {
            return on(includeParentCultures);
        }

        return [];
    }
}
