using Maroik.Core.Domain.Assistants;
using Maroik.Core.Service.Services;

namespace Maroik.Core.Service.Tests.Services;

/// <summary>The AI chat's limits the page mirrors come from the domain's policy.</summary>
public class AIPolicyServiceTests
{
    /// <summary>The longest question is the domain's.</summary>
    [Fact]
    public void GetRules_ReturnsTheDomainsLimits()
    {
        Assert.Equal(AssistantPolicy.MaxQuestionLength, new AIPolicyService().GetRules().MaxQuestionLength);
    }
}
