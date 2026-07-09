namespace Maroik.Core.Contract.Messaging;

/// <summary>
/// Queue message carrying a fully-built email ready to be sent by a consumer.
/// Published by <see cref="Interfaces.IEmailPublisher"/> and consumed by Maroik.Worker.
/// </summary>
public sealed record SendEmailMessage(string ToEmail, string Subject, string Body);
