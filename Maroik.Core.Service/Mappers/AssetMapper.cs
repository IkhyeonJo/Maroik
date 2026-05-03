using Maroik.Core.Contract.Domains.Finance;
using Maroik.Core.Contract.Dtos;

namespace Maroik.Core.Service.Mappers;

internal static class AssetMapper
{
    internal static AssetResponse ToResponse(Asset asset) => new()
    {
        ProductName = asset.ProductName,
        AccountEmail = asset.AccountEmail.Value,
        Item = asset.Item,
        Amount = asset.Balance.Amount,
        MonetaryUnit = asset.Balance.Currency,
        Created = asset.Created,
        Updated = asset.Updated,
        Note = asset.Note,
        Deleted = asset.Deleted
    };
}
