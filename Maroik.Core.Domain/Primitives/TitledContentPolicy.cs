using ErrorOr;
using Maroik.Core.Domain.Errors;

namespace Maroik.Core.Domain.Primitives;

/// <summary>
/// Shared length limits for a title + free-text body pair, used by aggregates that combine both
/// (<see cref="Boards.Board"/>'s Title/Content, <see cref="Calendars.CalendarEvent"/>'s
/// Title/Description). Centralized so the Create/Update methods on both aggregates can't drift
/// apart from each other or from the persisted column shapes they both share.
/// </summary>
public static class TitledContentPolicy
{
    /// <summary>Maximum length, in characters, of a title.</summary>
    public const int MaxTitleLength = 100;

    /// <summary>Maximum length, in characters, of the free-text body (content/description).</summary>
    public const int MaxBodyLength = 16384;

    /// <summary>
    /// Maximum length of a body as submitted, before it is sanitized. The sanitizer's time grows faster than the length for
    /// deeply nested HTML (1.1 MB took 68 s), so an input no stored body could come from is refused before it runs; the sanitized
    /// body is still held to <see cref="MaxBodyLength"/>. Four times that limit leaves room for what sanitizing removes: the
    /// edit page re-submits each image as a blob: URL plus its Data Protection token (~330 characters, against ~150 stored), so
    /// the largest storable post (~110 images) comes back at ~36,000.
    /// </summary>
    public const int MaxRawBodyLength = 4 * MaxBodyLength;

    /// <summary>
    /// Maximum number of inline images (<c>&lt;img&gt;</c> tags, a repeated image counted each time) in a body. Every view
    /// fetches each image whole from file storage and embeds it as base64, and an image may be as large as the attachment
    /// limit (10 MB): without a bound, a body of repeated image tags (~180 fit in <see cref="MaxBodyLength"/>) made one
    /// view build gigabytes in the web process's memory.
    /// </summary>
    public const int MaxInlineImages = 20;

    /// <summary>Refuses a body holding more than <see cref="MaxInlineImages"/> images.</summary>
    /// <param name="count">The number of <c>&lt;img&gt;</c> tags in the sanitized body.</param>
    public static ErrorOr<Success> CheckInlineImageCount(int count) =>
        count > MaxInlineImages
            ? DomainError.Validation("Content.TooManyImages", "You can insert at most {0} images.", MaxInlineImages)
            : Result.Success;
}
