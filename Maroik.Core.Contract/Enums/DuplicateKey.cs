namespace Maroik.Core.Contract.Enums;

/// <summary>
/// The business rules a <see cref="Exceptions.DuplicateKeyException"/> can report. Each names what was already taken, not
/// how the database enforces it, so the services stay unaware of constraint names and SQL states.
/// </summary>
public enum DuplicateKey
{
    /// <summary>Another account already has this e-mail address.</summary>
    AccountEmail,

    /// <summary>Another account already has this nickname, in any letter case.</summary>
    AccountNickname,

    /// <summary>The account already has an asset of this product name.</summary>
    AssetProductName,

    /// <summary>The account already has a calendar of this name.</summary>
    CalendarName,

    /// <summary>The calendar already has its sharing row.</summary>
    CalendarShared,
}
