using Maroik.Core.Contract.Interfaces;

namespace Maroik.Core.Service.Services;

/// <summary>
/// BCrypt-based implementation of <see cref="IPasswordService"/>.
/// Uses a work factor of 13 (2^13 iterations) which balances security and hash speed
/// — high enough to be brute-force resistant, low enough for interactive login latency.
/// </summary>
public class PasswordService : IPasswordService
{
    private const int WorkFactor = 13; // BCrypt work factor

    /// <summary>
    /// A valid BCrypt hash of an unguessable random value, computed once per process. Used only by
    /// <see cref="VerifyDummy"/> to spend the same CPU as a real verification on the
    /// "account not found" path, so login timing does not reveal whether an email exists.
    /// </summary>
    private static readonly string DummyHash =
        BCrypt.Net.BCrypt.HashPassword("maroik-timing-equalizer-" + Guid.NewGuid().ToString("N"), WorkFactor);

    /// <summary>
    /// Hashes the password using BCrypt.
    /// </summary>
    /// <param name="password">Original password</param>
    /// <returns>BCrypt hashed password</returns>
    public string HashPassword(string password)
    {
        return string.IsNullOrWhiteSpace(password) ? throw new ArgumentException("Password cannot be null or empty.", nameof(password)) : BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
    }

    /// <summary>
    /// Verifies the password using BCrypt.
    /// </summary>
    /// <param name="password">Input password</param>
    /// <param name="hashedPassword">Stored hashed password</param>
    /// <returns>Whether password matches</returns>
    public bool VerifyPassword(string password, string hashedPassword)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(hashedPassword))
            return false;

        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc />
    public bool VerifyDummy(string password)
    {
        _ = VerifyPassword(password, DummyHash);
        return false;
    }
}
