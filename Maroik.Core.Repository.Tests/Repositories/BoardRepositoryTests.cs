using Maroik.Core.Domain.Board;
using Maroik.Core.Contract.Enums;
using Maroik.Core.PostgreSQL.Data;
using Maroik.Core.Repository.Repositories;
using Microsoft.EntityFrameworkCore;
using EfBoard = Maroik.Core.PostgreSQL.Models.Board;

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

    // -- Helpers --------------------------------------------------------------

    private static EfBoard MakeBoard(string type, string title, string writer = "Alice", bool deleted = false) => new()
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

    private async Task SeedAsync(params EfBoard[] boards)
    {
        await _context.Boards.AddRangeAsync(boards);
        await _context.SaveChangesAsync();
    }

    // -- GetOneTypeBoardsAsync ------------------------------------------------

    /// <summary>Verifies that <c>GetOneTypeBoardsAsync</c> returns only boards of given type.</summary>
    [Fact]
    public async Task GetOneTypeBoardsAsync_ReturnsOnlyBoardsOfGivenType()
    {
        await SeedAsync(
            MakeBoard(nameof(BoardType.FreeForum), "Hello"),
            MakeBoard(nameof(BoardType.FreeForum), "World"),
            MakeBoard(nameof(BoardType.PrivateNote), "Note1"));

        IEnumerable<Board> result = await _sut.GetByTypeAsync(nameof(BoardType.FreeForum), TestContext.Current.CancellationToken);

        Assert.Equal(2, result.Count());
        Assert.All(result, b => Assert.Equal(nameof(BoardType.FreeForum), b.Type));
    }

    /// <summary>Verifies that <c>GetOneTypeBoardsAsync</c> returns empty when no matching type.</summary>
    [Fact]
    public async Task GetOneTypeBoardsAsync_ReturnsEmpty_WhenNoMatchingType()
    {
        IEnumerable<Board> result = await _sut.GetByTypeAsync("NonExistentType", TestContext.Current.CancellationToken);

        Assert.Empty(result);
    }

    // -- GetBoardByIdAsync ----------------------------------------------------

    /// <summary>Verifies that <c>GetBoardByIdAsync</c> returns board when found and not deleted.</summary>
    [Fact]
    public async Task GetBoardByIdAsync_ReturnsBoard_WhenFoundAndNotDeleted()
    {
        await SeedAsync(MakeBoard(nameof(BoardType.FreeForum), "My Post"));
        EfBoard efBoard = await _context.Boards.FirstAsync(TestContext.Current.CancellationToken);

        Board? result = await _sut.FindActiveByIdAsync(efBoard.Id, TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal("My Post", result.Title);
    }

    /// <summary>Verifies that <c>GetBoardByIdAsync</c> returns null when board is deleted.</summary>
    [Fact]
    public async Task GetBoardByIdAsync_ReturnsNull_WhenBoardIsDeleted()
    {
        await SeedAsync(MakeBoard(nameof(BoardType.FreeForum), "Deleted Post", deleted: true));
        EfBoard efBoard = await _context.Boards.FirstAsync(TestContext.Current.CancellationToken);

        Board? result = await _sut.FindActiveByIdAsync(efBoard.Id, TestContext.Current.CancellationToken);

        Assert.Null(result);
    }

    /// <summary>Verifies that <c>GetBoardByIdAsync</c> returns null when id does not exist.</summary>
    [Fact]
    public async Task GetBoardByIdAsync_ReturnsNull_WhenIdDoesNotExist()
    {
        Board? result = await _sut.FindActiveByIdAsync(99999L, TestContext.Current.CancellationToken);

        Assert.Null(result);
    }

    // -- WriteBoardAsync ------------------------------------------------------

    /// <summary>Verifies that <c>WriteBoardAsync</c> adds board to database.</summary>
    [Fact]
    public async Task WriteBoardAsync_AddsBoardToDatabase()
    {
        var board = Board.Reconstitute(
            id: 0,
            type: nameof(BoardType.FreeForum),
            title: "New Post",
            content: "Post content",
            writer: "Alice",
            created: DateTime.UtcNow,
            updated: DateTime.UtcNow,
            view: 0,
            deleted: false,
            locked: false,
            noticed: false);

        await _sut.WriteBoardAsync(board, TestContext.Current.CancellationToken);

        EfBoard? saved = await _context.Boards.FirstOrDefaultAsync(b => b.Title == "New Post", TestContext.Current.CancellationToken);
        Assert.NotNull(saved);
        Assert.Equal("Alice", saved.Writer);
    }

    // -- UpdateEntityAsync -----------------------------------------------------

    /// <summary>Verifies that <c>UpdateEntityAsync</c> updates existing board.</summary>
    [Fact]
    public async Task UpdateEntityAsync_UpdatesExistingBoard()
    {
        await SeedAsync(MakeBoard(nameof(BoardType.FreeForum), "Original Title"));
        EfBoard efBoard = await _context.Boards.FirstAsync(TestContext.Current.CancellationToken);

        var board = Board.Reconstitute(
            id: efBoard.Id,
            type: nameof(BoardType.FreeForum),
            title: "Updated Title",
            content: "Updated content",
            writer: "Alice",
            created: efBoard.Created,
            updated: DateTime.UtcNow,
            view: efBoard.View,
            deleted: false,
            locked: false,
            noticed: false);

        await _sut.UpdateEntityAsync(board, TestContext.Current.CancellationToken);

        EfBoard? updated = await _context.Boards.FirstOrDefaultAsync(b => b.Id == efBoard.Id, TestContext.Current.CancellationToken);
        Assert.NotNull(updated);
        Assert.Equal("Updated Title", updated.Title);
    }

    // -- UpdateEntityAsync -----------------------------------------------------

    /// <summary>Verifies that <c>UpdateEntityAsync</c> sets deleted flag when on board.</summary>
    [Fact]
    public async Task UpdateEntityAsync_SetsDeletedFlag_OnBoard()
    {
        // UpdateEntityAsync uses Update (soft delete) rather than Remove
        await SeedAsync(MakeBoard(nameof(BoardType.FreeForum), "Post to Delete"));
        EfBoard efBoard = await _context.Boards.FirstAsync(TestContext.Current.CancellationToken);

        var board = Board.Reconstitute(
            id: efBoard.Id,
            type: efBoard.Type ?? "",
            title: efBoard.Title ?? "",
            content: efBoard.Content,
            writer: efBoard.Writer ?? "",
            created: efBoard.Created,
            updated: DateTime.UtcNow,
            view: efBoard.View,
            deleted: true,   // soft-delete
            locked: efBoard.Locked,
            noticed: efBoard.Noticed);

        await _sut.UpdateEntityAsync(board, TestContext.Current.CancellationToken);

        EfBoard? result = await _context.Boards.FirstOrDefaultAsync(b => b.Id == efBoard.Id, TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.True(result.Deleted);
    }
}
