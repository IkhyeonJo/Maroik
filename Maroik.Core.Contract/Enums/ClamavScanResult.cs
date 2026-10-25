namespace Maroik.Core.Contract.Enums;

/// <summary>
/// Outcome of scanning a file stream with the ClamAV daemon (see <c>IClamavClient</c>).
/// Only <see cref="Clean"/> lets an upload through; the others are all refusals, kept apart so a daemon outage is never reported
/// to anyone (user or operator) as a virus detection, and so the File Manager can tell a file too large to scan from an outage.
/// </summary>
public enum ClamavScanResult
{
    /// <summary>The daemon scanned the stream and reported it clean.</summary>
    Clean = 0,

    /// <summary>The daemon scanned the stream and reported a detection.</summary>
    Infected,

    /// <summary>
    /// The scan could not be completed: the daemon was unreachable, timed out, answered with an
    /// error (e.g. stream too large) or sent a reply that could not be understood. Nothing is known
    /// about the file, so it is refused (fail-closed) — but it is not a detection.
    /// </summary>
    Unavailable,

    /// <summary>
    /// The daemon could not scan the stream whole because one of its limits was reached (size, recursion, file count or scan time —
    /// reported as "Heuristics.Limits.Exceeded…" with <c>AlertExceedsMax</c>). Like <see cref="Unavailable"/> nothing is known about
    /// the file and it is not a detection; kept apart so a caller can say the file was too large to scan.
    /// </summary>
    LimitExceeded
}
