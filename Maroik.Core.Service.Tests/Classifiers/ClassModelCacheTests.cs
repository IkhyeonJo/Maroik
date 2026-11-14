using Maroik.Core.Contract.Settings;
using Maroik.Core.Domain.Finances;
using Maroik.Core.Service.Classifiers;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace Maroik.Core.Service.Tests.Classifiers;

/// <summary>
/// Unit tests for <see cref="ClassModelCache"/>: the website keeps the models it read from the file storage in memory while they are
/// used — one unused for the session time (<c>ServerSetting.SessionExpireMinutes</c>) leaves, like the session it served — and never
/// more than <c>capacity</c> of them (the least recently used one leaves first); it asks for a training of an account's book at most
/// once per fingerprint within <see cref="ClassModelCache.TrainingRequestRetryAfter"/>.
/// </summary>
public sealed class ClassModelCacheTests : IDisposable
{
    /// <summary>The clock the cache reads.</summary>
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 11, 14, 0, 0, 0, TimeSpan.Zero));

    /// <summary>The session time of these tests.</summary>
    private const int SessionMinutes = 10;

    /// <summary>A cache of <paramref name="capacity"/> models, for a session time of <see cref="SessionMinutes"/>.</summary>
    private ClassModelCache Sut(int capacity = ClassModelCache.DefaultCapacity) =>
        new(_time, Options.Create(new ServerSetting { SessionExpireMinutes = SessionMinutes }), capacity);

    /// <summary>The cache the concurrency test shares between threads (disposed with the test class, after every task is done).</summary>
    private readonly ClassModelCache _shared;

    /// <summary>Creates the shared cache.</summary>
    public ClassModelCacheTests() => _shared = Sut();

    /// <summary>Stops the shared cache's sweep.</summary>
    public void Dispose() => _shared.Dispose();

    /// <summary>A small model.</summary>
    private static ContentClassifier Model() => ContentClassifier.Train([new ClassSample("지하철", new ClassSuggestion("ConsumerSpending", "TransportationCost"))]);

    /// <summary>A kept model is found again by its account, book and file; another file (a newer model) or book is not it.</summary>
    [Fact]
    public void Find_ReturnsTheKeptModel_OfThatFileOnly()
    {
        using ClassModelCache cache = Sut();
        ContentClassifier model = Model();

        cache.Keep("Expenditure", "a@test.com", "f1", model);

        Assert.Same(model, cache.Find("Expenditure", "a@test.com", "f1"));
        Assert.Null(cache.Find("Expenditure", "a@test.com", "f2"));
        Assert.Null(cache.Find("Income", "a@test.com", "f1"));
        Assert.Null(cache.Find("Expenditure", "b@test.com", "f1"));
    }

    /// <summary>Past the capacity the least recently used model leaves; a model just found counts as used.</summary>
    [Fact]
    public void Keep_PastTheCapacity_DropsTheLeastRecentlyUsed()
    {
        using ClassModelCache cache = Sut(capacity: 2);
        cache.Keep("Expenditure", "a@test.com", "fa", Model());
        cache.Keep("Expenditure", "b@test.com", "fb", Model());
        Assert.NotNull(cache.Find("Expenditure", "a@test.com", "fa"));

        cache.Keep("Expenditure", "c@test.com", "fc", Model());

        Assert.NotNull(cache.Find("Expenditure", "a@test.com", "fa"));
        Assert.Null(cache.Find("Expenditure", "b@test.com", "fb"));
        Assert.NotNull(cache.Find("Expenditure", "c@test.com", "fc"));
    }

    /// <summary>A newer model of the same account and book replaces the older one instead of taking a second place.</summary>
    [Fact]
    public void Keep_ANewerModel_ReplacesTheOlder()
    {
        using ClassModelCache cache = Sut(capacity: 2);
        cache.Keep("Expenditure", "a@test.com", "f1", Model());
        cache.Keep("Expenditure", "b@test.com", "fb", Model());

        cache.Keep("Expenditure", "a@test.com", "f2", Model());

        Assert.Null(cache.Find("Expenditure", "a@test.com", "f1"));
        Assert.NotNull(cache.Find("Expenditure", "a@test.com", "f2"));
        Assert.NotNull(cache.Find("Expenditure", "b@test.com", "fb"));
    }

    /// <summary>
    /// A model unused for the session time leaves memory on the next sweep (once a minute), with no request needed to notice it; one used
    /// meanwhile stays, since each use starts its time again.
    /// </summary>
    [Fact]
    public void AModelUnusedForTheSessionTime_LeavesMemory()
    {
        using ClassModelCache cache = Sut();
        cache.Keep("Expenditure", "used@test.com", "f", Model());
        cache.Keep("Expenditure", "idle@test.com", "f", Model());
        _time.Advance(TimeSpan.FromMinutes(SessionMinutes - 1));
        Assert.NotNull(cache.Find("Expenditure", "used@test.com", "f"));

        _time.Advance(TimeSpan.FromMinutes(2));

        Assert.Equal(1, cache.Count);
        Assert.NotNull(cache.Find("Expenditure", "used@test.com", "f"));
        Assert.Null(cache.Find("Expenditure", "idle@test.com", "f"));
    }

    /// <summary>
    /// A training of the same records is asked for once (the form asks on every pause in typing); new records, or the retry time
    /// passing without a model (a lost message), ask again.
    /// </summary>
    [Fact]
    public void ClaimTrainingRequest_AsksOncePerFingerprint_UntilTheRetryTime()
    {
        using ClassModelCache cache = Sut();

        Assert.True(cache.ClaimTrainingRequest("Expenditure", "a@test.com", "fp1"));
        Assert.False(cache.ClaimTrainingRequest("Expenditure", "a@test.com", "fp1"));
        Assert.True(cache.ClaimTrainingRequest("Income", "a@test.com", "fp1"));
        Assert.True(cache.ClaimTrainingRequest("Expenditure", "a@test.com", "fp2"));

        _time.Advance(ClassModelCache.TrainingRequestRetryAfter);
        Assert.True(cache.ClaimTrainingRequest("Expenditure", "a@test.com", "fp2"));
    }

    /// <summary>The training requests count against the capacity too, so the cache stays bounded however many accounts type.</summary>
    [Fact]
    public void ClaimTrainingRequest_IsBoundedByTheCapacity()
    {
        using ClassModelCache cache = Sut(capacity: 1);
        cache.ClaimTrainingRequest("Expenditure", "a@test.com", "fp");
        cache.ClaimTrainingRequest("Expenditure", "b@test.com", "fp");

        Assert.True(cache.ClaimTrainingRequest("Expenditure", "a@test.com", "fp"));
    }

    /// <summary>Keeps, claims and finds <paramref name="email"/>'s model in the shared cache on a thread-pool thread.</summary>
    private Task<ContentClassifier?> UseConcurrentlyAsync(string email, ContentClassifier model) => Task.Run(() =>
    {
        _shared.Keep("Expenditure", email, "f", model);
        _shared.ClaimTrainingRequest("Expenditure", email, "fp");
        return _shared.Find("Expenditure", email, "f");
    }, TestContext.Current.CancellationToken);

    /// <summary>Many requests at once (each user's form asks on every pause) neither fail nor lose the models kept meanwhile.</summary>
    [Fact]
    public async Task ConcurrentUse_KeepsEveryModel()
    {
        ContentClassifier model = Model();

        var uses = new List<Task<ContentClassifier?>>();
        for (int i = 0; i < 64; i++)
            uses.Add(UseConcurrentlyAsync($"u{i % 8}@test.com", model));
        await Task.WhenAll(uses);

        for (int i = 0; i < 8; i++)
            Assert.Same(model, _shared.Find("Expenditure", $"u{i}@test.com", "f"));
    }
}
