using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Contract.Utilities;
using Maroik.Core.Service.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Maroik.Core.Service.Tests.Services;

/// <summary>
/// Unit tests for <see cref="CalendarService"/>.
/// All external dependencies (repositories, ClamAV client, file client, logger)
/// are replaced with Moq mocks. Covers calendar and calendar-event CRUD,
/// calendar sharing / subscription, and reminder management.
/// </summary>
public class CalendarServiceTests
{
    private readonly Mock<ICalendarRepository> _calendarRepo = new();
    private readonly Mock<ICalendarEventRepository> _eventRepo = new();
    private readonly Mock<ICalendarEventAttachedFileRepository> _eventFileRepo = new();
    private readonly Mock<ICalendarEventReminderRepository> _reminderRepo = new();
    private readonly Mock<ICalendarSharedRepository> _sharedRepo = new();
    private readonly Mock<IOtherCalendarRepository> _otherCalendarRepo = new();
    private readonly Mock<IFileClient> _fileClient = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IDomainEventDispatcher> _dispatcher = new();
    private readonly IOptions<ServerSetting> _settings =
        Options.Create(new ServerSetting { FileStorageBaseUrl = "http://files.example.com" });

    /// <summary>Initializes the test fixture, setting up all required test doubles and the system under test.</summary>
    public CalendarServiceTests()
    {
        _unitOfWork.Setup(u => u.BeginAsync()).Returns(Task.CompletedTask);
        _unitOfWork.Setup(u => u.CommitAsync()).Returns(Task.CompletedTask);
        _unitOfWork.Setup(u => u.RollbackAsync()).Returns(Task.CompletedTask);
        _unitOfWork.Setup(u => u.DisposeAsync()).Returns(ValueTask.CompletedTask);
    }

    private CalendarService CreateSut() => new(
        _calendarRepo.Object,
        _eventRepo.Object,
        _eventFileRepo.Object,
        _reminderRepo.Object,
        _sharedRepo.Object,
        _otherCalendarRepo.Object,
        _fileClient.Object,
        _unitOfWork.Object,
        _dispatcher.Object,
        _settings,
        NullLogger<CalendarService>.Instance);

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static CalendarResponse MakeCalendar(long id = 1, string name = "MyCalendar", string email = "user@example.com") => new()
    {
        Id = id,
        Name = name,
        AccountEmail = email,
        HtmlColorCode = "#3788d8",
        TimeZoneIanaId = "UTC"
    };

    // ── CreateCalendarAsync ──────────────────────────────────────────────────

