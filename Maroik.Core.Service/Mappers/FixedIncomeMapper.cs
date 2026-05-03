using Maroik.Core.Contract.Domains.Finance;
using Maroik.Core.Contract.Dtos;

namespace Maroik.Core.Service.Mappers;

internal static class FixedIncomeMapper
{
    internal static FixedIncomeResponse ToResponse(FixedIncome fi) => new()
    {
        Id = fi.Id,
        AccountEmail = fi.AccountEmail.Value,
        MainClass = fi.MainClass,
        SubClass = fi.SubClass,
        Content = fi.Content,
        Amount = fi.Amount.Amount,
        MonetaryUnit = fi.Amount.Currency,
        DepositMyAssetProductName = fi.DepositMyAssetProductName,
        DepositMonth = fi.DepositMonth,
        DepositDay = fi.DepositDay,
        MaturityDate = fi.MaturityDate,
        Created = fi.Created,
        Updated = fi.Updated,
        Note = fi.Note,
        Unpunctuality = fi.Unpunctuality
    };
}
