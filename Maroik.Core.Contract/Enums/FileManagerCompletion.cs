namespace Maroik.Core.Contract.Enums;

/// <summary>Where a File Manager upload stands after <c>IFileManagerService.CompleteUploadAsync</c>.</summary>
public enum FileManagerCompletion
{
    /// <summary>Decided: the file is stored (a success), or refused (the result says why).</summary>
    Decided = 0,

    /// <summary>File storage is still scanning it in the background; nothing changed, asked again later.</summary>
    Scanning,

    /// <summary>
    /// The scan could not be finished (too large for the scanner's limits, or the scanner unavailable) and the account may keep such a
    /// file: the owner is asked (the result's message is the question) and answers with <c>AcceptUnscannedUploadAsync</c> or an abort.
    /// </summary>
    AwaitingDecision
}
