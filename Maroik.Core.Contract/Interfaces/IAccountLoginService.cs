using Maroik.Core.Contract.Dtos;

namespace Maroik.Core.Contract.Interfaces;

/// <summary>Signing in with an e-mail address and a password.</summary>
public interface IAccountLoginService
{
    /// <summary>Validates credentials and returns a <see cref="LoginResult"/> indicating success or failure.</summary>
    Task<LoginResult> LoginAsync(string email, string password, CancellationToken ct = default);
}
