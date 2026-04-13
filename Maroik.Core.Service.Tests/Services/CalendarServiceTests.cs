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
    private readonly IOptions<ServerSetting> _settings =
        Options.Create(new ServerSetting { FileStorageBaseUrl = "http://files.example.com" });

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

    [Fact]
    public async Task DeleteCalendarEventAsync_ReturnsFail_WhenEventNotFound()
    {
        _calendarRepo.Setup(r => r.GetCalendarsAsync("user@example.com")).ReturnsAsync([MakeCalendar(id: 1)]);
        _eventRepo.Setup(r => r.GetCalendarEventAsync(99)).ReturnsAsync((CalendarEventResponse?)null);
        var sut = CreateSut();

        ServiceResult result = await sut.DeleteCalendarEventAsync(99, "user@example.com");

        Assert.False(result.Success);
    }

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
