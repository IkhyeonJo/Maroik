using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Contract.Messages;
using Maroik.Core.Domain.Finances;
using Microsoft.Extensions.Logging;

namespace Maroik.Core.Service.Classifiers;

/// <summary>
/// The website's way to an account's class-suggestion model. The worker trains the models (<c>ClassModelTrainingService</c>) and
/// stores each as a file; the account's <see cref="ClassModel"/> record says which file, trained on which records, by which model
/// code. This reads that file once and keeps the model in <see cref="ClassModelCache"/>. When there is no model, or it was trained on
/// older records, or it cannot be read, it asks the worker for a training (<see cref="IClassModelTrainingPublisher"/>) and answers
/// meanwhile with the older model, or with none: the website never trains.
/// </summary>
public sealed class ClassModelSource(
    IClassModelRepository records,
    IFileClient files,
    IClassModelTrainingPublisher publisher,
    ClassModelCache cache,
    ILogger<ClassModelSource> logger)
{
    /// <summary>
    /// The model of <paramref name="accountEmail"/>'s <paramref name="book"/> to answer with for the account's current records, or null
    /// when there is none yet (or no record to learn from); asks for a training when the stored model is not current.
    /// <paramref name="learnableRecords"/> reads the account's learnable records. It runs only when their fingerprint is not known
    /// yet: on the first question, and on the first after <see cref="RecordsChanged"/>, so the form's question on every pause in
    /// typing does not read every record again.
    /// </summary>
    public async Task<ContentClassifier?> GetAsync(string book, string accountEmail, Func<CancellationToken, Task<IReadOnlyList<ClassSample>>> learnableRecords, CancellationToken ct = default)
    {
        if (!cache.TryFindFingerprint(book, accountEmail, out string? fingerprint, out long version))
        {
            IReadOnlyList<ClassSample> samples = await learnableRecords(ct);
            fingerprint = samples.Count == 0 ? null : ClassSampleFingerprint.Of(samples);
            cache.KeepFingerprint(book, accountEmail, version, fingerprint);
        }

        // Nothing to learn from: no model to look for, and the worker would have nothing to train on.
        if (fingerprint == null)
            return null;

        ClassModel? record = await records.FindAsync(accountEmail, book, ct);

        ContentClassifier? classifier = null;
        if (record != null && record.IsReadableBy(ContentClassifier.ModelVersion))
            classifier = cache.Find(book, accountEmail, record.FilePath) ?? await LoadAsync(record, ct);

        bool current = record != null && record.IsCurrent(fingerprint, ContentClassifier.ModelVersion) && classifier != null;
        if (!current && cache.ClaimTrainingRequest(book, accountEmail, fingerprint))
            await RequestTrainingAsync(book, accountEmail, ct);

        return classifier;
    }

    /// <summary>
    /// Tells the source that <paramref name="accountEmail"/>'s <paramref name="book"/> records changed (a committed create, edit or
    /// delete), so the next question reads them again and asks for a training of the new records.
    /// </summary>
    public void RecordsChanged(string book, string accountEmail) => cache.ForgetFingerprint(book, accountEmail);

    /// <summary>Reads the model file of <paramref name="record"/> and keeps it; null (logged) when it cannot be read.</summary>
    private async Task<ContentClassifier?> LoadAsync(ClassModel record, CancellationToken ct)
    {
        try
        {
            ContentClassifier classifier = ContentClassifier.FromBytes(await files.DownloadAsync(record.FilePath, ct));
            cache.Keep(record.Book, record.AccountEmail, record.FilePath, classifier);
            return classifier;
        }
        catch (Exception ex)
        {
            // Degraded, not failed: the suggestion is only a hint. The training asked for next writes a new file.
            logger.LogWarning(ex, "Could not read the {Book} class model {FilePath} of {Email}", record.Book, record.FilePath, record.AccountEmail);
            return null;
        }
    }

    /// <summary>Asks the worker to train <paramref name="accountEmail"/>'s <paramref name="book"/> model; a refused request is logged.</summary>
    private async Task RequestTrainingAsync(string book, string accountEmail, CancellationToken ct)
    {
        try
        {
            await publisher.PublishAsync(new TrainClassModelMessage(book, accountEmail), ct);
            logger.LogInformation("Requested a {Book} class model training for {Email}", book, accountEmail);
        }
        catch (Exception ex)
        {
            // Degraded, not failed: the suggestion answers with what it has; the request is made again after ClassModelCache.TrainingRequestRetryAfter.
            logger.LogWarning(ex, "Could not request a {Book} class model training for {Email}", book, accountEmail);
        }
    }
}
