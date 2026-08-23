using Maroik.Core.Domain.Finance;
using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Service.Extensions;
using Maroik.Core.Service.Mappers;
using Microsoft.Extensions.Logging;

namespace Maroik.Core.Service.Services;

/// <summary>
/// Implementation of <see cref="IAssetService"/> for managing personal financial assets.
/// Rename operations use a database transaction (via <see cref="IUnitOfWork"/>) to atomically
/// update the asset name; the rename to linked income, fixed-income, expenditure, and
/// fixed-expenditure records is handled by the database's <c>ON UPDATE CASCADE</c> foreign keys.
/// </summary>
public class AssetService : IAssetService
{
    private readonly IAssetRepository _assetRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AssetService> _logger;

    /// <summary>Initializes a new instance of <see cref="AssetService"/> with the supplied dependencies.</summary>
    public AssetService(IAssetRepository assetRepository, IUnitOfWork unitOfWork, ILogger<AssetService> logger)
    {
        _assetRepository = assetRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
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

        // Reject duplicate product names within the same account. A soft-deleted asset under the
        // same name doesn't block recreation from the user's perspective, but its row still
        // occupies the (ProductName, AccountEmail) primary key, so it must be revived (UPDATE)
        // below rather than inserted as a new row.
        Asset? existing = await FindAssetAsync(accountEmail, request.ProductName ?? "", ct);
        if (existing is { Deleted: false })
            return ServiceResult.Fail("The asset already exists.");

        if (existing != null)
        {
            var reviveResult = existing.Update(
                request.ProductName ?? existing.ProductName,
                request.Item!,
                request.Amount,
                request.MonetaryUnit ?? existing.Balance.Currency,
                request.Note,
                deleted: false);
            if (reviveResult.IsError)
                return ServiceResult.Fail(reviveResult.FirstError.Description);

            await _assetRepository.UpdateEntityAsync(existing, ct);
            return ServiceResult.Ok();
        }

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

        try
        {
            await _assetRepository.CreateAsync(asset, ct);
        }
        catch (Exception e) when (e.IsPostgresUniqueViolation())
        {
            // The prior FindAssetAsync check above is a read-then-write race: two concurrent
            // requests for the same (ProductName, AccountEmail) can both pass it. The primary
            // key constraint on that pair still rejects the second insert — turn that into the
            // same friendly result instead of letting the raw DB exception escape.
            return ServiceResult.Fail("The asset already exists.");
        }

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

            await _unitOfWork.CommitAsync(ct);
            return ServiceResult.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update asset {OriginalProductName} for account {AccountEmail}", originalProductName, accountEmail);
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
