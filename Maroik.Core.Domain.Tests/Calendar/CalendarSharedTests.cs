using Maroik.Core.Domain.Calendar;

namespace Maroik.Core.DomainTests.CalendarDomain;

/// <summary>
/// Unit tests for <see cref="CalendarShared"/>.
/// Covers private-share creation, reconstitution from trusted data, and flag updates.
/// </summary>
public class CalendarSharedTests
{
    // -- CreatePrivate -----------------------------------------------------------

    [Fact]
    public void CreatePrivate_SetsBothFlagsFalse()
    {
        var shared = CalendarShared.CreatePrivate(1);

        Assert.Equal(1, shared.Id);
        Assert.False(shared.User);
        Assert.False(shared.Anonymous);
    }

    // -- Reconstitute -----------------------------------------------------------

    [Fact]
    public void Reconstitute_RebuildsSharedSettings_WithoutValidation()
    {
        var shared = CalendarShared.Reconstitute(1, true, true);

        Assert.Equal(1, shared.Id);
        Assert.True(shared.User);
        Assert.True(shared.Anonymous);
    }

    // -- Update ---------------------------------------------------------------

    [Fact]
    public void Update_SetsBothFlags()
    {
        var shared = CalendarShared.CreatePrivate(1);

        var result = shared.Update(true, false);

        Assert.True(shared.User);
        Assert.False(shared.Anonymous);
        Assert.Same(shared, result);
    }

    [Fact]
    public void Update_CanTurnFlagsBackOff()
    {
        var shared = CalendarShared.Reconstitute(1, true, true);

        shared.Update(false, false);

        Assert.False(shared.User);
        Assert.False(shared.Anonymous);
    }
}
