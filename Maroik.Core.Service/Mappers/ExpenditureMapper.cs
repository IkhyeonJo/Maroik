using Maroik.Core.Contract.Domains.Finance;
using Maroik.Core.Contract.Dtos;

namespace Maroik.Core.Service.Mappers;

internal static class ExpenditureMapper
{
    internal static ExpenditureResponse ToResponse(Expenditure expenditure) => new()
    {
        Id = expenditure.Id,
        AccountEmail = expenditure.AccountEmail.Value,
        MainClass = expenditure.MainClass,
        SubClass = expenditure.SubClass,
        Content = expenditure.Content,
        Amount = expenditure.Amount.Amount,
        MonetaryUnit = expenditure.Amount.Currency,
        PaymentMethod = expenditure.PaymentMethod,
        MyDepositAsset = expenditure.MyDepositAsset,
        Created = expenditure.Created,
        Updated = expenditure.Updated,
        Note = expenditure.Note
    };
}
