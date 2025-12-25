using System;
using System.Collections.Generic;

namespace Maroik.Core.PostgreSQL.Models;

/// <summary>
/// Board
/// </summary>
public partial class Board
{
    /// <summary>
    /// PK
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Type
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// Title
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Content
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// Writer
    /// </summary>
    public string? Writer { get; set; }

    /// <summary>
    /// Created
    /// </summary>
    public DateTime Created { get; set; }

    /// <summary>
    /// Updated
    /// </summary>
    public DateTime Updated { get; set; }

    /// <summary>
    /// View
    /// </summary>
    public long View { get; set; }

    /// <summary>
    /// Deleted
    /// </summary>
    public bool Deleted { get; set; }

    /// <summary>
    /// Locked
    /// </summary>
    public bool Locked { get; set; }

    /// <summary>
    /// Noticed
    /// </summary>
    public bool Noticed { get; set; }

    public virtual ICollection<BoardAttachedFile> BoardAttachedFiles { get; set; } = new List<BoardAttachedFile>();

    public virtual ICollection<BoardComment> BoardComments { get; set; } = new List<BoardComment>();
}
