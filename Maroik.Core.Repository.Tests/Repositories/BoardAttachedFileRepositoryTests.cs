using Maroik.Core.Contract.Dtos;
using Maroik.Core.PostgreSQL.Data;
using Maroik.Core.PostgreSQL.Models;
using Maroik.Core.Repository.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Maroik.Core.Repository.Tests.Repositories;

/// <summary>
/// Tests for <see cref="BoardAttachedFileRepository"/> using an EF Core InMemory database.
/// FK constraints are not enforced by InMemory, so Board parent records are omitted.
/// </summary>
public class BoardAttachedFileRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly BoardAttachedFileRepository _sut;

    /// <summary>Initializes the test fixture, setting up all required test doubles and the system under test.</summary>
    public BoardAttachedFileRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _sut = new BoardAttachedFileRepository(_context);
    }

    /// <summary>Releases resources allocated by this test fixture (disposes the database context).</summary>
    public void Dispose() => _context.Dispose();

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static BoardAttachedFile MakeFile(long boardId, string name = "file.pdf", long size = 1024) => new()
    {
        BoardId = boardId,
        Name = name,
        Extension = ".pdf",
        Path = $"/upload/boards/{name}",
        Size = size
    };

    private async Task SeedAsync(params BoardAttachedFile[] files)
    {
        await _context.BoardAttachedFiles.AddRangeAsync(files);
        await _context.SaveChangesAsync();
    }

    // ── GetAllAsync ──────────────────────────────────────────────────────────

    /// <summary>Verifies that <c>GetAllAsync</c> returns all files.</summary>
    [Fact]
    public async Task GetAllAsync_ReturnsAllFiles()
    {
        await SeedAsync(MakeFile(1, "a.pdf"), MakeFile(2, "b.pdf"));

        List<BoardAttachedFileResponse> result = await _sut.GetAllAsync();

        Assert.Equal(2, result.Count);
    }

    /// <summary>Verifies that <c>GetAllAsync</c> returns empty when no files exist.</summary>
    [Fact]
    public async Task GetAllAsync_ReturnsEmpty_WhenNoFilesExist()
    {
        List<BoardAttachedFileResponse> result = await _sut.GetAllAsync();

        Assert.Empty(result);
    }

    // ── GetByBoardIdAsync ────────────────────────────────────────────────────

    /// <summary>Verifies that <c>GetByBoardIdAsync</c> returns file when found.</summary>
    [Fact]
    public async Task GetByBoardIdAsync_ReturnsFile_WhenFound()
    {
        await SeedAsync(MakeFile(10, "report.pdf"));

        BoardAttachedFileResponse? result = await _sut.GetByBoardIdAsync(10);

        Assert.NotNull(result);
        Assert.Equal(10, result.BoardId);
        Assert.Equal("report.pdf", result.Name);
    }

    /// <summary>Verifies that <c>GetByBoardIdAsync</c> returns null when not found.</summary>
    [Fact]
    public async Task GetByBoardIdAsync_ReturnsNull_WhenNotFound()
    {
        BoardAttachedFileResponse? result = await _sut.GetByBoardIdAsync(999);

        Assert.Null(result);
    }

    // ── GetByBoardIdsAsync ───────────────────────────────────────────────────

    /// <summary>Verifies that <c>GetByBoardIdsAsync</c> returns mapped dictionary.</summary>
    [Fact]
    public async Task GetByBoardIdsAsync_ReturnsMappedDictionary()
    {
        await SeedAsync(MakeFile(1, "a.pdf"), MakeFile(3, "c.pdf"));

        Dictionary<long, BoardAttachedFileResponse?> result = await _sut.GetByBoardIdsAsync([1L, 2L, 3L]);

        Assert.Equal(3, result.Count);
        Assert.NotNull(result[1]);
        Assert.Null(result[2]); // no file for board 2
        Assert.NotNull(result[3]);
    }

    /// <summary>Verifies that <c>GetByBoardIdsAsync</c> returns null values when none match.</summary>
    [Fact]
    public async Task GetByBoardIdsAsync_ReturnsNullValues_WhenNoneMatch()
    {
        Dictionary<long, BoardAttachedFileResponse?> result = await _sut.GetByBoardIdsAsync([100L, 200L]);

        Assert.All(result.Values, v => Assert.Null(v));
    }

    // ── SaveBoardAttachedFileAsync ───────────────────────────────────────────

    /// <summary>Verifies that <c>SaveBoardAttachedFileAsync</c> persists file.</summary>
    [Fact]
    public async Task SaveBoardAttachedFileAsync_PersistsFile()
    {
        var request = new BoardAttachedFileRequest
        {
            BoardId = 5,
            Name = "new.pdf",
            Extension = ".pdf",
            Path = "/upload/boards/new.pdf",
            Size = 2048
        };

        await _sut.SaveBoardAttachedFileAsync(request);

        BoardAttachedFile? saved = await _context.BoardAttachedFiles.FirstOrDefaultAsync(f => f.BoardId == 5, TestContext.Current.CancellationToken);
        Assert.NotNull(saved);
        Assert.Equal("new.pdf", saved.Name);
        Assert.Equal(2048, saved.Size);
    }

    // ── UpdateBoardAttachedFileAsync ─────────────────────────────────────────

    /// <summary>Verifies that <c>UpdateBoardAttachedFileAsync</c> updates existing file.</summary>
    [Fact]
    public async Task UpdateBoardAttachedFileAsync_UpdatesExistingFile()
    {
        var file = MakeFile(7, "old.pdf", 512);
        await SeedAsync(file);

        long id = (await _context.BoardAttachedFiles.FirstAsync(f => f.BoardId == 7, TestContext.Current.CancellationToken)).Id;

        var request = new BoardAttachedFileRequest
        {
            Id = id,
            BoardId = 7,
            Name = "updated.pdf",
            Extension = ".pdf",
            Path = "/upload/boards/updated.pdf",
            Size = 9999
        };

        await _sut.UpdateBoardAttachedFileAsync(request);

        BoardAttachedFile? updated = await _context.BoardAttachedFiles.FindAsync(new object[] { id }, TestContext.Current.CancellationToken);
        Assert.NotNull(updated);
        Assert.Equal("updated.pdf", updated.Name);
        Assert.Equal(9999, updated.Size);
    }
}
