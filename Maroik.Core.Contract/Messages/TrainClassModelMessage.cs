namespace Maroik.Core.Contract.Messages;

/// <summary>
/// Queue message asking Maroik.Worker to train the class-suggestion model of <paramref name="AccountEmail"/>'s <paramref name="Book"/>
/// ("Income" or "Expenditure") on the account's current records. Published by
/// <see cref="Interfaces.IClassModelTrainingPublisher"/> when a suggestion finds the model missing or older than the records.
/// </summary>
/// <param name="Book">"Income" or "Expenditure".</param>
/// <param name="AccountEmail">The account whose records the model learns from.</param>
public sealed record TrainClassModelMessage(string Book, string AccountEmail);
