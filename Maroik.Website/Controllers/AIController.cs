using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Enums;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Service.Constants;
using Maroik.Website.Attributes;
using Maroik.Website.Extensions;
using Maroik.Website.Models.ViewModels.AIChats;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;

namespace Maroik.Website.Controllers;

/// <summary>
/// The AI chat (menu entry <c>/AI/UserIndex</c>, User role only): a user asks about their own account book and a local model answers,
/// reading the records only through the service's read-only tools. The page is shown to every user, but only an account an administrator
/// allowed gets the chat. <c>Ask</c> streams the answer as newline-delimited JSON, one <c>{ type, text }</c> object per line, so the page
/// shows it as it is written; unlike the other actions it cannot answer one <c>{ result, error }</c> object, so a failure is its last line,
/// of type <c>error</c>, with the localized message (HTTP 200, never the exception's text).
/// </summary>
/// <param name="localizer">Localizer for the tool notices and the error messages.</param>
/// <param name="logger">Logs an unexpected failure of the chat.</param>
/// <param name="aiChatService">The AI chat use cases.</param>
/// <param name="aiPolicyService">The chat's limits the page mirrors.</param>
public class AIController(
    IHtmlLocalizer<AIController> localizer,
    ILogger<AIController> logger,
    IAIChatService aiChatService,
    IAIPolicyService aiPolicyService) : Controller
{
    /// <summary>The media type of <see cref="Ask"/>'s answer.</summary>
    private const string StreamContentType = "application/x-ndjson; charset=utf-8";

    #region Read
    /// <summary>The chat page, or — for an account that is not allowed — the note to ask the administrator.</summary>
    [HttpGet]
    public async Task<IActionResult> UserIndex(CancellationToken ct)
    {
        string email = HttpContext.GetLoggedInEmail();
        return View(new AIChatPageViewModel
        {
            Allowed = await aiChatService.IsAllowedAsync(email, ct),
            MaxQuestionLength = aiPolicyService.GetRules().MaxQuestionLength,
        });
    }
    #endregion

    #region Ask
    /// <summary>
    /// Answers a question, streamed: a <c>tool</c> line (the localized notice) for each tool the model runs, <c>text</c> lines with the
    /// answer's pieces, and an <c>error</c> line, the last, when the question is refused or the answer fails.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequiredHttpPostAccess(Role = AccountRoles.User)]
    public async Task<IActionResult> Ask([FromBody] AIQuestionInputViewModel? aiQuestionInputViewModel, CancellationToken ct)
    {
        Response.ContentType = StreamContentType;
        if (!ModelState.IsValid || aiQuestionInputViewModel is null)
        {
            await Response.WriteJsonLineAsync(new { type = "error", text = localizer["Input is invalid"].Value }, ct);
            return new EmptyResult();
        }

        AccountResponse account = HttpContext.GetLoggedInAccount();
        string email = account.RequireEmail();
        List<AIChatTurnRequest> history =
            [.. aiQuestionInputViewModel.History.Select(turn => new AIChatTurnRequest { Role = turn.Role, Content = turn.Content })];
        try
        {
            await foreach (AIChatEvent step in aiChatService.AskAsync(email, account.RequireTimeZoneIanaId(), aiQuestionInputViewModel.Question, history, ct))
            {
                await Response.WriteJsonLineAsync(step.Type switch
                {
                    AIChatEventType.Text => new { type = "text", text = step.Text },
                    AIChatEventType.Tool => new { type = "tool", text = localizer[step.ToolName].Value },
                    _ => new { type = "error", text = localizer[step.ErrorKey, step.ErrorArgs].ToPlainString() },
                }, ct);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Expected: the user left the page or pressed stop, which aborts the request. There is nobody to answer.
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to answer the AI chat for {Email}", email);
            await Response.WriteJsonLineAsync(new { type = "error", text = localizer[ServiceResult.TemporaryErrorKey].Value }, ct);
        }
        return new EmptyResult();
    }
    #endregion
}
