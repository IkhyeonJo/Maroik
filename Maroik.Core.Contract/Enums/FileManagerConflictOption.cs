namespace Maroik.Core.Contract.Enums;

/// <summary>What an upload does with a name already used in its folder, as the callers of <c>IFileManagerService</c> name it.</summary>
public enum FileManagerConflictOption
{
    /// <summary>Refuse and let the page ask the user.</summary>
    Ask,

    /// <summary>Replace the existing file.</summary>
    Overwrite,

    /// <summary>Keep both, under a new name.</summary>
    KeepBoth,
}
