using Maroik.Website.Attributes;
using Microsoft.Extensions.Localization;

namespace Maroik.Website.Tests.Fixtures;

/// <summary>A hand-written stand-in for <see cref="IStringLocalizer{ThrottleEmailSendAttribute}"/>: each member records its calls and runs the behaviour a test sets (<c>On…</c>); without one it returns an empty / default value.</summary>
public sealed class FakeThrottleEmailSendStringLocalizer : IStringLocalizer<ThrottleEmailSendAttribute>
{
    /// <summary>What a read of this indexer answers; <see langword="null"/> answers an empty / default value.</summary>
    public Func<string, LocalizedString>? OnItem { get; set; }

    /// <summary>The arguments of every read of this indexer, in order (appended under a lock on the list).</summary>
    public List<string> ItemCalls { get; } = [];

    /// <inheritdoc/>
    public LocalizedString this[string name]
    {
        get
        {
            lock (ItemCalls) ItemCalls.Add(name);
            return OnItem is { } on ? on(name) : default!;
        }
    }

    /// <summary>What a read of this indexer answers; <see langword="null"/> answers an empty / default value.</summary>
    public Func<string, object[], LocalizedString>? OnItemWithArguments { get; set; }

    /// <summary>The arguments of every read of this indexer, in order (appended under a lock on the list).</summary>
    public List<(string Name, object[] Arguments)> ItemWithArgumentsCalls { get; } = [];

    /// <inheritdoc/>
    public LocalizedString this[string name, object[] arguments]
    {
        get
        {
            lock (ItemWithArgumentsCalls) ItemWithArgumentsCalls.Add((name, arguments));
            return OnItemWithArguments is { } on ? on(name, arguments) : default!;
        }
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
