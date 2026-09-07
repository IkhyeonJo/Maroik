using Maroik.Core.Domain.Board;
using Maroik.Core.PostgreSQL.Data;
using Maroik.Core.Repository.Repositories;
using Microsoft.EntityFrameworkCore;
using EfBoard = Maroik.Core.PostgreSQL.Models.Board;

namespace Maroik.Core.Repository.Tests.Repositories;

/// <summary>
/// Tests for <see cref="BoardRepository"/> against a real PostgreSQL database (Testcontainers)
/// pre-loaded with the production <c>Init.sql</c> schema and seed data (272 boards). <c>Board.Type</c>
/// is constrained by <c>Board_Type_check</c> to <c>FreeForum</c> / <c>PrivateNote</c>, so tests
/// cannot isolate by a private type — instead each test tags its boards with a unique
/// <see cref="RepositoryTestBase.Unique"/> <c>Writer</c> and scopes list assertions to it. Includes
/// <c>IncrementViewAsync</c> (<c>ExecuteUpdateAsync</c>) and <c>FindActiveByIdForUpdateAsync</c>
/// (<c>SELECT ... FOR UPDATE</c>), neither runnable on the EF Core InMemory provider.
/// </summary>
public sealed class BoardRepositoryTests(DatabaseFixture database) : RepositoryTestBase(database)
{
    private BoardRepository Sut => new(Context);

    private EfBoard MakeBoard(string title, string? writer = null, string type = BoardTypes.FreeForum, bool deleted = false) => new()
    {
        Type = type,
        Title = title,
        Content = "Some content",
        Writer = writer ?? Unique("writer"),
        Created = DateTime.UtcNow,
        Updated = DateTime.UtcNow,
        View = 0,
        Deleted = deleted,
        Locked = false,
        Noticed = false
    };

    private async Task<EfBoard[]> SeedAsync(params EfBoard[] boards)
    {
        await Context.Boards.AddRangeAsync(boards);
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();
        return boards;
    }

    // -- GetByTypeAsync -----------------------------------------------------------

    /// <summary>Verifies that <c>GetByTypeAsync</c> returns only non-deleted boards of the given type.</summary>
    [Fact]
    public async Task GetByTypeAsync_ReturnsOnlyBoardsOfGivenType()
    {
        string mine = Unique("writer");
        await SeedAsync(
            MakeBoard("Hello", mine),
            MakeBoard("World", mine),
            MakeBoard("Note1", mine, type: BoardTypes.PrivateNote));

        List<Board> result = await Sut.GetByTypeAsync(BoardTypes.FreeForum, TestContext.Current.CancellationToken);

        Assert.All(result, b => Assert.Equal(BoardTypes.FreeForum, b.Type));
        Assert.Equal(2, result.Count(b => b.Writer == mine));
    }

    /// <summary>Verifies that <c>GetByTypeAsync</c> returns empty for a type that matches no rows.</summary>
    [Fact]
    public async Task GetByTypeAsync_ReturnsEmpty_WhenNoMatchingType()
    {
        List<Board> result = await Sut.GetByTypeAsync("NonExistentType", TestContext.Current.CancellationToken);

        Assert.Empty(result);
    }

    /// <summary>Verifies that <c>GetByTypeAsync</c> excludes soft-deleted boards.</summary>
    [Fact]
    public async Task GetByTypeAsync_ExcludesDeletedBoards()
    {
        string mine = Unique("writer");
        await SeedAsync(
            MakeBoard("Visible", mine),
            MakeBoard("Removed", mine, deleted: true));

        List<Board> result = await Sut.GetByTypeAsync(BoardTypes.FreeForum, TestContext.Current.CancellationToken);

        Board single = Assert.Single(result, b => b.Writer == mine);
        Assert.Equal("Visible", single.Title);
    }

    // -- FindActiveByIdAsync -------------------------------------------------