    /// <summary>Verifies that <c>CreateCalendarAsync</c> returns fail when calendar with same name exists.</summary>
    [Fact]
    public async Task CreateCalendarAsync_ReturnsFail_WhenCalendarWithSameNameExists()
    {
        _calendarRepo.Setup(r => r.GetCalendarsAsync("user@example.com"))
            .ReturnsAsync([MakeCalendar(name: "MyCalendar")]);
        var sut = CreateSut();

        ServiceResult result = await sut.CreateCalendarAsync(new CalendarRequest
        {
            AccountEmail = "user@example.com",
            Name = "MyCalendar"
        });

        Assert.False(result.Success);
        Assert.Contains("already exists", result.ErrorKey, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Verifies that <c>CreateCalendarAsync</c> returns success when name is unique.</summary>
    [Fact]
    public async Task CreateCalendarAsync_ReturnsSuccess_WhenNameIsUnique()
    {
        _calendarRepo.Setup(r => r.GetCalendarsAsync("user@example.com")).ReturnsAsync([]);
        _calendarRepo.Setup(r => r.CreateCalendarAsync(It.IsAny<CalendarRequest>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        ServiceResult result = await sut.CreateCalendarAsync(new CalendarRequest
        {
            AccountEmail = "user@example.com",
            Name = "NewCalendar"
        });

        Assert.True(result.Success);
        _calendarRepo.Verify(r => r.CreateCalendarAsync(It.IsAny<CalendarRequest>()), Times.Once);
    }

    // ── UpdateCalendarAsync ──────────────────────────────────────────────────

    /// <summary>Verifies that <c>UpdateCalendarAsync</c> returns fail when calendar not found.</summary>
    [Fact]
    public async Task UpdateCalendarAsync_ReturnsFail_WhenCalendarNotFound()
    {
        _calendarRepo.Setup(r => r.GetCalendarsAsync("user@example.com")).ReturnsAsync([]);
        var sut = CreateSut();

        ServiceResult result = await sut.UpdateCalendarAsync(new CalendarRequest
        {
            AccountEmail = "user@example.com",
            Id = 99,
            Name = "Updated"
        });

        Assert.False(result.Success);
    }

    /// <summary>Verifies that <c>UpdateCalendarAsync</c> returns success when calendar found.</summary>
    [Fact]
    public async Task UpdateCalendarAsync_ReturnsSuccess_WhenCalendarFound()
    {
        _calendarRepo.Setup(r => r.GetCalendarsAsync("user@example.com"))
            .ReturnsAsync([MakeCalendar(id: 1)]);
        _calendarRepo.Setup(r => r.UpdateCalendarAsync(It.IsAny<CalendarRequest>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        ServiceResult result = await sut.UpdateCalendarAsync(new CalendarRequest
        {
            AccountEmail = "user@example.com",
            Id = 1,
            Name = "Updated"
        });

        Assert.True(result.Success);
        _calendarRepo.Verify(r => r.UpdateCalendarAsync(It.IsAny<CalendarRequest>()), Times.Once);
    }

    // ── DeleteCalendarAsync ──────────────────────────────────────────────────

    /// <summary>Verifies that <c>DeleteCalendarAsync</c> returns fail when calendar not found.</summary>
    [Fact]
    public async Task DeleteCalendarAsync_ReturnsFail_WhenCalendarNotFound()
    {
        _calendarRepo.Setup(r => r.GetCalendarsAsync("user@example.com")).ReturnsAsync([]);
        var sut = CreateSut();

        ServiceResult result = await sut.DeleteCalendarAsync(new CalendarRequest
        {
            AccountEmail = "user@example.com",
            Id = 99
        });

        Assert.False(result.Success);
    }

    /// <summary>Verifies that <c>DeleteCalendarAsync</c> returns success when calendar found.</summary>
    [Fact]
    public async Task DeleteCalendarAsync_ReturnsSuccess_WhenCalendarFound()
    {
        _calendarRepo.Setup(r => r.GetCalendarsAsync("user@example.com"))
            .ReturnsAsync([MakeCalendar(id: 1)]);
        _calendarRepo.Setup(r => r.DeleteCalendarAsync(It.IsAny<CalendarRequest>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        ServiceResult result = await sut.DeleteCalendarAsync(new CalendarRequest
        {
            AccountEmail = "user@example.com",
            Id = 1
        });

        Assert.True(result.Success);
    }

    // ── EnsureDefaultCalendarAsync ───────────────────────────────────────────

    /// <summary>Verifies that <c>EnsureDefaultCalendarAsync</c> creates calendar when none exist.</summary>
    [Fact]
    public async Task EnsureDefaultCalendarAsync_CreatesCalendar_WhenNoneExist()
    {
        _calendarRepo.Setup(r => r.GetCalendarsAsync("user@example.com")).ReturnsAsync([]);
        _calendarRepo.Setup(r => r.CreateCalendarAsync(It.IsAny<CalendarRequest>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        ServiceResult result = await sut.EnsureDefaultCalendarAsync(new CalendarRequest
        {
            AccountEmail = "user@example.com",
            Name = "Default"
        });

        Assert.True(result.Success);
        _calendarRepo.Verify(r => r.CreateCalendarAsync(It.IsAny<CalendarRequest>()), Times.Once);
    }

    /// <summary>Verifies that <c>EnsureDefaultCalendarAsync</c> does not create when calendar exists.</summary>
    [Fact]
    public async Task EnsureDefaultCalendarAsync_DoesNotCreate_WhenCalendarExists()
    {
        _calendarRepo.Setup(r => r.GetCalendarsAsync("user@example.com"))
            .ReturnsAsync([MakeCalendar()]);
        var sut = CreateSut();

        ServiceResult result = await sut.EnsureDefaultCalendarAsync(new CalendarRequest
        {
            AccountEmail = "user@example.com",
            Name = "Default"
        });

        Assert.True(result.Success);
        _calendarRepo.Verify(r => r.CreateCalendarAsync(It.IsAny<CalendarRequest>()), Times.Never);
    }

    // ── EnsureCalendarSharedAsync ────────────────────────────────────────────

    /// <summary>Verifies that <c>EnsureCalendarSharedAsync</c> creates shared when not already shared.</summary>
    [Fact]
    public async Task EnsureCalendarSharedAsync_CreatesShared_WhenNotAlreadyShared()
    {
        _sharedRepo.Setup(r => r.GetCalendarSharedAsync()).ReturnsAsync([]);
        _sharedRepo.Setup(r => r.CreateCalendarSharedAsync(It.IsAny<CalendarSharedRequest>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        ServiceResult result = await sut.EnsureCalendarSharedAsync(1);

        Assert.True(result.Success);
        _sharedRepo.Verify(r => r.CreateCalendarSharedAsync(It.Is<CalendarSharedRequest>(req => req.CalendarId == 1)), Times.Once);
    }

    /// <summary>Verifies that <c>EnsureCalendarSharedAsync</c> does not create when already shared.</summary>
    [Fact]
    public async Task EnsureCalendarSharedAsync_DoesNotCreate_WhenAlreadyShared()
    {
        _sharedRepo.Setup(r => r.GetCalendarSharedAsync())
            .ReturnsAsync([new CalendarSharedResponse { CalendarId = 1 }]);
        var sut = CreateSut();

        ServiceResult result = await sut.EnsureCalendarSharedAsync(1);

        Assert.True(result.Success);
        _sharedRepo.Verify(r => r.CreateCalendarSharedAsync(It.IsAny<CalendarSharedRequest>()), Times.Never);
    }

    // ── CreateCalendarEventAsync ─────────────────────────────────────────────

    /// <summary>Verifies that <c>CreateCalendarEventAsync</c> returns fail when calendar not found.</summary>
    [Fact]
    public async Task CreateCalendarEventAsync_ReturnsFail_WhenCalendarNotFound()
    {
        _calendarRepo.Setup(r => r.GetCalendarsAsync("user@example.com")).ReturnsAsync([]);
        var sut = CreateSut();

        ServiceResult result = await sut.CreateCalendarEventAsync(
            new CalendarEventRequest { CalendarId = 1 }, "user@example.com", "1", [], null);

        Assert.False(result.Success);
        Assert.Contains("does not exist", result.ErrorKey, StringComparison.OrdinalIgnoreCase);
        _unitOfWork.Verify(u => u.RollbackAsync(), Times.Once);
    }

    /// <summary>Verifies that <c>CreateCalendarEventAsync</c> returns success with no attachment and no reminders.</summary>
    [Fact]
    public async Task CreateCalendarEventAsync_ReturnsSuccess_WithNoAttachmentAndNoReminders()
    {
        _calendarRepo.Setup(r => r.GetCalendarsAsync("user@example.com"))
            .ReturnsAsync([MakeCalendar(id: 1)]);
        _eventRepo.Setup(r => r.CreateCalendarEventAsync(It.IsAny<CalendarEventRequest>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        ServiceResult result = await sut.CreateCalendarEventAsync(
            new CalendarEventRequest { CalendarId = 1, Title = "Meeting" }, "user@example.com", "1", [], null);

        Assert.True(result.Success);
        _unitOfWork.Verify(u => u.CommitAsync(), Times.Once);
    }

    /// <summary>Verifies that <c>CreateCalendarEventAsync</c> creates reminders when provided.</summary>
    [Fact]
    public async Task CreateCalendarEventAsync_CreatesReminders_WhenProvided()
    {
        _calendarRepo.Setup(r => r.GetCalendarsAsync("user@example.com"))
            .ReturnsAsync([MakeCalendar(id: 1)]);
        _eventRepo.Setup(r => r.CreateCalendarEventAsync(It.IsAny<CalendarEventRequest>())).Returns(Task.CompletedTask);
        _reminderRepo.Setup(r => r.CreateCalendarEventReminderAsync(It.IsAny<CalendarEventReminderRequest>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        var reminders = new List<CalendarReminderInfo>
        {
            new() { Method = "Email", MinutesBeforeEvent = 30 },
            new() { Method = "Popup", MinutesBeforeEvent = 10 }
        };

        ServiceResult result = await sut.CreateCalendarEventAsync(
            new CalendarEventRequest { CalendarId = 1, Title = "Meeting" }, "user@example.com", "1", reminders, null);

        Assert.True(result.Success);
        _reminderRepo.Verify(r => r.CreateCalendarEventReminderAsync(It.IsAny<CalendarEventReminderRequest>()), Times.Exactly(2));
    }

    // ── UpdateCalendarEventAsync ─────────────────────────────────────────────

    /// <summary>Verifies that <c>UpdateCalendarEventAsync</c> returns fail when calendar not found.</summary>
    [Fact]
    public async Task UpdateCalendarEventAsync_ReturnsFail_WhenCalendarNotFound()
    {
        _calendarRepo.Setup(r => r.GetCalendarsAsync("user@example.com")).ReturnsAsync([]);
        var sut = CreateSut();

        ServiceResult result = await sut.UpdateCalendarEventAsync(
            new CalendarEventRequest { Id = 1, CalendarId = 1 }, "user@example.com", "1", [], null);

        Assert.False(result.Success);
        _unitOfWork.Verify(u => u.RollbackAsync(), Times.Once);
    }

    /// <summary>Verifies that <c>UpdateCalendarEventAsync</c> returns fail when event not found.</summary>
    [Fact]
    public async Task UpdateCalendarEventAsync_ReturnsFail_WhenEventNotFound()
    {
        _calendarRepo.Setup(r => r.GetCalendarsAsync("user@example.com"))
            .ReturnsAsync([MakeCalendar(id: 1)]);
        _eventRepo.Setup(r => r.GetCalendarEventAsync(1)).ReturnsAsync((CalendarEventResponse?)null);
        var sut = CreateSut();

        ServiceResult result = await sut.UpdateCalendarEventAsync(
            new CalendarEventRequest { Id = 1, CalendarId = 1 }, "user@example.com", "1", [], null);

        Assert.False(result.Success);
        _unitOfWork.Verify(u => u.RollbackAsync(), Times.Once);
    }

    /// <summary>Verifies that <c>UpdateCalendarEventAsync</c> returns success when valid and no attachment.</summary>
    [Fact]
    public async Task UpdateCalendarEventAsync_ReturnsSuccess_WhenValidAndNoAttachment()
    {
        _calendarRepo.Setup(r => r.GetCalendarsAsync("user@example.com"))
            .ReturnsAsync([MakeCalendar(id: 1)]);
        _eventRepo.Setup(r => r.GetCalendarEventAsync(1))
            .ReturnsAsync(new CalendarEventResponse { Id = 1, CalendarId = 1, Title = "Old" });
        _eventRepo.Setup(r => r.UpdateCalendarEventAsync(It.IsAny<CalendarEventRequest>())).Returns(Task.CompletedTask);
        _eventFileRepo.Setup(r => r.DeleteCalendarEventAttachedFileAsync(1)).ReturnsAsync(0);
        _reminderRepo.Setup(r => r.DeleteCalendarEventReminderAsync(1)).ReturnsAsync(0);
        var sut = CreateSut();

        ServiceResult result = await sut.UpdateCalendarEventAsync(
            new CalendarEventRequest { Id = 1, CalendarId = 1, Title = "New Title" }, "user@example.com", "1", [], null);

        Assert.True(result.Success);
        _unitOfWork.Verify(u => u.CommitAsync(), Times.Once);
    }

    // ── DeleteCalendarEventAsync ─────────────────────────────────────────────

    /// <summary>Verifies that <c>DeleteCalendarEventAsync</c> returns fail when event not found.</summary>
    [Fact]
    public async Task DeleteCalendarEventAsync_ReturnsFail_WhenEventNotFound()
    {
        _calendarRepo.Setup(r => r.GetCalendarsAsync("user@example.com")).ReturnsAsync([MakeCalendar(id: 1)]);
        _eventRepo.Setup(r => r.GetCalendarEventAsync(99)).ReturnsAsync((CalendarEventResponse?)null);
        var sut = CreateSut();

        ServiceResult result = await sut.DeleteCalendarEventAsync(99, "user@example.com");

        Assert.False(result.Success);
    }

    /// <summary>Verifies that <c>DeleteCalendarEventAsync</c> returns fail when event belongs to different calendar.</summary>
    [Fact]
    public async Task DeleteCalendarEventAsync_ReturnsFail_WhenEventBelongsToDifferentCalendar()
    {
        _calendarRepo.Setup(r => r.GetCalendarsAsync("user@example.com"))
            .ReturnsAsync([MakeCalendar(id: 1)]);
        _eventRepo.Setup(r => r.GetCalendarEventAsync(1))
            .ReturnsAsync(new CalendarEventResponse { Id = 1, CalendarId = 999 }); // different calendar
        var sut = CreateSut();

        ServiceResult result = await sut.DeleteCalendarEventAsync(1, "user@example.com");

        Assert.False(result.Success);
    }

    /// <summary>Verifies that <c>DeleteCalendarEventAsync</c> returns success when ownership verified.</summary>
    [Fact]
    public async Task DeleteCalendarEventAsync_ReturnsSuccess_WhenOwnershipVerified()
    {
        _calendarRepo.Setup(r => r.GetCalendarsAsync("user@example.com"))
            .ReturnsAsync([MakeCalendar(id: 1)]);
        _eventRepo.Setup(r => r.GetCalendarEventAsync(1))
            .ReturnsAsync(new CalendarEventResponse { Id = 1, CalendarId = 1 });
        _eventRepo.Setup(r => r.DeleteCalendarEventAsync(It.IsAny<CalendarEventRequest>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        ServiceResult result = await sut.DeleteCalendarEventAsync(1, "user@example.com");

        Assert.True(result.Success);
    }

    // ── UpdateOtherCalendarAsync ─────────────────────────────────────────────

    /// <summary>Verifies that <c>UpdateOtherCalendarAsync</c> clears existing and creates new.</summary>
    [Fact]
    public async Task UpdateOtherCalendarAsync_ClearsExistingAndCreatesNew()
    {
        const string email = "user@example.com";
        var existing = new List<OtherCalendarResponse>
        {
            new() { AccountEmail = email, CalendarId = 10 },
            new() { AccountEmail = email, CalendarId = 11 }
        };
        _otherCalendarRepo.Setup(r => r.GetOtherCalendarsAsync(email)).ReturnsAsync(existing);
        _otherCalendarRepo.Setup(r => r.DeleteOtherCalendarAsync(It.IsAny<OtherCalendarRequest>())).Returns(Task.CompletedTask);
        _otherCalendarRepo.Setup(r => r.CreateOtherCalendarAsync(It.IsAny<OtherCalendarRequest>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        var newEntries = new List<OtherCalendarRequest> { new() { CalendarId = 20 } };
        ServiceResult result = await sut.UpdateOtherCalendarAsync(email, newEntries);

        Assert.True(result.Success);
        _otherCalendarRepo.Verify(r => r.DeleteOtherCalendarAsync(It.IsAny<OtherCalendarRequest>()), Times.Exactly(2));
        _otherCalendarRepo.Verify(r => r.CreateOtherCalendarAsync(It.IsAny<OtherCalendarRequest>()), Times.Once);
        _unitOfWork.Verify(u => u.CommitAsync(), Times.Once);
    }
}
