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

    /// <summary>Initializes a new instance of <see cref="AssetService"/> with the supplied dependencies.</summary>
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

    /// <inheritdoc />
    public async Task<List<AssetResponse>> GetAssetsAsync(string accountEmail, CancellationToken ct = default)
        => (await _assetRepository.GetByAccountEmailAsync(accountEmail, ct)).Select(AssetMapper.ToResponse).ToList();

    /// <inheritdoc />
    public async Task<AssetResponse?> GetAssetAsync(string accountEmail, string productName, CancellationToken ct = default)
    {
        Asset? asset = await FindAssetAsync(accountEmail, productName, ct);
        return asset == null ? null : AssetMapper.ToResponse(asset);
    }

    /// <inheritdoc />
    public async Task<ServiceResult> CreateAsync(string accountEmail, AssetRequest request, CancellationToken ct = default)
    {
        if (!IsValidAssetItem(request.Item))
            return ServiceResult.Fail("Input is invalid");

        // Reject duplicate product names within the same account.
        Asset? existing = await FindAssetAsync(accountEmail, request.ProductName ?? "", ct);
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
        await _assetRepository.CreateAsync(asset, ct);
        return ServiceResult.Ok();
    }

    /// <inheritdoc />
    public async Task<ServiceResult> UpdateAsync(string accountEmail, AssetRequest request, string originalProductName, CancellationToken ct = default)
    {
        await _unitOfWork.BeginAsync(ct);
        try
        {
            if (!IsValidAssetItem(request.Item))
            {
                await _unitOfWork.RollbackAsync(ct);
                return ServiceResult.Fail("Input is invalid");
            }

            Asset? asset = await FindAssetAsync(accountEmail, originalProductName, ct);
            if (asset == null)
            {
                await _unitOfWork.RollbackAsync(ct);
                return ServiceResult.Fail("Fail to find the asset by given product name");
            }

            // Fall back to the existing currency when the request does not specify one.
            string currency = request.MonetaryUnit ?? asset.Balance.Currency;
            var updateResult = asset.Update(request.ProductName ?? originalProductName, request.Item ?? asset.Item, request.Amount, currency, request.Note, request.Deleted);
            if (updateResult.IsError)
            {
                await _unitOfWork.RollbackAsync(ct);
                return ServiceResult.Fail(updateResult.FirstError.Description);
            }

            int updated = await _assetRepository.UpdateAssetWithProductNameAsync(asset, originalProductName, ct);
            if (updated <= 0)
            {
                await _unitOfWork.RollbackAsync(ct);
                return ServiceResult.Fail("Input is invalid");
            }

            // Cascade the renamed product name to all related expenditure records within the same transaction.
            await _fixedExpenditureRepository.UpdateMyDepositAssetNameAsync(accountEmail, originalProductName, asset.ProductName, ct);
            await _expenditureRepository.UpdateMyDepositAssetNameAsync(accountEmail, originalProductName, asset.ProductName, ct);

            await _unitOfWork.CommitAsync(ct);
            return ServiceResult.Ok();
        }
        catch
        {
            await _unitOfWork.RollbackAsync(ct);
            return ServiceResult.Fail("Input is invalid");
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResult> DeleteAsync(string accountEmail, string productName, CancellationToken ct = default)
    {
        Asset? asset = await FindAssetAsync(accountEmail, productName, ct);
        if (asset == null)
            return ServiceResult.Fail("Fail to find the asset by given product name");

        // Soft-delete preserves the record for historical reporting while hiding it from active use.
        asset.SoftDelete();

        await _assetRepository.UpdateEntityAsync(asset, ct);
        return ServiceResult.Ok();
    }

    private Task<Asset?> FindAssetAsync(string accountEmail, string productName, CancellationToken ct = default)
        => _assetRepository.FindByEmailAndProductNameAsync(accountEmail, productName, ct);

    /// <summary>
    /// Returns <see langword="true"/> when <paramref name="item"/> is one of the recognized asset category values.
    /// Prevents invalid category strings from reaching the domain layer.
    /// </summary>
    private static bool IsValidAssetItem(string? item) =>
        item != null && Enum.TryParse<AssetItemType>(item, out _);
}
