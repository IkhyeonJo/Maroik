using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Mapster;

namespace Maroik.Core.Service.Services;

/// <summary>
/// Implementation of <see cref="IExpenditureService"/> for managing expenditure transaction records.
/// Asset ownership is verified before create/update operations to prevent cross-account data access.
/// All write operations are wrapped in a database transaction via <see cref="IUnitOfWork"/>.
/// </summary>
public class ExpenditureService : IExpenditureService
{
    private readonly IExpenditureRepository _expenditureRepository;
    private readonly IAssetRepository _assetRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ExpenditureService(
        IExpenditureRepository expenditureRepository,
        IAssetRepository assetRepository,
        IUnitOfWork unitOfWork)
    {
        _expenditureRepository = expenditureRepository;
        _assetRepository = assetRepository;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public Task<List<ExpenditureResponse>> GetExpendituresAsync(string accountEmail)
        => _expenditureRepository.GetExpendituresAsync(accountEmail);

    /// <inheritdoc />
    public async Task<ServiceResult> CreateAsync(string accountEmail, ExpenditureRequest request)
    {
        await _unitOfWork.BeginAsync();
        try
        {
            var validationResult = ValidateExpenditureClass(request.MainClass, request.SubClass);
            if (!validationResult.IsValid)
            {
                await _unitOfWork.RollbackAsync();
                return ServiceResult.Fail("Input is invalid");
            }

            request.AccountEmail = accountEmail;
            request.Amount = Math.Abs(request.Amount);
            request.Updated = DateTime.UtcNow;

            // Validate payment method asset
            AssetResponse? paymentAsset = await _assetRepository.GetAssetAsync(accountEmail, request.PaymentMethod ?? "");
            if (paymentAsset == null)
            {
                await _unitOfWork.RollbackAsync();
                return ServiceResult.Fail("Input is invalid");
            }
            if (paymentAsset.Deleted)
            {
                await _unitOfWork.RollbackAsync();
                return ServiceResult.Fail("Actions cannot be executed with assets that have already been deleted.");
            }

            AssetResponse? depositAsset = null;
            if (validationResult.RequiresDepositAsset)
            {
                if (request.PaymentMethod == request.MyDepositAsset)
                {
                    await _unitOfWork.RollbackAsync();
                    return ServiceResult.Fail("The PaymentMethod and MyDepositAsset value cannot be the same.");
                }

                depositAsset = await _assetRepository.GetAssetAsync(accountEmail, request.MyDepositAsset ?? "");
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
                if (paymentAsset.MonetaryUnit != depositAsset.MonetaryUnit)
                {
                    await _unitOfWork.RollbackAsync();
                    return ServiceResult.Fail("PaymentMethod MonetaryUnit must be same as MyDepositAsset MonetaryUnit.");
                }
            }
            else
            {
                request.MyDepositAsset = "";
            }

            await _expenditureRepository.CreateExpenditureAsync(request);

            paymentAsset.Amount -= Math.Abs(request.Amount);
            paymentAsset.Updated = DateTime.UtcNow;
            await _assetRepository.UpdateAssetAsync(paymentAsset.Adapt<AssetRequest>());

            if (depositAsset != null)
            {
                depositAsset.Amount += Math.Abs(request.Amount);
                depositAsset.Updated = DateTime.UtcNow;
                await _assetRepository.UpdateAssetAsync(depositAsset.Adapt<AssetRequest>());
            }

            await _unitOfWork.CommitAsync();
            return ServiceResult.Ok();
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            return ServiceResult.Fail("Input is invalid");
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResult> UpdateAsync(string accountEmail, ExpenditureRequest request)
    {
        await _unitOfWork.BeginAsync();
        try
        {
            var validationResult = ValidateExpenditureClass(request.MainClass, request.SubClass);
            if (!validationResult.IsValid)
            {
                await _unitOfWork.RollbackAsync();
                return ServiceResult.Fail("Input is invalid");
            }

            if (validationResult.RequiresDepositAsset && request.PaymentMethod == request.MyDepositAsset)
            {
                await _unitOfWork.RollbackAsync();
                return ServiceResult.Fail("The PaymentMethod and MyDepositAsset value cannot be the same.");
            }

            if (validationResult.RequiresDepositAsset)
            {
                var pm = await _assetRepository.GetAssetAsync(accountEmail, request.PaymentMethod ?? "");
                var dep = await _assetRepository.GetAssetAsync(accountEmail, request.MyDepositAsset ?? "");
                if (pm == null || dep == null)
                {
                    await _unitOfWork.RollbackAsync();
                    return ServiceResult.Fail("Input is invalid");
                }
                if (pm.MonetaryUnit != dep.MonetaryUnit)
                {
                    await _unitOfWork.RollbackAsync();
                    return ServiceResult.Fail("PaymentMethod MonetaryUnit must be same as MyDepositAsset MonetaryUnit.");
                }
            }

            List<ExpenditureResponse> expenditures = await _expenditureRepository.GetExpendituresAsync(accountEmail);
            ExpenditureResponse? previous = expenditures.FirstOrDefault(x => x.Id == request.Id);
            if (previous == null)
            {
                await _unitOfWork.RollbackAsync();
                return ServiceResult.Fail("Input is invalid");
            }

            // Validate previous assets not deleted
            AssetResponse? prevPayAsset = await _assetRepository.GetAssetAsync(accountEmail, previous.PaymentMethod ?? "");
            if (prevPayAsset?.Deleted == true)
            {
                await _unitOfWork.RollbackAsync();
                return ServiceResult.Fail("Actions cannot be executed with assets that have already been deleted.");
            }
            if (!string.IsNullOrEmpty(previous.MyDepositAsset))
            {
                AssetResponse? prevDepAsset = await _assetRepository.GetAssetAsync(accountEmail, previous.MyDepositAsset);
                if (prevDepAsset?.Deleted == true)
                {
                    await _unitOfWork.RollbackAsync();
                    return ServiceResult.Fail("Actions cannot be executed with assets that have already been deleted.");
                }
            }

            request.AccountEmail = accountEmail;
            request.Amount = Math.Abs(request.Amount);
            request.Updated = DateTime.UtcNow;
            if (!validationResult.RequiresDepositAsset)
                request.MyDepositAsset = "";

            await _expenditureRepository.UpdateExpenditureAsync(request);

            // Revert previous asset impact, then apply current asset impact
            await AdjustAssetBalancesAsync(accountEmail, previous, request);

            await _unitOfWork.CommitAsync();
            return ServiceResult.Ok();
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            return ServiceResult.Fail("Input is invalid");
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResult> DeleteAsync(string accountEmail, long id)
    {
        await _unitOfWork.BeginAsync();
        try
        {
            List<ExpenditureResponse> expenditures = await _expenditureRepository.GetExpendituresAsync(accountEmail);
            ExpenditureResponse? expenditure = expenditures.FirstOrDefault(x => x.Id == id);
            if (expenditure == null)
            {
                await _unitOfWork.RollbackAsync();
                return ServiceResult.Fail("Input is invalid");
            }

            // Revert the expenditure's asset impact (apply nothing new)
            await AdjustAssetBalancesAsync(accountEmail, expenditure, null);

            await _expenditureRepository.DeleteExpenditureAsync(expenditure.Adapt<ExpenditureRequest>());

            await _unitOfWork.CommitAsync();
            return ServiceResult.Ok();
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            return ServiceResult.Fail("Input is invalid");
        }
    }

    /// <summary>
    /// Reverts <paramref name="toRevert"/>'s asset impact and applies <paramref name="toApply"/>'s asset impact.
    /// Pass null for toApply when deleting.
    /// Uses a dictionary to batch all asset fetches and avoid double-fetch issues.
    /// </summary>
    private async Task AdjustAssetBalancesAsync(string accountEmail, ExpenditureResponse toRevert, ExpenditureRequest? toApply)
    {
        // Collect unique asset product names
        var names = new HashSet<string>(StringComparer.Ordinal);
        if (!string.IsNullOrEmpty(toRevert.PaymentMethod)) names.Add(toRevert.PaymentMethod);
        if (!string.IsNullOrEmpty(toRevert.MyDepositAsset)) names.Add(toRevert.MyDepositAsset);
        if (toApply != null)
        {
            if (!string.IsNullOrEmpty(toApply.PaymentMethod)) names.Add(toApply.PaymentMethod);
            if (!string.IsNullOrEmpty(toApply.MyDepositAsset)) names.Add(toApply.MyDepositAsset);
        }

        // Fetch all unique assets once
        var assets = new Dictionary<string, AssetResponse>(StringComparer.Ordinal);
        foreach (string name in names)
        {
            AssetResponse? a = await _assetRepository.GetAssetAsync(accountEmail, name);
            if (a != null) assets[name] = a;
        }

        // Revert: undo previous expenditure's effect
        if (!string.IsNullOrEmpty(toRevert.PaymentMethod) && assets.TryGetValue(toRevert.PaymentMethod, out var rp))
            rp.Amount += Math.Abs(toRevert.Amount);
        if (!string.IsNullOrEmpty(toRevert.MyDepositAsset) && assets.TryGetValue(toRevert.MyDepositAsset, out var rd))
            rd.Amount -= Math.Abs(toRevert.Amount);

        // Apply: apply new expenditure's effect
        if (toApply != null)
        {
            if (!string.IsNullOrEmpty(toApply.PaymentMethod) && assets.TryGetValue(toApply.PaymentMethod, out var ap))
                ap.Amount -= Math.Abs(toApply.Amount);
            if (!string.IsNullOrEmpty(toApply.MyDepositAsset) && assets.TryGetValue(toApply.MyDepositAsset, out var ad))
                ad.Amount += Math.Abs(toApply.Amount);
        }

        // Save all
        foreach (AssetResponse asset in assets.Values)
        {
            asset.Updated = DateTime.UtcNow;
            await _assetRepository.UpdateAssetAsync(asset.Adapt<AssetRequest>());
        }
    }

    private static (bool IsValid, bool RequiresDepositAsset) ValidateExpenditureClass(string? mainClass, string? subClass)
    {
        return mainClass switch
        {
            "RegularSavings" => subClass is "Deposit" or "Investment"
                ? (true, true) : (false, false),

            "NonConsumerSpending" => subClass switch
            {
                "PublicPension" or "DebtRepayment" => (true, true),
                "Tax" or "SocialInsurance" or "InterHouseholdTranserExpenses" or "NonProfitOrganizationTransfer" => (true, false),
                _ => (false, false)
            },

            "ConsumerSpending" => subClass is
                "MealOrEatOutExpenses" or "HousingOrSuppliesCost" or "EducationExpenses" or
                "MedicalExpenses" or "TransportationCost" or "CommunicationCost" or
                "LeisureOrCulture" or "ClothingOrShoes" or "PinMoney" or
                "ProtectionTypeInsurance" or "OtherExpenses" or "UnknownExpenditure"
                    ? (true, false) : (false, false),

            _ => (false, false)
        };
    }
}
