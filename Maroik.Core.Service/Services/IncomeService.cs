using ErrorOr;
using Maroik.Core.Domain.Finance;
using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Service.Extensions;
using Maroik.Core.Service.Mappers;
using Microsoft.Extensions.Logging;

namespace Maroik.Core.Service.Services;

/// <summary>
/// Implementation of <see cref="IIncomeService"/> for managing income transaction records.
/// Asset ownership is verified before create/update operations to prevent cross-account data access.
/// All write operations are wrapped in a database transaction via <see cref="IUnitOfWork"/>.
/// </summary>
public class IncomeService : IIncomeService
{
    private readonly IIncomeRepository _incomeRepository;
    private readonly IAssetBalanceDomainService _assetBalance;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<IncomeService> _logger;

    /// <summary>Initializes a new instance of <see cref="IncomeService"/> with the supplied dependencies.</summary>
    public IncomeService(IIncomeRepository incomeRepository, IAssetBalanceDomainService assetBalance, IUnitOfWork unitOfWork, ILogger<IncomeService> logger)
    {
        _incomeRepository = incomeRepository;
        _assetBalance = assetBalance;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<List<IncomeResponse>> GetIncomesAsync(string accountEmail, CancellationToken ct = default)
        => (await _incomeRepository.GetByAccountEmailAsync(accountEmail, ct)).Select(IncomeMapper.ToResponse).ToList();

    /// <inheritdoc />
    public async Task<IncomeResponse?> GetByIdAsync(string accountEmail, long id, CancellationToken ct = default)
    {
        var income = await _incomeRepository.FindByEmailAndIdAsync(accountEmail, id, ct);
        return income == null ? null : IncomeMapper.ToResponse(income);
    }

    /// <inheritdoc />
    public async Task<ServiceResult> CreateAsync(string accountEmail, IncomeRequest request, CancellationToken ct = default)
    {
        await _unitOfWork.BeginAsync(ct);
        try
        {
            var classResult = IncomeClassPolicy.Validate(request.MainClass, request.SubClass);
            if (classResult.IsError)
                return await FailAsync(classResult.FirstError, ct);

            Asset? depositAsset = await _assetBalance.GetAssetAsync(accountEmail, request.DepositMyAssetProductName ?? "", ct);
            if (depositAsset == null)
                return await FailAsync(ServiceResult.NotFound("Income.AssetNotFound", "The selected asset could not be found."), ct);
            if (depositAsset.Deleted)
                return await FailAsync(DeletedAssetResult, ct);

            string currency = depositAsset.Balance.Currency;
            var incomeResult = Income.Record(accountEmail, request.MainClass, request.SubClass,
                request.Content, Math.Abs(request.Amount), currency, request.DepositMyAssetProductName, request.Note, request.Created);
            if (incomeResult.IsError)
                return await FailAsync(incomeResult.FirstError, ct);

            var income = incomeResult.Value;
            await _incomeRepository.CreateAsync(income, ct);

            var depositResult = depositAsset.Deposit(income.Amount);
            if (depositResult.IsError)
                return await FailAsync(depositResult.FirstError, ct);
            await _assetBalance.SaveAsync(depositAsset, ct);

            await _unitOfWork.CommitAsync(ct);
            return ServiceResult.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create income for account {AccountEmail}", accountEmail);
            await _unitOfWork.RollbackAsync(ct);
            return UnexpectedFailure;
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResult> UpdateAsync(string accountEmail, IncomeRequest request, CancellationToken ct = default)
    {
        await _unitOfWork.BeginAsync(ct);
        try
        {
            var classResult = IncomeClassPolicy.Validate(request.MainClass, request.SubClass);
            if (classResult.IsError)
                return await FailAsync(classResult.FirstError, ct);

            // FOR UPDATE: lock the record for the rest of the transaction so a concurrent
            // UpdateAsync/DeleteAsync of the same income cannot read a stale amount here and
            // drift the linked asset balance.
            Income? previous = await _incomeRepository.FindByEmailAndIdForUpdateAsync(accountEmail, request.Id, ct);
            if (previous == null)
                return await FailAsync(ServiceResult.NotFound("Income.NotFound", "The income record could not be found."), ct);

            // Lock the previous and new deposit assets in a single ordinal-ordered batch so
            // concurrent transactions on the same asset pair (here or in ExpenditureService)
            // cannot deadlock on each other.
            Dictionary<string, Asset> touchedAssets = await _assetBalance.GetAssetsOrdinalAsync(accountEmail,
                [previous.DepositMyAssetProductName, request.DepositMyAssetProductName], ct);

            Asset? prevAsset = !string.IsNullOrEmpty(previous.DepositMyAssetProductName) && touchedAssets.TryGetValue(previous.DepositMyAssetProductName, out var pa) ? pa : null;
            if (prevAsset?.Deleted == true)
                return await FailAsync(DeletedAssetResult, ct);

            Asset? newAsset = !string.IsNullOrEmpty(request.DepositMyAssetProductName) && touchedAssets.TryGetValue(request.DepositMyAssetProductName, out var na) ? na : null;
            if (!string.IsNullOrEmpty(request.DepositMyAssetProductName) && newAsset == null)
                return await FailAsync(ServiceResult.NotFound("Income.AssetNotFound", "The selected asset could not be found."), ct);
            // A record must not be retargeted onto a since-archived asset, matching Create: a
            // deleted asset's balance must never be touched.
            if (newAsset?.Deleted == true)
                return await FailAsync(DeletedAssetResult, ct);
            string newCurrency = newAsset?.Balance.Currency ?? prevAsset?.Balance.Currency ?? "";

            var oldSnapshot = new IncomeSnapshot(previous.DepositMyAssetProductName, previous.Amount.Amount);

            var updateResult = previous.Update(request.MainClass, request.SubClass, request.Content,
                Math.Abs(request.Amount), newCurrency, request.DepositMyAssetProductName, request.Note, request.Created);
            if (updateResult.IsError)
                return await FailAsync(updateResult.FirstError, ct);

            await _incomeRepository.UpdateEntityAsync(previous, ct);
            var adjustResult = await AdjustAssetBalancesAsync(accountEmail, oldSnapshot,
                new IncomeSnapshot(previous.DepositMyAssetProductName, previous.Amount.Amount), ct);
            if (adjustResult.IsError)
                return await FailAsync(adjustResult.FirstError, ct);

            await _unitOfWork.CommitAsync(ct);
            return ServiceResult.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update income {IncomeId} for account {AccountEmail}", request.Id, accountEmail);
            await _unitOfWork.RollbackAsync(ct);
            return UnexpectedFailure;
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResult> DeleteAsync(string accountEmail, long id, CancellationToken ct = default)
    {
        await _unitOfWork.BeginAsync(ct);
        try
        {
            // FOR UPDATE: see UpdateAsync — lock the record so a concurrent edit/delete of the
            // same income cannot reverse its asset impact twice or from a stale amount.
            Income? income = await _incomeRepository.FindByEmailAndIdForUpdateAsync(accountEmail, id, ct);
            if (income == null)
                return await FailAsync(ServiceResult.NotFound("Income.NotFound", "The income record could not be found."), ct);

            // An income referencing a since-archived (soft-deleted) asset must not be deletable,
            // matching Create/Update: a deleted asset's balance must not be touched.
            Dictionary<string, Asset> assetsToCheck = await _assetBalance.GetAssetsOrdinalAsync(
                accountEmail, [income.DepositMyAssetProductName], ct);
            if (!string.IsNullOrEmpty(income.DepositMyAssetProductName) &&
                assetsToCheck.TryGetValue(income.DepositMyAssetProductName, out var assetToCheck) && assetToCheck.Deleted)
                return await FailAsync(DeletedAssetResult, ct);

            var adjustResult = await AdjustAssetBalancesAsync(accountEmail,
                new IncomeSnapshot(income.DepositMyAssetProductName, income.Amount.Amount), null, ct);
            if (adjustResult.IsError)
                return await FailAsync(adjustResult.FirstError, ct);

            await _incomeRepository.DeleteByIdAsync(income.Id, ct);

            await _unitOfWork.CommitAsync(ct);
            return ServiceResult.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete income {IncomeId} for account {AccountEmail}", id, accountEmail);
            await _unitOfWork.RollbackAsync(ct);
            return UnexpectedFailure;
        }
    }

    private static ServiceResult DeletedAssetResult => ServiceResult.Conflict(
        "Income.AssetDeleted", "Actions cannot be executed with assets that have already been deleted.");

    private static ServiceResult UnexpectedFailure => ServiceResult.Failure(
        "Income.Unexpected", "The operation could not be completed. Please try again.");

    /// <summary>Rolls back the current transaction and returns <paramref name="result"/>.</summary>
    private async Task<ServiceResult> FailAsync(ServiceResult result, CancellationToken ct)
    {
        await _unitOfWork.RollbackAsync(ct);
        return result;
    }

    /// <summary>Rolls back the current transaction and returns a failure mapped from <paramref name="error"/>.</summary>
    private Task<ServiceResult> FailAsync(Error error, CancellationToken ct) => FailAsync(ServiceResult.FromError(error), ct);

    /// <summary>
    /// Reverts <paramref name="toRevert"/>'s asset impact and applies <paramref name="toApply"/>'s asset impact.
    /// Pass null for toApply when deleting.
    /// </summary>
    private async Task<ErrorOr<Success>> AdjustAssetBalancesAsync(string accountEmail, IncomeSnapshot toRevert, IncomeSnapshot? toApply, CancellationToken ct = default)
    {
        List<AssetBalanceAdjustment> adjustments = [new(toRevert.DepositAssetName, toRevert.Amount, IsDeposit: false)];
        if (toApply.HasValue)
            adjustments.Add(new(toApply.Value.DepositAssetName, toApply.Value.Amount, IsDeposit: true));

        return await _assetBalance.ApplyAssetBalanceAdjustmentsAsync(
            accountEmail, adjustments, "Income.AssetNotFound", "The asset referenced by this income could not be found.", ct);
    }

    /// <summary>
    /// Captures the asset-related fields of an <see cref="Income"/> at a point in time so that
    /// the corresponding asset balance can be correctly reversed during update or delete operations.
    /// </summary>
    private readonly record struct IncomeSnapshot(string? DepositAssetName, decimal Amount);
}
