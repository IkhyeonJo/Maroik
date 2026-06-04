using Maroik.Core.Contract.Domains.Finance;
using Maroik.Core.Contract.Filters;
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

    /// <summary>Initializes a new instance of <see cref="FixedIncomeService"/> with the supplied dependencies.</summary>
    public FixedIncomeService(IFixedIncomeRepository fixedIncomeRepository, IAssetBalanceDomainService assetBalance)
    {
        _fixedIncomeRepository = fixedIncomeRepository;
        _assetBalance = assetBalance;
    }

    /// <inheritdoc />
    public async Task<List<FixedIncomeResponse>> GetFixedIncomesAsync(string accountEmail, CancellationToken ct = default)
        => (await _fixedIncomeRepository.FindAsync([FilterCondition.Equal("AccountEmail", accountEmail)], ct: ct)).Select(FixedIncomeMapper.ToResponse).ToList();

    /// <inheritdoc />
    public async Task<FixedIncomeResponse?> GetByIdAsync(string accountEmail, long id, CancellationToken ct = default)
    {
        var results = await _fixedIncomeRepository.FindAsync([
            FilterCondition.Equal("AccountEmail", accountEmail),
            FilterCondition.Equal("Id", id)
        ], ct: ct);
        var item = results.FirstOrDefault();
        return item == null ? null : FixedIncomeMapper.ToResponse(item);
    }

    /// <inheritdoc />
    public async Task<ServiceResult> CreateAsync(string accountEmail, FixedIncomeRequest request, CancellationToken ct = default)
    {
        var validation = ValidateRequest(request);
        if (!validation.Success) return validation;

        Asset? asset = await _assetBalance.GetAssetAsync(accountEmail, request.DepositMyAssetProductName ?? "", ct);
        if (asset == null) return ServiceResult.Fail("Input is invalid");

        string currency = asset.Balance.Currency;
        var registerResult = FixedIncome.Register(accountEmail, request.MainClass, request.SubClass,
            request.Content, Math.Abs(request.Amount), currency,
            request.DepositMyAssetProductName, request.DepositMonth, request.DepositDay,
            request.MaturityDate, request.Note);

        if (registerResult.IsError)
            return ServiceResult.Fail(registerResult.FirstError.Description);

        var fixedIncome = registerResult.Value;
        await _fixedIncomeRepository.CreateAsync(fixedIncome, ct);
        return ServiceResult.Ok();
    }

    /// <inheritdoc />
    public async Task<ServiceResult> UpdateAsync(string accountEmail, FixedIncomeRequest request, CancellationToken ct = default)
    {
        var validation = ValidateRequest(request);
        if (!validation.Success) return validation;

        List<FixedIncome> items = await _fixedIncomeRepository.FindAsync([FilterCondition.Equal("AccountEmail", accountEmail)], ct: ct);
        FixedIncome? fi = items.FirstOrDefault(x => x.Id == request.Id);
        if (fi == null) return ServiceResult.Fail("Input is invalid");

        Asset? asset = await _assetBalance.GetAssetAsync(accountEmail, request.DepositMyAssetProductName ?? "", ct);
        string currency = asset?.Balance.Currency ?? fi.Amount.Currency;

        var updateResult = fi.Update(request.MainClass, request.SubClass, request.Content,
            Math.Abs(request.Amount), currency, request.DepositMyAssetProductName,
            request.DepositMonth, request.DepositDay, request.MaturityDate, request.Note);

        if (updateResult.IsError) return ServiceResult.Fail(updateResult.FirstError.Description);

        await _fixedIncomeRepository.UpdateEntityAsync(fi, ct);
        return ServiceResult.Ok();
    }

    /// <inheritdoc />
    public async Task<ServiceResult> DeleteAsync(string accountEmail, long id, CancellationToken ct = default)
    {
        List<FixedIncome> items = await _fixedIncomeRepository.FindAsync([FilterCondition.Equal("AccountEmail", accountEmail)], ct: ct);
        FixedIncome? fi = items.FirstOrDefault(x => x.Id == id);
        if (fi == null) return ServiceResult.Fail("Input is invalid");

        await _fixedIncomeRepository.DeleteAsync([FilterCondition.Equal("Id", fi.Id)], ct);
        return ServiceResult.Ok();
    }

    /// <summary>
    /// Validates the income class, deposit month/day, and maturity date format of a fixed-income request.
    /// </summary>
    private static ServiceResult ValidateRequest(FixedIncomeRequest request)
    {
        if (IncomeClassPolicy.Validate(request.MainClass, request.SubClass).IsError)
            return ServiceResult.Fail("Input is invalid");

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
