using System.Text.RegularExpressions;
using Maroik.Core.Domain.Finance;
using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Service.Mappers;

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
    private readonly IAssetBalanceDomainService _assetBalance;

    /// <summary>Initializes a new instance of <see cref="FixedExpenditureService"/> with the supplied dependencies.</summary>
    public FixedExpenditureService(IFixedExpenditureRepository fixedExpenditureRepository, IAssetBalanceDomainService assetBalance)
    {
        _fixedExpenditureRepository = fixedExpenditureRepository;
        _assetBalance = assetBalance;
    }

    /// <inheritdoc />
    public async Task<List<FixedExpenditureResponse>> GetFixedExpendituresAsync(string accountEmail, CancellationToken ct = default)
        => (await _fixedExpenditureRepository.GetByAccountEmailAsync(accountEmail, ct)).Select(FixedExpenditureMapper.ToResponse).ToList();

    /// <inheritdoc />
    public async Task<FixedExpenditureResponse?> GetByIdAsync(string accountEmail, long id, CancellationToken ct = default)
    {
        var item = await _fixedExpenditureRepository.FindByEmailAndIdAsync(accountEmail, id, ct);
        return item == null ? null : FixedExpenditureMapper.ToResponse(item);
    }

    /// <inheritdoc />
    public async Task<ServiceResult> CreateAsync(string accountEmail, FixedExpenditureRequest request, CancellationToken ct = default)
    {
        var validation = ValidateClassAndDate(request);
        if (!validation.IsValid) return ServiceResult.Fail("Input is invalid");

        Asset? payAsset = await _assetBalance.GetAssetAsync(accountEmail, request.PaymentMethod ?? "", ct);
        if (payAsset == null) return ServiceResult.Fail("Input is invalid");
        if (payAsset.Deleted)
            return ServiceResult.Fail("Actions cannot be executed with assets that have already been deleted.");

        string? myDepositAsset = null;
        if (validation.RequiresDepositAsset)
        {
            if (request.PaymentMethod == request.MyDepositAsset)
                return ServiceResult.Fail("The PaymentMethod and MyDepositAsset value cannot be the same.");

            Asset? depAsset = await _assetBalance.GetAssetAsync(accountEmail, request.MyDepositAsset ?? "", ct);
            if (depAsset == null) return ServiceResult.Fail("Input is invalid");
            if (depAsset.Deleted)
                return ServiceResult.Fail("Actions cannot be executed with assets that have already been deleted.");
            if (payAsset.Balance.Currency != depAsset.Balance.Currency)
                return ServiceResult.Fail("PaymentMethod MonetaryUnit must be same as MyDepositAsset MonetaryUnit.");

            myDepositAsset = request.MyDepositAsset;
        }

        string currency = payAsset.Balance.Currency;
        var registerResult = FixedExpenditure.Register(accountEmail, request.MainClass, request.SubClass,
            request.Content, Math.Abs(request.Amount), currency, request.PaymentMethod,
            myDepositAsset, request.DepositMonth, request.DepositDay, request.MaturityDate, request.Note);

        if (registerResult.IsError)
            return ServiceResult.Fail(registerResult.FirstError.Description);

        var fixedExpenditure = registerResult.Value;
        await _fixedExpenditureRepository.CreateAsync(fixedExpenditure, ct);
        return ServiceResult.Ok();
    }

    /// <inheritdoc />
    public async Task<ServiceResult> UpdateAsync(string accountEmail, FixedExpenditureRequest request, CancellationToken ct = default)
    {
        var validation = ValidateClassAndDate(request);
        if (!validation.IsValid) return ServiceResult.Fail("Input is invalid");

        if (validation.RequiresDepositAsset)
        {
            if (request.PaymentMethod == request.MyDepositAsset)
                return ServiceResult.Fail("The PaymentMethod and MyDepositAsset value cannot be the same.");

            var payAsset = await _assetBalance.GetAssetAsync(accountEmail, request.PaymentMethod ?? "", ct);
            var depAsset = await _assetBalance.GetAssetAsync(accountEmail, request.MyDepositAsset ?? "", ct);
            if (payAsset == null || depAsset == null) return ServiceResult.Fail("Input is invalid");
            if (payAsset.Deleted || depAsset.Deleted)
                return ServiceResult.Fail("Actions cannot be executed with assets that have already been deleted.");
            if (payAsset.Balance.Currency != depAsset.Balance.Currency)
                return ServiceResult.Fail("PaymentMethod MonetaryUnit must be same as MyDepositAsset MonetaryUnit.");
        }

        List<FixedExpenditure> items = await _fixedExpenditureRepository.GetByAccountEmailAsync(accountEmail, ct);
        FixedExpenditure? fe = items.FirstOrDefault(x => x.Id == request.Id);
        if (fe == null) return ServiceResult.Fail("Input is invalid");

        Asset? payAssetForCurrency = await _assetBalance.GetAssetAsync(accountEmail, request.PaymentMethod ?? "", ct);
        if (payAssetForCurrency is { Deleted: true })
            return ServiceResult.Fail("Actions cannot be executed with assets that have already been deleted.");
        string currency = payAssetForCurrency?.Balance.Currency ?? fe.Amount.Currency;
        string? myDepositAsset = validation.RequiresDepositAsset ? request.MyDepositAsset : "";

        var updateResult = fe.Update(request.MainClass, request.SubClass, request.Content,
            Math.Abs(request.Amount), currency, request.PaymentMethod, myDepositAsset,
            request.DepositMonth, request.DepositDay, request.MaturityDate, request.Note);

        if (updateResult.IsError) return ServiceResult.Fail(updateResult.FirstError.Description);

        await _fixedExpenditureRepository.UpdateEntityAsync(fe, ct);
        return ServiceResult.Ok();
    }

    /// <inheritdoc />
    public async Task<ServiceResult> DeleteAsync(string accountEmail, long id, CancellationToken ct = default)
    {
        List<FixedExpenditure> items = await _fixedExpenditureRepository.GetByAccountEmailAsync(accountEmail, ct);
        FixedExpenditure? fe = items.FirstOrDefault(x => x.Id == id);
        if (fe == null) return ServiceResult.Fail("Input is invalid");

        await _fixedExpenditureRepository.DeleteByIdAsync(fe.Id, ct);
        return ServiceResult.Ok();
    }

    /// <summary>
    /// Validates the MainClass/SubClass combination and the deposit date fields of a fixed-expenditure request.
    /// Also determines whether the entry requires a deposit asset (e.g., savings or debt repayment categories).
    /// </summary>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="RegexMatchTimeoutException"></exception>
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
                switch (req.SubClass)
                {
                    case "PublicPension" or "DebtRepayment":
                        requiresDeposit = true;
                        break;
                    case "Tax" or "SocialInsurance" or "InterHouseholdTranserExpenses" or "NonProfitOrganizationTransfer":
                        requiresDeposit = false;
                        break;
                    default:
                        return (false, false);
                }
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
#pragma warning disable SYSLIB1045
        if (req.DepositDay < 1 || req.DepositDay > maxDay || !Regex.IsMatch(req.MaturityDate.ToString("yyyy-MM-dd"),
#pragma warning restore SYSLIB1045
                @"^\d{4}-((0[1-9])|(1[012]))-((0[1-9]|[12]\d)|3[01])$")) return (false, false);

        return (true, requiresDeposit);
    }
}
