using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Settings;
using Maroik.Core.Domain.Accounts;
using Maroik.Core.Domain.Calendars;
using Maroik.Core.Service.Services;
using Maroik.Core.Service.Tests.Fixtures;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace Maroik.Core.Service.Tests.Services;

/// <summary>
/// <see cref="CalendarService.OpenCalendarEventAttachedFileAsync"/>: an event's attachment is handed out
/// only after it is checked again that the event's calendar is one the requesting viewer may see (their
/// own, one they subscribe to that is still shared, or — for an anonymous visitor — one shared with
/// anonymous visitors), and it is streamed from file storage rather than embedded in the event detail.
/// A refused download is logged as a permission denial.
/// </summary>
public class CalendarServiceAttachmentDownloadTests : IAsyncDisposable
{
    /// <summary>Fake <c>IUnitOfWork</c> injected into the system under test.</summary>
    private readonly FakeUnitOfWork _unitOfWork = new();

    /// <summary>Fake <c>ICalendarRepository</c> injected into the system under test.</summary>
    private readonly FakeCalendarRepository _calendarRepo = new();
    /// <summary>Fake <c>ICalendarEventRepository</c> injected into the system under test.</summary>
    private readonly FakeCalendarEventRepository _eventRepo = new();
    /// <summary>Fake <c>ICalendarEventAttachedFileRepository</c> injected into the system under test.</summary>
    private readonly FakeCalendarEventAttachedFileRepository _attachedFileRepo = new();
    /// <summary>Fake <c>ICalendarSharedRepository</c> injected into the system under test.</summary>
    private readonly FakeCalendarSharedRepository _sharedRepo = new();
    /// <summary>Fake <c>IOtherCalendarRepository</c> injected into the system under test.</summary>
    private readonly FakeOtherCalendarRepository _otherCalendarRepo = new();
    /// <summary>Fake <c>IAttachmentContentService</c> that opens the stored file.</summary>
    private readonly FakeAttachmentContentService _attachmentContent = new();
    /// <summary>Captures the log entries the system under test writes.</summary>
    private readonly FakeLogger<CalendarService> _logger = new();

    /// <summary>The storage path of the attachment arranged by <see cref="GivenAnEventWithAnAttachment"/>.</summary>
    private const string StoredPath = "upload/Calendar/UserIndex/calendarEventAttachedFiles/7/A.zip";

    /// <summary>The service under test over the fake dependencies.</summary>
    private CalendarService CreateSut() => new(_calendarRepo, _eventRepo, _attachedFileRepo,
        new FakeCalendarEventReminderRepository(), _sharedRepo, _otherCalendarRepo,
        new FakeFileClient(), _unitOfWork, _attachmentContent,
        Options.Create(new ServerSetting()), _logger, new FakeTimeProvider());

    /// <summary>A signed-in viewer with <paramref name="email"/>.</summary>
    private static AccountResponse Viewer(string email = "alice@example.com") =>
        new() { Email = email, Nickname = "Alice", Role = Role.User };

    /// <summary>Calendar 3, owned by alice@example.com, holding event 7.</summary>
    private static readonly Calendar _calendar = Calendar.Reconstitute(3, "alice@example.com", "Mine", null, "UTC", "#3788d8", DateTime.UtcNow, DateTime.UtcNow);

    /// <summary>
    /// Arranges calendar 3 owned by alice@example.com holding event 7, which has a stored attachment "report.zip"; nothing is
    /// shared or subscribed (the repository's guest-shared and subscribed queries return nothing).
    /// </summary>
    private void GivenAnEventWithAnAttachment()
    {
        _calendarRepo.AnswerGetByAccountEmailAsync((_, _) => Task.FromResult<List<Calendar>>([]));
        _calendarRepo.AnswerGetByAccountEmailAsync((accountEmail, _) => accountEmail == "alice@example.com", (_, _) => Task.FromResult<List<Calendar>>([_calendar]));
        _calendarRepo.AnswerGetSharedWithGuestsAsync(_ => Task.FromResult<List<Calendar>>([]));
        _calendarRepo.AnswerGetSubscribedByAsync((_, _) => Task.FromResult<List<Calendar>>([]));
        _eventRepo.AnswerFindByIdAsync((id, _) => id == 7, (_, _) => Task.FromResult<CalendarEvent?>(CalendarEvent.Reconstitute(7, 3, "t", null, false, DateTime.UtcNow, DateTime.UtcNow.AddHours(1),
                null, null, null, null, null, DateTime.UtcNow, DateTime.UtcNow)));
        _attachedFileRepo.AnswerFindByCalendarEventIdAsync((calendarEventId, _) => calendarEventId == 7, (_, _) => Task.FromResult<CalendarEventAttachedFile?>(CalendarEventAttachedFile.Reconstitute(1, 7, 4, "report", ".zip", StoredPath)));
    }

