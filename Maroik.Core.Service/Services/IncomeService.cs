using Maroik.Core.Contract.Domains.Finance;
using Maroik.Core.Contract.Filters;
using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Service.Mappers;

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

    /// <summary>Initializes a new instance of <see cref="IncomeService"/> with the supplied dependencies.</summary>
    public IncomeService(IIncomeRepository incomeRepository, IAssetBalanceDomainService assetBalance, IUnitOfWork unitOfWork)
    {
        _incomeRepository = incomeRepository;
        _assetBalance = assetBalance;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<List<IncomeResponse>> GetIncomesAsync(string accountEmail, CancellationToken ct = default)
        => (await _incomeRepository.FindAsync([FilterCondition.Equal("AccountEmail", accountEmail)], ct: ct)).Select(IncomeMapper.ToResponse).ToList();

    /// <inheritdoc />
    public async Task<IncomeResponse?> GetByIdAsync(string accountEmail, long id, CancellationToken ct = default)
    {
        var results = await _incomeRepository.FindAsync([
            FilterCondition.Equal("AccountEmail", accountEmail),
            FilterCondition.Equal("Id", id)
        ], ct: ct);
        var income = results.FirstOrDefault();
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
                await _unitOfWork.RollbackAsync();
                return ServiceResult.Fail("Input is invalid");
            }

            Asset? depositAsset = await _assetBalance.GetAssetAsync(accountEmail, request.DepositMyAssetProductName ?? "", ct);
            if (depositAsset == null)
            {
                await _unitOfWork.RollbackAsync();
                return ServiceResult.Fail("Input is invalid");
            }
            if (depositAsset.Deleted)
            {
                await _unitOfWork.RollbackAsync();
                return ServiceResult.Fail("Actions cannot be executed with assets that have already been deleted.");
            }

            string currency = depositAsset.Balance.Currency;
            var incomeResult = Income.Record(accountEmail, request.MainClass, request.SubClass,
                request.Content, Math.Abs(request.Amount), currency, request.DepositMyAssetProductName, request.Note);
            if (incomeResult.IsError)
            {
                await _unitOfWork.RollbackAsync();
                return ServiceResult.Fail(incomeResult.FirstError.Description);
            }

            var income = incomeResult.Value;
            await _incomeRepository.CreateAsync(income, ct);

            depositAsset.Deposit(income.Amount.Amount, currency);
            await _assetBalance.SaveAsync(depositAsset, ct);

            await _unitOfWork.CommitAsync(ct);
            return ServiceResult.Ok();
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
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
                await _unitOfWork.RollbackAsync();
                return ServiceResult.Fail("Input is invalid");
            }

            List<Income> incomes = await _incomeRepository.FindAsync([FilterCondition.Equal("AccountEmail", accountEmail)], ct: ct);
            Income? previous = incomes.FirstOrDefault(x => x.Id == request.Id);
            if (previous == null)
            {
                await _unitOfWork.RollbackAsync();
                return ServiceResult.Fail("Input is invalid");
            }

            Asset? prevAsset = await _assetBalance.GetAssetAsync(accountEmail, previous.DepositMyAssetProductName ?? "", ct);
            if (prevAsset?.Deleted == true)
            {
                await _unitOfWork.RollbackAsync();
                return ServiceResult.Fail("Actions cannot be executed with assets that have already been deleted.");
            }

            Asset? newAsset = await _assetBalance.GetAssetAsync(accountEmail, request.DepositMyAssetProductName ?? "", ct);
            string newCurrency = newAsset?.Balance.Currency ?? prevAsset?.Balance.Currency ?? string.Empty;

            string previousAssetName = previous.DepositMyAssetProductName ?? "";
            decimal previousAmount = previous.Amount.Amount;
            string previousCurrency = prevAsset?.Balance.Currency ?? string.Empty;

            var updateResult = previous.Update(request.MainClass, request.SubClass, request.Content,
                Math.Abs(request.Amount), newCurrency, request.DepositMyAssetProductName, request.Note);
            if (updateResult.IsError)
            {
                await _unitOfWork.RollbackAsync();
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
        catch
        {
            await _unitOfWork.RollbackAsync();
            return ServiceResult.Fail("Input is invalid");
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResult> DeleteAsync(string accountEmail, long id, CancellationToken ct = default)
    {
        await _unitOfWork.BeginAsync(ct);
        try
        {
            List<Income> incomes = await _incomeRepository.FindAsync([FilterCondition.Equal("AccountEmail", accountEmail)], ct: ct);
            Income? income = incomes.FirstOrDefault(x => x.Id == id);
            if (income == null)
            {
                await _unitOfWork.RollbackAsync();
                return ServiceResult.Fail("Input is invalid");
            }

            Asset? asset = await _assetBalance.GetAssetAsync(accountEmail, income.DepositMyAssetProductName ?? "", ct);
            if (asset?.Deleted == true)
            {
                await _unitOfWork.RollbackAsync();
                return ServiceResult.Fail("Actions cannot be executed with assets that have already been deleted.");
            }

            if (asset != null)
            {
                asset.Withdraw(Math.Abs(income.Amount.Amount), asset.Balance.Currency);
                await _assetBalance.SaveAsync(asset, ct);
            }

            await _incomeRepository.DeleteAsync([FilterCondition.Equal("Id", income.Id)], ct);

            await _unitOfWork.CommitAsync(ct);
            return ServiceResult.Ok();
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            return ServiceResult.Fail("Input is invalid");
        }
    }
}
