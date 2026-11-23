using System.Globalization;
using System.Runtime.CompilerServices;
using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Domain.Assistants;
using Maroik.Core.Domain.Extensions;
using Maroik.Core.Service.Assistants;
using Microsoft.Extensions.Logging;

namespace Maroik.Core.Service.Services;

/// <summary>
/// Implementation of <see cref="IAIChatService"/>. Each question is one loop: the model gets the rules, the chat so far and the tools;
/// when it asks for tools, <see cref="AIToolbox"/> runs them for the signed-in account and the model is asked again with their answers,
/// at most <see cref="AssistantPolicy.MaxToolRounds"/> times; its text is streamed to the page as it is written. Nothing is stored: the
/// browser keeps the chat. The question's text is never logged.
/// </summary>
/// <param name="accountRepository">Reads whether the account may use the AI chat.</param>
/// <param name="llmClient">The model.</param>
/// <param name="toolbox">The read-only tools.</param>
/// <param name="timeProvider">The clock (the account's today).</param>
/// <param name="logger">Logs refusals and failures.</param>
public class AIChatService(
    IAccountRepository accountRepository,
    ILlmClient llmClient,
    AIToolbox toolbox,
    TimeProvider timeProvider,
    ILogger<AIChatService> logger) : IAIChatService
{
    /// <inheritdoc />
    public async Task<bool> IsAllowedAsync(string accountEmail, CancellationToken ct = default) =>
        (await accountRepository.FindByEmailAsync(accountEmail, ct))?.AIAllowed == true;

    /// <inheritdoc />
    public async IAsyncEnumerable<AIChatEvent> AskAsync(
        string accountEmail, string timeZoneIanaId, string? question, IReadOnlyList<AIChatTurnRequest> history,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        await using IAsyncEnumerator<AIChatEvent> steps = AnswerAsync(accountEmail, timeZoneIanaId, question, history, ct).GetAsyncEnumerator(ct);
        while (true)
        {
            // An iterator cannot yield inside a try with a catch, so each step is fetched here and yielded below.
            AIChatEvent? step;
            AIChatEvent? failure = null;
            try
            {
                step = await steps.MoveNextAsync() ? steps.Current : null;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                // Expected: the user left the page (the request was aborted). There is nobody to answer.
                step = null;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                logger.LogError(ex, "AI chat failed for {Email}: the AI server could not answer", accountEmail);
                step = null;
                failure = AIChatEvent.OfError(ServiceErrorKeys.AIUnavailable);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "AI chat failed for {Email}", accountEmail);
                step = null;
                failure = AIChatEvent.OfError(ServiceErrorKeys.TemporaryError);
            }

            if (failure is not null)
                yield return failure;
            if (step is null)
                yield break;
            yield return step;
        }
    }

    /// <summary>The answer's steps; a failure is thrown to <see cref="AskAsync"/>, which turns it into the last event.</summary>
    private async IAsyncEnumerable<AIChatEvent> AnswerAsync(
        string accountEmail, string timeZoneIanaId, string? question, IReadOnlyList<AIChatTurnRequest> history,
        [EnumeratorCancellation] CancellationToken ct)
    {
        if (!await IsAllowedAsync(accountEmail, ct))
        {
            logger.LogWarning("AI chat refused for {Email}: the account is not allowed to use it", accountEmail);
            yield return AIChatEvent.OfError(ServiceErrorKeys.AINotAllowed);
            yield break;
        }

        var questionResult = AssistantQuestion.Create(question);
        if (questionResult.IsError)
        {
            ServiceResult refusal = ServiceResult.FromError(questionResult.FirstError);
            yield return AIChatEvent.OfError(refusal.ErrorKey, refusal.ErrorArgs);
            yield break;
        }

        DateOnly today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime.ConvertTimeByTimeZoneIanaId(timeZoneIanaId));
        List<LlmMessage> messages =
        [
            new("system", SystemPrompt(today, timeZoneIanaId)),
            .. AssistantHistory.Trim(history.Select(turn => new AssistantTurn(turn.Role ?? "", turn.Content ?? "")))
                .Select(turn => new LlmMessage(turn.Role, turn.Content)),
            new(AssistantTurn.User, questionResult.Value),
        ];
        logger.LogInformation("AI chat question from {Email} ({HistoryTurns} earlier turns)", accountEmail, messages.Count - 2);

        for (int round = 0; round <= AssistantPolicy.MaxToolRounds; round++)
        {
            string text = "";
            List<LlmToolCall> calls = [];
            await foreach (LlmChunk chunk in llmClient.ChatAsync(new LlmChatRequest([.. messages], AIToolbox.Definitions), ct))
            {
                calls.AddRange(chunk.ToolCalls);
                if (chunk.Content.Length == 0)
                    continue;
                text += chunk.Content;
                yield return AIChatEvent.OfText(chunk.Content);
            }

            if (calls.Count == 0)
            {
                logger.LogInformation("AI chat answered {Email} after {Rounds} tool rounds", accountEmail, round);
                yield break;
            }

            messages.Add(new LlmMessage(AssistantTurn.Assistant, text, calls));
            foreach (LlmToolCall call in calls)
            {
                if (AIToolbox.IsKnown(call.Name))
                    yield return AIChatEvent.OfTool(call.Name);
                string answer = await toolbox.RunAsync(call, accountEmail, timeZoneIanaId, today, ct);
                messages.Add(new LlmMessage("tool", answer, [], call.Name));
            }
        }

        logger.LogWarning("AI chat gave no answer for {Email}: still calling tools after {Rounds} rounds", accountEmail, AssistantPolicy.MaxToolRounds + 1);
        yield return AIChatEvent.OfError(ServiceErrorKeys.AINoAnswer);
    }

    /// <summary>
    /// The model's rules. Short, since a model on the CPU reads every token of it with each question; written in English, which small
    /// models follow best, and answered in the user's language.
    /// </summary>
    private static string SystemPrompt(DateOnly today, string timeZoneIanaId) =>
        string.Create(CultureInfo.InvariantCulture, $"""
            You are the assistant of Maroik, the user's personal household account book (assets, incomes, expenditures, fixed incomes and fixed expenditures).
            Today is {today:yyyy-MM-dd} ({today.DayOfWeek}) in the user's time zone {timeZoneIanaId}.
            Rules:
            - Get every fact and number by calling the tools. Never guess or invent records or amounts.
            - For a period, pass its name (for example "this_month" or "last_week") instead of working out dates yourself. Use start_date and end_date only when the user names exact dates.
            - Use only the filters the user asked for. A question about all incomes or all spending of a period takes no main_class, sub_class or keyword.
            - Copy every amount exactly as the tools write it, with its currency (for example "734,750 KRW"). Use the totals the tools give and never add up amounts yourself. When there are totals in several currencies, give each of them.
            - If a tool returns no records, say that there are none.
            - You can only read. Never say that you added, changed or deleted anything. If the user asks you to add, change or delete a record, call no tool and answer that you can only read and that they can do it on the page.
            - Answer briefly in the language of the user's question, in plain text.
            """);
}
