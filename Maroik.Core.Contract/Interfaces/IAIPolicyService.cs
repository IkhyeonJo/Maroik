using Maroik.Core.Contract.Dtos;

namespace Maroik.Core.Contract.Interfaces;

/// <summary>The AI chat's rules the page needs (read from the domain's policy).</summary>
public interface IAIPolicyService
{
    /// <summary>The limits the page's form mirrors.</summary>
    AIChatRules GetRules();
}
