using Maroik.Core.Contract.Domains.Finance;
using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Service.Mappers;

namespace Maroik.Core.Service.Services;

/// <summary>
/// Implementation of <see cref="IExpenditureService"/> for managing expenditure transaction records.
/// Asset ownership is verified before create/update operations to prevent cross-account data access.
/// All write operations are wrapped in a database transaction via <see cref="IUnitOfWork"/>.
/// </summary>
public class ExpenditureService : IExpenditureService
{
    private readonly IExpenditureRepository _expenditureRepository;
    private readonly IAssetBalanceDomainService _assetBalance;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>Initializes a new instance of <see cref="ExpenditureService"/> with the supplied dependencies.</summary>
    public ExpenditureService(
        IExpenditureRepository expenditureRepository,
        IAssetBalanceDomainService assetBalance,
        IUnitOfWork unitOfWork)
    {
        _expenditureRepository = expenditureRepository;
        _assetBalance = assetBalance;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<List<ExpenditureResponse>> GetExpendituresAsync(string accountEmail)
        => (await _expenditureRepository.GetExpendituresAsync(accountEmail)).Select(ExpenditureMapper.ToResponse).ToList();

    /// <inheritdoc />
    public async Task<ServiceResult> CreateAsync(string accountEmail, ExpenditureRequest request)
    {
        await _unitOfWork.BeginAsync();
        try
        {
            var classResult = ExpenditureClassPolicy.Validate(request.MainClass, request.SubClass);
            if (classResult.IsError)
            {
                await _unitOfWork.RollbackAsync();
                return ServiceResult.Fail("Input is invalid");
            }

            Asset? paymentAsset = await _assetBalance.GetAssetAsync(accountEmail, request.PaymentMethod ?? "");
            if (paymentAsset == null)
            {
                await _unitOfWork.RollbackAsync();
                return ServiceResult.Fail("Input is invalid");
            }
            if (paymentAsset.Deleted)
            {
                await _unitOfWork.RollbackAsync();
                return ServiceResult.Fail("Actions cannot be executed with assets that have already been deleted.");
            }

            Asset? depositAsset = null;
            if (classResult.Value)
            {
                if (request.PaymentMethod == request.MyDepositAsset)
                {
                    await _unitOfWork.RollbackAsync();
                    return ServiceResult.Fail("The PaymentMethod and MyDepositAsset value cannot be the same.");
                }

                depositAsset = await _assetBalance.GetAssetAsync(accountEmail, request.MyDepositAsset ?? "");
                if (depositAsset == null)
                {
                    await _unitOfWork.RollbackAsync();
                    return ServiceResult.Fail("Input is invalid");
                }
                if (depositAsset.Deleted)
                {
                    await _unitOfWork.RollbackAsync();
                    return ServiceResult.Fail("Actions cannot be executed with assets that have already been deleted.");
                }
                if (paymentAsset.Balance.Currency != depositAsset.Balance.Currency)
                {
                    await _unitOfWork.RollbackAsync();
                    return ServiceResult.Fail("PaymentMethod MonetaryUnit must be same as MyDepositAsset MonetaryUnit.");
                }
            }

            string currency = paymentAsset.Balance.Currency;
            string? myDepositAsset = classResult.Value ? request.MyDepositAsset : string.Empty;

            var expenditureResult = Expenditure.Record(accountEmail, request.MainClass, request.SubClass,
                request.Content, Math.Abs(request.Amount), currency, request.PaymentMethod, myDepositAsset, request.Note);
            if (expenditureResult.IsError)
            {
                await _unitOfWork.RollbackAsync();
                return ServiceResult.Fail(expenditureResult.FirstError.Description);
            }

            var expenditure = expenditureResult.Value;
            await _expenditureRepository.CreateExpenditureAsync(expenditure);

            paymentAsset.Withdraw(expenditure.Amount.Amount, currency);
            await _assetBalance.SaveAsync(paymentAsset);

            if (depositAsset != null)
            {
                depositAsset.Deposit(expenditure.Amount.Amount, currency);
                await _assetBalance.SaveAsync(depositAsset);
            }

            await _unitOfWork.CommitAsync();
            return ServiceResult.Ok();
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            return ServiceResult.Fail("Input is invalid");
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResult> UpdateAsync(string accountEmail, ExpenditureRequest request)
    {
        await _unitOfWork.BeginAsync();
        try
        {
            var classResult = ExpenditureClassPolicy.Validate(request.MainClass, request.SubClass);
            if (classResult.IsError)
            {
                await _unitOfWork.RollbackAsync();
                return ServiceResult.Fail("Input is invalid");
            }

            if (classResult.Value && request.PaymentMethod == request.MyDepositAsset)
            {
                await _unitOfWork.RollbackAsync();
                return ServiceResult.Fail("The PaymentMethod and MyDepositAsset value cannot be the same.");
            }

            if (classResult.Value)
            {
                var pm = await _assetBalance.GetAssetAsync(accountEmail, request.PaymentMethod ?? "");
                var dep = await _assetBalance.GetAssetAsync(accountEmail, request.MyDepositAsset ?? "");
                if (pm == null || dep == null)
                {
                    await _unitOfWork.RollbackAsync();
                    return ServiceResult.Fail("Input is invalid");
                }
                if (pm.Balance.Currency != dep.Balance.Currency)
                {
                    await _unitOfWork.RollbackAsync();
                    return ServiceResult.Fail("PaymentMethod MonetaryUnit must be same as MyDepositAsset MonetaryUnit.");
                }
            }

            List<Expenditure> expenditures = await _expenditureRepository.GetExpendituresAsync(accountEmail);
            Expenditure? previous = expenditures.FirstOrDefault(x => x.Id == request.Id);
            if (previous == null)
            {
                await _unitOfWork.RollbackAsync();
                return ServiceResult.Fail("Input is invalid");
            }

            Asset? prevPayAsset = await _assetBalance.GetAssetAsync(accountEmail, previous.PaymentMethod ?? "");
            if (prevPayAsset?.Deleted == true)
            {
                await _unitOfWork.RollbackAsync();
                return ServiceResult.Fail("Actions cannot be executed with assets that have already been deleted.");
            }
            if (!string.IsNullOrEmpty(previous.MyDepositAsset))
            {
                Asset? prevDepAsset = await _assetBalance.GetAssetAsync(accountEmail, previous.MyDepositAsset);
                if (prevDepAsset?.Deleted == true)
                {
                    await _unitOfWork.RollbackAsync();
                    return ServiceResult.Fail("Actions cannot be executed with assets that have already been deleted.");
                }
            }

            Asset? newPayAsset = await _assetBalance.GetAssetAsync(accountEmail, request.PaymentMethod ?? "");
            string newCurrency = newPayAsset?.Balance.Currency ?? prevPayAsset?.Balance.Currency ?? string.Empty;
            string? myDepositAsset = classResult.Value ? request.MyDepositAsset : string.Empty;

            var oldSnapshot = new ExpenditureSnapshot(previous.PaymentMethod, previous.MyDepositAsset, previous.Amount.Amount);

            var updateResult = previous.Update(request.MainClass, request.SubClass, request.Content,
                Math.Abs(request.Amount), newCurrency, request.PaymentMethod, myDepositAsset, request.Note);
            if (updateResult.IsError)
            {
                await _unitOfWork.RollbackAsync();
                return ServiceResult.Fail(updateResult.FirstError.Description);
            }

            await _expenditureRepository.UpdateExpenditureAsync(previous);
            await AdjustAssetBalancesAsync(accountEmail, oldSnapshot,
                new ExpenditureSnapshot(previous.PaymentMethod, previous.MyDepositAsset, previous.Amount.Amount));

            await _unitOfWork.CommitAsync();
            return ServiceResult.Ok();
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            return ServiceResult.Fail("Input is invalid");
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResult> DeleteAsync(string accountEmail, long id)
    {
        await _unitOfWork.BeginAsync();
        try
        {
            List<Expenditure> expenditures = await _expenditureRepository.GetExpendituresAsync(accountEmail);
            Expenditure? expenditure = expenditures.FirstOrDefault(x => x.Id == id);
            if (expenditure == null)
            {
                await _unitOfWork.RollbackAsync();
                return ServiceResult.Fail("Input is invalid");
            }

            await AdjustAssetBalancesAsync(accountEmail,
                new ExpenditureSnapshot(expenditure.PaymentMethod, expenditure.MyDepositAsset, expenditure.Amount.Amount),
                null);
            await _expenditureRepository.DeleteExpenditureAsync(expenditure);

            await _unitOfWork.CommitAsync();
            return ServiceResult.Ok();
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            return ServiceResult.Fail("Input is invalid");
        }
    }

    /// <summary>
    /// Reverts <paramref name="toRevert"/>'s asset impact and applies <paramref name="toApply"/>'s asset impact.
    /// Pass null for toApply when deleting.
    /// Uses a dictionary to batch all asset fetches and avoid double-fetch issues.
    /// </summary>
    private async Task AdjustAssetBalancesAsync(string accountEmail, ExpenditureSnapshot toRevert, ExpenditureSnapshot? toApply)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        if (!string.IsNullOrEmpty(toRevert.PaymentMethod)) names.Add(toRevert.PaymentMethod);
        if (!string.IsNullOrEmpty(toRevert.MyDepositAsset)) names.Add(toRevert.MyDepositAsset);
        if (toApply.HasValue)
        {
            if (!string.IsNullOrEmpty(toApply.Value.PaymentMethod)) names.Add(toApply.Value.PaymentMethod);
            if (!string.IsNullOrEmpty(toApply.Value.MyDepositAsset)) names.Add(toApply.Value.MyDepositAsset);
        }

        var assets = new Dictionary<string, Asset>(StringComparer.Ordinal);
        foreach (string name in names)
        {
            Asset? a = await _assetBalance.GetAssetAsync(accountEmail, name);
            if (a != null) assets[name] = a;
        }

        if (!string.IsNullOrEmpty(toRevert.PaymentMethod) && assets.TryGetValue(toRevert.PaymentMethod, out var rp))
            rp.Deposit(Math.Abs(toRevert.Amount), rp.Balance.Currency);

        if (!string.IsNullOrEmpty(toRevert.MyDepositAsset) && assets.TryGetValue(toRevert.MyDepositAsset, out var rd))
            rd.Withdraw(Math.Abs(toRevert.Amount), rd.Balance.Currency);

        if (toApply.HasValue)
        {
            if (!string.IsNullOrEmpty(toApply.Value.PaymentMethod) && assets.TryGetValue(toApply.Value.PaymentMethod, out var ap))
                ap.Withdraw(Math.Abs(toApply.Value.Amount), ap.Balance.Currency);

            if (!string.IsNullOrEmpty(toApply.Value.MyDepositAsset) && assets.TryGetValue(toApply.Value.MyDepositAsset, out var ad))
                ad.Deposit(Math.Abs(toApply.Value.Amount), ad.Balance.Currency);
        }

        foreach (var asset in assets.Values)
            await _assetBalance.SaveAsync(asset);
    }

    private readonly record struct ExpenditureSnapshot(string? PaymentMethod, string? MyDepositAsset, decimal Amount);
}
