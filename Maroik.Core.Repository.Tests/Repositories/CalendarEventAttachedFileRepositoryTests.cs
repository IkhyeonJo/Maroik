using Maroik.Core.Contract.Dtos;
using Maroik.Core.PostgreSQL.Data;
using Maroik.Core.PostgreSQL.Models;
using Maroik.Core.Repository.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Maroik.Core.Repository.Tests.Repositories;

/// <summary>
/// Tests for <see cref="CalendarEventAttachedFileRepository"/>.
/// SQLite in-memory is used throughout because <see cref="CalendarEventAttachedFileRepository.DeleteCalendarEventAttachedFileAsync"/>
/// calls <c>ExecuteSqlInterpolatedAsync</c>, which requires a relational provider.
/// </summary>
public class CalendarEventAttachedFileRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ApplicationDbContext _context;
    private readonly CalendarEventAttachedFileRepository _sut;

    public CalendarEventAttachedFileRepositoryTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;
        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated();
        _sut = new CalendarEventAttachedFileRepository(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static CalendarEventAttachedFile MakeFile(long calendarEventId, string name = "doc.pdf") => new()
    {
        CalendarEventId = calendarEventId,
        Name = name,
        Extension = ".pdf",
        Path = $"/upload/calendar/{name}",
        Size = 1024
    };

    private async Task SeedAsync(params CalendarEventAttachedFile[] files)
    {
        await _context.CalendarEventAttachedFiles.AddRangeAsync(files);
        await _context.SaveChangesAsync();
    }

    // ── GetAllAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_ReturnsAllFiles()
    {
        await SeedAsync(MakeFile(1, "a.pdf"), MakeFile(2, "b.pdf"));

        List<CalendarEventAttachedFileResponse> result = await _sut.GetAllAsync();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsEmpty_WhenNoFilesExist()
    {
        List<CalendarEventAttachedFileResponse> result = await _sut.GetAllAsync();

        Assert.Empty(result);
    }

    // ── GetCalendarEventAttachedFileAsync ────────────────────────────────────

    [Fact]
    public async Task GetCalendarEventAttachedFileAsync_ReturnsFile_WhenFound()
    {
        await SeedAsync(MakeFile(10, "slide.pdf"));

        CalendarEventAttachedFileResponse? result = await _sut.GetCalendarEventAttachedFileAsync(10);

        Assert.NotNull(result);
        Assert.Equal(10, result.CalendarEventId);
        Assert.Equal("slide.pdf", result.Name);
    }

    [Fact]
    public async Task GetCalendarEventAttachedFileAsync_ReturnsNull_WhenNotFound()
    {
        CalendarEventAttachedFileResponse? result = await _sut.GetCalendarEventAttachedFileAsync(999);

        Assert.Null(result);
    }

    // ── SaveCalendarEventAttachedFileAsync ───────────────────────────────────

    [Fact]
    public async Task SaveCalendarEventAttachedFileAsync_PersistsFile()
    {
        var request = new CalendarEventAttachedFileRequest
        {
            CalendarEventId = 5,
            Name = "new.pdf",
            Extension = ".pdf",
            Path = "/upload/calendar/new.pdf",
            Size = 2048
        };

        await _sut.SaveCalendarEventAttachedFileAsync(request);

        CalendarEventAttachedFile? saved = await _context.CalendarEventAttachedFiles
            .FirstOrDefaultAsync(f => f.CalendarEventId == 5, TestContext.Current.CancellationToken);
        Assert.NotNull(saved);
        Assert.Equal("new.pdf", saved.Name);
    }

    // ── UpdateCalendarEventAttachedFileAsync ─────────────────────────────────

    [Fact]
    public async Task UpdateCalendarEventAttachedFileAsync_UpdatesExistingFile()
    {
        await SeedAsync(MakeFile(7, "old.pdf"));
        long id = (await _context.CalendarEventAttachedFiles.FirstAsync(f => f.CalendarEventId == 7, TestContext.Current.CancellationToken)).Id;

        var request = new CalendarEventAttachedFileRequest
        {
            Id = id,
            CalendarEventId = 7,
            Name = "updated.pdf",
            Extension = ".pdf",
            Path = "/upload/calendar/updated.pdf",
            Size = 9999
        };

        await _sut.UpdateCalendarEventAttachedFileAsync(request);

        CalendarEventAttachedFile? updated = await _context.CalendarEventAttachedFiles.FindAsync(new object[] { id }, TestContext.Current.CancellationToken);
        Assert.NotNull(updated);
        Assert.Equal("updated.pdf", updated.Name);
        Assert.Equal(9999, updated.Size);
    }

    // ── DeleteCalendarEventAttachedFileAsync ─────────────────────────────────

    [Fact]
    public async Task DeleteCalendarEventAttachedFileAsync_RemovesFilesByEventId()
    {
        await SeedAsync(MakeFile(20, "x.pdf"), MakeFile(20, "y.pdf"), MakeFile(30, "z.pdf"));

        int deleted = await _sut.DeleteCalendarEventAttachedFileAsync(20);

        Assert.Equal(2, deleted);
        Assert.False(await _context.CalendarEventAttachedFiles.AnyAsync(f => f.CalendarEventId == 20, TestContext.Current.CancellationToken));
        Assert.True(await _context.CalendarEventAttachedFiles.AnyAsync(f => f.CalendarEventId == 30, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DeleteCalendarEventAttachedFileAsync_ReturnsZero_WhenNoFilesMatch()
    {
        int deleted = await _sut.DeleteCalendarEventAttachedFileAsync(999);

        Assert.Equal(0, deleted);
    }
}
