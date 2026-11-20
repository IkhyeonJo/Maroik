using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Domain.Finances;
using Maroik.Core.Service.Classifiers;
using Microsoft.Extensions.Logging;

namespace Maroik.Core.Service.Services;

/// <summary>
/// The worker's training of an account's class-suggestion model (<see cref="IClassModelTrainingService"/>). It learns from the book's
/// current records (<see cref="IncomeClassSuggestionPolicy"/> / <see cref="ExpenditureClassSuggestionPolicy"/>), saves the model as a
/// new file (<see cref="ClassModel.StoragePathFor"/>: the file storage never overwrites), points the account's record at it, and only
/// then deletes the file it replaced — so a reader always finds the file its record names. A record already trained on these records
/// by this model code is left as it is (a request can be repeated). One worker consumes the queue one message at a time, so no two
/// trainings of an account run at once.
/// </summary>
public sealed class ClassModelTrainingService(
    IIncomeRepository incomes,
    IExpenditureRepository expenditures,
    IClassModelRepository records,
    IFileClient files,
    ILogger<ClassModelTrainingService> logger,
    TimeProvider timeProvider) : IClassModelTrainingService
{
    /// <inheritdoc />
    public async Task TrainAsync(string book, string accountEmail, CancellationToken ct = default)
    {
        IReadOnlyList<ClassSample> samples;
        switch (book)
        {
            case ClassModel.IncomeBook:
                samples = IncomeClassSuggestionPolicy.Learnable(await incomes.GetByAccountEmailAsync(accountEmail, ct));
                break;
            case ClassModel.ExpenditureBook:
                samples = ExpenditureClassSuggestionPolicy.Learnable(await expenditures.GetByAccountEmailAsync(accountEmail, ct));
                break;
            default:
                logger.LogWarning("Ignored a class model training of the unknown book {Book} for {Email}", book, accountEmail);
                return;
        }

        // Nothing to learn from: the website suggests nothing either (it asks only when there is a record to learn from).
        if (samples.Count == 0)
            return;

        string fingerprint = ClassSampleFingerprint.Of(samples);
        ClassModel? record = await records.FindAsync(accountEmail, book, ct);
        if (record != null && record.IsCurrent(fingerprint, ContentClassifier.ModelVersion))
            return;

        long started = timeProvider.GetTimestamp();
        byte[] model = ContentClassifier.Train(samples).ToBytes();
        string path = ClassModel.StoragePathFor(Guid.NewGuid());
        if (!await files.SaveClassModelAsync(path, model, ct))
        {
            // The client logged why; the record keeps its model, and the next suggestion that finds it out of date asks again.
            logger.LogWarning("The {Book} class model of {Email} could not be stored; the record keeps its model", book, accountEmail);
            return;
        }

        DateTime trained = timeProvider.GetUtcNow().UtcDateTime;
        try
        {
            if (record == null)
                await records.CreateAsync(ClassModel.Create(accountEmail, book, path, fingerprint, ContentClassifier.ModelVersion, trained), ct);
            else
                await records.UpdateEntityAsync(record.Retrained(path, fingerprint, ContentClassifier.ModelVersion, trained), ct);
        }
        catch
        {
            // Nothing points at the new file; remove it before the failure goes on to the worker, which logs it.
            await files.DeleteClassModelAsync(path, CancellationToken.None);
            throw;
        }

        logger.LogInformation("Trained the {Book} class model {ModelVersion} for {Email} on {Records} records of {Classes} classes in {ElapsedMs} ms, stored at {FilePath}",
            book, ContentClassifier.ModelVersion, accountEmail, samples.Count, samples.Select(s => s.Class).Distinct().Count(),
            (long)timeProvider.GetElapsedTime(started).TotalMilliseconds, path);

        // The record no longer names the old file. A failed delete leaves an orphan file, logged by the client.
        if (record != null)
            await files.DeleteClassModelAsync(record.FilePath, ct);
    }
}
