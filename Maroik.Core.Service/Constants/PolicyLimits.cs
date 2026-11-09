using Maroik.Core.Domain.Accounts;
using Maroik.Core.Domain.Boards;
using Maroik.Core.Domain.Budgets;
using Maroik.Core.Domain.Calendars;
using Maroik.Core.Domain.FileManagers;
using Maroik.Core.Domain.Finances;
using Maroik.Core.Domain.Menus;
using Maroik.Core.Domain.Primitives;
using Maroik.Core.Domain.Uploads;
using Maroik.Core.Domain.ValueObjects;

namespace Maroik.Core.Service.Constants;

/// <summary>
/// The Domain's compile-time limits, exposed by the Service layer to the Website, which may not read the Domain. A validation attribute
/// such as <c>[StringLength(PolicyLimits.PasswordMaxByteLength)]</c> takes only a constant, so it cannot call the policy services;
/// everything that is not a constant comes from there. Each value is the Domain's own constant, not a copy of it.
/// </summary>
public static class PolicyLimits
{
    /// <summary><see cref="Board.ListPageSize"/>.</summary>
    public const int BoardListPageSize = Board.ListPageSize;

    /// <summary><see cref="AttachmentUploadPolicy.AcceptedExtension"/>, as a file input's <c>accept</c>.</summary>
    public const string AttachmentAccept = AttachmentUploadPolicy.AcceptedExtension;

    /// <summary>What a budget request's sheet kind may be: a <see cref="BudgetKind"/>.</summary>
    public const string BudgetSheetKindPattern = "^(" + nameof(BudgetKind.Income) + "|" + nameof(BudgetKind.Expenditure) + ")$";

    /// <summary><see cref="BudgetWorkbook.MaxEditsPerRequest"/>.</summary>
    public const int BudgetMaxEditsPerRequest = BudgetWorkbook.MaxEditsPerRequest;

    /// <summary><see cref="PasswordPolicy.MinLength"/>.</summary>
    public const int PasswordMinLength = PasswordPolicy.MinLength;

    /// <summary><see cref="PasswordPolicy.MaxByteLength"/>.</summary>
    public const int PasswordMaxByteLength = PasswordPolicy.MaxByteLength;

    /// <summary><see cref="PasswordPolicy.Pattern"/>.</summary>
    public const string PasswordPattern = PasswordPolicy.Pattern;

    /// <summary><see cref="PasswordPolicy.ViolationMessage"/>.</summary>
    public const string PasswordViolationMessage = PasswordPolicy.ViolationMessage;

    /// <summary><see cref="ShortTextPolicy.MaxLength"/>.</summary>
    public const int ShortTextMaxLength = ShortTextPolicy.MaxLength;

    /// <summary><see cref="TitledContentPolicy.MaxTitleLength"/>.</summary>
    public const int TitleMaxLength = TitledContentPolicy.MaxTitleLength;

    /// <summary><see cref="TitledContentPolicy.MaxBodyLength"/>.</summary>
    public const int BodyMaxLength = TitledContentPolicy.MaxBodyLength;

    /// <summary><see cref="FileManagerUploadPolicy.MinQuotaGb"/>.</summary>
    public const int FileManagerMinQuotaGb = FileManagerUploadPolicy.MinQuotaGb;

    /// <summary><see cref="FileManagerUploadPolicy.QuotaGbStep"/>.</summary>
    public const string FileManagerQuotaGbStep = FileManagerUploadPolicy.QuotaGbStep;

    /// <summary><see cref="CurrencyCode.MaxLength"/>.</summary>
    public const int CurrencyMaxLength = CurrencyCode.MaxLength;

    /// <summary><see cref="MenuFieldPolicy.MinOrder"/>.</summary>
    public const long MenuMinOrder = MenuFieldPolicy.MinOrder;

    /// <summary><see cref="FinanceAmountPolicy.MaxDecimalPlaces"/>.</summary>
    public const int AmountMaxDecimalPlaces = FinanceAmountPolicy.MaxDecimalPlaces;

    /// <summary><see cref="CalendarReminderPolicy.ReminderTimeOfDayStepMinutes"/>.</summary>
    public const int ReminderTimeOfDayStepMinutes = CalendarReminderPolicy.ReminderTimeOfDayStepMinutes;
}
