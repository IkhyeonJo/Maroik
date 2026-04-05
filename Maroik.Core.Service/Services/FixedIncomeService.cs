using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Mapster;
using System.Text.RegularExpressions;

namespace Maroik.Core.Service.Services;

public class FixedIncomeService : IFixedIncomeService
{
    private readonly IFixedIncomeRepository _fixedIncomeRepository;

    public FixedIncomeService(IFixedIncomeRepository fixedIncomeRepository)
        => _fixedIncomeRepository = fixedIncomeRepository;

    public async Task<ServiceResult> CreateAsync(string accountEmail, FixedIncomeRequest request)
    {
        var validation = ValidateRequest(request);
        if (!validation.Success) return validation;

        request.AccountEmail = accountEmail;
        request.Amount = Math.Abs(request.Amount);
        request.Created = DateTime.UtcNow;
        request.Updated = DateTime.UtcNow;

        await _fixedIncomeRepository.CreateFixedIncomeAsync(request);
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> UpdateAsync(string accountEmail, FixedIncomeRequest request)
    {
        var validation = ValidateRequest(request);
        if (!validation.Success) return validation;

        List<FixedIncomeResponse> items = await _fixedIncomeRepository.GetFixedIncomesAsync(accountEmail);
        FixedIncomeResponse? existing = items.FirstOrDefault(x => x.Id == request.Id);
        if (existing == null)
            return ServiceResult.Fail("Input is invalid");

        existing.MainClass = request.MainClass;
        existing.SubClass = request.SubClass;
        existing.Content = request.Content ?? "";
        existing.Amount = Math.Abs(request.Amount);
        existing.DepositMyAssetProductName = request.DepositMyAssetProductName;
        existing.DepositMonth = request.DepositMonth;
        existing.DepositDay = request.DepositDay;
        existing.MaturityDate = request.MaturityDate;
        existing.Updated = DateTime.UtcNow;
        existing.Note = request.Note ?? "";
        existing.Unpunctuality = request.Unpunctuality;

        await _fixedIncomeRepository.UpdateFixedIncomeAsync(existing.Adapt<FixedIncomeRequest>());
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> DeleteAsync(string accountEmail, long id)
    {
        List<FixedIncomeResponse> items = await _fixedIncomeRepository.GetFixedIncomesAsync(accountEmail);
        FixedIncomeResponse? existing = items.FirstOrDefault(x => x.Id == id);
        if (existing == null)
            return ServiceResult.Fail("Input is invalid");

        await _fixedIncomeRepository.DeleteFixedIncomeAsync(existing.Adapt<FixedIncomeRequest>());
        return ServiceResult.Ok();
    }

    private static ServiceResult ValidateRequest(FixedIncomeRequest request)
    {
        bool classValid = request.MainClass switch
        {
            "RegularIncome" => request.SubClass is "LaborIncome" or "BusinessIncome" or "PensionIncome"
                or "FinancialIncome" or "RentalIncome" or "OtherIncome",
            "IrregularIncome" => request.SubClass is "LaborIncome" or "OtherIncome",
            _ => false
        };
        if (!classValid) return ServiceResult.Fail("Input is invalid");

        if (request.DepositMonth is < 1 or > 12)
            return ServiceResult.Fail("Input is invalid");

        int maxDay = request.DepositMonth switch
        {
            2 => 29,
            4 or 6 or 9 or 11 => 30,
            _ => 31
        };
        if (request.DepositDay < 1 || request.DepositDay > maxDay)
            return ServiceResult.Fail("Input is invalid");

        if (!Regex.IsMatch(request.MaturityDate.ToString("yyyy-MM-dd"),
            @"^\d{4}-((0[1-9])|(1[012]))-((0[1-9]|[12]\d)|3[01])$"))
            return ServiceResult.Fail("Input is invalid");

        return ServiceResult.Ok();
    }
}
