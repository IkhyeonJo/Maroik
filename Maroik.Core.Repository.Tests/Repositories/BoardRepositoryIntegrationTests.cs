using Maroik.Core.Contract.Misc.Enums;
using Maroik.Core.Domain.Board;
using Maroik.Core.PostgreSQL.Data;
using Maroik.Core.Repository.Repositories;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using OrmBoard = Maroik.Core.PostgreSQL.Models.Board;

namespace Maroik.Core.Repository.Tests.Repositories;

/// <summary>
/// Integration tests for <see cref="BoardRepository"/> against a real PostgreSQL instance
/// (spun up via Testcontainers). Covers <c>FindActiveByIdForUpdateAsync</c> — the row lock that
/// <c>BoardService.WriteCommentAsync</c> relies on to serialize concurrent comment writes on the
/// same post instead of racing on a stale <c>Count()</c> read.
/// </summary>
[Trait("Category", "Integration")]
public class BoardRepositoryIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17")
        .Build();

    private ApplicationDbContext _context = null!;
    private BoardRepository _sut = null!;

    /// <summary>Starts the Testcontainers PostgreSQL instance and creates the schema before each test.</summary>
    public async ValueTask InitializeAsync()
    {
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

        await _postgres.StartAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        _context = new ApplicationDbContext(options);
        await _context.Database.EnsureCreatedAsync();
        _sut = new BoardRepository(_context);
    }

    /// <summary>Disposes the database context and stops the PostgreSQL container after each test.</summary>
    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    private static OrmBoard MakeOrm(string writer = "Alice") => new()
    {
        Type = nameof(BoardType.FreeForum),
        Title = "Test Title",
        Content = "Test Content",
        Writer = writer,
        Deleted = false,
        Locked = false,
        Noticed = false
    };

    private async Task<long> SeedAsync(OrmBoard board)
    {
        await _context.Boards.AddAsync(board);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
        return board.Id;
    }

    // -- IncrementViewAsync (ExecuteUpdateAsync) -------------------------------------

    /// <summary>
    /// Verifies that <c>IncrementViewAsync</c> increments the view counter by one via
    /// <c>ExecuteUpdateAsync</c> — a translation the EF Core InMemory provider does not support,
    /// so this must run against a real PostgreSQL instance.
    /// </summary>
    [Fact]
    public async Task IncrementViewAsync_IncrementsViewByOne()
    {
        var board = MakeOrm();
        board.View = 5;
        long id = await SeedAsync(board);

        await _sut.IncrementViewAsync(id, TestContext.Current.CancellationToken);
        _context.ChangeTracker.Clear();

        OrmBoard? result = await _context.Boards.FirstOrDefaultAsync(b => b.Id == id, TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.Equal(6, result.View);
    }

    /// <summary>Verifies that incrementing a non-existent board's view count is a no-op.</summary>
    [Fact]
    public async Task IncrementViewAsync_IsNoOp_WhenBoardDoesNotExist()
    {
        await _sut.IncrementViewAsync(999, TestContext.Current.CancellationToken);
    }

    // -- FindActiveByIdForUpdateAsync ------------------------------------------------

    /// <summary>Returns the board when it exists and is not deleted.</summary>
    [Fact]
    public async Task FindActiveByIdForUpdateAsync_ReturnsBoard_WhenExists()
    {
        long id = await SeedAsync(MakeOrm());

        await using var unitOfWork = new UnitOfWork(_context);
        await unitOfWork.BeginAsync(TestContext.Current.CancellationToken);
        Board? result = await _sut.FindActiveByIdForUpdateAsync(id, TestContext.Current.CancellationToken);
        await unitOfWork.CommitAsync(TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal("Alice", result.Writer);
    }

    /// <summary>Returns null when the board does not exist.</summary>
    [Fact]
    public async Task FindActiveByIdForUpdateAsync_ReturnsNull_WhenNotFound()
    {
        await using var unitOfWork = new UnitOfWork(_context);
        await unitOfWork.BeginAsync(TestContext.Current.CancellationToken);
        Board? result = await _sut.FindActiveByIdForUpdateAsync(999, TestContext.Current.CancellationToken);
        await unitOfWork.CommitAsync(TestContext.Current.CancellationToken);

        Assert.Null(result);
    }

    /// <summary>Returns null when the board is soft-deleted.</summary>
    [Fact]
    public async Task FindActiveByIdForUpdateAsync_ReturnsNull_WhenDeleted()
    {
        var board = MakeOrm();
        board.Deleted = true;
        long id = await SeedAsync(board);

        await using var unitOfWork = new UnitOfWork(_context);
        await unitOfWork.BeginAsync(TestContext.Current.CancellationToken);
        Board? result = await _sut.FindActiveByIdForUpdateAsync(id, TestContext.Current.CancellationToken);
        await unitOfWork.CommitAsync(TestContext.Current.CancellationToken);

        Assert.Null(result);
    }

    /// <summary>
    /// Reproduces the race BoardService.WriteCommentAsync must avoid: several concurrent
    /// "read comment count, then insert a comment with Order = count" sequences against the same
    /// post. Each sequence locks the post row via FindActiveByIdForUpdateAsync inside its own
    /// transaction (mirroring how BoardService uses it), so PostgreSQL serializes them — each one
    /// blocks until the previous commits and then re-reads the up-to-date count — instead of
    /// letting them all read the same stale count and collide on the same Order. Each concurrent
    /// call uses its own <see cref="ApplicationDbContext"/> against the same database, since a
    /// context instance is not thread-safe for concurrent operations.
    /// </summary>
    [Fact]
    public async Task FindActiveByIdForUpdateAsync_SerializesConcurrentCommentOrderAssignment()
    {
        long boardId = await SeedAsync(MakeOrm());
        string connectionString = _postgres.GetConnectionString();
        const int writerCount = 5;

        IEnumerable<Task> tasks = Enumerable.Range(0, writerCount).Select(async i =>
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(connectionString).Options;
            await using var context = new ApplicationDbContext(options);
            var boardRepo = new BoardRepository(context);
            var commentRepo = new BoardCommentRepository(context);
            await using var unitOfWork = new UnitOfWork(context);

            await unitOfWork.BeginAsync(TestContext.Current.CancellationToken);
            _ = (await boardRepo.FindActiveByIdForUpdateAsync(boardId, TestContext.Current.CancellationToken))!;
            List<BoardComment> existing = await commentRepo.GetByBoardIdOrderedAsync(boardId, TestContext.Current.CancellationToken);
            BoardComment comment = BoardComment.Create(boardId, existing.Count, "/upload/avatar.jpg", $"Writer{i}", "Hi").Value;
            await commentRepo.CreateAsync(comment, TestContext.Current.CancellationToken);
            await unitOfWork.CommitAsync(TestContext.Current.CancellationToken);
        });
        await Task.WhenAll(tasks);

        _context.ChangeTracker.Clear();
        var commentRepoForAssert = new BoardCommentRepository(_context);
        List<BoardComment> comments = await commentRepoForAssert.GetByBoardIdOrderedAsync(boardId, TestContext.Current.CancellationToken);

        Assert.Equal(writerCount, comments.Count);
        Assert.Equal(Enumerable.Range(0, writerCount).Select(n => (long)n), comments.Select(c => c.Order).OrderBy(o => o));
    }
}
