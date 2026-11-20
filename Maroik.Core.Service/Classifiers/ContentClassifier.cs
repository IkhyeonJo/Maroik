using System.IO.Compression;
using System.Text;
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
/// and Contract take no package but <c>ErrorOr</c>). The worker trains a model and stores it (<see cref="ToBytes"/>); the website reads
/// it back (<see cref="FromBytes"/>, <see cref="ClassModelSource"/>) and never trains.
/// </para>
/// </summary>
public sealed class ContentClassifier
{
    /// <summary>
    /// The model's version, stored with every model record and logged with every training: features, trainer and file format. Change
    /// it when any of them changes: a stored model of another version is not read, and is trained again.
    /// </summary>
    public const string ModelVersion = "char-ngram-1-3+logistic-regression/1";

    /// <summary>The output column of the predicted class label.</summary>
    private const string PredictedLabel = "PredictedLabel";

    /// <summary>The entry of a stored model that holds the ML.NET model.</summary>
    private const string ModelEntry = "model.zip";

    /// <summary>The entry of a stored single-class model that holds its one class label.</summary>
    private const string SingleClassEntry = "class.txt";

    /// <summary>The ML.NET context the model runs in; null for a single-class model.</summary>
    private readonly MLContext? _ml;

    /// <summary>The trained pipeline; null for a single-class model.</summary>
    private readonly ITransformer? _model;

    /// <summary>The schema of a training row, stored with the model; null for a single-class model.</summary>
    private readonly DataViewSchema? _inputSchema;

    /// <summary>The one class a single-class model answers; null otherwise.</summary>
    private readonly ClassSuggestion? _only;

    /// <summary>A model answering through <paramref name="model"/>.</summary>
    private ContentClassifier(MLContext ml, ITransformer model, DataViewSchema inputSchema)
    {
        _ml = ml;
        _model = model;
        _inputSchema = inputSchema;
    }

    /// <summary>A model answering <paramref name="only"/> for every content.</summary>
    private ContentClassifier(ClassSuggestion only) => _only = only;

    /// <summary>
    /// Trains on <paramref name="samples"/> (at least one; see <see cref="ExpenditureClassSuggestionPolicy.Learnable"/>). A history
    /// of a single class has nothing to tell apart and answers that class for every content.
    /// </summary>
    public static ContentClassifier Train(IReadOnlyList<ClassSample> samples)
    {
        List<ClassSuggestion> classes = [.. samples.Select(s => s.Class).Distinct()];
        if (classes.Count == 1)
            return new ContentClassifier(classes[0]);

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
        return new ContentClassifier(ml, pipeline.Fit(data), data.Schema);
    }

    /// <summary>
    /// The model as the file the worker stores: a zip holding the ML.NET model (<c>model.zip</c>), or the one class of a single-class
    /// model (<c>class.txt</c>).
    /// </summary>
    public byte[] ToBytes()
    {
        using var file = new MemoryStream();
        using (var zip = new ZipArchive(file, ZipArchiveMode.Create, leaveOpen: true))
        {
            if (_only != null)
            {
                using var writer = new StreamWriter(zip.CreateEntry(SingleClassEntry).Open(), Encoding.UTF8);
                writer.Write(LabelOf(_only));
            }
            else
            {
                using Stream entry = zip.CreateEntry(ModelEntry).Open();
                _ml!.Model.Save(_model!, _inputSchema!, entry);
            }
        }

        return file.ToArray();
    }

    /// <summary>The model stored as <paramref name="bytes"/> (<see cref="ToBytes"/>).</summary>
    public static ContentClassifier FromBytes(byte[] bytes)
    {
        using var file = new MemoryStream(bytes, writable: false);
        using var zip = new ZipArchive(file, ZipArchiveMode.Read);
        if (zip.GetEntry(SingleClassEntry) is { } single)
        {
            using var reader = new StreamReader(single.Open(), Encoding.UTF8);
            return new ContentClassifier(ClassOf(reader.ReadToEnd()));
        }

        // ML.NET reads a model from a seekable stream; a zip entry is not one.
        using var model = new MemoryStream();
        using (Stream entry = (zip.GetEntry(ModelEntry) ?? throw new InvalidDataException("The stored class model holds no model.")).Open())
            entry.CopyTo(model);
        model.Position = 0;
        var ml = new MLContext(seed: 0);
        ITransformer transformer = ml.Model.Load(model, out DataViewSchema inputSchema);
        return new ContentClassifier(ml, transformer, inputSchema);
    }

    /// <summary>The class this model gives <paramref name="content"/> (normalized as <see cref="ClassSuggestionPolicy.Normalize"/> does).</summary>
    public ClassSuggestion Predict(string? content)
    {
        if (_only != null)
            return _only;

        // Each prediction transforms a one-row view: unlike a prediction engine, a transformer is safe to share between threads.
        IDataView predicted = _model!.Transform(_ml!.Data.LoadFromEnumerable([new Input { Text = ClassSuggestionPolicy.Normalize(content) }]));
        return ClassOf(predicted.GetColumn<string>(PredictedLabel).Single());
    }

    /// <summary>The model's label of a class: main class and sub-class, which never contain a '/'.</summary>
    private static string LabelOf(ClassSuggestion c) => $"{c.MainClass}/{c.SubClass}";

    /// <summary>The class of a label (<see cref="LabelOf"/>).</summary>
    private static ClassSuggestion ClassOf(string label)
    {
        int slash = label.IndexOf('/', StringComparison.Ordinal);
        return new ClassSuggestion(label[..slash], label[(slash + 1)..]);
    }

    /// <summary>A training or prediction row (ML.NET reads its public properties).</summary>
    private sealed class Input
    {
        /// <summary>The normalized content.</summary>
        public string Text { get; set; } = "";

        /// <summary>The class label (<see cref="LabelOf"/>); empty when predicting.</summary>
        public string Label { get; set; } = "";
    }
}
