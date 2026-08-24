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
            {
                await _unitOfWork.RollbackAsync(ct);
                return ServiceResult.Fail("Input is invalid");
            }

            bool isTransfer = classResult.Value;
            if (isTransfer && request.PaymentMethod == request.MyDepositAsset)
            {
                await _unitOfWork.RollbackAsync(ct);
                return ServiceResult.Fail("The PaymentMethod and MyDepositAsset value cannot be the same.");
            }

            // GetAssetsOrdinalAsync takes row locks held until commit/rollback, always in ordinal
            // name order, so that concurrent transactions touching the same asset pair (here,
            // elsewhere in this class, or in IncomeService) cannot deadlock on each other.
            Dictionary<string, Asset> touchedAssets = await _assetBalance.GetAssetsOrdinalAsync(
                accountEmail, [request.PaymentMethod, isTransfer ? request.MyDepositAsset : null], ct);

            Asset? paymentAsset = !string.IsNullOrEmpty(request.PaymentMethod) && touchedAssets.TryGetValue(request.PaymentMethod, out var pay) ? pay : null;
            Asset? depositAsset = isTransfer && !string.IsNullOrEmpty(request.MyDepositAsset) && touchedAssets.TryGetValue(request.MyDepositAsset, out var dep) ? dep : null;

            if (paymentAsset == null)
            {
                await _unitOfWork.RollbackAsync(ct);
                return ServiceResult.Fail("Input is invalid");
            }
            if (paymentAsset.Deleted)
            {
                await _unitOfWork.RollbackAsync(ct);
                return ServiceResult.Fail("Actions cannot be executed with assets that have already been deleted.");
            }

            if (isTransfer)
            {
                if (depositAsset == null)
                {
                    await _unitOfWork.RollbackAsync(ct);
                    return ServiceResult.Fail("Input is invalid");
                }
                if (depositAsset.Deleted)
                {
                    await _unitOfWork.RollbackAsync(ct);
                    return ServiceResult.Fail("Actions cannot be executed with assets that have already been deleted.");
                }
                if (paymentAsset.Balance.Currency != depositAsset.Balance.Currency)
                {
                    await _unitOfWork.RollbackAsync(ct);
                    return ServiceResult.Fail("PaymentMethod MonetaryUnit must be same as MyDepositAsset MonetaryUnit.");
                }
            }

            string currency = paymentAsset.Balance.Currency;
            string? myDepositAsset = isTransfer ? request.MyDepositAsset : "";

            var expenditureResult = Expenditure.Record(accountEmail, request.MainClass, request.SubClass,
                request.Content, Math.Abs(request.Amount), currency, request.PaymentMethod, myDepositAsset, request.Note);
            if (expenditureResult.IsError)
            {
                await _unitOfWork.RollbackAsync(ct);
                return ServiceResult.Fail(expenditureResult.FirstError.Description);
            }

            var expenditure = expenditureResult.Value;
            await _expenditureRepository.CreateAsync(expenditure, ct);

            var withdrawResult = paymentAsset.Withdraw(expenditure.Amount.Amount, currency);
            if (withdrawResult.IsError)
            {
                await _unitOfWork.RollbackAsync(ct);
                return ServiceResult.Fail(withdrawResult.FirstError.Description);
            }
            await _assetBalance.SaveAsync(paymentAsset, ct);

            if (depositAsset != null)
            {
                var depositResult = depositAsset.Deposit(expenditure.Amount.Amount, currency);
                if (depositResult.IsError)
                {
                    await _unitOfWork.RollbackAsync(ct);
                    return ServiceResult.Fail(depositResult.FirstError.Description);
                }
                await _assetBalance.SaveAsync(depositAsset, ct);
            }

            await _unitOfWork.CommitAsync(ct);
            return ServiceResult.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create expenditure for account {AccountEmail}", accountEmail);
            await _unitOfWork.RollbackAsync(ct);
            return ServiceResult.Fail("Input is invalid");
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
            {
                await _unitOfWork.RollbackAsync(ct);
                return ServiceResult.Fail("Input is invalid");
            }

            if (classResult.Value && request.PaymentMethod == request.MyDepositAsset)
            {
                await _unitOfWork.RollbackAsync(ct);
                return ServiceResult.Fail("The PaymentMethod and MyDepositAsset value cannot be the same.");
            }

            List<Expenditure> expenditures = await _expenditureRepository.GetByAccountEmailAsync(accountEmail, ct);
            Expenditure? previous = expenditures.FirstOrDefault(x => x.Id == request.Id);
            if (previous == null)
            {
                await _unitOfWork.RollbackAsync(ct);
                return ServiceResult.Fail("Input is invalid");
            }

            // Lock every asset this update can touch (old + new payment/deposit) in a single
            // ordinal-ordered batch so concurrent transactions on the same asset pair cannot deadlock.
            Dictionary<string, Asset> touchedAssets = await _assetBalance.GetAssetsOrdinalAsync(accountEmail,
                [request.PaymentMethod, classResult.Value ? request.MyDepositAsset : null, previous.PaymentMethod, previous.MyDepositAsset], ct);

            Asset? newPayAsset = !string.IsNullOrEmpty(request.PaymentMethod) && touchedAssets.TryGetValue(request.PaymentMethod, out var np) ? np : null;

            if (classResult.Value)
            {
                Asset? newDepAsset = !string.IsNullOrEmpty(request.MyDepositAsset) && touchedAssets.TryGetValue(request.MyDepositAsset, out var nd) ? nd : null;
                if (newPayAsset == null || newDepAsset == null)
                {
                    await _unitOfWork.RollbackAsync(ct);
                    return ServiceResult.Fail("Input is invalid");
                }
                if (newPayAsset.Balance.Currency != newDepAsset.Balance.Currency)
                {
                    await _unitOfWork.RollbackAsync(ct);
                    return ServiceResult.Fail("PaymentMethod MonetaryUnit must be same as MyDepositAsset MonetaryUnit.");
                }
            }

            Asset? prevPayAsset = !string.IsNullOrEmpty(previous.PaymentMethod) && touchedAssets.TryGetValue(previous.PaymentMethod, out var pp) ? pp : null;
            if (prevPayAsset?.Deleted == true)
            {
                await _unitOfWork.RollbackAsync(ct);
                return ServiceResult.Fail("Actions cannot be executed with assets that have already been deleted.");
            }
            if (!string.IsNullOrEmpty(previous.MyDepositAsset))
            {
                Asset? prevDepAsset = touchedAssets.GetValueOrDefault(previous.MyDepositAsset);
                if (prevDepAsset?.Deleted == true)
                {
                    await _unitOfWork.RollbackAsync(ct);
                    return ServiceResult.Fail("Actions cannot be executed with assets that have already been deleted.");
                }
            }

            string newCurrency = newPayAsset?.Balance.Currency ?? prevPayAsset?.Balance.Currency ?? "";
            string? myDepositAsset = classResult.Value ? request.MyDepositAsset : "";

            var oldSnapshot = new ExpenditureSnapshot(previous.PaymentMethod, previous.MyDepositAsset, previous.Amount.Amount);

            var updateResult = previous.Update(request.MainClass, request.SubClass, request.Content,
                Math.Abs(request.Amount), newCurrency, request.PaymentMethod, myDepositAsset, request.Note);
            if (updateResult.IsError)
            {
                await _unitOfWork.RollbackAsync(ct);
                return ServiceResult.Fail(updateResult.FirstError.Description);
            }

            await _expenditureRepository.UpdateEntityAsync(previous, ct);
            var adjustResult = await AdjustAssetBalancesAsync(accountEmail, oldSnapshot,
                new ExpenditureSnapshot(previous.PaymentMethod, previous.MyDepositAsset, previous.Amount.Amount), ct);
            if (adjustResult.IsError)
            {
                await _unitOfWork.RollbackAsync(ct);
                return ServiceResult.Fail(adjustResult.FirstError.Description);
            }

            await _unitOfWork.CommitAsync(ct);
            return ServiceResult.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update expenditure {ExpenditureId} for account {AccountEmail}", request.Id, accountEmail);
            await _unitOfWork.RollbackAsync(ct);
            return ServiceResult.Fail("Input is invalid");
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
            {
                await _unitOfWork.RollbackAsync(ct);
                return ServiceResult.Fail("Input is invalid");
            }

            // An expenditure referencing a since-archived (soft-deleted) asset must not be
            // deletable, matching Create/Update: a deleted asset's balance must not be touched.
            Dictionary<string, Asset> assetsToCheck = await _assetBalance.GetAssetsOrdinalAsync(
                accountEmail, [expenditure.PaymentMethod, expenditure.MyDepositAsset], ct);
            if (!string.IsNullOrEmpty(expenditure.PaymentMethod) &&
                assetsToCheck.TryGetValue(expenditure.PaymentMethod, out var payAssetToCheck) && payAssetToCheck.Deleted)
            {
                await _unitOfWork.RollbackAsync(ct);
                return ServiceResult.Fail("Actions cannot be executed with assets that have already been deleted.");
            }
            if (!string.IsNullOrEmpty(expenditure.MyDepositAsset) &&
                assetsToCheck.TryGetValue(expenditure.MyDepositAsset, out var depAssetToCheck) && depAssetToCheck.Deleted)
            {
                await _unitOfWork.RollbackAsync(ct);
                return ServiceResult.Fail("Actions cannot be executed with assets that have already been deleted.");
            }

            var adjustResult = await AdjustAssetBalancesAsync(accountEmail,
                new ExpenditureSnapshot(expenditure.PaymentMethod, expenditure.MyDepositAsset, expenditure.Amount.Amount),
                null, ct);
            if (adjustResult.IsError)
            {
                await _unitOfWork.RollbackAsync(ct);
                return ServiceResult.Fail(adjustResult.FirstError.Description);
            }

            await _expenditureRepository.DeleteByIdAsync(expenditure.Id, ct);

            await _unitOfWork.CommitAsync(ct);
            return ServiceResult.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete expenditure {ExpenditureId} for account {AccountEmail}", id, accountEmail);
            await _unitOfWork.RollbackAsync(ct);
            return ServiceResult.Fail("Input is invalid");
        }
    }

    /// <summary>
    /// Reverts <paramref name="toRevert"/>'s asset impact and applies <paramref name="toApply"/>'s asset impact.
    /// Pass null for toApply when deleting.
    /// Uses a dictionary to batch all asset fetches and avoid double-fetch issues.
    /// </summary>
    private async Task<ErrorOr<Success>> AdjustAssetBalancesAsync(string accountEmail, ExpenditureSnapshot toRevert, ExpenditureSnapshot? toApply, CancellationToken ct = default)
    {
        Dictionary<string, Asset> assets = await _assetBalance.GetAssetsOrdinalAsync(accountEmail,
            [toRevert.PaymentMethod, toRevert.MyDepositAsset, toApply?.PaymentMethod, toApply?.MyDepositAsset], ct);

        if (!string.IsNullOrEmpty(toRevert.PaymentMethod) && assets.TryGetValue(toRevert.PaymentMethod, out var rp))
        {
            var result = rp.Deposit(Math.Abs(toRevert.Amount), rp.Balance.Currency);
            if (result.IsError) return result.Errors;
        }

        if (!string.IsNullOrEmpty(toRevert.MyDepositAsset) && assets.TryGetValue(toRevert.MyDepositAsset, out var rd))
        {
            var result = rd.Withdraw(Math.Abs(toRevert.Amount), rd.Balance.Currency);
            if (result.IsError) return result.Errors;
        }

        if (toApply.HasValue)
        {
            if (!string.IsNullOrEmpty(toApply.Value.PaymentMethod) && assets.TryGetValue(toApply.Value.PaymentMethod, out var ap))
            {
                var result = ap.Withdraw(Math.Abs(toApply.Value.Amount), ap.Balance.Currency);
                if (result.IsError) return result.Errors;
            }

            if (!string.IsNullOrEmpty(toApply.Value.MyDepositAsset) && assets.TryGetValue(toApply.Value.MyDepositAsset, out var ad))
            {
                var result = ad.Deposit(Math.Abs(toApply.Value.Amount), ad.Balance.Currency);
                if (result.IsError) return result.Errors;
            }
        }

        foreach (var asset in assets.Values)
            await _assetBalance.SaveAsync(asset, ct);

        return Result.Success;
    }

    /// <summary>
    /// Captures the asset-related fields of an <see cref="Expenditure"/> at a point in time so that
    /// the corresponding asset balances can be correctly reversed during update or delete operations.
    /// </summary>
    private readonly record struct ExpenditureSnapshot(string? PaymentMethod, string? MyDepositAsset, decimal Amount);
}
