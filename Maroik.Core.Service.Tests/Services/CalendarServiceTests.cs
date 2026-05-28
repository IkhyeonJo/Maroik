using Maroik.Core.Contract.Domains.Calendar;
using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Filters;
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
    private readonly Mock<IRsaService> _rsa = new();
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
        _rsa.Object,
        _settings,
        NullLogger<CalendarService>.Instance);

    // -- Helpers --------------------------------------------------------------

    private static Calendar MakeCalendar(long id = 1, string name = "MyCalendar", string email = "user@example.com") =>
        Calendar.Reconstitute(
            id: id,
            accountEmail: email,
            name: name,
            description: null,
            timeZoneIanaId: "UTC",
            htmlColorCode: "#3788d8",
            created: DateTime.UtcNow,
            updated: DateTime.UtcNow);

    private static CalendarEvent MakeCalendarEvent(long id = 1, long calendarId = 1, string title = "Event") =>
        CalendarEvent.Reconstitute(
            id: id,
            calendarId: calendarId,
            title: title,
            description: null,
            allDay: false,
            startDate: DateTime.UtcNow,
            endDate: DateTime.UtcNow.AddHours(1),
            startTz: null,
            endTz: null,
            location: null,
            status: null,
            recurrenceId: null,
            created: DateTime.UtcNow,
            updated: DateTime.UtcNow);

    // -- CreateCalendarAsync --------------------------------------------------

    /// <summary>Verifies that <c>CreateCalendarAsync</c> returns fail when calendar with same name exists.</summary>
    [Fact]
    public async Task CreateCalendarAsync_ReturnsFail_WhenCalendarWithSameNameExists()
    {
        _calendarRepo.Setup(r => r.FindAsync(It.IsAny<IReadOnlyList<FilterCondition>>(), It.IsAny<string?>(), It.IsAny<bool>()))
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
        _calendarRepo.Setup(r => r.FindAsync(It.IsAny<IReadOnlyList<FilterCondition>>(), It.IsAny<string?>(), It.IsAny<bool>())).ReturnsAsync([]);
        _calendarRepo.Setup(r => r.CreateAsync(It.IsAny<Calendar>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        ServiceResult result = await sut.CreateCalendarAsync(new CalendarRequest
        {
            AccountEmail = "user@example.com",
            Name = "NewCalendar",
            TimeZoneIanaId = "UTC",
            HtmlColorCode = "#3788d8"
        });

        Assert.True(result.Success);
        _calendarRepo.Verify(r => r.CreateAsync(It.IsAny<Calendar>()), Times.Once);
    }

    // -- UpdateCalendarAsync --------------------------------------------------

    /// <summary>Verifies that <c>UpdateCalendarAsync</c> returns fail when calendar not found.</summary>
    [Fact]
    public async Task UpdateCalendarAsync_ReturnsFail_WhenCalendarNotFound()
    {
        _calendarRepo.Setup(r => r.FindAsync(It.IsAny<IReadOnlyList<FilterCondition>>(), It.IsAny<string?>(), It.IsAny<bool>())).ReturnsAsync([]);
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
        _calendarRepo.Setup(r => r.FindAsync(It.IsAny<IReadOnlyList<FilterCondition>>(), It.IsAny<string?>(), It.IsAny<bool>()))
            .ReturnsAsync([MakeCalendar(id: 1)]);
        _calendarRepo.Setup(r => r.UpdateEntityAsync(It.IsAny<Calendar>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        ServiceResult result = await sut.UpdateCalendarAsync(new CalendarRequest
        {
            AccountEmail = "user@example.com",
            Id = 1,
            Name = "Updated",
            TimeZoneIanaId = "UTC",
            HtmlColorCode = "#3788d8"
        });

        Assert.True(result.Success);
        _calendarRepo.Verify(r => r.UpdateEntityAsync(It.IsAny<Calendar>()), Times.Once);
    }

    // -- DeleteCalendarAsync --------------------------------------------------

    /// <summary>Verifies that <c>DeleteCalendarAsync</c> returns fail when calendar not found.</summary>
    [Fact]
    public async Task DeleteCalendarAsync_ReturnsFail_WhenCalendarNotFound()
    {
        _calendarRepo.Setup(r => r.FindAsync(It.IsAny<IReadOnlyList<FilterCondition>>(), It.IsAny<string?>(), It.IsAny<bool>())).ReturnsAsync([]);
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
        _calendarRepo.Setup(r => r.FindAsync(It.IsAny<IReadOnlyList<FilterCondition>>(), It.IsAny<string?>(), It.IsAny<bool>()))
            .ReturnsAsync([MakeCalendar(id: 1)]);
        _calendarRepo.Setup(r => r.DeleteAsync(It.IsAny<IReadOnlyList<FilterCondition>>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        ServiceResult result = await sut.DeleteCalendarAsync(new CalendarRequest
        {
            AccountEmail = "user@example.com",
            Id = 1
        });

        Assert.True(result.Success);
    }

    // -- EnsureDefaultCalendarAsync -------------------------------------------

    /// <summary>Verifies that <c>EnsureDefaultCalendarAsync</c> creates calendar when none exist.</summary>
    [Fact]
    public async Task EnsureDefaultCalendarAsync_CreatesCalendar_WhenNoneExist()
    {
        _calendarRepo.Setup(r => r.FindAsync(It.IsAny<IReadOnlyList<FilterCondition>>(), It.IsAny<string?>(), It.IsAny<bool>())).ReturnsAsync([]);
        _calendarRepo.Setup(r => r.CreateAsync(It.IsAny<Calendar>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        ServiceResult result = await sut.EnsureDefaultCalendarAsync(new CalendarRequest
        {
            AccountEmail = "user@example.com",
            Name = "Default",
            TimeZoneIanaId = "UTC",
            HtmlColorCode = "#3788d8"
        });

        Assert.True(result.Success);
        _calendarRepo.Verify(r => r.CreateAsync(It.IsAny<Calendar>()), Times.Once);
    }

    /// <summary>Verifies that <c>EnsureDefaultCalendarAsync</c> does not create when calendar exists.</summary>
    [Fact]
    public async Task EnsureDefaultCalendarAsync_DoesNotCreate_WhenCalendarExists()
    {
        _calendarRepo.Setup(r => r.FindAsync(It.IsAny<IReadOnlyList<FilterCondition>>(), It.IsAny<string?>(), It.IsAny<bool>()))
            .ReturnsAsync([MakeCalendar()]);
        var sut = CreateSut();

        ServiceResult result = await sut.EnsureDefaultCalendarAsync(new CalendarRequest
        {
            AccountEmail = "user@example.com",
            Name = "Default"
        });

        Assert.True(result.Success);
        _calendarRepo.Verify(r => r.CreateAsync(It.IsAny<Calendar>()), Times.Never);
    }

    // -- EnsureCalendarSharedAsync --------------------------------------------

    /// <summary>Verifies that <c>EnsureCalendarSharedAsync</c> creates shared when not already shared.</summary>
    [Fact]
    public async Task EnsureCalendarSharedAsync_CreatesShared_WhenNotAlreadyShared()
    {
        _sharedRepo.Setup(r => r.FindAsync(It.IsAny<IReadOnlyList<FilterCondition>>(), It.IsAny<string?>(), It.IsAny<bool>())).ReturnsAsync([]);
        _sharedRepo.Setup(r => r.CreateAsync(It.IsAny<CalendarShared>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        ServiceResult result = await sut.EnsureCalendarSharedAsync(1);

        Assert.True(result.Success);
        _sharedRepo.Verify(r => r.CreateAsync(It.Is<CalendarShared>(s => s.Id == 1)), Times.Once);
    }

    /// <summary>Verifies that <c>EnsureCalendarSharedAsync</c> does not create when already shared.</summary>
    [Fact]
    public async Task EnsureCalendarSharedAsync_DoesNotCreate_WhenAlreadyShared()
    {
        _sharedRepo.Setup(r => r.FindAsync(It.IsAny<IReadOnlyList<FilterCondition>>(), It.IsAny<string?>(), It.IsAny<bool>()))
            .ReturnsAsync([CalendarShared.Reconstitute(1, false, false)]);
        var sut = CreateSut();

        ServiceResult result = await sut.EnsureCalendarSharedAsync(1);

        Assert.True(result.Success);
        _sharedRepo.Verify(r => r.CreateAsync(It.IsAny<CalendarShared>()), Times.Never);
    }

    // -- CreateCalendarEventAsync ---------------------------------------------

    /// <summary>Verifies that <c>CreateCalendarEventAsync</c> returns fail when calendar not found.</summary>
    [Fact]
    public async Task CreateCalendarEventAsync_ReturnsFail_WhenCalendarNotFound()
    {
        _calendarRepo.Setup(r => r.FindAsync(It.IsAny<IReadOnlyList<FilterCondition>>(), It.IsAny<string?>(), It.IsAny<bool>())).ReturnsAsync([]);
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
        _calendarRepo.Setup(r => r.FindAsync(It.IsAny<IReadOnlyList<FilterCondition>>(), It.IsAny<string?>(), It.IsAny<bool>()))
            .ReturnsAsync([MakeCalendar(id: 1)]);
        _eventRepo.Setup(r => r.CreateCalendarEventAsync(It.IsAny<CalendarEvent>())).ReturnsAsync(1L);
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
        _calendarRepo.Setup(r => r.FindAsync(It.IsAny<IReadOnlyList<FilterCondition>>(), It.IsAny<string?>(), It.IsAny<bool>()))
            .ReturnsAsync([MakeCalendar(id: 1)]);
        _eventRepo.Setup(r => r.CreateCalendarEventAsync(It.IsAny<CalendarEvent>())).ReturnsAsync(1L);
        _reminderRepo.Setup(r => r.CreateAsync(It.IsAny<CalendarEventReminder>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        var reminders = new List<CalendarReminderInfo>
        {
            new() { Method = "Email", MinutesBeforeEvent = 30 },
            new() { Method = "Popup", MinutesBeforeEvent = 10 }
        };

        ServiceResult result = await sut.CreateCalendarEventAsync(
            new CalendarEventRequest { CalendarId = 1, Title = "Meeting" }, "user@example.com", "1", reminders, null);

        Assert.True(result.Success);
        _reminderRepo.Verify(r => r.CreateAsync(It.IsAny<CalendarEventReminder>()), Times.Exactly(2));
    }

    // -- UpdateCalendarEventAsync ---------------------------------------------

    /// <summary>Verifies that <c>UpdateCalendarEventAsync</c> returns fail when calendar not found.</summary>
    [Fact]
    public async Task UpdateCalendarEventAsync_ReturnsFail_WhenCalendarNotFound()
    {
        _calendarRepo.Setup(r => r.FindAsync(It.IsAny<IReadOnlyList<FilterCondition>>(), It.IsAny<string?>(), It.IsAny<bool>())).ReturnsAsync([]);
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
        _calendarRepo.Setup(r => r.FindAsync(It.IsAny<IReadOnlyList<FilterCondition>>(), It.IsAny<string?>(), It.IsAny<bool>()))
            .ReturnsAsync([MakeCalendar(id: 1)]);
        _eventRepo.Setup(r => r.FindAsync(It.IsAny<IReadOnlyList<FilterCondition>>(), It.IsAny<string?>(), It.IsAny<bool>())).ReturnsAsync([]);
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
        _calendarRepo.Setup(r => r.FindAsync(It.IsAny<IReadOnlyList<FilterCondition>>(), It.IsAny<string?>(), It.IsAny<bool>()))
            .ReturnsAsync([MakeCalendar(id: 1)]);
        _eventRepo.Setup(r => r.FindAsync(It.IsAny<IReadOnlyList<FilterCondition>>(), It.IsAny<string?>(), It.IsAny<bool>()))
            .ReturnsAsync([MakeCalendarEvent(id: 1, calendarId: 1, title: "Old")]);
        _eventRepo.Setup(r => r.UpdateEntityAsync(It.IsAny<CalendarEvent>())).Returns(Task.CompletedTask);
        _eventFileRepo.Setup(r => r.DeleteAsync(It.IsAny<IReadOnlyList<FilterCondition>>())).Returns(Task.CompletedTask);
        _reminderRepo.Setup(r => r.DeleteAsync(It.IsAny<IReadOnlyList<FilterCondition>>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        ServiceResult result = await sut.UpdateCalendarEventAsync(
            new CalendarEventRequest { Id = 1, CalendarId = 1, Title = "New Title" }, "user@example.com", "1", [], null);

        Assert.True(result.Success);
        _unitOfWork.Verify(u => u.CommitAsync(), Times.Once);
    }

    // -- DeleteCalendarEventAsync ---------------------------------------------

    /// <summary>Verifies that <c>DeleteCalendarEventAsync</c> returns fail when event not found.</summary>
    [Fact]
    public async Task DeleteCalendarEventAsync_ReturnsFail_WhenEventNotFound()
    {
        _calendarRepo.Setup(r => r.FindAsync(It.IsAny<IReadOnlyList<FilterCondition>>(), It.IsAny<string?>(), It.IsAny<bool>())).ReturnsAsync([MakeCalendar(id: 1)]);
        _eventRepo.Setup(r => r.FindAsync(It.IsAny<IReadOnlyList<FilterCondition>>(), It.IsAny<string?>(), It.IsAny<bool>())).ReturnsAsync([]);
        var sut = CreateSut();

        ServiceResult result = await sut.DeleteCalendarEventAsync(99, "user@example.com");

        Assert.False(result.Success);
    }

    /// <summary>Verifies that <c>DeleteCalendarEventAsync</c> returns fail when event belongs to different calendar.</summary>
    [Fact]
    public async Task DeleteCalendarEventAsync_ReturnsFail_WhenEventBelongsToDifferentCalendar()
    {
        _calendarRepo.Setup(r => r.FindAsync(It.IsAny<IReadOnlyList<FilterCondition>>(), It.IsAny<string?>(), It.IsAny<bool>()))
            .ReturnsAsync([MakeCalendar(id: 1)]);
        _eventRepo.Setup(r => r.FindAsync(It.IsAny<IReadOnlyList<FilterCondition>>(), It.IsAny<string?>(), It.IsAny<bool>()))
            .ReturnsAsync([MakeCalendarEvent(id: 1, calendarId: 999)]); // different calendar
        var sut = CreateSut();

        ServiceResult result = await sut.DeleteCalendarEventAsync(1, "user@example.com");

        Assert.False(result.Success);
    }

    /// <summary>Verifies that <c>DeleteCalendarEventAsync</c> returns success when ownership verified.</summary>
    [Fact]
    public async Task DeleteCalendarEventAsync_ReturnsSuccess_WhenOwnershipVerified()
    {
        _calendarRepo.Setup(r => r.FindAsync(It.IsAny<IReadOnlyList<FilterCondition>>(), It.IsAny<string?>(), It.IsAny<bool>()))
            .ReturnsAsync([MakeCalendar(id: 1)]);
        _eventRepo.Setup(r => r.FindAsync(It.IsAny<IReadOnlyList<FilterCondition>>(), It.IsAny<string?>(), It.IsAny<bool>()))
            .ReturnsAsync([MakeCalendarEvent(id: 1, calendarId: 1)]);
        _eventRepo.Setup(r => r.DeleteAsync(It.IsAny<IReadOnlyList<FilterCondition>>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        ServiceResult result = await sut.DeleteCalendarEventAsync(1, "user@example.com");

        Assert.True(result.Success);
    }

    // -- UpdateOtherCalendarAsync ---------------------------------------------

    /// <summary>Verifies that <c>UpdateOtherCalendarAsync</c> clears existing and creates new.</summary>
    [Fact]
    public async Task UpdateOtherCalendarAsync_ClearsExistingAndCreatesNew()
    {
        const string email = "user@example.com";
        _otherCalendarRepo.Setup(r => r.DeleteAsync(It.IsAny<IReadOnlyList<FilterCondition>>())).Returns(Task.CompletedTask);
        _otherCalendarRepo.Setup(r => r.CreateAsync(It.IsAny<OtherCalendar>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        var newEntries = new List<OtherCalendarRequest> { new() { CalendarId = 20 } };
        ServiceResult result = await sut.UpdateOtherCalendarAsync(email, newEntries);

        Assert.True(result.Success);
        _otherCalendarRepo.Verify(r => r.DeleteAsync(It.IsAny<IReadOnlyList<FilterCondition>>()), Times.Once);
        _otherCalendarRepo.Verify(r => r.CreateAsync(It.IsAny<OtherCalendar>()), Times.Once);
        _unitOfWork.Verify(u => u.CommitAsync(), Times.Once);
    }
}
