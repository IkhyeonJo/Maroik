using System.Security.Cryptography;
using System.Text;
using Maroik.Core.Domain.Finances;

namespace Maroik.Core.Service.Classifiers;

/// <summary>
/// The fingerprint of the records a class-suggestion model learns from: a SHA-256 of each sample's content and class, in order. It
/// changes when a record is added, deleted, re-worded or re-classified, and not when an amount or a date changes (the model never
/// reads them). A model record (<see cref="ClassModel.Fingerprint"/>) keeps the fingerprint it was trained on, so the website can tell
/// whether the stored model is current.
/// </summary>
public static class ClassSampleFingerprint
{
    /// <summary>The fingerprint of <paramref name="samples"/>, in upper-case hex.</summary>
    public static string Of(IReadOnlyList<ClassSample> samples)
    {
        var text = new StringBuilder();
        foreach (ClassSample s in samples)
            text.Append(s.Text).Append('\u001f').Append(s.Class.MainClass).Append('\u001f').Append(s.Class.SubClass).Append('\u001e');
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text.ToString())));
    }
}
