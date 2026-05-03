using Maroik.Core.Contract.Domains.Finance;
using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Service.Mappers;

namespace Maroik.Core.Service.Services;

/// <summary>
/// Implementation of <see cref="IAssetService"/> for managing personal financial assets.
/// Rename operations use a database transaction (via <see cref="IUnitOfWork"/>) to atomically
/// update the asset name and cascade the rename to all linked expenditure and fixed-expenditure records.
/// </summary>
public class AssetService : IAssetService
{
    private readonly IAssetRepository _assetRepository;
    private readonly IExpenditureRepository _expenditureRepository;
    private readonly IFixedExpenditureRepository _fixedExpenditureRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDomainEventDispatcher _dispatcher;

    /// <summary>Initializes a new instance of <see cref="AssetService"/> with the supplied dependencies.</summary>
    public AssetService(
        IAssetRepository assetRepository,
        IExpenditureRepository expenditureRepository,
        IFixedExpenditureRepository fixedExpenditureRepository,
        IUnitOfWork unitOfWork,
        IDomainEventDispatcher dispatcher)
    {
        _assetRepository = assetRepository;
        _expenditureRepository = expenditureRepository;
        _fixedExpenditureRepository = fixedExpenditureRepository;
        _unitOfWork = unitOfWork;
        _dispatcher = dispatcher;
    }

    /// <inheritdoc />
    public async Task<List<AssetResponse>> GetAssetsAsync(string accountEmail)
        => (await _assetRepository.GetAssetsAsync(accountEmail)).Select(AssetMapper.ToResponse).ToList();

    /// <inheritdoc />
    public async Task<AssetResponse?> GetAssetAsync(string accountEmail, string productName)
    {
        Asset? asset = await _assetRepository.GetAssetAsync(accountEmail, productName);
        return asset == null ? null : AssetMapper.ToResponse(asset);
    }

    /// <inheritdoc />
    public async Task<ServiceResult> CreateAsync(string accountEmail, AssetRequest request)
    {
        if (!IsValidAssetItem(request.Item))
            return ServiceResult.Fail("Input is invalid");

        Asset? existing = await _assetRepository.GetAssetAsync(accountEmail, request.ProductName ?? "");
        if (existing != null)
            return ServiceResult.Fail("The asset already exists.");

        var createResult = Asset.Create(
            request.ProductName,
            accountEmail,
            request.Item,
            request.Amount,
            request.MonetaryUnit,
            request.Note);

        if (createResult.IsError)
            return ServiceResult.Fail(createResult.FirstError.Description);

        var asset = createResult.Value;
        await _assetRepository.CreateAssetAsync(asset);
        await _dispatcher.DispatchAsync(asset.PopDomainEvents());
        return ServiceResult.Ok();
    }

    /// <inheritdoc />
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

            Asset? asset = await _assetRepository.GetAssetAsync(accountEmail, originalProductName);
            if (asset == null)
            {
                await _unitOfWork.RollbackAsync();
                return ServiceResult.Fail("Fail to find the asset by given product name");
            }

            string currency = request.MonetaryUnit ?? asset.Balance.Currency;
            var updateResult = asset.Update(request.ProductName ?? originalProductName, request.Item ?? asset.Item, request.Amount, currency, request.Note, request.Deleted);
            if (updateResult.IsError)
            {
                await _unitOfWork.RollbackAsync();
                return ServiceResult.Fail(updateResult.FirstError.Description);
            }

            int updated = await _assetRepository.UpdateAssetWithProductNameAsync(asset, originalProductName);
            if (updated <= 0)
            {
                await _unitOfWork.RollbackAsync();
                return ServiceResult.Fail("Input is invalid");
            }

            await _fixedExpenditureRepository.UpdateMyDepositAssetWithProductNameAsync(
                accountEmail, originalProductName, asset.ProductName);
            await _expenditureRepository.UpdateMyDepositAssetWithProductNameAsync(
                accountEmail, originalProductName, asset.ProductName);

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
    public async Task<ServiceResult> DeleteAsync(string accountEmail, string productName)
    {
        Asset? asset = await _assetRepository.GetAssetAsync(accountEmail, productName);
        if (asset == null)
            return ServiceResult.Fail("Fail to find the asset by given product name");

        asset.SoftDelete();

        await _assetRepository.UpdateAssetAsync(asset);
        return ServiceResult.Ok();
    }

    private static bool IsValidAssetItem(string? item) => item is
        "FreeDepositAndWithdrawal" or "TrustAsset" or "CashAsset" or "SavingsAsset" or
        "InvestmentAsset" or "RealEstate" or "Movables" or "OtherPhysicalAsset" or "InsuranceAsset";
}
