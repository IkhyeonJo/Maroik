using System;
using System.Collections.Generic;

namespace Maroik.Core.PostgreSQL.Models;

/// <summary>
/// ClassModel (trained class-suggestion model file of an account's book)
/// </summary>
public partial class ClassModel
{
    /// <summary>
    /// PK
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Account Email (ID)
    /// </summary>
    public string AccountEmail { get; set; } = null!;

    /// <summary>
    /// Income or Expenditure
    /// </summary>
    public string Book { get; set; } = null!;

    /// <summary>
    /// Model file path in the file storage (upload/ClassModels/{guid}.zip)
    /// </summary>
    public string FilePath { get; set; } = null!;

    /// <summary>
    /// SHA-256 of the records the model was trained on
    /// </summary>
    public string Fingerprint { get; set; } = null!;

    /// <summary>
    /// Model code version that wrote the file
    /// </summary>
    public string ModelVersion { get; set; } = null!;

    /// <summary>
    /// Trained (UTC)
    /// </summary>
    public DateTime Trained { get; set; }

    /// <summary>The account that owns this model.</summary>
    public virtual Account AccountEmailNavigation { get; set; } = null!;
}
