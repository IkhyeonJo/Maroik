using ErrorOr;
using Maroik.Core.Contract.Domains.Primitives;

namespace Maroik.Core.Contract.Domains.Menu;

/// <summary>Top-level sidebar navigation menu item.</summary>
public sealed class Category : Entity<long>
{
    /// <summary>Internal code name (e.g. "Dashboard").</summary>
    public string? Name { get; private set; }

    /// <summary>Label shown in the sidebar navigation.</summary>
    public string? DisplayName { get; private set; }

    /// <summary>Icon path displayed beside the label.</summary>
    public string? IconPath { get; private set; }

    /// <summary>Target MVC controller name.</summary>
    public string? Controller { get; private set; }

    /// <summary>Target MVC action name.</summary>
    public string? Action { get; private set; }

    /// <summary>Minimum role to display this menu item (e.g. "Admin", "User").</summary>
    public string? Role { get; private set; }

    /// <summary>Sidebar display order (ascending).</summary>
    public long Order { get; private set; }

    private Category(
        long id,
        string? name,
        string? displayName,
        string? iconPath,
        string? controller,
        string? action,
        string? role,
        long order) : base(id)
    {
        Name = name;
        DisplayName = displayName;
        IconPath = iconPath;
        Controller = controller;
        Action = action;
        Role = role;
        Order = order;
    }

    /// <summary>
    /// Rebuilds a <see cref="Category"/> from trusted repository data.
    /// Bypasses validation ?? use only with data from the trusted repository layer.
    /// </summary>
    public static Category Reconstitute(
        long id, string? name, string? displayName, string? iconPath,
        string? controller, string? action, string? role, long order)
        => new(id, name, displayName, iconPath, controller, action, role, order);

    /// <summary>Creates a new navigation category after validating required fields.</summary>
    public static ErrorOr<Category> Create(
        long id, string? name, string? displayName, string? iconPath,
        string? controller, string? action, string? role, long order)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Error.Validation("Category.NameEmpty", "Category name cannot be empty.");

        return new Category(id, name, displayName, iconPath, controller, action, role, order);
    }

    /// <summary>Updates the mutable fields of this category.</summary>
    public ErrorOr<Category> Update(
        string? name, string? displayName, string? iconPath,
        string? controller, string? action, string? role, long order)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Error.Validation("Category.NameEmpty", "Category name cannot be empty.");

        Name = name;
        DisplayName = displayName;
        IconPath = iconPath;
        Controller = controller;
        Action = action;
        Role = role;
        Order = order;
        return this;
    }
}
