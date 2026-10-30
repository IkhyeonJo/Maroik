using System.Buffers.Binary;
using System.Text;

namespace Maroik.FileStorage.Tests.Fixtures;

/// <summary>
/// Writes ZIP archives byte by byte, so a test can set what no archiving library lets it choose: the
/// encryption flag (general purpose bit 0) of an entry, in the central directory and / or the local
/// header, and the Zip64 end-of-central-directory records. Every entry is stored (method 0) and holds
/// its own name as its data; an encrypted entry's bytes are not really encrypted, since only the flag
/// is read by the code under test.
/// </summary>
internal static class ZipBytes
{
    /// <summary>
    /// An archive holding <paramref name="entries"/> in order. <paramref name="flagLocalHeaders"/> = false
    /// sets an encrypted entry's flag in the central directory only; <paramref name="zip64"/> writes the
    /// Zip64 end records and leaves the sentinels (0xFFFF / 0xFFFFFFFF) in the classic one;
    /// <paramref name="annotateEntries"/> gives every central directory header a 4-byte extra field and a comment.
    /// </summary>
    public static byte[] Build(
        IReadOnlyList<(string Name, bool Encrypted)> entries,
        string comment = "",
        bool flagLocalHeaders = true,
        bool zip64 = false,
        bool annotateEntries = false)
    {
        using var archive = new MemoryStream();
        var offsets = new List<int>();
        foreach (var (name, encrypted) in entries)
        {
            offsets.Add((int)archive.Position);
            byte[] nameBytes = Encoding.UTF8.GetBytes(name);
            WriteUInt32(archive, 0x04034B50);
            WriteUInt16(archive, 20);
            WriteUInt16(archive, (ushort)(encrypted && flagLocalHeaders ? 1 : 0));
            WriteUInt16(archive, 0);
            WriteUInt32(archive, 0);
            WriteUInt32(archive, 0);
            WriteUInt32(archive, (uint)nameBytes.Length);
            WriteUInt32(archive, (uint)nameBytes.Length);
            WriteUInt16(archive, (ushort)nameBytes.Length);
            WriteUInt16(archive, 0);
            archive.Write(nameBytes);
            archive.Write(nameBytes);
        }

        int centralDirectoryOffset = (int)archive.Position;
        for (int i = 0; i < entries.Count; i++)
        {
            var (name, encrypted) = entries[i];
            byte[] nameBytes = Encoding.UTF8.GetBytes(name);
            // An extra field of id 0xCAFE with no data (ignored by readers), and a comment.
            byte[] extraBytes = annotateEntries ? [0xFE, 0xCA, 0x00, 0x00] : [];
            byte[] entryCommentBytes = annotateEntries ? Encoding.UTF8.GetBytes("about " + name) : [];
            WriteUInt32(archive, 0x02014B50);
            WriteUInt16(archive, 20);
            WriteUInt16(archive, 20);
            WriteUInt16(archive, (ushort)(encrypted ? 1 : 0));
            WriteUInt16(archive, 0);
            WriteUInt32(archive, 0);
            WriteUInt32(archive, 0);
            WriteUInt32(archive, (uint)nameBytes.Length);
            WriteUInt32(archive, (uint)nameBytes.Length);
            WriteUInt16(archive, (ushort)nameBytes.Length);
            WriteUInt16(archive, (ushort)extraBytes.Length);
            WriteUInt16(archive, (ushort)entryCommentBytes.Length);
            WriteUInt16(archive, 0);
            WriteUInt16(archive, 0);
            WriteUInt32(archive, 0);
            WriteUInt32(archive, (uint)offsets[i]);
            archive.Write(nameBytes);
            archive.Write(extraBytes);
            archive.Write(entryCommentBytes);
        }

        int centralDirectorySize = (int)archive.Position - centralDirectoryOffset;
        if (zip64)
        {
            int zip64EndOffset = (int)archive.Position;
            WriteUInt32(archive, 0x06064B50);
            WriteUInt64(archive, 44);
            WriteUInt16(archive, 45);
            WriteUInt16(archive, 45);
            WriteUInt32(archive, 0);
            WriteUInt32(archive, 0);
            WriteUInt64(archive, (ulong)entries.Count);
            WriteUInt64(archive, (ulong)entries.Count);
            WriteUInt64(archive, (ulong)centralDirectorySize);
            WriteUInt64(archive, (ulong)centralDirectoryOffset);

            WriteUInt32(archive, 0x07064B50);
            WriteUInt32(archive, 0);
            WriteUInt64(archive, (ulong)zip64EndOffset);
            WriteUInt32(archive, 1);
        }

        byte[] commentBytes = Encoding.UTF8.GetBytes(comment);
        WriteUInt32(archive, 0x06054B50);
        WriteUInt16(archive, 0);
        WriteUInt16(archive, 0);
        WriteUInt16(archive, zip64 ? ushort.MaxValue : (ushort)entries.Count);
        WriteUInt16(archive, zip64 ? ushort.MaxValue : (ushort)entries.Count);
        WriteUInt32(archive, zip64 ? uint.MaxValue : (uint)centralDirectorySize);
        WriteUInt32(archive, zip64 ? uint.MaxValue : (uint)centralDirectoryOffset);
        WriteUInt16(archive, (ushort)commentBytes.Length);
        archive.Write(commentBytes);
        return archive.ToArray();
    }

    /// <summary>Writes <paramref name="value"/> little-endian.</summary>
    private static void WriteUInt16(Stream stream, ushort value)
    {
        Span<byte> bytes = stackalloc byte[2];
        BinaryPrimitives.WriteUInt16LittleEndian(bytes, value);
        stream.Write(bytes);
    }

    /// <summary>Writes <paramref name="value"/> little-endian.</summary>
    private static void WriteUInt32(Stream stream, uint value)
    {
        Span<byte> bytes = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(bytes, value);
        stream.Write(bytes);
    }

    /// <summary>Writes <paramref name="value"/> little-endian.</summary>
    private static void WriteUInt64(Stream stream, ulong value)
    {
        Span<byte> bytes = stackalloc byte[8];
        BinaryPrimitives.WriteUInt64LittleEndian(bytes, value);
        stream.Write(bytes);
    }
}
