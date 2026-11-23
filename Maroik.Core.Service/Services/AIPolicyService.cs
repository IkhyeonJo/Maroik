using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Domain.Assistants;

namespace Maroik.Core.Service.Services;

/// <summary>Implementation of <see cref="IAIPolicyService"/> over the domain's <see cref="AssistantPolicy"/>.</summary>
public sealed class AIPolicyService : IAIPolicyService
{
    /// <inheritdoc />
    public AIChatRules GetRules() => new(AssistantPolicy.MaxQuestionLength);
}
