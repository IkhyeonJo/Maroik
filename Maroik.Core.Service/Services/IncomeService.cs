using Maroik.Core.Contract.Domains.Finance;
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
            if (IncomeClassPolicy.Validate(request.MainClass, request.SubClass).IsError)
            {
                await _unitOfWork.RollbackAsync(ct);
                return ServiceResult.Fail("Input is invalid");
            }

            Asset? depositAsset = await _assetBalance.GetAssetAsync(accountEmail, request.DepositMyAssetProductName ?? "", ct);
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

            string currency = depositAsset.Balance.Currency;
            var incomeResult = Income.Record(accountEmail, request.MainClass, request.SubClass,
                request.Content, Math.Abs(request.Amount), currency, request.DepositMyAssetProductName, request.Note);
            if (incomeResult.IsError)
            {
                await _unitOfWork.RollbackAsync(ct);
                return ServiceResult.Fail(incomeResult.FirstError.Description);
            }

            var income = incomeResult.Value;
            await _incomeRepository.CreateAsync(income, ct);

            depositAsset.Deposit(income.Amount.Amount, currency);
            await _assetBalance.SaveAsync(depositAsset, ct);

            await _unitOfWork.CommitAsync(ct);
            return ServiceResult.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create income for account {AccountEmail}", accountEmail);
            await _unitOfWork.RollbackAsync(ct);
            return ServiceResult.Fail("Input is invalid");
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResult> UpdateAsync(string accountEmail, IncomeRequest request, CancellationToken ct = default)
    {
        await _unitOfWork.BeginAsync(ct);
        try
        {
            if (IncomeClassPolicy.Validate(request.MainClass, request.SubClass).IsError)
            {
                await _unitOfWork.RollbackAsync(ct);
                return ServiceResult.Fail("Input is invalid");
            }

            List<Income> incomes = await _incomeRepository.GetByAccountEmailAsync(accountEmail, ct);
            Income? previous = incomes.FirstOrDefault(x => x.Id == request.Id);
            if (previous == null)
            {
                await _unitOfWork.RollbackAsync(ct);
                return ServiceResult.Fail("Input is invalid");
            }

            // Lock the previous and new deposit assets in a single ordinal-ordered batch so
            // concurrent transactions on the same asset pair (here or in ExpenditureService)
            // cannot deadlock on each other.
            Dictionary<string, Asset> touchedAssets = await _assetBalance.GetAssetsOrdinalAsync(accountEmail,
                [previous.DepositMyAssetProductName, request.DepositMyAssetProductName], ct);

            Asset? prevAsset = !string.IsNullOrEmpty(previous.DepositMyAssetProductName) && touchedAssets.TryGetValue(previous.DepositMyAssetProductName, out var pa) ? pa : null;
            if (prevAsset?.Deleted == true)
            {
                await _unitOfWork.RollbackAsync(ct);
                return ServiceResult.Fail("Actions cannot be executed with assets that have already been deleted.");
            }

            Asset? newAsset = !string.IsNullOrEmpty(request.DepositMyAssetProductName) && touchedAssets.TryGetValue(request.DepositMyAssetProductName, out var na) ? na : null;
            string newCurrency = newAsset?.Balance.Currency ?? prevAsset?.Balance.Currency ?? "";

            string previousAssetName = previous.DepositMyAssetProductName;
            decimal previousAmount = previous.Amount.Amount;
            string previousCurrency = prevAsset?.Balance.Currency ?? "";

            var updateResult = previous.Update(request.MainClass, request.SubClass, request.Content,
                Math.Abs(request.Amount), newCurrency, request.DepositMyAssetProductName, request.Note);
            if (updateResult.IsError)
            {
                await _unitOfWork.RollbackAsync(ct);
                return ServiceResult.Fail(updateResult.FirstError.Description);
            }

            await _incomeRepository.UpdateEntityAsync(previous, ct);

            if (previous.DepositMyAssetProductName == previousAssetName)
            {
                if (prevAsset != null)
                {
                    prevAsset.Withdraw(Math.Abs(previousAmount), previousCurrency);
                    prevAsset.Deposit(previous.Amount.Amount, previousCurrency);
                    await _assetBalance.SaveAsync(prevAsset, ct);
                }
            }
            else
            {
                if (prevAsset != null)
                {
                    prevAsset.Withdraw(Math.Abs(previousAmount), previousCurrency);
                    await _assetBalance.SaveAsync(prevAsset, ct);
                }
                if (newAsset != null)
                {
                    newAsset.Deposit(previous.Amount.Amount, newCurrency);
                    await _assetBalance.SaveAsync(newAsset, ct);
                }
            }

            await _unitOfWork.CommitAsync(ct);
            return ServiceResult.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update income {IncomeId} for account {AccountEmail}", request.Id, accountEmail);
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
            List<Income> incomes = await _incomeRepository.GetByAccountEmailAsync(accountEmail, ct);
            Income? income = incomes.FirstOrDefault(x => x.Id == id);
            if (income == null)
            {
                await _unitOfWork.RollbackAsync(ct);
                return ServiceResult.Fail("Input is invalid");
            }

            Asset? asset = await _assetBalance.GetAssetAsync(accountEmail, income.DepositMyAssetProductName, ct);
            if (asset?.Deleted == true)
            {
                await _unitOfWork.RollbackAsync(ct);
                return ServiceResult.Fail("Actions cannot be executed with assets that have already been deleted.");
            }

            if (asset != null)
            {
                asset.Withdraw(Math.Abs(income.Amount.Amount), asset.Balance.Currency);
                await _assetBalance.SaveAsync(asset, ct);
            }

            await _incomeRepository.DeleteByIdAsync(income.Id, ct);

            await _unitOfWork.CommitAsync(ct);
            return ServiceResult.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete income {IncomeId} for account {AccountEmail}", id, accountEmail);
            await _unitOfWork.RollbackAsync(ct);
            return ServiceResult.Fail("Input is invalid");
        }
    }
}
