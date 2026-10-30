using System.Buffers.Binary;

namespace Maroik.FileStorage.Helpers;

/// <summary>
/// Reads just enough of a ZIP archive's structure (PKWARE APPNOTE 4.3) to tell whether any entry is encrypted: the end of
/// central directory record (classic, or Zip64 through its locator) gives where the central directory starts and how many
/// entries it holds, and each central directory header carries the entry's general purpose flag, whose bit 0 is set for every
/// kind of encryption (traditional PKWARE, WinZip AES, strong encryption). Nothing is decompressed or decrypted.
/// <para>
/// The central directory decides, as it does for the extractors (they list and open entries from it). Every length and offset
/// read from the archive is checked against the buffer before it is followed; an archive whose structure cannot be followed is
/// reported unreadable rather than guessed at, and the caller refuses it.
/// </para>
/// </summary>
internal static class ZipCentralDirectory
{
    private const uint EndOfCentralDirectorySignature = 0x06054B50;
    private const uint Zip64EndOfCentralDirectorySignature = 0x06064B50;
    private const uint Zip64EndLocatorSignature = 0x07064B50;
    private const uint CentralDirectoryHeaderSignature = 0x02014B50;

    /// <summary>Fixed part of the end of central directory record (the comment follows it).</summary>
    private const int EndOfCentralDirectoryLength = 22;

    /// <summary>The Zip64 end of central directory locator, which sits right before the classic end record.</summary>
    private const int Zip64EndLocatorLength = 20;

    /// <summary>Fixed part of the Zip64 end of central directory record (up to and including the central directory offset).</summary>
    private const int Zip64EndOfCentralDirectoryLength = 56;

    /// <summary>Fixed part of a central directory header (name, extra field and comment follow it).</summary>
    private const int CentralDirectoryHeaderLength = 46;

    /// <summary>General purpose bit 0: the entry is encrypted.</summary>
    private const ushort EncryptedFlag = 0x0001;

    /// <summary>
    /// Returns <see langword="false"/> when <paramref name="archive"/>'s end record or central directory cannot be followed;
    /// otherwise <see langword="true"/>, with <paramref name="encrypted"/> telling whether any entry has the encryption flag.
    /// </summary>
    internal static bool TryFindEncryptedEntry(ReadOnlySpan<byte> archive, out bool encrypted)
    {
        encrypted = false;
        int end = FindEndOfCentralDirectory(archive);
        if (end < 0)
            return false;

        long entryCount = BinaryPrimitives.ReadUInt16LittleEndian(archive[(end + 10)..]);
        long offset = BinaryPrimitives.ReadUInt32LittleEndian(archive[(end + 16)..]);
        // A field at its maximum is a sentinel: the real value is in the Zip64 end record.
        if (entryCount == ushort.MaxValue || offset == uint.MaxValue
            || BinaryPrimitives.ReadUInt32LittleEndian(archive[(end + 12)..]) == uint.MaxValue)
        {
            if (!TryReadZip64End(archive, end, out entryCount, out offset))
                return false;
        }

        for (long i = 0; i < entryCount; i++)
        {
            if (offset > archive.Length - CentralDirectoryHeaderLength)
                return false;
            ReadOnlySpan<byte> header = archive[(int)offset..];
            if (BinaryPrimitives.ReadUInt32LittleEndian(header) != CentralDirectoryHeaderSignature)
                return false;

            if ((BinaryPrimitives.ReadUInt16LittleEndian(header[8..]) & EncryptedFlag) != 0)
                encrypted = true;

            offset += CentralDirectoryHeaderLength
                + BinaryPrimitives.ReadUInt16LittleEndian(header[28..])
                + BinaryPrimitives.ReadUInt16LittleEndian(header[30..])
                + BinaryPrimitives.ReadUInt16LittleEndian(header[32..]);
            if (offset > archive.Length)
                return false;
        }

        return true;
    }

    /// <summary>
    /// The offset of the end of central directory record, or -1. It is searched backwards from the end, across at most a
    /// maximal (65,535-byte) comment, and a candidate counts only if its comment length reaches exactly to the end of the
    /// archive, so a comment that happens to contain the signature is not taken for the record.
    /// </summary>
    private static int FindEndOfCentralDirectory(ReadOnlySpan<byte> archive)
    {
        int lowest = Math.Max(0, archive.Length - EndOfCentralDirectoryLength - ushort.MaxValue);
        for (int at = archive.Length - EndOfCentralDirectoryLength; at >= lowest; at--)
        {
            if (BinaryPrimitives.ReadUInt32LittleEndian(archive[at..]) == EndOfCentralDirectorySignature
                && at + EndOfCentralDirectoryLength + BinaryPrimitives.ReadUInt16LittleEndian(archive[(at + 20)..]) == archive.Length)
                return at;
        }
        return -1;
    }

    /// <summary>
    /// Reads the entry count and the central directory offset from the Zip64 end record that the locator before the classic end
    /// record at <paramref name="end"/> points to. Values no in-memory archive can reach count as unreadable.
    /// </summary>
    private static bool TryReadZip64End(ReadOnlySpan<byte> archive, int end, out long entryCount, out long offset)
    {
        entryCount = 0;
        offset = 0;
        int locator = end - Zip64EndLocatorLength;
        if (locator < 0 || BinaryPrimitives.ReadUInt32LittleEndian(archive[locator..]) != Zip64EndLocatorSignature)
            return false;

        ulong record = BinaryPrimitives.ReadUInt64LittleEndian(archive[(locator + 8)..]);
        if (archive.Length < Zip64EndOfCentralDirectoryLength || record > (ulong)(archive.Length - Zip64EndOfCentralDirectoryLength))
            return false;
        ReadOnlySpan<byte> zip64End = archive[(int)record..];
        if (BinaryPrimitives.ReadUInt32LittleEndian(zip64End) != Zip64EndOfCentralDirectorySignature)
            return false;

        ulong count = BinaryPrimitives.ReadUInt64LittleEndian(zip64End[32..]);
        ulong start = BinaryPrimitives.ReadUInt64LittleEndian(zip64End[48..]);
        if (count > (ulong)archive.Length || start > (ulong)archive.Length)
            return false;

        entryCount = (long)count;
        offset = (long)start;
        return true;
    }
}
