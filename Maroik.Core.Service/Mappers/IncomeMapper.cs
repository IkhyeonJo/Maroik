using Maroik.Core.Contract.Domains.Finance;
using Maroik.Core.Contract.Dtos;

namespace Maroik.Core.Service.Mappers;

internal static class IncomeMapper
{
    internal static IncomeResponse ToResponse(Income income) => new()
    {
        Id = income.Id,
        AccountEmail = income.AccountEmail.Value,
        MainClass = income.MainClass,
        SubClass = income.SubClass,
        Content = income.Content,
        Amount = income.Amount.Amount,
        MonetaryUnit = income.Amount.Currency,
        DepositMyAssetProductName = income.DepositMyAssetProductName,
        Created = income.Created,
        Updated = income.Updated,
        Note = income.Note
    };
}
