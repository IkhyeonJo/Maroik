using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Maroik.Core.Domain.Finances;
using Microsoft.Extensions.Logging;

namespace Maroik.Core.Service.Classifiers;

/// <summary>
/// One <see cref="ContentClassifier"/> per account and book (income or expenditure), kept in memory (a singleton) and trained again
/// only when the records it learns from change. The forms ask on every pause in typing; training on several thousand records takes
/// about a second, so it must not happen per request.
/// <para>
/// "Changed" is judged by a fingerprint of the learnable records' content and class, in order: adding, deleting, re-wording or
/// re-classifying a record changes it; changing an amount or a date does not (the model never reads them). Nothing is stored:
/// after a restart the first suggestion trains the model again. Each training is logged at Information with the model's metadata
/// (book, account, <see cref="ContentClassifier.ModelVersion"/>, records, classes, duration), so a suggestion can be traced to the
/// model that made it. Two requests of one account at the same moment may both train; the later write wins and both answers are
/// valid models of the same records.
/// </para>
/// </summary>
/// <param name="logger">Records each training and its metadata.</param>
/// <param name="timeProvider">Times each training.</param>
public sealed class ContentClassifierCache(ILogger<ContentClassifierCache> logger, TimeProvider timeProvider)
{
    /// <summary>Each (book, account)'s model with the fingerprint of the records it was trained on.</summary>
    private readonly ConcurrentDictionary<(string Book, string Account), (string Fingerprint, ContentClassifier Classifier)> _models = new();

    /// <summary>
    /// The model of <paramref name="accountEmail"/>'s <paramref name="book"/> ("Income", "Expenditure") for <paramref name="samples"/>:
    /// the kept one when they are unchanged, a new one otherwise.
    /// </summary>
    public ContentClassifier GetOrTrain(string book, string accountEmail, IReadOnlyList<ClassSample> samples)
    {
        string fingerprint = FingerprintOf(samples);
        if (_models.TryGetValue((book, accountEmail), out var kept) && string.Equals(kept.Fingerprint, fingerprint, StringComparison.Ordinal))
            return kept.Classifier;

        long started = timeProvider.GetTimestamp();
        ContentClassifier classifier = ContentClassifier.Train(samples);
        _models[(book, accountEmail)] = (fingerprint, classifier);
        logger.LogInformation("Trained the {Book} class model {ModelVersion} for {Email} on {Records} records of {Classes} classes in {ElapsedMs} ms",
            book, ContentClassifier.ModelVersion, accountEmail, samples.Count, samples.Select(s => s.Class).Distinct().Count(),
            (long)timeProvider.GetElapsedTime(started).TotalMilliseconds);
        return classifier;
    }

    /// <summary>A SHA-256 of every sample's content and class, in order.</summary>
    private static string FingerprintOf(IReadOnlyList<ClassSample> samples)
    {
        var text = new StringBuilder();
        foreach (ClassSample s in samples)
            text.Append(s.Text).Append('\u001f').Append(s.Class.MainClass).Append('\u001f').Append(s.Class.SubClass).Append('\u001e');
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text.ToString())));
    }
}
