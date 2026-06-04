using Maroik.Core.Contract.Domains.Calendar;
using Maroik.Core.PostgreSQL.Data;
using Maroik.Core.Repository.Repositories;
using Microsoft.EntityFrameworkCore;
using EfCalendarEventAttachedFile = Maroik.Core.PostgreSQL.Models.CalendarEventAttachedFile;

namespace Maroik.Core.Repository.Tests.Repositories;

/// <summary>
/// Tests for <see cref="CalendarEventAttachedFileRepository"/> using an EF Core InMemory database.
/// </summary>
public class CalendarEventAttachedFileRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly CalendarEventAttachedFileRepository _sut;

    /// <summary>Initializes the test fixture, setting up all required test doubles and the system under test.</summary>
    public CalendarEventAttachedFileRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _sut = new CalendarEventAttachedFileRepository(_context);
    }

    /// <summary>Releases resources allocated by this test fixture (disposes the database context).</summary>
    public void Dispose() => _context.Dispose();

    // -- Helpers --------------------------------------------------------------

    private static EfCalendarEventAttachedFile MakeFile(long calendarEventId, string name = "doc.pdf") => new()
    {
        CalendarEventId = calendarEventId,
        Name = name,
        Extension = ".pdf",
        Path = $"/upload/calendar/{name}",
        Size = 1024
    };

    private async Task SeedAsync(params EfCalendarEventAttachedFile[] files)
    {
        await _context.CalendarEventAttachedFiles.AddRangeAsync(files);
        await _context.SaveChangesAsync();
    }

    // -- GetCalendarEventAttachedFileAsync ------------------------------------

    /// <summary>Verifies that <c>GetCalendarEventAttachedFileAsync</c> returns file when found.</summary>
    [Fact]
    public async Task GetCalendarEventAttachedFileAsync_ReturnsFile_WhenFound()
    {
        await SeedAsync(MakeFile(10, "slide.pdf"));

        CalendarEventAttachedFile? result = await _sut.FindByCalendarEventIdAsync(10L);

        Assert.NotNull(result);
        Assert.Equal(10, result.CalendarEventId);
        Assert.Equal("slide.pdf", result.Name);
    }

    /// <summary>Verifies that <c>GetCalendarEventAttachedFileAsync</c> returns null when not found.</summary>
    [Fact]
    public async Task GetCalendarEventAttachedFileAsync_ReturnsNull_WhenNotFound()
    {
        CalendarEventAttachedFile? result = await _sut.FindByCalendarEventIdAsync(999L);

        Assert.Null(result);
    }

    // -- SaveCalendarEventAttachedFileAsync -----------------------------------

    /// <summary>Verifies that <c>SaveCalendarEventAttachedFileAsync</c> persists file.</summary>
    [Fact]
    public async Task SaveCalendarEventAttachedFileAsync_PersistsFile()
    {
        var file = CalendarEventAttachedFile.Reconstitute(
            id: 0,
            calendarEventId: 5,
            size: 2048,
            name: "new.pdf",
            extension: ".pdf",
            path: "/upload/calendar/new.pdf");

        await _sut.CreateAsync(file);

        EfCalendarEventAttachedFile? saved = await _context.CalendarEventAttachedFiles
            .FirstOrDefaultAsync(f => f.CalendarEventId == 5, TestContext.Current.CancellationToken);
        Assert.NotNull(saved);
        Assert.Equal("new.pdf", saved.Name);
    }

    // -- UpdateEntityAsync ---------------------------------

    /// <summary>Verifies that <c>UpdateEntityAsync</c> updates existing file.</summary>
    [Fact]
    public async Task UpdateEntityAsync_UpdatesExistingFile()
    {
        await SeedAsync(MakeFile(7, "old.pdf"));
        long id = (await _context.CalendarEventAttachedFiles.FirstAsync(f => f.CalendarEventId == 7, TestContext.Current.CancellationToken)).Id;

        var file = CalendarEventAttachedFile.Reconstitute(
            id: id,
            calendarEventId: 7,
            size: 9999,
            name: "updated.pdf",
            extension: ".pdf",
            path: "/upload/calendar/updated.pdf");

        await _sut.UpdateEntityAsync(file);

        EfCalendarEventAttachedFile? updated = await _context.CalendarEventAttachedFiles.FindAsync(new object[] { id }, TestContext.Current.CancellationToken);
        Assert.NotNull(updated);
        Assert.Equal("updated.pdf", updated.Name);
        Assert.Equal(9999, updated.Size);
    }

    // -- DeleteCalendarEventAttachedFileAsync ---------------------------------

    /// <summary>Verifies that <c>DeleteCalendarEventAttachedFileAsync</c> removes files by event id.</summary>
    [Fact]
    public async Task DeleteCalendarEventAttachedFileAsync_RemovesFilesByEventId()
    {
        await SeedAsync(MakeFile(20, "x.pdf"), MakeFile(20, "y.pdf"), MakeFile(30, "z.pdf"));

        await _sut.DeleteByCalendarEventIdAsync(20L);

        Assert.False(await _context.CalendarEventAttachedFiles.AnyAsync(f => f.CalendarEventId == 20, TestContext.Current.CancellationToken));
        Assert.True(await _context.CalendarEventAttachedFiles.AnyAsync(f => f.CalendarEventId == 30, TestContext.Current.CancellationToken));
    }

    /// <summary>Verifies that <c>DeleteCalendarEventAttachedFileAsync</c> does nothing when no files match.</summary>
    [Fact]
    public async Task DeleteCalendarEventAttachedFileAsync_ReturnsZero_WhenNoFilesMatch()
    {
        await _sut.DeleteByCalendarEventIdAsync(999L);

        Assert.False(await _context.CalendarEventAttachedFiles.AnyAsync(TestContext.Current.CancellationToken));
    }
}
