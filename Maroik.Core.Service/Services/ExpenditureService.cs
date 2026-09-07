using ErrorOr;
using Maroik.Core.Domain.Finance;
using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Service.Extensions;
using Maroik.Core.Service.Mappers;
using Microsoft.Extensions.Logging;

namespace Maroik.Core.Service.Services;

/// <summary>
/// Implementation of <see cref="IExpenditureService"/> for managing expenditure transaction records.
/// Asset ownership is verified before create/update operations to prevent cross-account data access.
/// All write operations are wrapped in a database transaction via <see cref="IUnitOfWork"/>.
/// </summary>
public class ExpenditureService : IExpenditureService
{
    private readonly IExpenditureRepository _expenditureRepository;
    private readonly IAssetBalanceDomainService _assetBalance;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ExpenditureService> _logger;

    /// <summary>Initializes a new instance of <see cref="ExpenditureService"/> with the supplied dependencies.</summary>
    public ExpenditureService(
        IExpenditureRepository expenditureRepository,
        IAssetBalanceDomainService assetBalance,
        IUnitOfWork unitOfWork,
        ILogger<ExpenditureService> logger)
    {
        _expenditureRepository = expenditureRepository;
        _assetBalance = assetBalance;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<List<ExpenditureResponse>> GetExpendituresAsync(string accountEmail, CancellationToken ct = default)
        => (await _expenditureRepository.GetByAccountEmailAsync(accountEmail, ct)).Select(ExpenditureMapper.ToResponse).ToList();

    /// <inheritdoc />
    public async Task<ExpenditureResponse?> GetByIdAsync(string accountEmail, long id, CancellationToken ct = default)
    {
        var expenditure = await _expenditureRepository.FindByEmailAndIdAsync(accountEmail, id, ct);
        return expenditure == null ? null : ExpenditureMapper.ToResponse(expenditure);
    }

    /// <inheritdoc />
    public async Task<ServiceResult> CreateAsync(string accountEmail, ExpenditureRequest request, CancellationToken ct = default)
    {
        await _unitOfWork.BeginAsync(ct);
        try
        {
            var classResult = ExpenditureClassPolicy.Validate(request.MainClass, request.SubClass);
            if (classResult.IsError)
                return await FailAsync("Input is invalid", ct);

            bool isTransfer = classResult.Value;
            if (isTransfer && request.PaymentMethod == request.MyDepositAsset)
                return await FailAsync("The PaymentMethod and MyDepositAsset value cannot be the same.", ct);

            // GetAssetsOrdinalAsync takes row locks held until commit/rollback, always in ordinal
            // name order, so that concurrent transactions touching the same asset pair (here,
            // elsewhere in this class, or in IncomeService) cannot deadlock on each other.
            Dictionary<string, Asset> touchedAssets = await _assetBalance.GetAssetsOrdinalAsync(
                accountEmail, [request.PaymentMethod, isTransfer ? request.MyDepositAsset : null], ct);

            Asset? paymentAsset = !string.IsNullOrEmpty(request.PaymentMethod) && touchedAssets.TryGetValue(request.PaymentMethod, out var pay) ? pay : null;
            Asset? depositAsset = isTransfer && !string.IsNullOrEmpty(request.MyDepositAsset) && touchedAssets.TryGetValue(request.MyDepositAsset, out var dep) ? dep : null;

            if (paymentAsset == null)
                return await FailAsync("Input is invalid", ct);
            if (paymentAsset.Deleted)
                return await FailAsync("Actions cannot be executed with assets that have already been deleted.", ct);

            if (isTransfer)
            {
                if (depositAsset == null)
                    return await FailAsync("Input is invalid", ct);
                if (depositAsset.Deleted)
                    return await FailAsync("Actions cannot be executed with assets that have already been deleted.", ct);
                if (paymentAsset.Balance.Currency != depositAsset.Balance.Currency)
                    return await FailAsync("PaymentMethod MonetaryUnit must be same as MyDepositAsset MonetaryUnit.", ct);
            }

            string currency = paymentAsset.Balance.Currency;
            string? myDepositAsset = isTransfer ? request.MyDepositAsset : null;

            var expenditureResult = Expenditure.Record(accountEmail, request.MainClass, request.SubClass,
                request.Content, Math.Abs(request.Amount), currency, request.PaymentMethod, myDepositAsset, request.Note, request.Created);
            if (expenditureResult.IsError)
                return await FailAsync(expenditureResult.FirstError.Description, ct);

            var expenditure = expenditureResult.Value;
            await _expenditureRepository.CreateAsync(expenditure, ct);

            var withdrawResult = paymentAsset.Withdraw(expenditure.Amount);
            if (withdrawResult.IsError)
                return await FailAsync(withdrawResult.FirstError.Description, ct);
            await _assetBalance.SaveAsync(paymentAsset, ct);

            if (depositAsset != null)
            {
                var depositResult = depositAsset.Deposit(expenditure.Amount);
                if (depositResult.IsError)
                    return await FailAsync(depositResult.FirstError.Description, ct);
                await _assetBalance.SaveAsync(depositAsset, ct);
            }

            await _unitOfWork.CommitAsync(ct);
            return ServiceResult.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create expenditure for account {AccountEmail}", accountEmail);
            return await FailAsync("Input is invalid", ct);
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResult> UpdateAsync(string accountEmail, ExpenditureRequest request, CancellationToken ct = default)
    {
        await _unitOfWork.BeginAsync(ct);
        try
        {
            var classResult = ExpenditureClassPolicy.Validate(request.MainClass, request.SubClass);
            if (classResult.IsError)
                return await FailAsync("Input is invalid", ct);

            if (classResult.Value && request.PaymentMethod == request.MyDepositAsset)
                return await FailAsync("The PaymentMethod and MyDepositAsset value cannot be the same.", ct);

            List<Expenditure> expenditures = await _expenditureRepository.GetByAccountEmailAsync(accountEmail, ct);
            Expenditure? previous = expenditures.FirstOrDefault(x => x.Id == request.Id);
            if (previous == null)
                return await FailAsync("Input is invalid", ct);

            // Lock every asset this update can touch (old + new payment/deposit) in a single
            // ordinal-ordered batch so concurrent transactions on the same asset pair cannot deadlock.
            Dictionary<string, Asset> touchedAssets = await _assetBalance.GetAssetsOrdinalAsync(accountEmail,
                [request.PaymentMethod, classResult.Value ? request.MyDepositAsset : null, previous.PaymentMethod, previous.MyDepositAsset], ct);

            Asset? newPayAsset = !string.IsNullOrEmpty(request.PaymentMethod) && touchedAssets.TryGetValue(request.PaymentMethod, out var np) ? np : null;

            if (classResult.Value)
            {
                Asset? newDepAsset = !string.IsNullOrEmpty(request.MyDepositAsset) && touchedAssets.TryGetValue(request.MyDepositAsset, out var nd) ? nd : null;
                if (newPayAsset == null || newDepAsset == null)
                    return await FailAsync("Input is invalid", ct);
                if (newPayAsset.Balance.Currency != newDepAsset.Balance.Currency)
                    return await FailAsync("PaymentMethod MonetaryUnit must be same as MyDepositAsset MonetaryUnit.", ct);
            }

            Asset? prevPayAsset = !string.IsNullOrEmpty(previous.PaymentMethod) && touchedAssets.TryGetValue(previous.PaymentMethod, out var pp) ? pp : null;
            if (prevPayAsset?.Deleted == true)
                return await FailAsync("Actions cannot be executed with assets that have already been deleted.", ct);
            if (!string.IsNullOrEmpty(previous.MyDepositAsset))
            {
                Asset? prevDepAsset = touchedAssets.GetValueOrDefault(previous.MyDepositAsset);
                if (prevDepAsset?.Deleted == true)
                    return await FailAsync("Actions cannot be executed with assets that have already been deleted.", ct);
            }

            string newCurrency = newPayAsset?.Balance.Currency ?? prevPayAsset?.Balance.Currency ?? "";
            string? myDepositAsset = classResult.Value ? request.MyDepositAsset : null;

            var oldSnapshot = new ExpenditureSnapshot(previous.PaymentMethod, previous.MyDepositAsset, previous.Amount.Amount);

            var updateResult = previous.Update(request.MainClass, request.SubClass, request.Content,
                Math.Abs(request.Amount), newCurrency, request.PaymentMethod, myDepositAsset, request.Note, request.Created);
            if (updateResult.IsError)
                return await FailAsync(updateResult.FirstError.Description, ct);

            await _expenditureRepository.UpdateEntityAsync(previous, ct);
            var adjustResult = await AdjustAssetBalancesAsync(accountEmail, oldSnapshot,
                new ExpenditureSnapshot(previous.PaymentMethod, previous.MyDepositAsset, previous.Amount.Amount), ct);
            if (adjustResult.IsError)
                return await FailAsync(adjustResult.FirstError.Description, ct);

            await _unitOfWork.CommitAsync(ct);
            return ServiceResult.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update expenditure {ExpenditureId} for account {AccountEmail}", request.Id, accountEmail);
            return await FailAsync("Input is invalid", ct);
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResult> DeleteAsync(string accountEmail, long id, CancellationToken ct = default)
    {
        await _unitOfWork.BeginAsync(ct);
        try
        {
            List<Expenditure> expenditures = await _expenditureRepository.GetByAccountEmailAsync(accountEmail, ct);
            Expenditure? expenditure = expenditures.FirstOrDefault(x => x.Id == id);
            if (expenditure == null)
                return await FailAsync("Input is invalid", ct);

            // An expenditure referencing a since-archived (soft-deleted) asset must not be
            // deletable, matching Create/Update: a deleted asset's balance must not be touched.
            Dictionary<string, Asset> assetsToCheck = await _assetBalance.GetAssetsOrdinalAsync(
                accountEmail, [expenditure.PaymentMethod, expenditure.MyDepositAsset], ct);
            if (!string.IsNullOrEmpty(expenditure.PaymentMethod) &&
                assetsToCheck.TryGetValue(expenditure.PaymentMethod, out var payAssetToCheck) && payAssetToCheck.Deleted)
                return await FailAsync("Actions cannot be executed with assets that have already been deleted.", ct);
            if (!string.IsNullOrEmpty(expenditure.MyDepositAsset) &&
                assetsToCheck.TryGetValue(expenditure.MyDepositAsset, out var depAssetToCheck) && depAssetToCheck.Deleted)
                return await FailAsync("Actions cannot be executed with assets that have already been deleted.", ct);

            var adjustResult = await AdjustAssetBalancesAsync(accountEmail,
                new ExpenditureSnapshot(expenditure.PaymentMethod, expenditure.MyDepositAsset, expenditure.Amount.Amount),
                null, ct);
            if (adjustResult.IsError)
                return await FailAsync(adjustResult.FirstError.Description, ct);

            await _expenditureRepository.DeleteByIdAsync(expenditure.Id, ct);

            await _unitOfWork.CommitAsync(ct);
            return ServiceResult.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete expenditure {ExpenditureId} for account {AccountEmail}", id, accountEmail);
            return await FailAsync("Input is invalid", ct);
        }
    }

    /// <summary>Rolls back the current transaction and returns a failure <see cref="ServiceResult"/> with <paramref name="message"/>.</summary>
    private async Task<ServiceResult> FailAsync(string message, CancellationToken ct)
    {
        await _unitOfWork.RollbackAsync(ct);
        return ServiceResult.Fail(message);
    }

    /// <summary>
    /// Reverts <paramref name="toRevert"/>'s asset impact and applies <paramref name="toApply"/>'s asset impact.
    /// Pass null for toApply when deleting.
    /// Uses a dictionary to batch all asset fetches and avoid double-fetch issues.
    /// </summary>
    private async Task<ErrorOr<Success>> AdjustAssetBalancesAsync(string accountEmail, ExpenditureSnapshot toRevert, ExpenditureSnapshot? toApply, CancellationToken ct = default)
    {
        List<AssetBalanceAdjustment> adjustments =
        [
            new(toRevert.PaymentMethod, toRevert.Amount, IsDeposit: true),
            new(toRevert.MyDepositAsset, toRevert.Amount, IsDeposit: false)
        ];
        if (toApply.HasValue)
        {
            adjustments.Add(new(toApply.Value.PaymentMethod, toApply.Value.Amount, IsDeposit: false));
            adjustments.Add(new(toApply.Value.MyDepositAsset, toApply.Value.Amount, IsDeposit: true));
        }

        return await _assetBalance.ApplyAssetBalanceAdjustmentsAsync(
            accountEmail, adjustments, "Expenditure.AssetNotFound", "The asset referenced by this expenditure could not be found.", ct);
    }

    /// <summary>
    /// Captures the asset-related fields of an <see cref="Expenditure"/> at a point in time so that
    /// the corresponding asset balances can be correctly reversed during update or delete operations.
    /// </summary>
    private readonly record struct ExpenditureSnapshot(string? PaymentMethod, string? MyDepositAsset, decimal Amount);
}
