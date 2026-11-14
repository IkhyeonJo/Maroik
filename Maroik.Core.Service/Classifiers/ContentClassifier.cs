using Maroik.Core.Domain.Finances;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers;
using Microsoft.ML.Transforms.Text;

namespace Maroik.Core.Service.Classifiers;

/// <summary>
/// The text model behind the expenditure class suggestion: it answers, for content the account never recorded before, the class
/// its past content most resembles. <see cref="ExpenditureClassSuggestionPolicy"/> decides what may be suggested and answers the
/// content recorded before; this model takes the rest.
/// <para>
/// Features are TF-IDF weighted character n-grams of length 1 to 3 (L2-normalized, lower-cased, punctuation dropped): Korean content
/// is often written without spaces ("스타벅스강남점"), and its pieces still match a past "스타벅스". The classifier is a logistic
/// regression over all classes (ML.NET's maximum entropy trainer). On the owner's 8,649 records, trained on the older 80 %, this
/// model classified the newer 20 % right 98.2 % of the time (the same in an offline Python check), against 91.3 % for a naive
/// Bayes over two-letter pieces and 89.4 % for a naive Bayes on these same features.
/// </para>
/// <para>
/// Training is deterministic (a fixed seed and one thread), so the same records always give the same answers. It is an in-process
/// library like Magick.NET — no external call — which is why it lives in the Service, the layer that holds such libraries (Domain
/// and Contract take no package but <c>ErrorOr</c>). <see cref="ContentClassifierCache"/> keeps one model per account.
/// </para>
/// </summary>
public sealed class ContentClassifier
{
    /// <summary>
    /// The model's version, logged with every training (<see cref="ContentClassifierCache"/>): features and trainer. Change it when
    /// either changes, so a logged suggestion can be traced to the model that made it.
    /// </summary>
    public const string ModelVersion = "char-ngram-1-3+logistic-regression/1";

    /// <summary>The output column of the predicted class label.</summary>
    private const string PredictedLabel = "PredictedLabel";

    /// <summary>Answers the class for normalized content.</summary>
    private readonly Func<string, ClassSuggestion> _predict;

    /// <summary>A classifier that answers through <paramref name="predict"/>.</summary>
    private ContentClassifier(Func<string, ClassSuggestion> predict) => _predict = predict;

    /// <summary>
    /// Trains on <paramref name="samples"/> (at least one; see <see cref="ExpenditureClassSuggestionPolicy.Learnable"/>). A history
    /// of a single class has nothing to tell apart and answers that class for every content.
    /// </summary>
    public static ContentClassifier Train(IReadOnlyList<ClassSample> samples)
    {
        Dictionary<string, ClassSuggestion> classes = samples
            .Select(s => s.Class)
            .Distinct()
            .ToDictionary(LabelOf, c => c, StringComparer.Ordinal);
        if (classes.Count == 1)
        {
            ClassSuggestion only = classes.Values.Single();
            return new ContentClassifier(_ => only);
        }

        var ml = new MLContext(seed: 0);
        IDataView data = ml.Data.LoadFromEnumerable(samples.Select(s => new Input { Text = s.Text, Label = LabelOf(s.Class) }));
        var pipeline = ml.Transforms.Conversion.MapValueToKey(nameof(Input.Label))
            .Append(ml.Transforms.Text.FeaturizeText("Features", new TextFeaturizingEstimator.Options
            {
                CaseMode = TextNormalizingEstimator.CaseMode.Lower,
                KeepDiacritics = true,
                KeepPunctuations = false,
                KeepNumbers = true,
                WordFeatureExtractor = null,
                CharFeatureExtractor = new WordBagEstimator.Options
                {
                    NgramLength = 3, UseAllLengths = true, Weighting = NgramExtractingEstimator.WeightingCriteria.TfIdf
                },
                Norm = TextFeaturizingEstimator.NormFunction.L2
            }, nameof(Input.Text)))
            .Append(ml.MulticlassClassification.Trainers.SdcaMaximumEntropy(new SdcaMaximumEntropyMulticlassTrainer.Options { NumberOfThreads = 1 }))
            .Append(ml.Transforms.Conversion.MapKeyToValue(PredictedLabel));
        var model = pipeline.Fit(data);

        // Each prediction transforms a one-row view: unlike a prediction engine, a transformer is safe to share between threads.
        return new ContentClassifier(text =>
        {
            IDataView predicted = model.Transform(ml.Data.LoadFromEnumerable([new Input { Text = text }]));
            return classes[predicted.GetColumn<string>(PredictedLabel).Single()];
        });
    }

    /// <summary>The class this model gives <paramref name="content"/> (normalized as <see cref="ClassSuggestionPolicy.Normalize"/> does).</summary>
    public ClassSuggestion Predict(string? content) => _predict(ClassSuggestionPolicy.Normalize(content));

    /// <summary>The model's label of a class: main class and sub-class, which never contain a '/'.</summary>
    private static string LabelOf(ClassSuggestion c) => $"{c.MainClass}/{c.SubClass}";

    /// <summary>A training or prediction row (ML.NET reads its public properties).</summary>
    private sealed class Input
    {
        /// <summary>The normalized content.</summary>
        public string Text { get; set; } = "";

        /// <summary>The class label (<see cref="LabelOf"/>); empty when predicting.</summary>
        public string Label { get; set; } = "";
    }
}
