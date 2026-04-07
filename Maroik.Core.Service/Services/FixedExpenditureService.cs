using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Mapster;
using System.Text.RegularExpressions;

namespace Maroik.Core.Service.Services;

/// <summary>
/// Implementation of <see cref="IFixedExpenditureService"/> for managing recurring expenditure entries.
/// Validates the <c>MaturityDate</c> format and deposit day before persisting.
/// Computes <c>Noticed</c> and <c>Expired</c> flags based on the current date,
/// and verifies asset ownership before write operations.
/// </summary>
public class FixedExpenditureService : IFixedExpenditureService
{
    private readonly IFixedExpenditureRepository _fixedExpenditureRepository;
    private readonly IAssetRepository _assetRepository;

    public FixedExpenditureService(IFixedExpenditureRepository fixedExpenditureRepository, IAssetRepository assetRepository)
    {
        _fixedExpenditureRepository = fixedExpenditureRepository;
        _assetRepository = assetRepository;
    }

    public Task<List<FixedExpenditureResponse>> GetFixedExpendituresAsync(string accountEmail)
        => _fixedExpenditureRepository.GetFixedExpendituresAsync(accountEmail);

    public async Task<ServiceResult> CreateAsync(string accountEmail, FixedExpenditureRequest request)
    {
        var validation = ValidateClassAndDate(request);
        if (!validation.IsValid) return ServiceResult.Fail("Input is invalid");

        if (validation.RequiresDepositAsset)
        {
            if (request.PaymentMethod == request.MyDepositAsset)
                return ServiceResult.Fail("The PaymentMethod and MyDepositAsset value cannot be the same.");

            var payAsset = await _assetRepository.GetAssetAsync(accountEmail, request.PaymentMethod ?? "");
            var depAsset = await _assetRepository.GetAssetAsync(accountEmail, request.MyDepositAsset ?? "");
            if (payAsset == null || depAsset == null)
                return ServiceResult.Fail("Input is invalid");
            if (payAsset.MonetaryUnit != depAsset.MonetaryUnit)
                return ServiceResult.Fail("PaymentMethod MonetaryUnit must be same as MyDepositAsset MonetaryUnit.");
        }
        else
        {
            var payAsset = await _assetRepository.GetAssetAsync(accountEmail, request.PaymentMethod ?? "");
            if (payAsset == null) return ServiceResult.Fail("Input is invalid");
            request.MyDepositAsset = "";
        }

        request.AccountEmail = accountEmail;
        request.Amount = Math.Abs(request.Amount);
        request.Created = DateTime.UtcNow;
        request.Updated = DateTime.UtcNow;

        await _fixedExpenditureRepository.CreateFixedExpenditureAsync(request);
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> UpdateAsync(string accountEmail, FixedExpenditureRequest request)
    {
        var validation = ValidateClassAndDate(request);
        if (!validation.IsValid) return ServiceResult.Fail("Input is invalid");

        if (validation.RequiresDepositAsset)
        {
            if (request.PaymentMethod == request.MyDepositAsset)
                return ServiceResult.Fail("The PaymentMethod and MyDepositAsset value cannot be the same.");

            var payAsset = await _assetRepository.GetAssetAsync(accountEmail, request.PaymentMethod ?? "");
            var depAsset = await _assetRepository.GetAssetAsync(accountEmail, request.MyDepositAsset ?? "");
            if (payAsset == null || depAsset == null)
                return ServiceResult.Fail("Input is invalid");
            if (payAsset.MonetaryUnit != depAsset.MonetaryUnit)
                return ServiceResult.Fail("PaymentMethod MonetaryUnit must be same as MyDepositAsset MonetaryUnit.");
        }

        List<FixedExpenditureResponse> items = await _fixedExpenditureRepository.GetFixedExpendituresAsync(accountEmail);
        FixedExpenditureResponse? existing = items.FirstOrDefault(x => x.Id == request.Id);
        if (existing == null)
            return ServiceResult.Fail("Input is invalid");

        existing.MainClass = request.MainClass;
        existing.SubClass = request.SubClass;
        existing.Content = request.Content ?? "";
        existing.Amount = Math.Abs(request.Amount);
        existing.PaymentMethod = request.PaymentMethod;
        existing.MyDepositAsset = request.MyDepositAsset ?? "";
        existing.DepositMonth = request.DepositMonth;
        existing.DepositDay = request.DepositDay;
        existing.MaturityDate = request.MaturityDate;
        existing.Updated = DateTime.UtcNow;
        existing.Note = request.Note ?? "";
        existing.Unpunctuality = request.Unpunctuality;

        await _fixedExpenditureRepository.UpdateFixedExpenditureAsync(existing.Adapt<FixedExpenditureRequest>());
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> DeleteAsync(string accountEmail, long id)
    {
        List<FixedExpenditureResponse> items = await _fixedExpenditureRepository.GetFixedExpendituresAsync(accountEmail);
        FixedExpenditureResponse? existing = items.FirstOrDefault(x => x.Id == id);
        if (existing == null)
            return ServiceResult.Fail("Input is invalid");

        await _fixedExpenditureRepository.DeleteFixedExpenditureAsync(existing.Adapt<FixedExpenditureRequest>());
        return ServiceResult.Ok();
    }

    private static (bool IsValid, bool RequiresDepositAsset) ValidateClassAndDate(FixedExpenditureRequest req)
    {
        bool requiresDeposit;
        switch (req.MainClass)
        {
            case "RegularSavings":
                if (req.SubClass is not ("Deposit" or "Investment")) return (false, false);
                requiresDeposit = true;
                break;
            case "NonConsumerSpending":
                if (req.SubClass is "PublicPension" or "DebtRepayment") requiresDeposit = true;
                else if (req.SubClass is "Tax" or "SocialInsurance" or "InterHouseholdTranserExpenses" or "NonProfitOrganizationTransfer") requiresDeposit = false;
                else return (false, false);
                break;
            case "ConsumerSpending":
                if (req.SubClass is not ("MealOrEatOutExpenses" or "HousingOrSuppliesCost" or "EducationExpenses" or
                    "MedicalExpenses" or "TransportationCost" or "CommunicationCost" or "LeisureOrCulture" or
                    "ClothingOrShoes" or "PinMoney" or "ProtectionTypeInsurance" or "OtherExpenses" or "UnknownExpenditure"))
                    return (false, false);
                requiresDeposit = false;
                break;
            default:
                return (false, false);
        }

        if (req.DepositMonth is < 1 or > 12) return (false, false);
        int maxDay = req.DepositMonth switch { 2 => 29, 4 or 6 or 9 or 11 => 30, _ => 31 };
        if (req.DepositDay < 1 || req.DepositDay > maxDay) return (false, false);

        if (!Regex.IsMatch(req.MaturityDate.ToString("yyyy-MM-dd"),
            @"^\d{4}-((0[1-9])|(1[012]))-((0[1-9]|[12]\d)|3[01])$"))
            return (false, false);

        return (true, requiresDeposit);
    }
}
