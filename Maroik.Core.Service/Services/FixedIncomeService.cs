using Maroik.Core.Contract.Domains.Finance;
using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Service.Mappers;
using System.Text.RegularExpressions;

namespace Maroik.Core.Service.Services;

/// <summary>
/// Implementation of <see cref="IFixedIncomeService"/> for managing recurring income entries.
/// Validates the <c>MaturityDate</c> format (yyyy-MM-dd) and the deposit day (1–28 to avoid
/// month-boundary issues) before persisting. Computes <c>Noticed</c> and <c>Expired</c> flags
/// based on the current date relative to the recurrence schedule and maturity date.
/// </summary>
public class FixedIncomeService : IFixedIncomeService
{
    private readonly IFixedIncomeRepository _fixedIncomeRepository;
    private readonly IAssetBalanceDomainService _assetBalance;
    private readonly IDomainEventDispatcher _dispatcher;

    /// <summary>Initializes a new instance of <see cref="FixedIncomeService"/> with the supplied dependencies.</summary>
    public FixedIncomeService(IFixedIncomeRepository fixedIncomeRepository, IAssetBalanceDomainService assetBalance, IDomainEventDispatcher dispatcher)
    {
        _fixedIncomeRepository = fixedIncomeRepository;
        _assetBalance = assetBalance;
        _dispatcher = dispatcher;
    }

    /// <inheritdoc />
    public async Task<List<FixedIncomeResponse>> GetFixedIncomesAsync(string accountEmail)
        => (await _fixedIncomeRepository.GetFixedIncomesAsync(accountEmail)).Select(FixedIncomeMapper.ToResponse).ToList();

    /// <inheritdoc />
    public async Task<ServiceResult> CreateAsync(string accountEmail, FixedIncomeRequest request)
    {
        var validation = ValidateRequest(request);
        if (!validation.Success) return validation;

        Asset? asset = await _assetBalance.GetAssetAsync(accountEmail, request.DepositMyAssetProductName ?? "");
        if (asset == null) return ServiceResult.Fail("Input is invalid");

        string currency = asset.Balance.Currency;
        var registerResult = FixedIncome.Register(0, accountEmail, request.MainClass, request.SubClass,
            request.Content, Math.Abs(request.Amount), currency,
            request.DepositMyAssetProductName, request.DepositMonth, request.DepositDay,
            request.MaturityDate, request.Note);

        if (registerResult.IsError)
            return ServiceResult.Fail(registerResult.FirstError.Description);

        var fixedIncome = registerResult.Value;
        await _fixedIncomeRepository.CreateFixedIncomeAsync(fixedIncome);
        await _dispatcher.DispatchAsync(fixedIncome.PopDomainEvents());
        return ServiceResult.Ok();
    }

    /// <inheritdoc />
    public async Task<ServiceResult> UpdateAsync(string accountEmail, FixedIncomeRequest request)
    {
        var validation = ValidateRequest(request);
        if (!validation.Success) return validation;

        List<FixedIncome> items = await _fixedIncomeRepository.GetFixedIncomesAsync(accountEmail);
        FixedIncome? fi = items.FirstOrDefault(x => x.Id == request.Id);
        if (fi == null) return ServiceResult.Fail("Input is invalid");

        Asset? asset = await _assetBalance.GetAssetAsync(accountEmail, request.DepositMyAssetProductName ?? "");
        string currency = asset?.Balance.Currency ?? fi.Amount.Currency;

        var updateResult = fi.Update(request.MainClass, request.SubClass, request.Content,
            Math.Abs(request.Amount), currency, request.DepositMyAssetProductName,
            request.DepositMonth, request.DepositDay, request.MaturityDate, request.Note);

        if (updateResult.IsError) return ServiceResult.Fail(updateResult.FirstError.Description);

        await _fixedIncomeRepository.UpdateFixedIncomeAsync(fi);
        return ServiceResult.Ok();
    }

    /// <inheritdoc />
    public async Task<ServiceResult> DeleteAsync(string accountEmail, long id)
    {
        List<FixedIncome> items = await _fixedIncomeRepository.GetFixedIncomesAsync(accountEmail);
        FixedIncome? fi = items.FirstOrDefault(x => x.Id == id);
        if (fi == null) return ServiceResult.Fail("Input is invalid");

        await _fixedIncomeRepository.DeleteFixedIncomeAsync(fi);
        return ServiceResult.Ok();
    }

    private static ServiceResult ValidateRequest(FixedIncomeRequest request)
    {
        bool classValid = request.MainClass switch
        {
            "RegularIncome" => request.SubClass is "LaborIncome" or "BusinessIncome" or "PensionIncome"
                or "FinancialIncome" or "RentalIncome" or "OtherIncome",
            "IrregularIncome" => request.SubClass is "LaborIncome" or "OtherIncome",
            _ => false
        };
        if (!classValid) return ServiceResult.Fail("Input is invalid");

        if (request.DepositMonth is < 1 or > 12)
            return ServiceResult.Fail("Input is invalid");

        int maxDay = request.DepositMonth switch { 2 => 29, 4 or 6 or 9 or 11 => 30, _ => 31 };
        if (request.DepositDay < 1 || request.DepositDay > maxDay)
            return ServiceResult.Fail("Input is invalid");

        if (!Regex.IsMatch(request.MaturityDate.ToString("yyyy-MM-dd"),
            @"^\d{4}-((0[1-9])|(1[012]))-((0[1-9]|[12]\d)|3[01])$"))
            return ServiceResult.Fail("Input is invalid");

        return ServiceResult.Ok();
    }
}
