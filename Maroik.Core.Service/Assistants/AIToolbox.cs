using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Unicode;
using ErrorOr;
using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Domain.Assistants;
using Maroik.Core.Domain.Extensions;
using Maroik.Core.Domain.Finances;
using Microsoft.Extensions.Logging;

namespace Maroik.Core.Service.Assistants;

/// <summary>
/// The tools the AI chat's model may call (function calling). Every tool only reads, and only the signed-in account's records: the
/// account is the caller's, never an argument, so a model cannot be talked into reading someone else's data or changing anything. The
/// period is resolved by the domain (<see cref="AssistantPeriod"/>) from the account's today, the sums are added up here
/// (<see cref="AssistantTotals"/>), and the model only words the answer.
/// </summary>
/// <param name="assetService">Reads the account's assets.</param>
/// <param name="incomeService">Reads the account's incomes.</param>
/// <param name="expenditureService">Reads the account's expenditures.</param>
/// <param name="fixedIncomeService">Reads the account's fixed incomes.</param>
/// <param name="fixedExpenditureService">Reads the account's fixed expenditures.</param>
/// <param name="logger">Logs a refused tool call.</param>
public class AIToolbox(
    IAssetService assetService,
    IIncomeService incomeService,
    IExpenditureService expenditureService,
    IFixedIncomeService fixedIncomeService,
    IFixedExpenditureService fixedExpenditureService,
    ILogger<AIToolbox> logger)
{
    /// <summary>The date format of the answers (the same as the arguments').</summary>
    private const string DateFormat = AssistantPeriod.DateFormat;

    /// <summary>
    /// The answers' JSON: snake_case like the arguments, and Korean written as it is (an escaped <c>\uXXXX</c> costs a model six tokens
    /// for one letter). The text only goes to the model, never into a page.
    /// </summary>
    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
    };

    /// <summary>The tools, in the order the model sees them.</summary>
    public static IReadOnlyList<LlmToolDefinition> Definitions { get; } =
    [
        new("get_assets", "Lists the user's assets (bank accounts, cash, savings ...) with their current balance, and the total per currency.",
            Schema(new JsonObject())),
        new("get_incomes", "Lists the user's incomes in a period (newest first) with the total per currency.",
            Schema(PeriodProperties(Enum.GetNames<IncomeMainClass>(), Enum.GetNames<IncomeSubClass>()))),
        new("get_expenditures", "Lists the user's expenditures in a period (newest first) with the total per currency.",
            Schema(PeriodProperties(Enum.GetNames<ExpenditureMainClass>(), Enum.GetNames<ExpenditureSubClass>()))),
        new("get_expenditure_totals_by_class",
            "Adds up the user's expenditures in a period per class (main class / sub-class) and currency, largest first. Use it for 'what did I spend most on'.",
            Schema(PeriodProperties(null, null))),
        new("get_fixed_incomes", "Lists the user's fixed (recurring) incomes that are still running, with their scheduled deposit date.",
            Schema(new JsonObject())),
        new("get_fixed_expenditures", "Lists the user's fixed (recurring) expenditures that are still running, with their scheduled payment date.",
            Schema(new JsonObject())),
    ];

    /// <summary>A JSON Schema object with <paramref name="properties"/>, none of them required.</summary>
    private static string Schema(JsonObject properties) =>
        new JsonObject { ["type"] = "object", ["properties"] = properties }.ToJsonString();

    /// <summary>
    /// The period arguments of a period tool, and its <c>main_class</c> / <c>sub_class</c> / <c>keyword</c> filters when it lists records
    /// of those classes.
    /// </summary>
    private static JsonObject PeriodProperties(string[]? mainClasses, string[]? subClasses)
    {
        JsonObject properties = new()
        {
            ["period"] = new JsonObject
            {
                ["type"] = "string",
                ["enum"] = new JsonArray([.. AssistantPeriod.Names.Select(JsonNode? (name) => JsonValue.Create(name))]),
                ["description"] = "The period to read, counted from today. Prefer this over dates. Default: this_month.",
            },
            ["start_date"] = new JsonObject { ["type"] = "string", ["description"] = "First day (yyyy-MM-dd), only when the user names exact dates; give end_date too." },
            ["end_date"] = new JsonObject { ["type"] = "string", ["description"] = "Last day (yyyy-MM-dd), included." },
        };
        if (mainClasses is not null)
        {
            properties["main_class"] = new JsonObject
            {
                ["type"] = "string",
                ["enum"] = new JsonArray([.. mainClasses.Select(JsonNode? (name) => JsonValue.Create(name))]),
                ["description"] = "Only this main class. Leave it out unless the user names a broad kind of record.",
            };
            properties["sub_class"] = new JsonObject
            {
                ["type"] = "string",
                ["enum"] = new JsonArray([.. (subClasses ?? []).Select(JsonNode? (name) => JsonValue.Create(name))]),
                ["description"] = "Only this sub-class (for example food is MealOrEatOutExpenses). Leave it out unless the user names one.",
            };
            properties["keyword"] = new JsonObject { ["type"] = "string", ["description"] = "Only records whose content, sub-class or note contains this word (a shop or item the user names)." };
        }
        return properties;
    }

    /// <summary>Whether <paramref name="toolName"/> is one of <see cref="Definitions"/>.</summary>
    public static bool IsKnown(string toolName) => Definitions.Any(d => d.Name == toolName);

    /// <summary>
    /// Runs <paramref name="call"/> for <paramref name="accountEmail"/> and returns its JSON answer. A refused call (unknown tool, malformed
    /// or invalid arguments) answers <c>{"error": "..."}</c> for the model to correct.
    /// </summary>
    /// <param name="call">The tool call the model wrote.</param>
    /// <param name="accountEmail">The signed-in account: the only one whose records are read.</param>
    /// <param name="timeZoneIanaId">The account's time zone, in which the period's days are counted.</param>
    /// <param name="today">Today in that time zone.</param>
    /// <param name="ct">Cancels the reads.</param>
    public async Task<string> RunAsync(LlmToolCall call, string accountEmail, string timeZoneIanaId, DateOnly today, CancellationToken ct)
    {
        Dictionary<string, string>? args = ReadArguments(call.ArgumentsJson);
        if (args is null)
            return Refuse(call.Name, accountEmail, "Assistant.MalformedArguments", "Arguments must be a JSON object.");

        return call.Name switch
        {
            "get_assets" => await GetAssetsAsync(accountEmail, ct),
            "get_incomes" => await GetIncomesAsync(args, accountEmail, timeZoneIanaId, today, ct),
            "get_expenditures" => await GetExpendituresAsync(args, accountEmail, timeZoneIanaId, today, ct),
            "get_expenditure_totals_by_class" => await GetExpenditureTotalsByClassAsync(args, accountEmail, timeZoneIanaId, today, ct),
            "get_fixed_incomes" => await GetFixedIncomesAsync(accountEmail, today, ct),
            "get_fixed_expenditures" => await GetFixedExpendituresAsync(accountEmail, today, ct),
            _ => Refuse(call.Name, accountEmail, "Assistant.UnknownTool", "Unknown tool. Use one of: " + string.Join(", ", Definitions.Select(d => d.Name)) + "."),
        };
    }

    /// <summary>The account's assets that are not deleted.</summary>
    private async Task<string> GetAssetsAsync(string accountEmail, CancellationToken ct)
    {
        List<AssetResponse> assets = [.. (await assetService.GetAssetsAsync(accountEmail, ct)).Where(a => !a.Deleted)];
        return Answer(new
        {
            Assets = assets.Select(a => new { Name = a.ProductName, a.Item, Balance = AmountText(a.Amount, a.MonetaryUnit) }),
            Totals = Totals(assets.Select(a => ("all", a.MonetaryUnit, a.Amount))),
        });
    }

    /// <summary>The account's incomes of the period.</summary>
    private async Task<string> GetIncomesAsync(Dictionary<string, string> args, string accountEmail, string timeZoneIanaId, DateOnly today, CancellationToken ct)
    {
        const string tool = "get_incomes";
        ErrorOr<AssistantPeriod> period = ResolvePeriod(args, today);
        if (period.IsError)
            return Refuse(tool, accountEmail, period.FirstError.Code, period.FirstError.Description);
        if (!TryClass(args, "main_class", Enum.GetNames<IncomeMainClass>(), out string? mainClass))
            return Refuse(tool, accountEmail, "Assistant.UnknownMainClass", "main_class must be one of: " + string.Join(", ", Enum.GetNames<IncomeMainClass>()) + ".");
        if (!TryClass(args, "sub_class", Enum.GetNames<IncomeSubClass>(), out string? subClass))
            return Refuse(tool, accountEmail, "Assistant.UnknownSubClass", "sub_class must be one of: " + string.Join(", ", Enum.GetNames<IncomeSubClass>()) + ".");

        (DateTime start, DateTime end) = period.Value.ToUtcRange(timeZoneIanaId);
        string? keyword = args.GetValueOrDefault("keyword");
        List<IncomeResponse> rows =
        [
            .. (await incomeService.GetIncomesAsync(accountEmail, ct))
                .Where(i => i.Created >= start && i.Created < end)
                .Where(i => mainClass is null || i.MainClass == mainClass)
                .Where(i => subClass is null || i.SubClass == subClass)
                .Where(i => Matches(keyword, i.Content, i.SubClass, i.Note))
                .OrderByDescending(i => i.Created)
        ];
        return Answer(new
        {
            StartDate = period.Value.StartDate.ToString(DateFormat, CultureInfo.InvariantCulture),
            EndDate = period.Value.EndDate.ToString(DateFormat, CultureInfo.InvariantCulture),
            rows.Count,
            Records = rows.Take(AssistantPolicy.MaxToolRows).Select(i => new
            {
                Date = LocalDate(i.Created, timeZoneIanaId), i.MainClass, i.SubClass, i.Content, Amount = AmountText(i.Amount, i.MonetaryUnit),
                DepositAsset = i.DepositMyAssetProductName, i.Note,
            }),
            Totals = Totals(rows.Select(i => ("all", i.MonetaryUnit, i.Amount))),
        });
    }

    /// <summary>The account's expenditures of the period.</summary>
    private async Task<string> GetExpendituresAsync(Dictionary<string, string> args, string accountEmail, string timeZoneIanaId, DateOnly today, CancellationToken ct)
    {
        const string tool = "get_expenditures";
        ErrorOr<AssistantPeriod> period = ResolvePeriod(args, today);
        if (period.IsError)
            return Refuse(tool, accountEmail, period.FirstError.Code, period.FirstError.Description);
        if (!TryClass(args, "main_class", Enum.GetNames<ExpenditureMainClass>(), out string? mainClass))
            return Refuse(tool, accountEmail, "Assistant.UnknownMainClass", "main_class must be one of: " + string.Join(", ", Enum.GetNames<ExpenditureMainClass>()) + ".");
        if (!TryClass(args, "sub_class", Enum.GetNames<ExpenditureSubClass>(), out string? subClass))
            return Refuse(tool, accountEmail, "Assistant.UnknownSubClass", "sub_class must be one of: " + string.Join(", ", Enum.GetNames<ExpenditureSubClass>()) + ".");

        string? keyword = args.GetValueOrDefault("keyword");
        List<ExpenditureResponse> rows =
        [
            .. (await ExpendituresOfAsync(period.Value, accountEmail, timeZoneIanaId, ct))
                .Where(e => mainClass is null || e.MainClass == mainClass)
                .Where(e => subClass is null || e.SubClass == subClass)
                .Where(e => Matches(keyword, e.Content, e.SubClass, e.Note))
        ];
        return Answer(new
        {
            StartDate = period.Value.StartDate.ToString(DateFormat, CultureInfo.InvariantCulture),
            EndDate = period.Value.EndDate.ToString(DateFormat, CultureInfo.InvariantCulture),
            rows.Count,
            Records = rows.Take(AssistantPolicy.MaxToolRows).Select(e => new
            {
                Date = LocalDate(e.Created, timeZoneIanaId), e.MainClass, e.SubClass, e.Content, Amount = AmountText(e.Amount, e.MonetaryUnit),
                e.PaymentMethod, e.Note,
            }),
            Totals = Totals(rows.Select(e => ("all", e.MonetaryUnit, e.Amount))),
        });
    }

    /// <summary>The account's expenditures of the period, added up per class and currency, the largest first.</summary>
    private async Task<string> GetExpenditureTotalsByClassAsync(Dictionary<string, string> args, string accountEmail, string timeZoneIanaId, DateOnly today, CancellationToken ct)
    {
        ErrorOr<AssistantPeriod> period = ResolvePeriod(args, today);
        if (period.IsError)
            return Refuse("get_expenditure_totals_by_class", accountEmail, period.FirstError.Code, period.FirstError.Description);

        List<ExpenditureResponse> rows = await ExpendituresOfAsync(period.Value, accountEmail, timeZoneIanaId, ct);
        IReadOnlyList<AssistantTotal> totals = AssistantTotals.Sum(rows.Select(e => ($"{e.MainClass} / {e.SubClass}", e.MonetaryUnit ?? "", e.Amount)));
        return Answer(new
        {
            StartDate = period.Value.StartDate.ToString(DateFormat, CultureInfo.InvariantCulture),
            EndDate = period.Value.EndDate.ToString(DateFormat, CultureInfo.InvariantCulture),
            Totals = totals.OrderBy(t => t.Currency, StringComparer.Ordinal).ThenByDescending(t => t.Amount)
                .Select(t => new { Class = t.Group, Total = AmountText(t.Amount, t.Currency), t.Count }),
        });
    }

    /// <summary>The account's fixed incomes that have not passed their maturity date.</summary>
    private async Task<string> GetFixedIncomesAsync(string accountEmail, DateOnly today, CancellationToken ct)
    {
        List<FixedIncomeResponse> rows = [.. (await fixedIncomeService.GetFixedIncomesAsync(accountEmail, ct)).Where(f => IsRunning(f.MaturityDate, today))];
        return Answer(new
        {
            Schedules = rows.Select(f => new
            {
                f.MainClass, f.SubClass, f.Content, Amount = AmountText(f.Amount, f.MonetaryUnit), DepositAsset = f.DepositMyAssetProductName,
                f.DepositMonth, f.DepositDay, MaturityDate = MaturityText(f.MaturityDate), f.Note,
            }),
            Totals = Totals(rows.Select(f => ("all", f.MonetaryUnit, f.Amount))),
        });
    }

    /// <summary>The account's fixed expenditures that have not passed their maturity date.</summary>
    private async Task<string> GetFixedExpendituresAsync(string accountEmail, DateOnly today, CancellationToken ct)
    {
        List<FixedExpenditureResponse> rows = [.. (await fixedExpenditureService.GetFixedExpendituresAsync(accountEmail, ct)).Where(f => IsRunning(f.MaturityDate, today))];
        return Answer(new
        {
            Schedules = rows.Select(f => new
            {
                f.MainClass, f.SubClass, f.Content, Amount = AmountText(f.Amount, f.MonetaryUnit), f.PaymentMethod,
                f.DepositMonth, f.DepositDay, MaturityDate = MaturityText(f.MaturityDate), f.Note,
            }),
            Totals = Totals(rows.Select(f => ("all", f.MonetaryUnit, f.Amount))),
        });
    }

    /// <summary>The account's expenditures whose time falls on the period's days in its time zone, newest first.</summary>
    private async Task<List<ExpenditureResponse>> ExpendituresOfAsync(AssistantPeriod period, string accountEmail, string timeZoneIanaId, CancellationToken ct)
    {
        (DateTime start, DateTime end) = period.ToUtcRange(timeZoneIanaId);
        return [.. (await expenditureService.GetExpendituresAsync(accountEmail, ct)).Where(e => e.Created >= start && e.Created < end).OrderByDescending(e => e.Created)];
    }

    /// <summary>The period the arguments name (see <see cref="AssistantPeriod.Resolve"/>).</summary>
    private static ErrorOr<AssistantPeriod> ResolvePeriod(Dictionary<string, string> args, DateOnly today) =>
        AssistantPeriod.Resolve(args.GetValueOrDefault("period"), args.GetValueOrDefault("start_date"), args.GetValueOrDefault("end_date"), today);

    /// <summary>A class argument (<c>main_class</c> / <c>sub_class</c>): absent (null), or one of <paramref name="allowed"/>; false for any other value.</summary>
    private static bool TryClass(Dictionary<string, string> args, string name, string[] allowed, out string? value)
    {
        value = args.GetValueOrDefault(name);
        return value is null || allowed.Contains(value, StringComparer.Ordinal);
    }

    /// <summary>
    /// An amount written out with its currency, every stored decimal place kept and a thousands separator ("734,750 KRW", "12.5 USD").
    /// A small model copies such a text reliably but garbles a bare number it has to format itself (a 734750 total came out as
    /// "73,475,000" in the trial with Llama 3.1 8B).
    /// </summary>
    private static string AmountText(decimal amount, string? currency) =>
        (amount.ToString("#,0.####", CultureInfo.InvariantCulture) + " " + (currency ?? "")).TrimEnd();

    /// <summary>Whether one of <paramref name="fields"/> contains <paramref name="keyword"/>, ignoring case; true when there is no keyword.</summary>
    private static bool Matches(string? keyword, params string?[] fields) =>
        keyword is null || fields.Any(field => field?.Contains(keyword, StringComparison.OrdinalIgnoreCase) == true);

    /// <summary>Whether a schedule with this maturity date still runs today (<see cref="FixedSchedulePolicy.IsExpired"/>).</summary>
    private static bool IsRunning(DateTime maturityDate, DateOnly today) =>
        !FixedSchedulePolicy.IsExpired(maturityDate, today.ToDateTime(TimeOnly.MinValue));

    /// <summary>The maturity date as text, or null for a schedule that never ends.</summary>
    private static string? MaturityText(DateTime maturityDate) =>
        maturityDate.Date == FixedSchedulePolicy.NoMaturityDate ? null : maturityDate.ToString(DateFormat, CultureInfo.InvariantCulture);

    /// <summary>The local day of a stored UTC time.</summary>
    private static string LocalDate(DateTime createdUtc, string timeZoneIanaId) =>
        createdUtc.ConvertTimeByTimeZoneIanaId(timeZoneIanaId).ToString(DateFormat, CultureInfo.InvariantCulture);

    /// <summary>The totals per currency of <paramref name="items"/>.</summary>
    private static IEnumerable<object> Totals(IEnumerable<(string Group, string? Currency, decimal Amount)> items) =>
        AssistantTotals.Sum(items.Select(i => (i.Group, i.Currency ?? "", i.Amount))).Select(t => new { Total = AmountText(t.Amount, t.Currency), t.Count });

    /// <summary>The arguments as text by name, a blank or null value left out; null when they are not a JSON object.</summary>
    private static Dictionary<string, string>? ReadArguments(string json)
    {
        try
        {
            using JsonDocument doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(json) ? "{}" : json);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
                return null;

            Dictionary<string, string> args = new(StringComparer.Ordinal);
            foreach (JsonProperty property in doc.RootElement.EnumerateObject())
            {
                string? value = property.Value.ValueKind switch
                {
                    JsonValueKind.String => property.Value.GetString(),
                    JsonValueKind.Null => null,
                    _ => property.Value.GetRawText(),
                };
                if (!string.IsNullOrWhiteSpace(value))
                    args[property.Name] = value.Trim();
            }
            return args;
        }
        catch (JsonException)
        {
            // Expected: the model wrote malformed arguments. The caller answers it with a refusal it can correct, and logs that.
            return null;
        }
    }

    /// <summary>A refusal for the model, logged with the tool and the reason's code (the arguments may hold the user's words, so they are not).</summary>
    private string Refuse(string tool, string accountEmail, string code, string reason)
    {
        logger.LogInformation("AI tool {Tool} refused for {Email}: {Code}", tool, accountEmail, code);
        return Answer(new { Error = reason });
    }

    /// <summary>The JSON of an answer.</summary>
    private static string Answer(object value) => JsonSerializer.Serialize(value, _json);
}