    /// <summary>Calendar 3 is shared with guests: the repository's guest query returns it.</summary>
    private void GivenSharedWithGuests() =>
        _calendarRepo.AnswerGetSharedWithGuestsAsync(_ => Task.FromResult<List<Calendar>>([_calendar]));

    /// <summary><paramref name="email"/> subscribes to calendar 3 while it is shared with users: the repository's subscribed query returns it.</summary>
    private void GivenSubscribed(string email) =>
        _calendarRepo.AnswerGetSubscribedByAsync((accountEmail, _) => accountEmail == email, (_, _) => Task.FromResult<List<Calendar>>([_calendar]));

    /// <summary>Asserts the call was refused as a missing event, nothing was opened, and the denial was logged.</summary>
    private void AssertRefusedAndLogged(ServiceResult result, AttachmentDownload? file)
    {
        Assert.False(result.Success);
        Assert.Equal("CalendarEvent.NotFound", result.ErrorCode);
        Assert.Equal("The calendar event could not be found.", result.ErrorKey);
        Assert.Null(file);
        Assert.Empty(_attachmentContent.OpenFileAsyncCalls);
        FakeLogRecord denial = Assert.Single(_logger.Collector.GetSnapshot(), r => r.Level == LogLevel.Warning);
        Assert.Contains("Attachment download refused", denial.Message, StringComparison.Ordinal);
        Assert.Contains("7", denial.Message, StringComparison.Ordinal);
    }

