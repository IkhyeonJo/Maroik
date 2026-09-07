using ErrorOr;
using Maroik.Core.Domain.Finance;
using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Service.Mappers;

namespace Maroik.Core.Service.Services;

/// <summary>
/// Implementation of <see cref="IFixedExpenditureService"/> for managing recurring expenditure entries.
/// Validates the expenditure class and deposit day before persisting.
/// Computes <c>Noticed</c> and <c>Expired</c> flags based on the current date,
/// and verifies asset ownership before write operations.
/// A fixed-expenditure entry is a schedule, not a balance movement, so these operations are single
/// non-transactional repository writes and read the referenced asset without a row lock.
/// </summary>
public class FixedExpenditureService : IFixedExpenditureService
{
    private readonly IFixedExpenditureRepository _fixedExpenditureRepository;
    private readonly IAssetBalanceDomainService _assetBalance;

    /// <summary>Initializes a new instance of <see cref="FixedExpenditureService"/> with the supplied dependencies.</summary>
    public FixedExpenditureService(IFixedExpenditureRepository fixedExpenditureRepository, IAssetBalanceDomainService assetBalance)
    {
        _fixedExpenditureRepository = fixedExpenditureRepository;
        _assetBalance = assetBalance;
    }

    /// <inheritdoc />
    public async Task<List<FixedExpenditureResponse>> GetFixedExpendituresAsync(string accountEmail, CancellationToken ct = default)
        => (await _fixedExpenditureRepository.GetByAccountEmailAsync(accountEmail, ct)).Select(FixedExpenditureMapper.ToResponse).ToList();

    /// <inheritdoc />
    public async Task<FixedExpenditureResponse?> GetByIdAsync(string accountEmail, long id, CancellationToken ct = default)
    {
        var item = await _fixedExpenditureRepository.FindByEmailAndIdAsync(accountEmail, id, ct);
        return item == null ? null : FixedExpenditureMapper.ToResponse(item);
    }

    /// <inheritdoc />
    public async Task<ServiceResult> CreateAsync(string accountEmail, FixedExpenditureRequest request, CancellationToken ct = default)
    {
        var validation = ValidateClassAndDate(request);
        if (validation.IsError) return ServiceResult.FromError(validation.FirstError);
        bool requiresDepositAsset = validation.Value;

        Asset? payAsset = await _assetBalance.GetAssetForReadAsync(accountEmail, request.PaymentMethod ?? "", ct);
        if (payAsset == null) return AssetNotFoundResult;
        if (payAsset.Deleted) return DeletedAssetResult;

        string? myDepositAsset = null;
        if (requiresDepositAsset)
        {
            if (request.PaymentMethod == request.MyDepositAsset)
                return SameAssetResult;

            Asset? depAsset = await _assetBalance.GetAssetForReadAsync(accountEmail, request.MyDepositAsset ?? "", ct);
            if (depAsset == null) return AssetNotFoundResult;
            if (depAsset.Deleted) return DeletedAssetResult;
            if (payAsset.Balance.Currency != depAsset.Balance.Currency)
                return CurrencyMismatchResult;

            myDepositAsset = request.MyDepositAsset;
        }

        string currency = payAsset.Balance.Currency;
        var registerResult = FixedExpenditure.Register(accountEmail, request.MainClass, request.SubClass,
            request.Content, Math.Abs(request.Amount), currency, request.PaymentMethod,
            myDepositAsset, request.DepositMonth, request.DepositDay, request.MaturityDate, request.Note);

        if (registerResult.IsError)
            return ServiceResult.FromError(registerResult.FirstError);

        var fixedExpenditure = registerResult.Value;
        await _fixedExpenditureRepository.CreateAsync(fixedExpenditure, ct);
        return ServiceResult.Ok();
    }

