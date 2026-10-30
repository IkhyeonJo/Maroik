using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;
using Maroik.FileStorage.Helpers;
using Maroik.FileStorage.Tests.Fixtures;

namespace Maroik.FileStorage.Tests.Helpers;

/// <summary>
/// Unit tests for <see cref="ZipCentralDirectory.TryFindEncryptedEntry"/>: it walks the central directory and reports whether
/// any entry has general purpose bit 0 (encrypted) set, and refuses to answer for an archive whose end records or central
/// directory it cannot follow. Plain archives come from <see cref="ZipArchive"/> (what real tools write); the encrypted ones and
/// the Zip64 ones from <see cref="ZipBytes"/>, since the BCL writes neither.
/// </summary>
public class ZipCentralDirectoryTests
{
    /// <summary>An archive written by <see cref="ZipArchive"/> with the given entries (and comment).</summary>
    private static byte[] BclZip(string comment = "", params string[] names)
    {
        using var stream = new MemoryStream();
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            zip.Comment = comment;
            foreach (string name in names)
            {
                using Stream entry = zip.CreateEntry(name).Open();
                entry.Write(Encoding.UTF8.GetBytes("content of " + name));
            }
        }
        return stream.ToArray();
    }

    /// <summary>Reads <paramref name="archive"/>, asserting it was readable, and returns whether an entry is encrypted.</summary>
    private static bool Encrypted(byte[] archive)
    {
        Assert.True(ZipCentralDirectory.TryFindEncryptedEntry(archive, out bool encrypted));
        return encrypted;
    }

    /// <summary>Asserts that <paramref name="archive"/> is reported unreadable.</summary>
    private static void AssertUnreadable(byte[] archive)
    {
        Assert.False(ZipCentralDirectory.TryFindEncryptedEntry(archive, out bool encrypted));
        Assert.False(encrypted);
    }

    /// <summary>The length of a Zip64 locator followed by a classic end record without a comment.</summary>
    private const int Zip64LocatorAndEndRecord = 20 + 22;

    /// <summary>The offset of the classic end-of-central-directory record of an archive without a comment.</summary>
    private static int EndRecord(byte[] archive) => archive.Length - 22;

    // -- Readable archives ---------------------------------------------------------

    /// <summary>A plain archive written by a real tool has no encrypted entry.</summary>
    [Fact]
    public void ReportsNoEncryption_ForAPlainArchive()
    {
        Assert.False(Encrypted(BclZip("", "a.txt", "dir/b.txt", "c.txt")));
    }

    /// <summary>
    /// Each header is stepped over by its name, extra field and comment lengths, so headers that carry extra fields and comments
    /// still lead to the next one, and the encrypted last entry is found.
    /// </summary>
    [Fact]
    public void StepsOverEntryExtraFieldsAndComments()
    {
        Assert.True(Encrypted(ZipBytes.Build([("a.txt", false), ("b.txt", false), ("c.txt", true)], annotateEntries: true)));
    }

    /// <summary>An archive without entries is readable and holds nothing encrypted.</summary>
    [Fact]
    public void ReportsNoEncryption_ForAnEmptyArchive()
    {
        Assert.False(Encrypted(BclZip()));
    }

    /// <summary>
    /// Only bit 0 means encrypted: an entry with a UTF-8 name carries bit 11, and an archive written to a stream that cannot
    /// seek carries bit 3 (sizes in a data descriptor); neither is encryption.
    /// </summary>
    [Fact]
    public void IgnoresTheOtherFlagBits()
    {
        using var unseekable = new MemoryStream();
        using (var forwardOnly = new ForwardOnlyStream(unseekable))
        using (var zip = new ZipArchive(forwardOnly, ZipArchiveMode.Create))
        {
            using Stream entry = zip.CreateEntry("한글.txt").Open();
            entry.Write("내용"u8);
        }
        byte[] archive = unseekable.ToArray();
        int centralDirectory = BinaryPrimitives.ReadInt32LittleEndian(archive.AsSpan(EndRecord(archive) + 16));
        ushort flags = BinaryPrimitives.ReadUInt16LittleEndian(archive.AsSpan(centralDirectory + 8));
        Assert.Equal(0x0808, flags & 0x0808);

        Assert.False(Encrypted(archive));
    }

    /// <summary>A single encrypted entry is reported.</summary>
    [Fact]
    public void ReportsEncryption_ForAnEncryptedEntry()
    {
        Assert.True(Encrypted(ZipBytes.Build([("secret.txt", true)])));
    }

    /// <summary>One encrypted entry is enough, wherever it sits among plain ones.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void ReportsEncryption_WhenAnyOneEntryIsEncrypted(int encryptedIndex)
    {
        var entries = Enumerable.Range(0, 3).Select(i => ($"f{i}.txt", i == encryptedIndex)).ToArray();

        Assert.True(Encrypted(ZipBytes.Build(entries)));
    }

    /// <summary>The central directory decides: an entry flagged there is encrypted even when its local header says otherwise.</summary>
    [Fact]
    public void ReadsTheFlagFromTheCentralDirectory()
    {
        Assert.True(Encrypted(ZipBytes.Build([("plain.txt", false), ("secret.txt", true)], flagLocalHeaders: false)));
    }

    /// <summary>The end record is found behind an archive comment.</summary>
    [Fact]
    public void FindsTheEndRecord_BehindAComment()
    {
        Assert.False(Encrypted(BclZip("a comment", "a.txt")));
        Assert.True(Encrypted(ZipBytes.Build([("secret.txt", true)], comment: "a comment")));
    }

    /// <summary>
    /// A comment that itself holds the end record's signature does not mislead the search: a candidate counts only when its
    /// comment length reaches exactly to the end of the archive.
    /// </summary>
    [Fact]
    public void IsNotMisled_ByTheEndSignatureInsideTheComment()
    {
        string comment = "PK\u0005\u0006" + new string('x', 30);

        Assert.True(Encrypted(ZipBytes.Build([("secret.txt", true)], comment: comment)));
    }

    /// <summary>A Zip64 archive's entry count, size and offset are read from its Zip64 end record.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ReadsAZip64Archive(bool encrypted)
    {
        Assert.Equal(encrypted, Encrypted(ZipBytes.Build([("a.txt", false), ("b.txt", encrypted)], zip64: true)));
    }

    /// <summary>
    /// Any one sentinel in the classic end record sends the reader to the Zip64 one: here the other two fields are zeroed (an
    /// empty archive whose directory starts at the first local header), and only the Zip64 values find the encrypted entry.
    /// </summary>
    [Theory]
    [InlineData(10)]
    [InlineData(12)]
    [InlineData(16)]
    public void TakesTheZip64Values_WhenAnyOneClassicFieldIsASentinel(int sentinelField)
    {
        byte[] archive = ZipBytes.Build([("a.txt", false), ("b.txt", true)], zip64: true);
        int end = EndRecord(archive);
        BinaryPrimitives.WriteUInt16LittleEndian(archive.AsSpan(end + 8), 0);
        BinaryPrimitives.WriteUInt16LittleEndian(archive.AsSpan(end + 10), 0);
        BinaryPrimitives.WriteUInt32LittleEndian(archive.AsSpan(end + 12), 0);
        BinaryPrimitives.WriteUInt32LittleEndian(archive.AsSpan(end + 16), 0);
        if (sentinelField == 10)
            BinaryPrimitives.WriteUInt16LittleEndian(archive.AsSpan(end + 10), ushort.MaxValue);
        else
            BinaryPrimitives.WriteUInt32LittleEndian(archive.AsSpan(end + sentinelField), uint.MaxValue);

        Assert.True(Encrypted(archive));
    }

    // -- Unreadable archives -------------------------------------------------------

    /// <summary>
    /// A locator and an end record alone, too short to hold a Zip64 end record: the locator points into the end record, whose
    /// disk-number fields spell the Zip64 signature, and the reader must not run past the buffer to read the rest.
    /// </summary>
    [Fact]
    public void IsUnreadable_WhenTooShortForAZip64EndRecord()
    {
        byte[] archive = new byte[Zip64LocatorAndEndRecord];
        BinaryPrimitives.WriteUInt32LittleEndian(archive, 0x07064B50);
        BinaryPrimitives.WriteInt64LittleEndian(archive.AsSpan(8), 24);
        BinaryPrimitives.WriteUInt32LittleEndian(archive.AsSpan(20), 0x06054B50);
        BinaryPrimitives.WriteUInt32LittleEndian(archive.AsSpan(24), 0x06064B50);
        BinaryPrimitives.WriteUInt32LittleEndian(archive.AsSpan(20 + 16), uint.MaxValue);

        AssertUnreadable(archive);
    }

    /// <summary>An end record at the very start of the buffer, with a sentinel, has no room for a locator before it.</summary>
    [Fact]
    public void IsUnreadable_WhenASentinelEndRecordHasNoRoomForALocator()
    {
        byte[] archive = new byte[22];
        BinaryPrimitives.WriteUInt32LittleEndian(archive, 0x06054B50);
        BinaryPrimitives.WriteUInt32LittleEndian(archive.AsSpan(16), uint.MaxValue);

        AssertUnreadable(archive);
    }

    /// <summary>Fewer bytes than an end record cannot be an archive.</summary>
    [Fact]
    public void IsUnreadable_WhenShorterThanAnEndRecord()
    {
        AssertUnreadable([0x50, 0x4B, 0x03, 0x04]);
    }

    /// <summary>An archive cut short loses its end record.</summary>
    [Fact]
    public void IsUnreadable_WithoutAnEndRecord()
    {
        byte[] archive = ZipBytes.Build([("a.txt", false)]);

        AssertUnreadable(archive[..^1]);
    }

    /// <summary>A central directory that would start past the end of the archive.</summary>
    [Fact]
    public void IsUnreadable_WhenTheCentralDirectoryOffsetIsOutOfRange()
    {
        byte[] archive = ZipBytes.Build([("a.txt", false)]);
        BinaryPrimitives.WriteUInt32LittleEndian(archive.AsSpan(EndRecord(archive) + 16), (uint)archive.Length);

        AssertUnreadable(archive);
    }

    /// <summary>An entry count larger than the central directory holds runs into the end record.</summary>
    [Fact]
    public void IsUnreadable_WhenTheEntryCountExceedsTheCentralDirectory()
    {
        byte[] archive = ZipBytes.Build([("a.txt", false)]);
        BinaryPrimitives.WriteUInt16LittleEndian(archive.AsSpan(EndRecord(archive) + 10), 2);

        AssertUnreadable(archive);
    }

    /// <summary>A central directory offset that does not land on a central directory header.</summary>
    [Fact]
    public void IsUnreadable_WhenTheOffsetMissesTheCentralDirectory()
    {
        byte[] archive = ZipBytes.Build([("a.txt", false)]);
        int offset = BinaryPrimitives.ReadInt32LittleEndian(archive.AsSpan(EndRecord(archive) + 16));
        BinaryPrimitives.WriteUInt32LittleEndian(archive.AsSpan(EndRecord(archive) + 16), (uint)(offset - 2));

        AssertUnreadable(archive);
    }

    /// <summary>A central directory header whose name runs past the end of the archive.</summary>
    [Fact]
    public void IsUnreadable_WhenAHeaderRunsPastTheEnd()
    {
        byte[] archive = ZipBytes.Build([("a.txt", false)]);
        int offset = BinaryPrimitives.ReadInt32LittleEndian(archive.AsSpan(EndRecord(archive) + 16));
        BinaryPrimitives.WriteUInt16LittleEndian(archive.AsSpan(offset + 28), ushort.MaxValue);

        AssertUnreadable(archive);
    }

    /// <summary>A Zip64 sentinel in the classic end record with no Zip64 locator before it.</summary>
    [Fact]
    public void IsUnreadable_WhenAZip64SentinelHasNoLocator()
    {
        byte[] archive = ZipBytes.Build([("a.txt", false)]);
        BinaryPrimitives.WriteUInt32LittleEndian(archive.AsSpan(EndRecord(archive) + 16), uint.MaxValue);

        AssertUnreadable(archive);
    }

    /// <summary>A Zip64 locator that does not point at a Zip64 end record.</summary>
    [Fact]
    public void IsUnreadable_WhenTheZip64LocatorMissesItsRecord()
    {
        byte[] archive = ZipBytes.Build([("a.txt", false)], zip64: true);
        int locator = EndRecord(archive) - 20;
        long record = BinaryPrimitives.ReadInt64LittleEndian(archive.AsSpan(locator + 8));
        BinaryPrimitives.WriteInt64LittleEndian(archive.AsSpan(locator + 8), record + 1);

        AssertUnreadable(archive);
    }

    /// <summary>A Zip64 locator that points past the end of the archive.</summary>
    [Fact]
    public void IsUnreadable_WhenTheZip64LocatorIsOutOfRange()
    {
        byte[] archive = ZipBytes.Build([("a.txt", false)], zip64: true);
        BinaryPrimitives.WriteInt64LittleEndian(archive.AsSpan(EndRecord(archive) - 20 + 8), long.MaxValue);

        AssertUnreadable(archive);
    }

    /// <summary>A Zip64 central directory offset beyond what any buffered upload can hold.</summary>
    [Fact]
    public void IsUnreadable_WhenTheZip64OffsetIsOutOfRange()
    {
        byte[] archive = ZipBytes.Build([("a.txt", false)], zip64: true);
        int record = (int)BinaryPrimitives.ReadInt64LittleEndian(archive.AsSpan(EndRecord(archive) - 20 + 8));
        BinaryPrimitives.WriteUInt64LittleEndian(archive.AsSpan(record + 48), ulong.MaxValue);

        AssertUnreadable(archive);
    }

    /// <summary>A Zip64 entry count beyond what the archive could ever hold.</summary>
    [Fact]
    public void IsUnreadable_WhenTheZip64EntryCountIsOutOfRange()
    {
        byte[] archive = ZipBytes.Build([("a.txt", false)], zip64: true);
        int record = (int)BinaryPrimitives.ReadInt64LittleEndian(archive.AsSpan(EndRecord(archive) - 20 + 8));
        BinaryPrimitives.WriteUInt64LittleEndian(archive.AsSpan(record + 32), ulong.MaxValue);

        AssertUnreadable(archive);
    }
}
