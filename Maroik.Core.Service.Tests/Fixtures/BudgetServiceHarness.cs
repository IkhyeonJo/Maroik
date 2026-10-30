using System.Data;
using Maroik.Core.Domain.Accounts;
using Maroik.Core.Domain.Budgets;
using Maroik.Core.Domain.Finances;
using Maroik.Core.Service.Services;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Time.Testing;

namespace Maroik.Core.Service.Tests.Fixtures;

/// <summary>
/// Builds a <see cref="BudgetService"/> for its tests: in-memory sheet / line / cell repositories, a fake account (time zone UTC) with
/// one KRW asset (so new sheets get KRW lines), income and expenditure records the tests add, a fake unit of work, a captured log and a
/// stopped clock.
/// </summary>
internal sealed class BudgetServiceHarness
{
    /// <summary>The account the tests act as.</summary>
    public const string Email = "user@example.com";

    /// <summary>The fixed "current time".</summary>
    public static readonly DateTime Now = new(2026, 5, 24, 12, 0, 0, DateTimeKind.Utc);

    /// <summary>The lines.</summary>
    public InMemoryBudgetSheetLineRepository Lines { get; } = new();

    /// <summary>The cells.</summary>
    public InMemoryBudgetSheetCellRepository Cells { get; } = new();

    /// <summary>The sheets.</summary>
    public InMemoryBudgetSheetRepository Sheets { get; }

    /// <summary>The account repository.</summary>
    public FakeAccountRepository Accounts { get; } = new();

    /// <summary>The asset repository.</summary>
    public FakeAssetRepository Assets { get; } = new();

    /// <summary>The income repository.</summary>
    public FakeIncomeRepository Incomes { get; } = new();

    /// <summary>The expenditure repository.</summary>
    public FakeExpenditureRepository Expenditures { get; } = new();

    /// <summary>The unit of work.</summary>
    public FakeUnitOfWork UnitOfWork { get; } = new();

    /// <summary>The captured log.</summary>
    public FakeLogger<BudgetService> Logger { get; } = new();

    /// <summary>The stopped clock.</summary>
    public FakeTimeProvider Time { get; } = new(new DateTimeOffset(Now));

    /// <summary>The expenditure records the repository answers with.</summary>
    public List<Expenditure> Spent { get; } = [];

    /// <summary>Wires the fakes.</summary>
    public BudgetServiceHarness()
    {
        Sheets = new InMemoryBudgetSheetRepository(Lines);
        Account account = Domain.Accounts.Account.Reconstitute(Email, "hash", "User", null, "User", "UTC", "KRW", false, 0, true, true, null, null, Now, Now, null, false, "stamp", false);
        Accounts.AnswerFindByEmailAsync((emailArg, _) => emailArg == Email, (_, _) => Task.FromResult<Account?>(account));
        Accounts.AnswerFindByEmailForUpdateAsync((emailArg, _) => emailArg == Email, (_, _) => Task.FromResult<Account?>(account));
        Assets.AnswerGetByAccountEmailAsync((accountEmail, _) => accountEmail == Email, (_, _) => Task.FromResult<List<Asset>>([Asset.Reconstitute("Wallet", Email, "FreeDepositAndWithdrawal", 0m, "KRW", null, false, Now, Now)]));
        Incomes.AnswerGetByAccountEmailAndDateRangeAsync((accountEmail, _, _, _) => accountEmail == Email, (_, _, _, _) => Task.FromResult<List<Income>>([]));
        Expenditures.AnswerGetByAccountEmailAndDateRangeAsync((accountEmail, _, _, _) => accountEmail == Email, (_, from, to, _) => Task.FromResult<List<Expenditure>>([.. Spent.Where(x => x.Created >= from && x.Created < to)]));
        UnitOfWork.AnswerBeginAsync((_, _) => Task.CompletedTask);
        UnitOfWork.AnswerCommitAsync(_ => Task.CompletedTask);
        UnitOfWork.AnswerRollbackAsync(_ => Task.CompletedTask);
    }

    /// <summary>The service under test.</summary>
    public BudgetService Sut => new(Sheets, Lines, Cells, Accounts, Assets, Incomes, Expenditures, UnitOfWork, Logger, Time);

    /// <summary>Stores a sheet with lines for <see cref="Email"/> (or another account) and returns its id.</summary>
    public long Sheet(BudgetKind kind, int year, string email = Email, params BudgetLine[] lines)
    {
        _ = Sheets.CreateWithLinesAsync(BudgetSheet.Create(email, kind, year, Now.AddDays(-1)).Value, lines);
        return Sheets.All.Max(s => s.Id);
    }

    /// <summary>Stores a cell input of a sheet.</summary>
    public void Cell(long sheetId, string rowKey, string column, string input) =>
        _ = Cells.CreateAsync(BudgetSheetCell.Create(sheetId, rowKey, column, input, Now.AddDays(-1)));

    /// <summary>Adds an expenditure record created at <paramref name="createdUtc"/>.</summary>
    public void Spend(BudgetLine line, decimal amount, DateTime createdUtc) =>
        Spent.Add(Expenditure.Reconstitute(Spent.Count + 1, Email, line.MainClass, line.SubClass, "c", amount, line.Currency, "card", null, null, createdUtc, createdUtc));

    /// <summary>Asserts the unit of work ran <paramref name="begun"/> transactions, committing <paramref name="committed"/> and rolling back the rest.</summary>
    public void AssertTransactions(int begun, int committed)
    {
        Assert.Equal(begun, UnitOfWork.BeginAsyncCalls.Count);
        Assert.Equal(committed, UnitOfWork.CommitAsyncCalls.Count);
        Assert.Equal(begun - committed, UnitOfWork.RollbackAsyncCalls.Count);
    }
}
