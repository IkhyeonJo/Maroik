using Maroik.Core.Contract.Domains.Finance;
using Maroik.Core.Contract.Dtos;

namespace Maroik.Core.Service.Mappers;

internal static class FixedExpenditureMapper
{
    internal static FixedExpenditureResponse ToResponse(FixedExpenditure fe) => new()
    {
        Id = fe.Id,
        AccountEmail = fe.AccountEmail.Value,
        MainClass = fe.MainClass,
        SubClass = fe.SubClass,
        Content = fe.Content,
        Amount = fe.Amount.Amount,
        MonetaryUnit = fe.Amount.Currency,
        PaymentMethod = fe.PaymentMethod,
        MyDepositAsset = fe.MyDepositAsset,
        DepositMonth = fe.DepositMonth,
        DepositDay = fe.DepositDay,
        MaturityDate = fe.MaturityDate,
        Created = fe.Created,
        Updated = fe.Updated,
        Note = fe.Note,
        Unpunctuality = fe.Unpunctuality
    };
}
