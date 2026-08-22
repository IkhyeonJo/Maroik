using ErrorOr;
using Maroik.Core.Domain.Primitives;

namespace Maroik.Core.Domain.Menu;

/// <summary>Sidebar dropdown sub-menu item belonging to a <see cref="Category"/>.</summary>
public sealed class SubCategory : Entity<long>
{
    /// <summary>ID of the parent category.</summary>
    public long CategoryId { get; private set; }

    /// <summary>Internal code name (e.g. "FreeForum").</summary>
    public string? Name { get; private set; }

    /// <summary>Label displayed in the sidebar dropdown.</summary>
    public string? DisplayName { get; private set; }

    /// <summary>Icon path displayed beside the label.</summary>
    public string? IconPath { get; private set; }

    /// <summary>Target MVC action name (shares the parent category's controller).</summary>
    public string? Action { get; private set; }

    /// <summary>Minimum role to display this sub-menu item (e.g. "Admin", "User").</summary>
    public string? Role { get; private set; }

    /// <summary>Display order within the parent category (ascending).</summary>
    public long Order { get; private set; }

    private SubCategory(
        long id,
        long categoryId,
        string? name,
        string? displayName,
        string? iconPath,
        string? action,
        string? role,
        long order) : base(id)
    {
        CategoryId = categoryId;
        Name = name;
        DisplayName = displayName;
        IconPath = iconPath;
        Action = action;
        Role = role;
        Order = order;
    }

    /// <summary>
    /// Rebuilds a <see cref="SubCategory"/> from trusted repository data.
    /// Bypasses validation — use only with data from the trusted repository layer.
    /// </summary>
    public static SubCategory Reconstitute(
        long id, long categoryId, string? name, string? displayName,
        string? iconPath, string? action, string? role, long order)
        => new(id, categoryId, name, displayName, iconPath, action, role, order);

    /// <summary>Creates a new sub-category after validating required fields.</summary>
    public static ErrorOr<SubCategory> Create(
        long id, long categoryId, string? name, string? displayName,
        string? iconPath, string? action, string? role, long order)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Error.Validation("SubCategory.NameEmpty", "Sub-category name cannot be empty.");

        return new SubCategory(id, categoryId, name, displayName, iconPath, action, role, order);
    }
}
