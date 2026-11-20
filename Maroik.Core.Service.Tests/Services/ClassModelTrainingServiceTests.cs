using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Domain.Finances;
using Maroik.Core.Service.Classifiers;
using Maroik.Core.Service.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace Maroik.Core.Service.Tests.Services;

/// <summary>
/// Unit tests for <see cref="ClassModelTrainingService"/>, the worker's training of an account's class-suggestion model: it learns from
/// the book's current records, saves the model as a new file, points the account's record at it, then deletes the file it replaced —
/// and does nothing when the record already matches the records and the model code.
/// </summary>
public class ClassModelTrainingServiceTests
{
    private const string Email = "a@test.com";
    private const string OldPath = "upload/ClassModels/11111111-1111-1111-1111-111111111111.zip";

    private readonly Mock<IIncomeRepository> _incomes = new();
    private readonly Mock<IExpenditureRepository> _expenditures = new();
    private readonly Mock<IClassModelRepository> _records = new();
    private readonly Mock<IFileClient> _files = new();
    private readonly FakeLogger<ClassModelTrainingService> _logger = new();

    /// <summary>The fixed "current time" of these tests.</summary>
    private static readonly DateTime _now = new(2026, 11, 14, 12, 0, 0, DateTimeKind.Utc);

    /// <summary>The files saved, by path.</summary>
    private readonly Dictionary<string, byte[]> _saved = [];

