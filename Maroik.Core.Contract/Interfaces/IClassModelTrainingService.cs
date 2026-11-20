namespace Maroik.Core.Contract.Interfaces;

/// <summary>
/// Trains an account's class-suggestion model of one book on its current records (run by Maroik.Worker for each
/// <c>TrainClassModelMessage</c>): the model file is saved in the file storage, the account's model record points at it, and the file it
/// replaced is deleted. Nothing is trained when the record already matches the records and the model code.
/// </summary>
public interface IClassModelTrainingService
{
    /// <summary>Trains the model of <paramref name="accountEmail"/>'s <paramref name="book"/> ("Income" or "Expenditure") when it is out of date.</summary>
    Task TrainAsync(string book, string accountEmail, CancellationToken ct = default);
}
