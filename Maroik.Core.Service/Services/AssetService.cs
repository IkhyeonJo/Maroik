using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Mapster;

namespace Maroik.Core.Service.Services;

public class AssetService : IAssetService
{
    private readonly IAssetRepository _assetRepository;
    private readonly IExpenditureRepository _expenditureRepository;
    private readonly IFixedExpenditureRepository _fixedExpenditureRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AssetService(
        IAssetRepository assetRepository,
        IExpenditureRepository expenditureRepository,
        IFixedExpenditureRepository fixedExpenditureRepository,
        IUnitOfWork unitOfWork)
    {
        _assetRepository = assetRepository;
        _expenditureRepository = expenditureRepository;
        _fixedExpenditureRepository = fixedExpenditureRepository;
        _unitOfWork = unitOfWork;
    }

    public Task<List<AssetResponse>> GetAssetsAsync(string accountEmail)
        => _assetRepository.GetAssetsAsync(accountEmail);

    public Task<AssetResponse?> GetAssetAsync(string accountEmail, string productName)
        => _assetRepository.GetAssetAsync(accountEmail, productName);

    public async Task<ServiceResult> CreateAsync(string accountEmail, AssetRequest request)
    {
        if (!IsValidAssetItem(request.Item))
            return ServiceResult.Fail("Input is invalid");

        AssetResponse? existing = await _assetRepository.GetAssetAsync(accountEmail, request.ProductName ?? "");
        if (existing != null)
            return ServiceResult.Fail("The asset already exists.");

        request.AccountEmail = accountEmail;
        request.Created = DateTime.UtcNow;
        request.Updated = DateTime.UtcNow;
        request.Deleted = false;

        await _assetRepository.CreateAssetAsync(request);
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> UpdateAsync(string accountEmail, AssetRequest request, string originalProductName)
    {
        await _unitOfWork.BeginAsync();
        try
        {
            if (!IsValidAssetItem(request.Item))
            {
                await _unitOfWork.RollbackAsync();
                return ServiceResult.Fail("Input is invalid");
            }

            AssetResponse? existing = await _assetRepository.GetAssetAsync(accountEmail, originalProductName);
            if (existing == null)
            {
                await _unitOfWork.RollbackAsync();
                return ServiceResult.Fail("Fail to find the asset by given product name");
            }

            existing.ProductName = request.ProductName;
            existing.Item = request.Item;
            existing.Amount = request.Amount;
            existing.MonetaryUnit = request.MonetaryUnit;
            existing.Note = request.Note;
            existing.Deleted = request.Deleted;
            existing.Updated = DateTime.UtcNow;

            int updated = await _assetRepository.UpdateAssetWithProductNameAsync(existing.Adapt<AssetRequest>(), originalProductName);
            if (updated <= 0)
            {
                await _unitOfWork.RollbackAsync();
                return ServiceResult.Fail("Input is invalid");
            }

            await _fixedExpenditureRepository.UpdateMyDepositAssetWithProductNameAsync(
                accountEmail, originalProductName, request.ProductName ?? "");
            await _expenditureRepository.UpdateMyDepositAssetWithProductNameAsync(
                accountEmail, originalProductName, request.ProductName ?? "");

            await _unitOfWork.CommitAsync();
            return ServiceResult.Ok();
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            return ServiceResult.Fail("Input is invalid");
        }
    }

    public async Task<ServiceResult> DeleteAsync(string accountEmail, string productName)
    {
        AssetResponse? asset = await _assetRepository.GetAssetAsync(accountEmail, productName);
        if (asset == null)
            return ServiceResult.Fail("Fail to find the asset by given product name");

        asset.Deleted = true;
        await _assetRepository.UpdateAssetAsync(asset.Adapt<AssetRequest>());
        return ServiceResult.Ok();
    }

    private static bool IsValidAssetItem(string? item) => item is
        "FreeDepositAndWithdrawal" or "TrustAsset" or "CashAsset" or "SavingsAsset" or
        "InvestmentAsset" or "RealEstate" or "Movables" or "OtherPhysicalAsset" or "InsuranceAsset";
}
