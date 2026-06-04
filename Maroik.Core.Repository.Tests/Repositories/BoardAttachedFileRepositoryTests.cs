using Maroik.Core.Contract.Domains.Board;
using Maroik.Core.PostgreSQL.Data;
using Maroik.Core.Repository.Repositories;
using Microsoft.EntityFrameworkCore;
using OrmBoardAttachedFile = Maroik.Core.PostgreSQL.Models.BoardAttachedFile;

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

    // -- Helpers --------------------------------------------------------------

    private static OrmBoardAttachedFile MakeFile(long boardId, string name = "file.pdf", long size = 1024) => new()
    {
        BoardId = boardId,
        Name = name,
        Extension = ".pdf",
        Path = $"/upload/boards/{name}",
        Size = size
    };

    private async Task SeedAsync(params OrmBoardAttachedFile[] files)
    {
        await _context.BoardAttachedFiles.AddRangeAsync(files);
        await _context.SaveChangesAsync();
    }

    // -- GetByBoardIdAsync ----------------------------------------------------

    /// <summary>Verifies that <c>GetByBoardIdAsync</c> returns file when found.</summary>
    [Fact]
    public async Task GetByBoardIdAsync_ReturnsFile_WhenFound()
    {
        await SeedAsync(MakeFile(10, "report.pdf"));

        BoardAttachedFile? result = await _sut.FindByBoardIdAsync(10L, TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal(10, result.BoardId);
        Assert.Equal("report.pdf", result.Name);
    }

    /// <summary>Verifies that <c>GetByBoardIdAsync</c> returns null when not found.</summary>
    [Fact]
    public async Task GetByBoardIdAsync_ReturnsNull_WhenNotFound()
    {
        BoardAttachedFile? result = await _sut.FindByBoardIdAsync(999L, TestContext.Current.CancellationToken);

        Assert.Null(result);
    }

    // -- GetByBoardIdsAsync ---------------------------------------------------

    /// <summary>Verifies that <c>GetByBoardIdsAsync</c> returns mapped dictionary.</summary>
    [Fact]
    public async Task GetByBoardIdsAsync_ReturnsMappedDictionary()
    {
        await SeedAsync(MakeFile(1, "a.pdf"), MakeFile(3, "c.pdf"));

        Dictionary<long, BoardAttachedFile?> result = await _sut.GetByBoardIdsAsync([1L, 2L, 3L], TestContext.Current.CancellationToken);

        Assert.Equal(3, result.Count);
        Assert.NotNull(result[1]);
        Assert.Null(result[2]); // no file for board 2
        Assert.NotNull(result[3]);
    }

    /// <summary>Verifies that <c>GetByBoardIdsAsync</c> returns null values when none match.</summary>
    [Fact]
    public async Task GetByBoardIdsAsync_ReturnsNullValues_WhenNoneMatch()
    {
        Dictionary<long, BoardAttachedFile?> result = await _sut.GetByBoardIdsAsync([100L, 200L], TestContext.Current.CancellationToken);

        Assert.All(result.Values, v => Assert.Null(v));
    }

    // -- SaveBoardAttachedFileAsync -------------------------------------------

    /// <summary>Verifies that <c>SaveBoardAttachedFileAsync</c> persists file.</summary>
    [Fact]
    public async Task SaveBoardAttachedFileAsync_PersistsFile()
    {
        var file = BoardAttachedFile.Reconstitute(
            id: 0,
            boardId: 5,
            size: 2048,
            name: "new.pdf",
            extension: ".pdf",
            path: "/upload/boards/new.pdf");

        await _sut.CreateAsync(file, TestContext.Current.CancellationToken);

        OrmBoardAttachedFile? saved = await _context.BoardAttachedFiles.FirstOrDefaultAsync(f => f.BoardId == 5, TestContext.Current.CancellationToken);
        Assert.NotNull(saved);
        Assert.Equal("new.pdf", saved.Name);
        Assert.Equal(2048, saved.Size);
    }

    // -- UpdateEntityAsync -----------------------------------------

    /// <summary>Verifies that <c>UpdateEntityAsync</c> updates existing file.</summary>
    [Fact]
    public async Task UpdateEntityAsync_UpdatesExistingFile()
    {
        var seedFile = MakeFile(7, "old.pdf", 512);
        await SeedAsync(seedFile);

        long id = (await _context.BoardAttachedFiles.FirstAsync(f => f.BoardId == 7, TestContext.Current.CancellationToken)).Id;

        var file = BoardAttachedFile.Reconstitute(
            id: id,
            boardId: 7,
            size: 9999,
            name: "updated.pdf",
            extension: ".pdf",
            path: "/upload/boards/updated.pdf");

        await _sut.UpdateEntityAsync(file, TestContext.Current.CancellationToken);

        OrmBoardAttachedFile? updated = await _context.BoardAttachedFiles.FindAsync(new object[] { id }, TestContext.Current.CancellationToken);
        Assert.NotNull(updated);
        Assert.Equal("updated.pdf", updated.Name);
        Assert.Equal(9999, updated.Size);
    }
}
