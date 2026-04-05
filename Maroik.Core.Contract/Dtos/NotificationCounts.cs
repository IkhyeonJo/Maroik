namespace Maroik.Core.Contract.Dtos;

public class NotificationCounts
{
    public int FixedIncomesNoticed { get; init; }
    public int FixedExpendituresNoticed { get; init; }
    public int FixedIncomesExpired { get; init; }
    public int FixedExpendituresExpired { get; init; }
}
