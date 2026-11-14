using Maroik.Core.Domain.Finances;
using Maroik.Core.Service.Classifiers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Time.Testing;

namespace Maroik.Core.Service.Tests.Classifiers;

/// <summary>
/// Unit tests for <see cref="ContentClassifierCache"/>: an account's model of one book (income or expenditure) is trained once and
/// reused until the records it learns from change, so typing in the form does not retrain on every pause; each training is logged
/// with its model metadata.
/// </summary>
public class ContentClassifierCacheTests
{
    /// <summary>Captures what the cache logs.</summary>
    private readonly FakeLogger<ContentClassifierCache> _logger = new();

    /// <summary>A cache over <see cref="_logger"/> and a stopped clock.</summary>
    private ContentClassifierCache CreateSut() => new(_logger, new FakeTimeProvider(new DateTimeOffset(2026, 11, 14, 0, 0, 0, TimeSpan.Zero)));

    /// <summary>A learnable sample of <paramref name="text"/> in consumer spending / <paramref name="subClass"/>.</summary>
    private static ClassSample Sample(string text, string subClass) =>
        new(text, new ClassSuggestion("ConsumerSpending", subClass));

    /// <summary>Two samples of two classes.</summary>
    private static List<ClassSample> Samples() => [Sample("지하철", "TransportationCost"), Sample("스타벅스", "MealOrEatOutExpenses")];

    /// <summary>The same records give back the same model, without training (or logging) again.</summary>
    [Fact]
    public void GetOrTrain_SameRecords_ReusesTheModel()
    {
        ContentClassifierCache cache = CreateSut();

        Assert.Same(cache.GetOrTrain("Expenditure", "a@example.com", Samples()), cache.GetOrTrain("Expenditure", "a@example.com", Samples()));
        Assert.Single(_logger.Collector.GetSnapshot());
    }

    /// <summary>A changed record (here a re-classified one) trains a new model; another account never shares one.</summary>
    [Fact]
    public void GetOrTrain_ChangedRecordsOrAnotherAccount_TrainsAnew()
    {
        ContentClassifierCache cache = CreateSut();
        ContentClassifier first = cache.GetOrTrain("Expenditure", "a@example.com", Samples());

        List<ClassSample> reclassified = [Sample("지하철", "TransportationCost"), Sample("스타벅스", "LeisureOrCulture")];
        Assert.NotSame(first, cache.GetOrTrain("Expenditure", "a@example.com", reclassified));
        Assert.NotSame(cache.GetOrTrain("Expenditure", "a@example.com", reclassified), cache.GetOrTrain("Expenditure", "b@example.com", reclassified));
    }

    /// <summary>An account's income and expenditure models are kept side by side: asking for one does not evict the other.</summary>
    [Fact]
    public void GetOrTrain_KeepsEachBooksModelApart()
    {
        ContentClassifierCache cache = CreateSut();
        ContentClassifier expenditure = cache.GetOrTrain("Expenditure", "a@example.com", Samples());
        List<ClassSample> incomes = [new("급여", new ClassSuggestion("RegularIncome", "LaborIncome")), new("이자", new ClassSuggestion("RegularIncome", "FinancialIncome"))];

        ContentClassifier income = cache.GetOrTrain("Income", "a@example.com", incomes);

        Assert.Same(expenditure, cache.GetOrTrain("Expenditure", "a@example.com", Samples()));
        Assert.Same(income, cache.GetOrTrain("Income", "a@example.com", incomes));
    }

    /// <summary>
    /// A training is logged at Information with the model's metadata — book, account, model version, how many records of how many
    /// classes, and how long it took — so a suggestion can be traced to the model that made it.
    /// </summary>
    [Fact]
    public void GetOrTrain_LogsTheTrainedModelsMetadata()
    {
        CreateSut().GetOrTrain("Expenditure", "a@example.com", Samples());

        FakeLogRecord record = Assert.Single(_logger.Collector.GetSnapshot());
        Assert.Equal(LogLevel.Information, record.Level);
        Assert.Equal(
            $"Trained the Expenditure class model {ContentClassifier.ModelVersion} for a@example.com on 2 records of 2 classes in 0 ms",
            record.Message);
    }

    /// <summary>
    /// Requests of several accounts at once (the form of each user asks on every pause in typing) all get a working model, and
    /// afterwards each account's model is kept, not lost to a concurrent write.
    /// </summary>
    [Fact]
    public async Task GetOrTrain_ConcurrentRequests_AllGetAModel_AndTheModelsAreKept()
    {
        ContentClassifierCache cache = CreateSut();
        string[] accounts = ["a@example.com", "b@example.com", "c@example.com", "d@example.com"];

        ClassSuggestion[] answers = await Task.WhenAll(Enumerable.Range(0, 32).Select(i => Task.Run(() =>
            cache.GetOrTrain("Expenditure", accounts[i % accounts.Length], Samples()).Predict("지하철"), TestContext.Current.CancellationToken)));

        Assert.All(answers, a => Assert.Equal(new ClassSuggestion("ConsumerSpending", "TransportationCost"), a));
        _logger.Collector.Clear();
        foreach (string account in accounts)
            cache.GetOrTrain("Expenditure", account, Samples());
        Assert.Empty(_logger.Collector.GetSnapshot());
    }
}
