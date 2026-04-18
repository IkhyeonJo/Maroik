using Maroik.Core.Contract.Dtos;
using Maroik.Core.PostgreSQL.Data;
using Maroik.Core.PostgreSQL.Models;
using Maroik.Core.Repository.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Maroik.Core.Repository.Tests.Repositories;

/// <summary>
/// Tests for <see cref="BoardRepository"/> using an EF Core InMemory database.
/// IncrementViewAsync uses ExecuteUpdateAsync which is supported by the InMemory provider.
/// </summary>
public class BoardRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly BoardRepository _sut;

    /// <summary>Initializes the test fixture, setting up all required test doubles and the system under test.</summary>
    public BoardRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _sut = new BoardRepository(_context);
    }

    /// <summary>Releases resources allocated by this test fixture (disposes the database context).</summary>
    public void Dispose() => _context.Dispose();

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static Board MakeBoard(string type, string title, string writer = "Alice", bool deleted = false) => new()
    {
        Type = type,
        Title = title,
        Content = "Some content",
        Writer = writer,
        Created = DateTime.UtcNow,
        Updated = DateTime.UtcNow,
        View = 0,
        Deleted = deleted,
        Locked = false,
        Noticed = false
    };

    private async Task SeedAsync(params Board[] boards)
    {
        await _context.Boards.AddRangeAsync(boards);
        await _context.SaveChangesAsync();
    }

    // ── GetOneTypeBoardsAsync ────────────────────────────────────────────────

    /// <summary>Verifies that <c>GetOneTypeBoardsAsync</c> returns only boards of given type.</summary>
    [Fact]
    public async Task GetOneTypeBoardsAsync_ReturnsOnlyBoardsOfGivenType()
    {
        await SeedAsync(
            MakeBoard("FreeForum", "Hello"),
            MakeBoard("FreeForum", "World"),
            MakeBoard("PrivateNote", "Note1"));

        IEnumerable<BoardResponse> result = await _sut.GetOneTypeBoardsAsync("FreeForum");

        Assert.Equal(2, result.Count());
        Assert.All(result, b => Assert.Equal("FreeForum", b.Type));
    }

    /// <summary>Verifies that <c>GetOneTypeBoardsAsync</c> returns empty when no matching type.</summary>
    [Fact]
    public async Task GetOneTypeBoardsAsync_ReturnsEmpty_WhenNoMatchingType()
    {
        IEnumerable<BoardResponse> result = await _sut.GetOneTypeBoardsAsync("NonExistentType");

        Assert.Empty(result);
    }

    // ── GetBoardByIdAsync ────────────────────────────────────────────────────

    /// <summary>Verifies that <c>GetBoardByIdAsync</c> returns board when found and not deleted.</summary>
    [Fact]
    public async Task GetBoardByIdAsync_ReturnsBoard_WhenFoundAndNotDeleted()
    {
        await SeedAsync(MakeBoard("FreeForum", "My Post"));
        Board board = await _context.Boards.FirstAsync(TestContext.Current.CancellationToken);

        BoardResponse? result = await _sut.GetBoardByIdAsync(board.Id);

        Assert.NotNull(result);
        Assert.Equal("My Post", result.Title);
    }

    /// <summary>Verifies that <c>GetBoardByIdAsync</c> returns null when board is deleted.</summary>
    [Fact]
    public async Task GetBoardByIdAsync_ReturnsNull_WhenBoardIsDeleted()
    {
        await SeedAsync(MakeBoard("FreeForum", "Deleted Post", deleted: true));
        Board board = await _context.Boards.FirstAsync(TestContext.Current.CancellationToken);

        BoardResponse? result = await _sut.GetBoardByIdAsync(board.Id);

        Assert.Null(result);
    }

    /// <summary>Verifies that <c>GetBoardByIdAsync</c> returns null when id does not exist.</summary>
    [Fact]
    public async Task GetBoardByIdAsync_ReturnsNull_WhenIdDoesNotExist()
    {
        BoardResponse? result = await _sut.GetBoardByIdAsync(99999);

        Assert.Null(result);
    }

    // ── WriteBoardAsync ──────────────────────────────────────────────────────

    /// <summary>Verifies that <c>WriteBoardAsync</c> adds board to database.</summary>
    [Fact]
    public async Task WriteBoardAsync_AddsBoardToDatabase()
    {
        var request = new BoardRequest
        {
            Type = "FreeForum",
            Title = "New Post",
            Content = "Post content",
            Writer = "Alice",
            Created = DateTime.UtcNow,
            Updated = DateTime.UtcNow,
            Deleted = false,
            Locked = false,
            Noticed = false
        };

        await _sut.WriteBoardAsync(request);

        Board? saved = await _context.Boards.FirstOrDefaultAsync(b => b.Title == "New Post", TestContext.Current.CancellationToken);
        Assert.NotNull(saved);
        Assert.Equal("Alice", saved.Writer);
    }

    // ── UpdateBoardAsync ─────────────────────────────────────────────────────

    /// <summary>Verifies that <c>UpdateBoardAsync</c> updates existing board.</summary>
    [Fact]
    public async Task UpdateBoardAsync_UpdatesExistingBoard()
    {
        await SeedAsync(MakeBoard("FreeForum", "Original Title"));
        Board board = await _context.Boards.FirstAsync(TestContext.Current.CancellationToken);

        var request = new BoardRequest
        {
            Id = board.Id,
            Type = "FreeForum",
            Title = "Updated Title",
            Content = "Updated content",
            Writer = "Alice",
            Created = board.Created,
            Updated = DateTime.UtcNow,
            Deleted = false,
            Locked = false,
            Noticed = false
        };

        await _sut.UpdateBoardAsync(request);

        Board? updated = await _context.Boards.FirstOrDefaultAsync(b => b.Id == board.Id, TestContext.Current.CancellationToken);
        Assert.NotNull(updated);
        Assert.Equal("Updated Title", updated.Title);
    }

    // ── DeleteBoardAsync ─────────────────────────────────────────────────────

    /// <summary>Verifies that <c>DeleteBoardAsync</c> sets deleted flag when on board.</summary>
    [Fact]
    public async Task DeleteBoardAsync_SetsDeletedFlag_OnBoard()
    {
        // DeleteBoardAsync uses Update (soft delete) rather than Remove
        await SeedAsync(MakeBoard("FreeForum", "Post to Delete"));
        Board board = await _context.Boards.FirstAsync(TestContext.Current.CancellationToken);

        var request = new BoardRequest
        {
            Id = board.Id,
            Type = board.Type,
            Title = board.Title,
            Content = board.Content,
            Writer = board.Writer,
            Created = board.Created,
            Updated = DateTime.UtcNow,
            View = board.View,
            Deleted = true,   // soft-delete
            Locked = board.Locked,
            Noticed = board.Noticed
        };

        await _sut.DeleteBoardAsync(request);

        Board? result = await _context.Boards.FirstOrDefaultAsync(b => b.Id == board.Id, TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.True(result.Deleted);
    }
}