    /// <summary>Every save succeeds and is recorded; every delete succeeds.</summary>
    public ClassModelTrainingServiceTests()
    {
        _files.Setup(f => f.SaveClassModelAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .Callback((string path, byte[] bytes, CancellationToken _) => _saved[path] = bytes).ReturnsAsync(true);
        _files.Setup(f => f.DeleteClassModelAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _expenditures.Setup(r => r.GetByAccountEmailAsync(Email, It.IsAny<CancellationToken>())).ReturnsAsync(Expenditures());
        _incomes.Setup(r => r.GetByAccountEmailAsync(Email, It.IsAny<CancellationToken>())).ReturnsAsync(
        [
            Income.Reconstitute(1, Email, "RegularIncome", "LaborIncome", "11월 급여", 1m, "KRW", "Wallet", null, _now, _now),
            Income.Reconstitute(2, Email, "RegularIncome", "FinancialIncome", "예금 이자", 1m, "KRW", "Wallet", null, _now, _now),
        ]);
    }

    /// <summary>Two expenditures of two classes.</summary>
    private static List<Expenditure> Expenditures() =>
    [
        Expenditure.Reconstitute(1, Email, "ConsumerSpending", "TransportationCost", "지하철", 1m, "KRW", "Wallet", null, null, _now, _now),
        Expenditure.Reconstitute(2, Email, "ConsumerSpending", "MealOrEatOutExpenses", "스타벅스", 1m, "KRW", "Wallet", null, null, _now, _now),
    ];

    /// <summary>The fingerprint of <see cref="Expenditures"/>.</summary>
    private static string ExpenditureFingerprint => ClassSampleFingerprint.Of(ExpenditureClassSuggestionPolicy.Learnable(Expenditures()));

    /// <summary>The service under test.</summary>
    private ClassModelTrainingService Sut() => new(_incomes.Object, _expenditures.Object, _records.Object, _files.Object, _logger,
        new FakeTimeProvider(new DateTimeOffset(_now)));

    /// <summary>The account's stored record of <paramref name="book"/>.</summary>
    private void Recorded(string book, string fingerprint, string? version = null) =>
        _records.Setup(r => r.FindAsync(Email, book, It.IsAny<CancellationToken>())).ReturnsAsync(
            ClassModel.Reconstitute(7, Email, book, OldPath, fingerprint, version ?? ContentClassifier.ModelVersion, _now.AddDays(-1)));

    /// <summary>
    /// A first training saves a model of the records as a new model file, creates the record pointing at it (with the records'
    /// fingerprint, the model version and the time), and logs the model's metadata at Information.
    /// </summary>
    [Fact]
    public async Task TrainAsync_FirstTraining_SavesTheModel_AndCreatesTheRecord()
    {
        ClassModel? created = null;
        _records.Setup(r => r.CreateAsync(It.IsAny<ClassModel>(), It.IsAny<CancellationToken>())).Callback((ClassModel m, CancellationToken _) => created = m);

        await Sut().TrainAsync(ClassModel.ExpenditureBook, Email, TestContext.Current.CancellationToken);

        (string path, byte[] bytes) = Assert.Single(_saved);
        Assert.True(ClassModel.IsStoragePath(path));
        Assert.Equal(new ClassSuggestion("ConsumerSpending", "TransportationCost"), ContentClassifier.FromBytes(bytes).Predict("지하철 2호선"));
        Assert.NotNull(created);
        Assert.Equal((Email, ClassModel.ExpenditureBook, path, ExpenditureFingerprint, ContentClassifier.ModelVersion, _now),
            (created.AccountEmail, created.Book, created.FilePath, created.Fingerprint, created.ModelVersion, created.Trained));
        _files.Verify(f => f.DeleteClassModelAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        FakeLogRecord info = Assert.Single(_logger.Collector.GetSnapshot(), r => r.Level == LogLevel.Information);
        Assert.Equal($"Trained the Expenditure class model {ContentClassifier.ModelVersion} for {Email} on 2 records of 2 classes in 0 ms, stored at {path}", info.Message);
    }

    /// <summary>A retraining rewrites the same record to the new file, then deletes the file it replaced.</summary>
    [Fact]
    public async Task TrainAsync_Retraining_UpdatesTheRecord_ThenDeletesTheOldFile()
    {
        Recorded(ClassModel.ExpenditureBook, "older-records");
        ClassModel? updated = null;
        var order = new List<string>();
        _records.Setup(r => r.UpdateEntityAsync(It.IsAny<ClassModel>(), It.IsAny<CancellationToken>()))
            .Callback((ClassModel m, CancellationToken _) => { updated = m; order.Add("update"); });
        _files.Setup(f => f.DeleteClassModelAsync(OldPath, It.IsAny<CancellationToken>())).Callback(() => order.Add("delete")).ReturnsAsync(true);

        await Sut().TrainAsync(ClassModel.ExpenditureBook, Email, TestContext.Current.CancellationToken);

        string path = Assert.Single(_saved).Key;
        Assert.Equal((7L, path, ExpenditureFingerprint), (updated?.Id, updated?.FilePath, updated?.Fingerprint));
        Assert.Equal(["update", "delete"], order);
        _records.Verify(r => r.CreateAsync(It.IsAny<ClassModel>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>A record of these records and this model code is current: nothing is trained, saved or written.</summary>
    [Fact]
    public async Task TrainAsync_ACurrentModel_IsLeftAsItIs()
    {
        Recorded(ClassModel.ExpenditureBook, ExpenditureFingerprint);

        await Sut().TrainAsync(ClassModel.ExpenditureBook, Email, TestContext.Current.CancellationToken);

        Assert.Empty(_saved);
        _records.Verify(r => r.UpdateEntityAsync(It.IsAny<ClassModel>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>A record of these records written by other model code is trained again.</summary>
    [Fact]
    public async Task TrainAsync_AModelOfAnotherVersion_IsTrainedAgain()
    {
        Recorded(ClassModel.ExpenditureBook, ExpenditureFingerprint, version: "older-version");

        await Sut().TrainAsync(ClassModel.ExpenditureBook, Email, TestContext.Current.CancellationToken);

        Assert.Single(_saved);
    }

    /// <summary>The income book learns from the incomes.</summary>
    [Fact]
    public async Task TrainAsync_TheIncomeBook_LearnsFromTheIncomes()
    {
        await Sut().TrainAsync(ClassModel.IncomeBook, Email, TestContext.Current.CancellationToken);

        Assert.Equal(new ClassSuggestion("RegularIncome", "LaborIncome"), ContentClassifier.FromBytes(Assert.Single(_saved).Value).Predict("12월 급여"));
        _records.Verify(r => r.CreateAsync(It.Is<ClassModel>(m => m.Book == ClassModel.IncomeBook), It.IsAny<CancellationToken>()), Times.Once);
        _expenditures.Verify(r => r.GetByAccountEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>Without a record to learn from nothing is trained.</summary>
    [Fact]
    public async Task TrainAsync_NoLearnableRecord_TrainsNothing()
    {
        _expenditures.Setup(r => r.GetByAccountEmailAsync(Email, It.IsAny<CancellationToken>())).ReturnsAsync([]);

        await Sut().TrainAsync(ClassModel.ExpenditureBook, Email, TestContext.Current.CancellationToken);

        Assert.Empty(_saved);
        _records.Verify(r => r.FindAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>A book other than Income or Expenditure is refused, logged at Warning, and nothing is read.</summary>
    [Fact]
    public async Task TrainAsync_AnUnknownBook_IsLoggedAndIgnored()
    {
        await Sut().TrainAsync("Savings", Email, TestContext.Current.CancellationToken);

        Assert.Empty(_saved);
        Assert.Equal($"Ignored a class model training of the unknown book Savings for {Email}",
            Assert.Single(_logger.Collector.GetSnapshot(), r => r.Level == LogLevel.Warning).Message);
    }

    /// <summary>A model the file storage refuses leaves the record as it was (logged at Warning); the next suggestion asks again.</summary>
    [Fact]
    public async Task TrainAsync_ARefusedSave_LeavesTheRecord()
    {
        Recorded(ClassModel.ExpenditureBook, "older-records");
        _files.Setup(f => f.SaveClassModelAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        await Sut().TrainAsync(ClassModel.ExpenditureBook, Email, TestContext.Current.CancellationToken);

        _records.Verify(r => r.UpdateEntityAsync(It.IsAny<ClassModel>(), It.IsAny<CancellationToken>()), Times.Never);
        _files.Verify(f => f.DeleteClassModelAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.Equal($"The Expenditure class model of {Email} could not be stored; the record keeps its model",
            Assert.Single(_logger.Collector.GetSnapshot(), r => r.Level == LogLevel.Warning).Message);
    }

    /// <summary>
    /// When the record cannot be written the new file is deleted again (nothing points at it) and the failure propagates to the
    /// worker, which logs it; the old file stays, since the record still points at it.
    /// </summary>
    [Fact]
    public async Task TrainAsync_ARecordWriteFailure_DeletesTheNewFile_AndPropagates()
    {
        Recorded(ClassModel.ExpenditureBook, "older-records");
        _records.Setup(r => r.UpdateEntityAsync(It.IsAny<ClassModel>(), It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("db down"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => Sut().TrainAsync(ClassModel.ExpenditureBook, Email, TestContext.Current.CancellationToken));

        string path = Assert.Single(_saved).Key;
        _files.Verify(f => f.DeleteClassModelAsync(path, It.IsAny<CancellationToken>()), Times.Once);
        _files.Verify(f => f.DeleteClassModelAsync(OldPath, It.IsAny<CancellationToken>()), Times.Never);
    }
}
