using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Contract.Messages;
using Maroik.Core.Contract.Settings;
using Maroik.Core.Domain.Finances;
using Maroik.Core.Service.Classifiers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace Maroik.Core.Service.Tests.Classifiers;

/// <summary>
/// Unit tests for <see cref="ClassModelSource"/>, the website's way to an account's class-suggestion model: it reads the model the
/// worker stored (the record says which file), keeps it in memory, and asks the worker for a training when there is no model or it
/// was trained on older records — answering meanwhile with the older model, or with none.
/// </summary>
public sealed class ClassModelSourceTests : IDisposable
{
    private const string Email = "a@test.com";
    private const string Book = ClassModel.ExpenditureBook;
    private const string ModelPath = "upload/ClassModels/0f8fad5b-d9cb-469f-a165-70867728950e.zip";

    private readonly Mock<IClassModelRepository> _records = new();
    private readonly Mock<IFileClient> _files = new();
    private readonly Mock<IClassModelTrainingPublisher> _publisher = new();
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 11, 14, 0, 0, 0, TimeSpan.Zero));
    private readonly FakeLogger<ClassModelSource> _logger = new();
    private readonly ClassModelCache _cache;

    /// <summary>The account's records.</summary>
    private static readonly List<ClassSample> _samples =
    [
        new("지하철", new ClassSuggestion("ConsumerSpending", "TransportationCost")),
        new("스타벅스", new ClassSuggestion("ConsumerSpending", "MealOrEatOutExpenses")),
    ];

    /// <summary>Creates the cache the sources of a test share.</summary>
    public ClassModelSourceTests() => _cache = new ClassModelCache(_time, Options.Create(new ServerSetting { SessionExpireMinutes = 10 }));

    /// <summary>Stops the cache's sweep.</summary>
    public void Dispose() => _cache.Dispose();

    /// <summary>A source over the mocks and the shared cache.</summary>
    private ClassModelSource Sut() => new(_records.Object, _files.Object, _publisher.Object, _cache, _logger);

    /// <summary>The account's record says the model at <see cref="ModelPath"/> was trained on <paramref name="samples"/> by <paramref name="version"/>; the file holds it.</summary>
    private void Stored(List<ClassSample> samples, string? version = null)
    {
        _records.Setup(r => r.FindAsync(Email, Book, It.IsAny<CancellationToken>())).ReturnsAsync(ClassModel.Reconstitute(1, Email, Book, ModelPath,
            ClassSampleFingerprint.Of(samples), version ?? ContentClassifier.ModelVersion, new DateTime(2026, 11, 13)));
        _files.Setup(f => f.DownloadAsync(ModelPath, It.IsAny<CancellationToken>())).ReturnsAsync(ContentClassifier.Train(samples).ToBytes());
    }

    /// <summary>Asks the source for the model of <see cref="_samples"/>.</summary>
    private Task<ContentClassifier?> GetAsync() => Sut().GetAsync(Book, Email, _samples, TestContext.Current.CancellationToken);

    /// <summary>The training requests sent.</summary>
    private void VerifyRequested(Times times) =>
        _publisher.Verify(p => p.PublishAsync(new TrainClassModelMessage(Book, Email), It.IsAny<CancellationToken>()), times);

    /// <summary>A current model is read from its file and answers; it is read once (then kept) and no training is asked for.</summary>
    [Fact]
    public async Task GetAsync_ACurrentModel_IsReadOnce_AndAnswers()
    {
        Stored(_samples);

        ContentClassifier? first = await GetAsync();
        ContentClassifier? second = await GetAsync();

        Assert.Equal(new ClassSuggestion("ConsumerSpending", "TransportationCost"), first?.Predict("지하철 2호선"));
        Assert.Same(first, second);
        _files.Verify(f => f.DownloadAsync(ModelPath, It.IsAny<CancellationToken>()), Times.Once);
        VerifyRequested(Times.Never());
    }

    /// <summary>
    /// Without a model there is no answer yet: a training is asked for once (logged at Information), not on every pause in typing.
    /// </summary>
    [Fact]
    public async Task GetAsync_NoModel_AsksForATrainingOnce()
    {
        Assert.Null(await GetAsync());
        Assert.Null(await GetAsync());

        VerifyRequested(Times.Once());
        Assert.Contains(_logger.Collector.GetSnapshot(), r => r is { Level: LogLevel.Information, Message: $"Requested a {Book} class model training for {Email}" });
    }

    /// <summary>A model of older records still answers while the training of the new records is asked for.</summary>
    [Fact]
    public async Task GetAsync_AModelOfOlderRecords_AnswersMeanwhile_AndAsksForATraining()
    {
        Stored([_samples[0]]);

        ContentClassifier? model = await GetAsync();

        Assert.Equal(new ClassSuggestion("ConsumerSpending", "TransportationCost"), model?.Predict("스타벅스"));
        VerifyRequested(Times.Once());
    }

    /// <summary>A model written by other model code is not read at all; a training with this code is asked for.</summary>
    [Fact]
    public async Task GetAsync_AModelOfAnotherVersion_IsNotRead_AndATrainingIsAskedFor()
    {
        Stored(_samples, version: "older-version");

        Assert.Null(await GetAsync());

        _files.Verify(f => f.DownloadAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        VerifyRequested(Times.Once());
    }

    /// <summary>A model file that cannot be read gives no answer (logged at Warning with the exception) and a training is asked for.</summary>
    [Fact]
    public async Task GetAsync_AnUnreadableModel_GivesNoAnswer_AndAsksForATraining()
    {
        Stored(_samples);
        _files.Setup(f => f.DownloadAsync(ModelPath, It.IsAny<CancellationToken>())).ThrowsAsync(new HttpRequestException("storage down"));

        Assert.Null(await GetAsync());

        VerifyRequested(Times.Once());
        FakeLogRecord warning = Assert.Single(_logger.Collector.GetSnapshot(), r => r.Level == LogLevel.Warning);
        Assert.Equal($"Could not read the {Book} class model {ModelPath} of {Email}", warning.Message);
        Assert.IsType<HttpRequestException>(warning.Exception);
    }

    /// <summary>A training request the queue refuses is logged at Warning; the suggestion still answers with what it has.</summary>
    [Fact]
    public async Task GetAsync_ARefusedTrainingRequest_IsLogged_AndTheOlderModelStillAnswers()
    {
        Stored([_samples[0]]);
        _publisher.Setup(p => p.PublishAsync(It.IsAny<TrainClassModelMessage>(), It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("broker down"));

        Assert.NotNull(await GetAsync());

        FakeLogRecord warning = Assert.Single(_logger.Collector.GetSnapshot(), r => r.Level == LogLevel.Warning);
        Assert.Equal($"Could not request a {Book} class model training for {Email}", warning.Message);
        Assert.IsType<InvalidOperationException>(warning.Exception);
    }
}
