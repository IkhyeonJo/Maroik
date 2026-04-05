using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Mapster;

namespace Maroik.Core.Service.Services;

public class IncomeService : IIncomeService
{
    private readonly IIncomeRepository _incomeRepository;
    private readonly IAssetRepository _assetRepository;
    private readonly IUnitOfWork _unitOfWork;

    public IncomeService(IIncomeRepository incomeRepository, IAssetRepository assetRepository, IUnitOfWork unitOfWork)
    {
        _incomeRepository = incomeRepository;
        _assetRepository = assetRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ServiceResult> CreateAsync(string accountEmail, IncomeRequest request)
    {
        await _unitOfWork.BeginAsync();
        try
        {
            if (!IsValidIncomeClass(request.MainClass, request.SubClass))
            {
                await _unitOfWork.RollbackAsync();
                return ServiceResult.Fail("Input is invalid");
            }

            AssetResponse? asset = await _assetRepository.GetAssetAsync(accountEmail, request.DepositMyAssetProductName ?? "");
            if (asset == null)
            {
                await _unitOfWork.RollbackAsync();
                return ServiceResult.Fail("Input is invalid");
            }
            if (asset.Deleted)
            {
                await _unitOfWork.RollbackAsync();
                return ServiceResult.Fail("Actions cannot be executed with assets that have already been deleted.");
            }

            request.AccountEmail = accountEmail;
            request.Amount = Math.Abs(request.Amount);
            request.Updated = DateTime.UtcNow;
            await _incomeRepository.CreateIncomeAsync(request);

            asset.Amount += Math.Abs(request.Amount);
            asset.Updated = DateTime.UtcNow;
            await _assetRepository.UpdateAssetAsync(asset.Adapt<AssetRequest>());

            await _unitOfWork.CommitAsync();
            return ServiceResult.Ok();
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            return ServiceResult.Fail("Input is invalid");
        }
    }

    public async Task<ServiceResult> UpdateAsync(string accountEmail, IncomeRequest request)
    {
        await _unitOfWork.BeginAsync();
        try
        {
            if (!IsValidIncomeClass(request.MainClass, request.SubClass))
            {
                await _unitOfWork.RollbackAsync();
                return ServiceResult.Fail("Input is invalid");
            }

            List<IncomeResponse> incomes = await _incomeRepository.GetIncomesAsync(accountEmail);
            IncomeResponse? existing = incomes.FirstOrDefault(x => x.Id == request.Id);
            if (existing == null)
            {
                await _unitOfWork.RollbackAsync();
                return ServiceResult.Fail("Input is invalid");
            }

            AssetResponse? assetToValidate = await _assetRepository.GetAssetAsync(accountEmail, existing.DepositMyAssetProductName ?? "");
            if (assetToValidate?.Deleted == true)
            {
                await _unitOfWork.RollbackAsync();
                return ServiceResult.Fail("Actions cannot be executed with assets that have already been deleted.");
            }

            string previousAssetName = existing.DepositMyAssetProductName ?? "";
            decimal previousAmount = existing.Amount;

            existing.MainClass = request.MainClass;
            existing.SubClass = request.SubClass;
            existing.Content = request.Content;
            existing.Amount = Math.Abs(request.Amount);
            existing.DepositMyAssetProductName = request.DepositMyAssetProductName;
            existing.Note = request.Note;
            existing.Created = request.Created;
            existing.Updated = DateTime.UtcNow;
            await _incomeRepository.UpdateIncomeAsync(existing.Adapt<IncomeRequest>());

            if (existing.DepositMyAssetProductName == previousAssetName)
            {
                AssetResponse? asset = await _assetRepository.GetAssetAsync(accountEmail, existing.DepositMyAssetProductName!);
                if (asset != null)
                {
                    asset.Amount = asset.Amount - Math.Abs(previousAmount) + Math.Abs(existing.Amount);
                    asset.Updated = DateTime.UtcNow;
                    await _assetRepository.UpdateAssetAsync(asset.Adapt<AssetRequest>());
                }
            }
            else
            {
                AssetResponse? oldAsset = await _assetRepository.GetAssetAsync(accountEmail, previousAssetName);
                if (oldAsset != null)
                {
                    oldAsset.Amount -= Math.Abs(previousAmount);
                    oldAsset.Updated = DateTime.UtcNow;
                    await _assetRepository.UpdateAssetAsync(oldAsset.Adapt<AssetRequest>());
                }

                AssetResponse? newAsset = await _assetRepository.GetAssetAsync(accountEmail, existing.DepositMyAssetProductName!);
                if (newAsset != null)
                {
                    newAsset.Amount += Math.Abs(existing.Amount);
                    newAsset.Updated = DateTime.UtcNow;
                    await _assetRepository.UpdateAssetAsync(newAsset.Adapt<AssetRequest>());
                }
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

    public async Task<ServiceResult> DeleteAsync(string accountEmail, long id)
    {
        await _unitOfWork.BeginAsync();
        try
        {
            List<IncomeResponse> incomes = await _incomeRepository.GetIncomesAsync(accountEmail);
            IncomeResponse? income = incomes.FirstOrDefault(x => x.Id == id);
            if (income == null)
            {
                await _unitOfWork.RollbackAsync();
                return ServiceResult.Fail("Input is invalid");
            }

            AssetResponse? asset = await _assetRepository.GetAssetAsync(accountEmail, income.DepositMyAssetProductName ?? "");
            if (asset?.Deleted == true)
            {
                await _unitOfWork.RollbackAsync();
                return ServiceResult.Fail("Actions cannot be executed with assets that have already been deleted.");
            }

            if (asset != null)
            {
                asset.Amount -= Math.Abs(income.Amount);
                asset.Updated = DateTime.UtcNow;
                await _assetRepository.UpdateAssetAsync(asset.Adapt<AssetRequest>());
            }

            await _incomeRepository.DeleteIncomeAsync(income.Adapt<IncomeRequest>());

            await _unitOfWork.CommitAsync();
            return ServiceResult.Ok();
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            return ServiceResult.Fail("Input is invalid");
        }
    }

    private static bool IsValidIncomeClass(string? mainClass, string? subClass) => mainClass switch
    {
        "RegularIncome" => subClass is "LaborIncome" or "BusinessIncome" or "PensionIncome" or "FinancialIncome" or "RentalIncome" or "OtherIncome",
        "IrregularIncome" => subClass is "LaborIncome" or "OtherIncome",
        _ => false
    };
}