    /// <inheritdoc />
    public async Task<ServiceResult> UpdateAsync(string accountEmail, FixedExpenditureRequest request, CancellationToken ct = default)
    {
        var validation = ValidateClassAndDate(request);
        if (validation.IsError) return ServiceResult.FromError(validation.FirstError);
        bool requiresDepositAsset = validation.Value;

        if (requiresDepositAsset)
        {
            if (request.PaymentMethod == request.MyDepositAsset)
                return SameAssetResult;

            var payAsset = await _assetBalance.GetAssetForReadAsync(accountEmail, request.PaymentMethod ?? "", ct);
            var depAsset = await _assetBalance.GetAssetForReadAsync(accountEmail, request.MyDepositAsset ?? "", ct);
            if (payAsset == null || depAsset == null) return AssetNotFoundResult;
            if (payAsset.Deleted || depAsset.Deleted) return DeletedAssetResult;
            if (payAsset.Balance.Currency != depAsset.Balance.Currency)
                return CurrencyMismatchResult;
        }

        FixedExpenditure? fe = await _fixedExpenditureRepository.FindByEmailAndIdAsync(accountEmail, request.Id, ct);
        if (fe == null)
            return ServiceResult.NotFound("FixedExpenditure.NotFound", "The fixed-expenditure record could not be found.");

        Asset? payAssetForCurrency = await _assetBalance.GetAssetForReadAsync(accountEmail, request.PaymentMethod ?? "", ct);
        if (payAssetForCurrency is { Deleted: true })
            return DeletedAssetResult;
        string currency = payAssetForCurrency?.Balance.Currency ?? fe.Amount.Currency;
        string? myDepositAsset = requiresDepositAsset ? request.MyDepositAsset : null;

        var updateResult = fe.Update(request.MainClass, request.SubClass, request.Content,
            Math.Abs(request.Amount), currency, request.PaymentMethod, myDepositAsset,
            request.DepositMonth, request.DepositDay, request.MaturityDate, request.Note);

        if (updateResult.IsError) return ServiceResult.FromError(updateResult.FirstError);

        await _fixedExpenditureRepository.UpdateEntityAsync(fe, ct);
        return ServiceResult.Ok();
    }

    /// <inheritdoc />
    public async Task<ServiceResult> DeleteAsync(string accountEmail, long id, CancellationToken ct = default)
    {
        FixedExpenditure? fe = await _fixedExpenditureRepository.FindByEmailAndIdAsync(accountEmail, id, ct);
        if (fe == null)
            return ServiceResult.NotFound("FixedExpenditure.NotFound", "The fixed-expenditure record could not be found.");

        await _fixedExpenditureRepository.DeleteByIdAsync(fe.Id, ct);
        return ServiceResult.Ok();
    }

    private static ServiceResult SameAssetResult => ServiceResult.Fail(
        "The PaymentMethod and MyDepositAsset value cannot be the same.");

    private static ServiceResult CurrencyMismatchResult => ServiceResult.Fail(
        "PaymentMethod MonetaryUnit must be same as MyDepositAsset MonetaryUnit.");

    private static ServiceResult AssetNotFoundResult => ServiceResult.NotFound(
        "FixedExpenditure.AssetNotFound", "The selected asset could not be found.");

    private static ServiceResult DeletedAssetResult => ServiceResult.Conflict(
        "FixedExpenditure.AssetDeleted", "Actions cannot be executed with assets that have already been deleted.");

    /// <summary>
    /// Validates the MainClass/SubClass combination and the deposit month/day of a fixed-expenditure
    /// request, and reports whether the entry requires a deposit asset (savings / debt-repayment
    /// categories) as the success value. The maturity date is already a parsed <see cref="DateTime"/>
    /// here — the controller enforces its yyyy-MM-dd string format via ParseExact.
    /// </summary>
    private static ErrorOr<bool> ValidateClassAndDate(FixedExpenditureRequest req)
    {
        var classResult = ExpenditureClassPolicy.Validate(req.MainClass, req.SubClass);
        if (classResult.IsError) return classResult.FirstError;
        bool requiresDeposit = classResult.Value;

        if (req.DepositMonth is < 1 or > 12)
            return Error.Validation("FixedExpenditure.DepositMonth", "Deposit month must be between 1 and 12.");

        int maxDay = FixedSchedulePolicy.MaxDepositDay(req.DepositMonth);
        if (req.DepositDay < 1 || req.DepositDay > maxDay)
            return Error.Validation("FixedExpenditure.DepositDay",
                $"Deposit day must be between 1 and {maxDay} for month {req.DepositMonth}.");

        return requiresDeposit;
    }
}
