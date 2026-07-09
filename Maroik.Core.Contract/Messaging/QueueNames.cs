namespace Maroik.Core.Contract.Messaging;

/// <summary>Well-known RabbitMQ queue names shared between publishers and Maroik.Worker consumers.</summary>
public static class QueueNames
{
    /// <summary>Durable work queue carrying <see cref="SendEmailMessage"/> items.</summary>
    public const string Email = "maroik.email";
}
