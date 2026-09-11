using ErrorOr;
using Maroik.Core.Domain.Finance;
using Maroik.Core.Domain.Localization;
using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Service.Mappers;

namespace Maroik.Core.Service.Services;

/// <summary>
/// Implementation of <see cref="IFixedIncomeService"/> for managing recurring income entries.
/// Validates the income class and the deposit day against the deposit month's actual maximum
/// (28–31, per calendar month) before persisting. Computes <c>Noticed</c> and <c>Expired</c>
/// flags based on the current date relative to the recurrence schedule and maturity date.
/// A fixed-income entry is a schedule, not a balance movement, so these operations are single
/// non-transactional repository writes and read the referenced asset without a row lock.
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
        => (await _fixedIncomeRepository.GetByAccountEmailAsync(accountEmail, ct)).Select(FixedIncomeMapper.ToResponse).ToList();

    /// <inheritdoc />
    public async Task<List<FixedIncomeResponse>> SearchFixedIncomesAsync(string accountEmail, string search, CancellationToken ct = default)
        => (await _fixedIncomeRepository.SearchByAccountEmailAsync(accountEmail, search, ct)).Select(FixedIncomeMapper.ToResponse).ToList();

    /// <inheritdoc />
    public async Task<FixedIncomeResponse?> GetByIdAsync(string accountEmail, long id, CancellationToken ct = default)
    {
        var item = await _fixedIncomeRepository.FindByEmailAndIdAsync(accountEmail, id, ct);
        return item == null ? null : FixedIncomeMapper.ToResponse(item);
    }

    /// <inheritdoc />
    public async Task<ServiceResult> CreateAsync(string accountEmail, FixedIncomeRequest request, CancellationToken ct = default)
    {
        var validation = ValidateRequest(request);
        if (validation.IsError) return ServiceResult.FromError(validation.FirstError);

        Asset? asset = await _assetBalance.GetAssetForReadAsync(accountEmail, request.DepositMyAssetProductName ?? "", ct);
        if (asset == null)
            return ServiceResult.NotFound("FixedIncome.AssetNotFound", "The selected asset could not be found.");
        if (asset.Deleted)
            return DeletedAssetResult;

        string currency = asset.Balance.Currency;
        var registerResult = FixedIncome.Register(accountEmail, request.MainClass, request.SubClass,
            request.Content, Math.Abs(request.Amount), currency,
            request.DepositMyAssetProductName, request.DepositMonth, request.DepositDay,
            request.MaturityDate, request.Note);

        if (registerResult.IsError)
            return ServiceResult.FromError(registerResult.FirstError);

        var fixedIncome = registerResult.Value;
        await _fixedIncomeRepository.CreateAsync(fixedIncome, ct);
        return ServiceResult.Ok();
    }

    /// <inheritdoc />
    public async Task<ServiceResult> UpdateAsync(string accountEmail, FixedIncomeRequest request, CancellationToken ct = default)
    {
        var validation = ValidateRequest(request);
        if (validation.IsError) return ServiceResult.FromError(validation.FirstError);

        FixedIncome? fi = await _fixedIncomeRepository.FindByEmailAndIdAsync(accountEmail, request.Id, ct);
        if (fi == null)
            return ServiceResult.NotFound("FixedIncome.NotFound", "The fixed-income record could not be found.");

        Asset? asset = await _assetBalance.GetAssetForReadAsync(accountEmail, request.DepositMyAssetProductName ?? "", ct);
        if (asset == null)
            return ServiceResult.NotFound("FixedIncome.AssetNotFound", "The selected asset could not be found.");
        if (asset.Deleted)
            return DeletedAssetResult;
        string currency = asset.Balance.Currency;

        var updateResult = fi.Update(request.MainClass, request.SubClass, request.Content,
            Math.Abs(request.Amount), currency, request.DepositMyAssetProductName,
            request.DepositMonth, request.DepositDay, request.MaturityDate, request.Note);

        if (updateResult.IsError) return ServiceResult.FromError(updateResult.FirstError);

        await _fixedIncomeRepository.UpdateEntityAsync(fi, ct);
        return ServiceResult.Ok();
    }

    /// <inheritdoc />
    public async Task<ServiceResult> DeleteAsync(string accountEmail, long id, CancellationToken ct = default)
    {
        FixedIncome? fi = await _fixedIncomeRepository.FindByEmailAndIdAsync(accountEmail, id, ct);
        if (fi == null)
            return ServiceResult.NotFound("FixedIncome.NotFound", "The fixed-income record could not be found.");

        await _fixedIncomeRepository.DeleteByIdAsync(fi.Id, ct);
        return ServiceResult.Ok();
    }

    private static ServiceResult DeletedAssetResult => ServiceResult.Conflict(
        "FixedIncome.AssetDeleted", "Actions cannot be executed with assets that have already been deleted.");

    /// <summary>
    /// Validates the income class and deposit month/day of a fixed-income request.
    /// (The maturity date is already a parsed <see cref="DateTime"/> by this point — the
    /// controller enforces its yyyy-MM-dd string format via ParseExact.)
    /// </summary>
    private static ErrorOr<Success> ValidateRequest(FixedIncomeRequest request)
    {
        var classResult = IncomeClassPolicy.Validate(request.MainClass, request.SubClass);
        if (classResult.IsError) return classResult.FirstError;

        if (request.DepositMonth is < 1 or > 12)
            return Error.Validation("FixedIncome.DepositMonth", "Deposit month must be between 1 and 12.");

        int maxDay = FixedSchedulePolicy.MaxDepositDay(request.DepositMonth);
        if (request.DepositDay < 1 || request.DepositDay > maxDay)
            return LocalizableError.Validation("FixedIncome.DepositDay",
                "Deposit day must be between 1 and {0} for month {1}.", maxDay, request.DepositMonth);

        return Result.Success;
    }
}
