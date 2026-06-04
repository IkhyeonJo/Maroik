using Maroik.Core.Contract.Domains.Board;
using Maroik.Core.Contract.Utilities;
using Maroik.Core.PostgreSQL.Data;
using Maroik.Core.Repository.Repositories;
using Microsoft.EntityFrameworkCore;
using EfBoard = Maroik.Core.PostgreSQL.Models.Board;
using EfBoardComment = Maroik.Core.PostgreSQL.Models.BoardComment;

namespace Maroik.Core.Repository.Tests.Repositories;

/// <summary>
/// Tests for <see cref="BoardCommentRepository"/> using an EF Core InMemory database.
/// </summary>
public class BoardCommentRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly BoardCommentRepository _sut;

    /// <summary>Initializes the test fixture, setting up all required test doubles and the system under test.</summary>
    public BoardCommentRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _sut = new BoardCommentRepository(_context);
    }

    /// <summary>Releases resources allocated by this test fixture (disposes the database context).</summary>
    public void Dispose() => _context.Dispose();

    // -- Helpers --------------------------------------------------------------

    private static EfBoardComment MakeComment(long boardId, long order, string writer = "Alice", bool deleted = false) => new()
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
        var board = new EfBoard
        {
            Type = nameof(BoardType.FreeForum),
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

    private async Task SeedAsync(params EfBoardComment[] comments)
    {
        await _context.BoardComments.AddRangeAsync(comments);
        await _context.SaveChangesAsync();
    }

    // -- GetOneBoardCommentsAsync ---------------------------------------------

    /// <summary>Verifies that <c>GetOneBoardCommentsAsync</c> returns only comments for given board.</summary>
    [Fact]
    public async Task GetOneBoardCommentsAsync_ReturnsOnlyCommentsForGivenBoard()
    {
        long board1Id = await SeedBoardAsync();
        long board2Id = await SeedBoardAsync();
        await SeedAsync(
            MakeComment(board1Id, 1),
            MakeComment(board1Id, 2),
            MakeComment(board2Id, 1));

        IEnumerable<BoardComment> result = await _sut.GetByBoardIdOrderedAsync(board1Id, TestContext.Current.CancellationToken);

        Assert.Equal(2, result.Count());
        Assert.All(result, c => Assert.Equal(board1Id, c.BoardId));
    }

    /// <summary>Verifies that <c>GetOneBoardCommentsAsync</c> returns ordered by order.</summary>
    [Fact]
    public async Task GetOneBoardCommentsAsync_ReturnsOrderedByOrder()
    {
        long boardId = await SeedBoardAsync();
        await SeedAsync(
            MakeComment(boardId, 3),
            MakeComment(boardId, 1),
            MakeComment(boardId, 2));

        IEnumerable<BoardComment> result = await _sut.GetByBoardIdOrderedAsync(boardId, TestContext.Current.CancellationToken);
        List<BoardComment> list = result.ToList();

        Assert.Equal(1, list[0].Order);
        Assert.Equal(2, list[1].Order);
        Assert.Equal(3, list[2].Order);
    }

    /// <summary>Verifies that <c>GetOneBoardCommentsAsync</c> returns empty when no board comments.</summary>
    [Fact]
    public async Task GetOneBoardCommentsAsync_ReturnsEmpty_WhenNoBoardComments()
    {
        long boardId = await SeedBoardAsync();

        IEnumerable<BoardComment> result = await _sut.GetByBoardIdOrderedAsync(boardId, TestContext.Current.CancellationToken);

        Assert.Empty(result);
    }

    // -- GetBoardCommentAsync -------------------------------------------------

    /// <summary>Verifies that <c>GetBoardCommentAsync</c> returns comment when found.</summary>
    [Fact]
    public async Task GetBoardCommentAsync_ReturnsComment_WhenFound()
    {
        long boardId = await SeedBoardAsync();
        await SeedAsync(MakeComment(boardId, 1, writer: "Bob"));
        EfBoardComment comment = await _context.BoardComments.FirstAsync(TestContext.Current.CancellationToken);

        BoardComment? result = await _sut.FindByIdAsync(comment.Id, TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal("Bob", result.Writer);
    }

    /// <summary>Verifies that <c>GetBoardCommentAsync</c> returns null when not found.</summary>
    [Fact]
    public async Task GetBoardCommentAsync_ReturnsNull_WhenNotFound()
    {
        BoardComment? result = await _sut.FindByIdAsync(99999L, TestContext.Current.CancellationToken);

        Assert.Null(result);
    }

    // -- GetCommentCountsByBoardIdsAsync --------------------------------------

    /// <summary>Verifies that <c>GetCommentCountsByBoardIdsAsync</c> returns correct counts.</summary>
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

        Dictionary<long, int> counts = await _sut.GetCommentCountsByBoardIdsAsync([boardId1, boardId2], TestContext.Current.CancellationToken);

        Assert.Equal(2, counts[boardId1]);  // 2 non-deleted
        Assert.Equal(1, counts[boardId2]);
    }

    /// <summary>Verifies that <c>GetCommentCountsByBoardIdsAsync</c> returns zero for boards with no comments.</summary>
    [Fact]
    public async Task GetCommentCountsByBoardIdsAsync_ReturnsZero_ForBoardsWithNoComments()
    {
        long boardId = await SeedBoardAsync();

        Dictionary<long, int> counts = await _sut.GetCommentCountsByBoardIdsAsync([boardId], TestContext.Current.CancellationToken);

        Assert.Equal(0, counts[boardId]);
    }

    // -- WriteBoardCommentAsync -----------------------------------------------

    /// <summary>Verifies that <c>WriteBoardCommentAsync</c> adds comment to database.</summary>
    [Fact]
    public async Task WriteBoardCommentAsync_AddsCommentToDatabase()
    {
        long boardId = await SeedBoardAsync();

        var comment = BoardComment.Reconstitute(
            id: 0,
            boardId: boardId,
            order: 1,
            avatarImagePath: "/upload/alice.jpg",
            writer: "Alice",
            content: "Great post!",
            created: DateTime.UtcNow,
            deleted: false);

        await _sut.CreateAsync(comment, TestContext.Current.CancellationToken);

        EfBoardComment? saved = await _context.BoardComments.FirstOrDefaultAsync(
            c => c.BoardId == boardId && c.Content == "Great post!", TestContext.Current.CancellationToken);
        Assert.NotNull(saved);
        Assert.Equal("Alice", saved.Writer);
    }

    // -- UpdateEntityAsync ----------------------------------------------

    /// <summary>Verifies that <c>UpdateEntityAsync</c> soft deletes comment.</summary>
    [Fact]
    public async Task UpdateEntityAsync_SoftDeletesComment()
    {
        long boardId = await SeedBoardAsync();
        await SeedAsync(MakeComment(boardId, 1));
        EfBoardComment efComment = await _context.BoardComments.FirstAsync(TestContext.Current.CancellationToken);

        var comment = BoardComment.Reconstitute(
            id: efComment.Id,
            boardId: efComment.BoardId,
            order: efComment.Order,
            avatarImagePath: efComment.AvatarImagePath,
            writer: efComment.Writer ?? "",
            content: efComment.Content,
            created: efComment.Created,
            deleted: true);   // soft-delete

        await _sut.UpdateEntityAsync(comment, TestContext.Current.CancellationToken);

        EfBoardComment? result = await _context.BoardComments.FirstOrDefaultAsync(c => c.Id == efComment.Id, TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.True(result.Deleted);
    }
}