    /// <summary>The owner of the event's calendar gets the opened stream, named after the stored file.</summary>
    [Fact]
    public async Task OpenCalendarEventAttachedFileAsync_ReturnsTheStream_ToTheOwnerOfTheCalendar()
    {
        GivenAnEventWithAnAttachment();
        var stream = new MemoryStream([1, 2, 3, 4]);
        _attachmentContent.AnswerOpenFileAsync((filePath, _) => filePath == StoredPath, (_, _) => Task.FromResult<Stream?>(stream));

        var (result, file) = await CreateSut().OpenCalendarEventAttachedFileAsync(7, Viewer(), TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.NotNull(file);
        Assert.Same(stream, file.Content);
        Assert.Equal("report.zip", file.FileName);
    }

    /// <summary>Another account that neither owns nor subscribes to the calendar is refused.</summary>
    [Fact]
    public async Task OpenCalendarEventAttachedFileAsync_RefusesAndLogs_AnAccountThatMayNotSeeTheCalendar()
    {
        GivenAnEventWithAnAttachment();

        var (result, file) = await CreateSut().OpenCalendarEventAttachedFileAsync(7, Viewer("mallory@example.com"), TestContext.Current.CancellationToken);

        AssertRefusedAndLogged(result, file);
    }

    /// <summary>An account subscribed to a calendar that is still shared with users may download.</summary>
    [Fact]
    public async Task OpenCalendarEventAttachedFileAsync_ReturnsTheStream_ToASubscriberOfASharedCalendar()
    {
        GivenAnEventWithAnAttachment();
        GivenSubscribed("bob@example.com");
        _attachmentContent.AnswerOpenFileAsync((filePath, _) => filePath == StoredPath, (_, _) => Task.FromResult<Stream?>(new MemoryStream([1])));

        var (result, file) = await CreateSut().OpenCalendarEventAttachedFileAsync(7, Viewer("bob@example.com"), TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.Equal("report.zip", file!.FileName);
    }

    /// <summary>An anonymous visitor may download from a calendar shared with anonymous visitors.</summary>
    [Fact]
    public async Task OpenCalendarEventAttachedFileAsync_ReturnsTheStream_ToAnAnonymousVisitor_OnAnAnonymouslySharedCalendar()
    {
        GivenAnEventWithAnAttachment();
        GivenSharedWithGuests();
        _attachmentContent.AnswerOpenFileAsync((filePath, _) => filePath == StoredPath, (_, _) => Task.FromResult<Stream?>(new MemoryStream([1])));

        var (result, _) = await CreateSut().OpenCalendarEventAttachedFileAsync(7, null, TestContext.Current.CancellationToken);

        Assert.True(result.Success);
    }

    /// <summary>An anonymous visitor is refused a calendar that is shared with users only.</summary>
    [Fact]
    public async Task OpenCalendarEventAttachedFileAsync_RefusesAndLogs_AnAnonymousVisitor_OnACalendarNotSharedWithThem()
    {
        GivenAnEventWithAnAttachment();

        var (result, file) = await CreateSut().OpenCalendarEventAttachedFileAsync(7, null, TestContext.Current.CancellationToken);

        AssertRefusedAndLogged(result, file);
    }

    /// <summary>A missing event is refused the same way as an invisible one.</summary>
    [Fact]
    public async Task OpenCalendarEventAttachedFileAsync_RefusesAndLogs_AMissingEvent()
    {
        GivenAnEventWithAnAttachment();
        _eventRepo.AnswerFindByIdAsync((id, _) => id == 7, (_, _) => Task.FromResult<CalendarEvent?>(null));

        var (result, file) = await CreateSut().OpenCalendarEventAttachedFileAsync(7, Viewer(), TestContext.Current.CancellationToken);

        AssertRefusedAndLogged(result, file);
    }

    /// <summary>An event without an attachment, or with the cleared (empty-path) record, is reported as such.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OpenCalendarEventAttachedFileAsync_ReportsNotFound_WhenTheEventHasNoAttachment(bool clearedRecord)
    {
        GivenAnEventWithAnAttachment();
        _attachedFileRepo.AnswerFindByCalendarEventIdAsync((calendarEventId, _) => calendarEventId == 7, (_, _) => Task.FromResult<CalendarEventAttachedFile?>(clearedRecord ? CalendarEventAttachedFile.Reconstitute(1, 7, 0, "", "", "") : null));

        var (result, file) = await CreateSut().OpenCalendarEventAttachedFileAsync(7, Viewer(), TestContext.Current.CancellationToken);

        Assert.Equal("CalendarEvent.AttachedFileNotFound", result.ErrorCode);
        Assert.Equal("The attached file could not be found.", result.ErrorKey);
        Assert.Null(file);
        Assert.Empty(_attachmentContent.OpenFileAsyncCalls);
    }

    /// <summary>A file the storage service cannot hand out is a failure (logged where it happens), not a missing file.</summary>
    [Fact]
    public async Task OpenCalendarEventAttachedFileAsync_ReportsAFailure_WhenTheFileCannotBeOpened()
    {
        GivenAnEventWithAnAttachment();
        _attachmentContent.AnswerOpenFileAsync((_, _) => Task.FromResult<Stream?>(null));

        var (result, file) = await CreateSut().OpenCalendarEventAttachedFileAsync(7, Viewer(), TestContext.Current.CancellationToken);

        Assert.Equal("CalendarEvent.AttachedFileUnavailable", result.ErrorCode);
        Assert.Equal(ServiceResult.TemporaryErrorKey, result.ErrorKey);
        Assert.Equal(ServiceErrorType.Failure, result.ErrorType);
        Assert.Null(file);
    }

    /// <summary>A refused anonymous download logs the viewer as "Anonymous"; a signed-in one logs its e-mail.</summary>
    [Theory]
    [InlineData(null, "Anonymous")]
    [InlineData("mallory@example.com", "mallory@example.com")]
    public async Task OpenCalendarEventAttachedFileAsync_LogsWhoWasRefused(string? viewerEmail, string expected)
    {
        GivenAnEventWithAnAttachment();

        await CreateSut().OpenCalendarEventAttachedFileAsync(7, viewerEmail == null ? null : Viewer(viewerEmail), TestContext.Current.CancellationToken);

        FakeLogRecord denial = Assert.Single(_logger.Collector.GetSnapshot(), r => r.Level == LogLevel.Warning);
        Assert.EndsWith($"is missing or not visible to {expected}", denial.Message, StringComparison.Ordinal);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await _unitOfWork.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}
