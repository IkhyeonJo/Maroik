using Maroik.Core.Contract.Dtos;
using Maroik.Core.PostgreSQL.Data;
using Maroik.Core.PostgreSQL.Models;
using Maroik.Core.Repository.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Maroik.Core.Repository.Tests.Repositories;

/// <summary>
/// Tests for <see cref="BoardCommentRepository"/> using an EF Core InMemory database.
/// </summary>
public class BoardCommentRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly BoardCommentRepository _sut;

    public BoardCommentRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _sut = new BoardCommentRepository(_context);
    }

    public void Dispose() => _context.Dispose();

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static BoardComment MakeComment(long boardId, long order, string writer = "Alice", bool deleted = false) => new()
    {
        BoardId = boardId,
        Order = order,
        AvatarImagePath = "/upload/default.jpg",
        Writer = writer,
        Content = "Comment content",
        Created = DateTime.UtcNow,
        Deleted = deleted
    };

    private async Task<long> SeedBoardAsync()
    {
        var board = new Board
        {
            Type = "FreeForum",
            Title = "Parent Board",
            Content = "Content",
            Writer = "Alice",
            Created = DateTime.UtcNow,
            Updated = DateTime.UtcNow
        };
        await _context.Boards.AddAsync(board);
        await _context.SaveChangesAsync();
        return board.Id;
    }

    private async Task SeedAsync(params BoardComment[] comments)
    {
        await _context.BoardComments.AddRangeAsync(comments);
        await _context.SaveChangesAsync();
    }

    // ── GetOneBoardCommentsAsync ─────────────────────────────────────────────

    [Fact]
    public async Task GetOneBoardCommentsAsync_ReturnsOnlyCommentsForGivenBoard()
    {
        long board1Id = await SeedBoardAsync();
        long board2Id = await SeedBoardAsync();
        await SeedAsync(
            MakeComment(board1Id, 1),
            MakeComment(board1Id, 2),
            MakeComment(board2Id, 1));

        IEnumerable<BoardCommentResponse> result = await _sut.GetOneBoardCommentsAsync(board1Id);

        Assert.Equal(2, result.Count());
        Assert.All(result, c => Assert.Equal(board1Id, c.BoardId));
    }

    [Fact]
    public async Task GetOneBoardCommentsAsync_ReturnsOrderedByOrder()
    {
        long boardId = await SeedBoardAsync();
        await SeedAsync(
            MakeComment(boardId, 3),
            MakeComment(boardId, 1),
            MakeComment(boardId, 2));

        IEnumerable<BoardCommentResponse> result = await _sut.GetOneBoardCommentsAsync(boardId);
        List<BoardCommentResponse> list = result.ToList();

        Assert.Equal(1, list[0].Order);
        Assert.Equal(2, list[1].Order);
        Assert.Equal(3, list[2].Order);
    }

    [Fact]
    public async Task GetOneBoardCommentsAsync_ReturnsEmpty_WhenNoBoardComments()
    {
        long boardId = await SeedBoardAsync();

        IEnumerable<BoardCommentResponse> result = await _sut.GetOneBoardCommentsAsync(boardId);

        Assert.Empty(result);
    }

    // ── GetBoardCommentAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task GetBoardCommentAsync_ReturnsComment_WhenFound()
    {
        long boardId = await SeedBoardAsync();
        await SeedAsync(MakeComment(boardId, 1, writer: "Bob"));
        BoardComment comment = await _context.BoardComments.FirstAsync(TestContext.Current.CancellationToken);

        BoardCommentResponse? result = await _sut.GetBoardCommentAsync((int)comment.Id);

        Assert.NotNull(result);
        Assert.Equal("Bob", result.Writer);
    }

    [Fact]
    public async Task GetBoardCommentAsync_ReturnsNull_WhenNotFound()
    {
        BoardCommentResponse? result = await _sut.GetBoardCommentAsync(99999);

        Assert.Null(result);
    }

    // ── GetCommentCountsByBoardIdsAsync ──────────────────────────────────────

    [Fact]
    public async Task GetCommentCountsByBoardIdsAsync_ReturnsCorrectCounts()
    {
        long boardId1 = await SeedBoardAsync();
        long boardId2 = await SeedBoardAsync();
        await SeedAsync(
            MakeComment(boardId1, 1, deleted: false),
            MakeComment(boardId1, 2, deleted: false),
            MakeComment(boardId1, 3, deleted: true),  // deleted, not counted
            MakeComment(boardId2, 1, deleted: false));

        Dictionary<long, int> counts = await _sut.GetCommentCountsByBoardIdsAsync([boardId1, boardId2]);

        Assert.Equal(2, counts[boardId1]);  // 2 non-deleted
        Assert.Equal(1, counts[boardId2]);
    }

    [Fact]
    public async Task GetCommentCountsByBoardIdsAsync_ReturnsZero_ForBoardsWithNoComments()
    {
        long boardId = await SeedBoardAsync();

        Dictionary<long, int> counts = await _sut.GetCommentCountsByBoardIdsAsync([boardId]);

        Assert.Equal(0, counts[boardId]);
    }

    // ── WriteBoardCommentAsync ───────────────────────────────────────────────

    [Fact]
    public async Task WriteBoardCommentAsync_AddsCommentToDatabase()
    {
        long boardId = await SeedBoardAsync();

        var request = new BoardCommentRequest
        {
            BoardId = boardId,
            Order = 1,
            AvatarImagePath = "/upload/alice.jpg",
            Writer = "Alice",
            Content = "Great post!",
            Created = DateTime.UtcNow,
            Deleted = false
        };

        await _sut.WriteBoardCommentAsync(request);

        BoardComment? saved = await _context.BoardComments.FirstOrDefaultAsync(
            c => c.BoardId == boardId && c.Content == "Great post!", TestContext.Current.CancellationToken);
        Assert.NotNull(saved);
        Assert.Equal("Alice", saved.Writer);
    }

    // ── DeleteBoardCommentAsync ──────────────────────────────────────────────

    [Fact]
    public async Task DeleteBoardCommentAsync_SoftDeletesComment()
    {
        long boardId = await SeedBoardAsync();
        await SeedAsync(MakeComment(boardId, 1));
        BoardComment comment = await _context.BoardComments.FirstAsync(TestContext.Current.CancellationToken);

        var request = new BoardCommentRequest
        {
            Id = comment.Id,
            BoardId = comment.BoardId,
            Order = comment.Order,
            AvatarImagePath = comment.AvatarImagePath,
            Writer = comment.Writer,
            Content = comment.Content,
            Created = comment.Created,
            Deleted = true   // soft-delete
        };

        await _sut.DeleteBoardCommentAsync(request);

        BoardComment? result = await _context.BoardComments.FirstOrDefaultAsync(c => c.Id == comment.Id, TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.True(result.Deleted);
    }
}
