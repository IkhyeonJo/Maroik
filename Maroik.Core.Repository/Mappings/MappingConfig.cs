using Mapster;

namespace Maroik.Core.Repository.Mappings;

public class MappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // Convention-based mapping - entity and DTO property names are identical.
        // Mapster resolves them automatically without explicit rules.
    }
}
