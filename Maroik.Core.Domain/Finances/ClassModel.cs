using Maroik.Core.Domain.Primitives;

namespace Maroik.Core.Domain.Finances;

/// <summary>
/// The record of an account's trained class-suggestion model of one book (income or expenditure). The model itself is a file in the
/// file storage (<see cref="FilePath"/>); this record says where it is, which records it was trained on (<see cref="Fingerprint"/>)
/// and by which model code (<see cref="ModelVersion"/>). The worker writes it after each training; the website only reads it.
/// </summary>
public sealed class ClassModel : AggregateRoot<long>
{
    /// <summary>The income book.</summary>
    public const string IncomeBook = "Income";

    /// <summary>The expenditure book.</summary>
    public const string ExpenditureBook = "Expenditure";

    /// <summary>The account the model belongs to.</summary>
    public string AccountEmail { get; }

    /// <summary><see cref="IncomeBook"/> or <see cref="ExpenditureBook"/>.</summary>
    public string Book { get; }

    /// <summary>Where the model file is stored (<see cref="StoragePathFor"/>).</summary>
    public string FilePath { get; }

    /// <summary>The fingerprint of the records the model was trained on.</summary>
    public string Fingerprint { get; }

    /// <summary>The model code's version the file was written by.</summary>
    public string ModelVersion { get; }

    /// <summary>When the model was trained (UTC).</summary>
    public DateTime Trained { get; }

    /// <summary>Sets every field.</summary>
    private ClassModel(long id, string accountEmail, string book, string filePath, string fingerprint, string modelVersion, DateTime trained) : base(id)
    {
        AccountEmail = accountEmail;
        Book = book;
        FilePath = filePath;
        Fingerprint = fingerprint;
        ModelVersion = modelVersion;
        Trained = trained;
    }

    /// <summary>The storage folder of the model files.</summary>
    public const string StorageDirectory = "upload/ClassModels";

    /// <summary>
    /// The storage path of a model file: a new GUID per training, so a retraining writes a new file next to the old one (the file
    /// storage never overwrites) and the old one is deleted once the record points at the new one.
    /// </summary>
    public static string StoragePathFor(Guid id) => $"{StorageDirectory}/{id:D}.zip";

    /// <summary>
    /// Whether <paramref name="path"/> is exactly a model file's storage path (<see cref="StoragePathFor"/>, the GUID in its canonical
    /// lower-case form): the file storage saves and deletes nothing else through its model API.
    /// </summary>
    public static bool IsStoragePath(string? path)
    {
        const string prefix = StorageDirectory + "/";
        const string suffix = ".zip";
        if (path is null || !path.StartsWith(prefix, StringComparison.Ordinal) || !path.EndsWith(suffix, StringComparison.Ordinal))
            return false;
        string name = path[prefix.Length..^suffix.Length];
        return Guid.TryParseExact(name, "D", out Guid id) && string.Equals(id.ToString("D"), name, StringComparison.Ordinal);
    }

    /// <summary>Rebuilds a record from the repository.</summary>
    public static ClassModel Reconstitute(long id, string accountEmail, string book, string filePath, string fingerprint, string modelVersion, DateTime trained) =>
        new(id, accountEmail, book, filePath, fingerprint, modelVersion, trained);

    /// <summary>A new record of a first training.</summary>
    public static ClassModel Create(string accountEmail, string book, string filePath, string fingerprint, string modelVersion, DateTime trained) =>
        new(0, accountEmail, book, filePath, fingerprint, modelVersion, trained);

    /// <summary>Whether the model was trained on these records by this code.</summary>
    public bool IsCurrent(string fingerprint, string modelVersion) =>
        IsReadableBy(modelVersion) && string.Equals(Fingerprint, fingerprint, StringComparison.Ordinal);

    /// <summary>
    /// Whether code of <paramref name="modelVersion"/> can read the model file. A model of an older fingerprint still answers while its
    /// successor trains; one written by other model code (features or trainer changed) is not read at all.
    /// </summary>
    public bool IsReadableBy(string modelVersion) => string.Equals(ModelVersion, modelVersion, StringComparison.Ordinal);

    /// <summary>This record after a retraining.</summary>
    public ClassModel Retrained(string filePath, string fingerprint, string modelVersion, DateTime trained) =>
        new(Id, AccountEmail, Book, filePath, fingerprint, modelVersion, trained);
}