    /// <summary>Verifies that <c>FindActiveByIdAsync</c> returns the board when it exists and is not deleted.</summary>
    [Fact]
    public async Task FindActiveByIdAsync_ReturnsBoard_WhenFoundAndNotDeleted()
    {
        EfBoard[] seeded = await SeedAsync(MakeBoard("My Post"));

        Board? result = await Sut.FindActiveByIdAsync(seeded[0].Id, TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal("My Post", result.Title);
    }

    /// <summary>Verifies that <c>FindActiveByIdAsync</c> returns null when the board is deleted.</summary>
    [Fact]
    public async Task FindActiveByIdAsync_ReturnsNull_WhenBoardIsDeleted()
    {
        EfBoard[] seeded = await SeedAsync(MakeBoard("Deleted Post", deleted: true));

        Board? result = await Sut.FindActiveByIdAsync(seeded[0].Id, TestContext.Current.CancellationToken);

        Assert.Null(result);
    }

    /// <summary>Verifies that <c>FindActiveByIdAsync</c> returns null when the id does not exist.</summary>
    [Fact]
    public async Task FindActiveByIdAsync_ReturnsNull_WhenIdDoesNotExist()
    {
        Board? result = await Sut.FindActiveByIdAsync(long.MaxValue, TestContext.Current.CancellationToken);

        Assert.Null(result);
    }

    // -- WriteBoardAsync ----------------------------------------------------------

    /// <summary>Verifies that <c>WriteBoardAsync</c> inserts a board and returns its DB-generated id.</summary>
    [Fact]
    public async Task WriteBoardAsync_AddsBoardToDatabase()
    {
        string writer = Unique("writer");
        var board = Board.Reconstitute(
            id: 0, type: BoardTypes.FreeForum, title: "New Post", content: "Post content",
            writer: writer, created: DateTime.UtcNow, updated: DateTime.UtcNow,
            view: 0, deleted: false, locked: false, noticed: false);

        long id = await Sut.WriteBoardAsync(board, TestContext.Current.CancellationToken);
        Context.ChangeTracker.Clear();

        Assert.True(id > 0);
        EfBoard? saved = await Context.Boards.FirstOrDefaultAsync(b => b.Id == id, TestContext.Current.CancellationToken);
        Assert.NotNull(saved);
        Assert.Equal(writer, saved.Writer);
    }

    /// <summary>PostgreSQL enforces <c>Board_Type_check</c> — an unknown board type throws (InMemory did not).</summary>
    [Fact]
    public async Task WriteBoardAsync_Throws_WhenTypeIsNotAllowed()
    {
        var board = Board.Reconstitute(
            id: 0, type: "Announcement", title: "Nope", content: "x",
            writer: Unique("writer"), created: DateTime.UtcNow, updated: DateTime.UtcNow,
            view: 0, deleted: false, locked: false, noticed: false);

        await Assert.ThrowsAsync<DbUpdateException>(
            () => Sut.WriteBoardAsync(board, TestContext.Current.CancellationToken));
    }

    // -- UpdateEntityAsync ------------------------------------------------------

    /// <summary>Verifies that <c>UpdateEntityAsync</c> persists changes to an existing board.</summary>
    [Fact]
    public async Task UpdateEntityAsync_UpdatesExistingBoard()
    {
        EfBoard[] seeded = await SeedAsync(MakeBoard("Original Title"));

        var board = Board.Reconstitute(
            id: seeded[0].Id, type: BoardTypes.FreeForum, title: "Updated Title", content: "Updated content",
            writer: seeded[0].Writer!, created: seeded[0].Created, updated: DateTime.UtcNow,
            view: seeded[0].View, deleted: false, locked: false, noticed: false);

        await Sut.UpdateEntityAsync(board, TestContext.Current.CancellationToken);
        Context.ChangeTracker.Clear();

        EfBoard? updated = await Context.Boards.FirstOrDefaultAsync(b => b.Id == seeded[0].Id, TestContext.Current.CancellationToken);
        Assert.NotNull(updated);
        Assert.Equal("Updated Title", updated.Title);
    }

    /// <summary>Verifies that <c>UpdateEntityAsync</c> is how a board is soft-deleted (flag, not row removal).</summary>
    [Fact]
    public async Task UpdateEntityAsync_SetsDeletedFlag_OnBoard()
    {
        EfBoard[] seeded = await SeedAsync(MakeBoard("Post to Delete"));

        var board = Board.Reconstitute(
            id: seeded[0].Id, type: seeded[0].Type!, title: seeded[0].Title!, content: seeded[0].Content,
            writer: seeded[0].Writer!, created: seeded[0].Created, updated: DateTime.UtcNow,
            view: seeded[0].View, deleted: true, locked: seeded[0].Locked, noticed: seeded[0].Noticed);

        await Sut.UpdateEntityAsync(board, TestContext.Current.CancellationToken);
        Context.ChangeTracker.Clear();

        EfBoard? result = await Context.Boards.FirstOrDefaultAsync(b => b.Id == seeded[0].Id, TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.True(result.Deleted);
    }

    // -- IncrementViewAsync (ExecuteUpdateAsync) -----------------------------------

    /// <summary>Verifies that <c>IncrementViewAsync</c> bumps the view counter by one via <c>ExecuteUpdateAsync</c>.</summary>
    [Fact]
    public async Task IncrementViewAsync_IncrementsViewByOne()
    {
        var seed = MakeBoard("Viewed");
        seed.View = 5;
        EfBoard[] seeded = await SeedAsync(seed);

        await Sut.IncrementViewAsync(seeded[0].Id, TestContext.Current.CancellationToken);
        Context.ChangeTracker.Clear();

        EfBoard? result = await Context.Boards.FirstOrDefaultAsync(b => b.Id == seeded[0].Id, TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.Equal(6, result.View);
    }

    /// <summary>Verifies that incrementing a non-existent board's view count is a no-op.</summary>
    [Fact]
    public async Task IncrementViewAsync_IsNoOp_WhenBoardDoesNotExist()
    {
        await Sut.IncrementViewAsync(long.MaxValue, TestContext.Current.CancellationToken);
    }

    // -- FindActiveByIdForUpdateAsync (SELECT ... FOR UPDATE) --------------------

    /// <summary>Verifies that the row-locking lookup returns the board when it exists.</summary>
    [Fact]
    public async Task FindActiveByIdForUpdateAsync_ReturnsBoard_WhenExists()
    {
        EfBoard[] seeded = await SeedAsync(MakeBoard("Locked"));

        await using var unitOfWork = new UnitOfWork(Context);
        await unitOfWork.BeginAsync(TestContext.Current.CancellationToken);
        Board? result = await Sut.FindActiveByIdForUpdateAsync(seeded[0].Id, TestContext.Current.CancellationToken);
        await unitOfWork.CommitAsync(TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal(seeded[0].Writer, result.Writer);
    }

    /// <summary>Verifies that the row-locking lookup returns null when the board does not exist.</summary>
    [Fact]
    public async Task FindActiveByIdForUpdateAsync_ReturnsNull_WhenNotFound()
    {
        await using var unitOfWork = new UnitOfWork(Context);
        await unitOfWork.BeginAsync(TestContext.Current.CancellationToken);
        Board? result = await Sut.FindActiveByIdForUpdateAsync(long.MaxValue, TestContext.Current.CancellationToken);
        await unitOfWork.CommitAsync(TestContext.Current.CancellationToken);

        Assert.Null(result);
    }

    /// <summary>Verifies that the row-locking lookup returns null when the board is soft-deleted.</summary>
    [Fact]
    public async Task FindActiveByIdForUpdateAsync_ReturnsNull_WhenDeleted()
    {
        EfBoard[] seeded = await SeedAsync(MakeBoard("Gone", deleted: true));

        await using var unitOfWork = new UnitOfWork(Context);
        await unitOfWork.BeginAsync(TestContext.Current.CancellationToken);
        Board? result = await Sut.FindActiveByIdForUpdateAsync(seeded[0].Id, TestContext.Current.CancellationToken);
        await unitOfWork.CommitAsync(TestContext.Current.CancellationToken);

        Assert.Null(result);
    }

    /// <summary>
    /// Reproduces the race <c>BoardService.WriteCommentAsync</c> must avoid: several concurrent
    /// "read comment count, insert comment with Order = count" sequences against one post. Each
    /// sequence locks the post row via <c>FindActiveByIdForUpdateAsync</c> in its own transaction,
    /// so PostgreSQL serializes them and every comment gets a distinct <c>Order</c> 0..n-1 instead
    /// of colliding on a stale count.
    /// </summary>
    [Fact]
    public async Task FindActiveByIdForUpdateAsync_SerializesConcurrentCommentOrderAssignment()
    {
        const int writerCount = 5;
        EfBoard[] seeded = await SeedAsync(MakeBoard("Hot post"));
        long boardId = seeded[0].Id;

        ApplicationDbContext[] contexts = [.. Enumerable.Range(0, writerCount).Select(_ => NewDbContext())];

        await Task.WhenAll(contexts.Select(async (context, i) =>
        {
            var boardRepo = new BoardRepository(context);
            var commentRepo = new BoardCommentRepository(context);
            await using var unitOfWork = new UnitOfWork(context);

            await unitOfWork.BeginAsync(TestContext.Current.CancellationToken);
            _ = (await boardRepo.FindActiveByIdForUpdateAsync(boardId, TestContext.Current.CancellationToken))!;
            List<BoardComment> existing = await commentRepo.GetByBoardIdOrderedAsync(boardId, TestContext.Current.CancellationToken);
            BoardComment comment = BoardComment.Create(boardId, existing.Count, "/upload/avatar.jpg", $"Writer{i}", "Hi").Value;
            await commentRepo.CreateAsync(comment, TestContext.Current.CancellationToken);
            await unitOfWork.CommitAsync(TestContext.Current.CancellationToken);
        }));

        Context.ChangeTracker.Clear();
        List<BoardComment> comments = await new BoardCommentRepository(Context)
            .GetByBoardIdOrderedAsync(boardId, TestContext.Current.CancellationToken);

        Assert.Equal(writerCount, comments.Count);
        Assert.Equal(Enumerable.Range(0, writerCount).Select(n => (long)n), comments.Select(c => c.Order).OrderBy(o => o));
    }
}
