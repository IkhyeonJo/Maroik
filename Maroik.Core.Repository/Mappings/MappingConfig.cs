using Mapster;

namespace Maroik.Core.Repository.Mappings;

/// <summary>
/// Mapster mapping configuration for the Repository layer.
/// Registered via <c>TypeAdapterConfig.GlobalSettings.Scan(Assembly)</c> at startup
/// because it implements <see cref="IRegister"/>.
/// </summary>
public class MappingConfig : IRegister
{
    /// <summary>
    /// Registers type-adapter rules for entity-to-DTO and DTO-to-entity mappings.
    /// All entity and DTO property names are identical by convention, so Mapster
    /// resolves them automatically — no explicit mapping rules are needed here.
    /// </summary>
    public void Register(TypeAdapterConfig config)
    {
        // Convention-based mapping - entity and DTO property names are identical.
        // Mapster resolves them automatically without explicit rules.
    }
}
