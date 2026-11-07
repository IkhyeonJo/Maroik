namespace Maroik.Core.Contract.Enums;

/// <summary>How a fixed-income or fixed-expenditure row stands, as <c>IPolicyService</c> reports it to the grids.</summary>
public enum FixedScheduleRowState
{
    /// <summary>Its maturity date has passed.</summary>
    Expired,

    /// <summary>Its deposit day falls inside the notice window.</summary>
    Noticed,

    /// <summary>Neither.</summary>
    Normal,
}
