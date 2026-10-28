// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace Maroik.Website.Models.ViewModels.FileManagers;

/// <summary>The selected items of a move, a delete or a multi-download; a move also names its target.</summary>
public class FileManagerSelectionInputViewModel
{
    /// <summary>The selected item IDs.</summary>
    public List<long> Ids { get; set; } = [];

    /// <summary>For a move: the target folder (null = the root).</summary>
    public long? TargetFolderId { get; set; }
}
