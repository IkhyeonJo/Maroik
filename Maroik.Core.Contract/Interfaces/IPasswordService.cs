namespace Maroik.Core.Contract.Interfaces;

/// <summary>
/// Service interface for password hashing and verification using BCrypt.
/// Never store or compare plain-text passwords; always use these methods.
/// </summary>
public interface IPasswordService
{
    /// <summary>
    /// Hashes a plain-text password using BCrypt.
    /// </summary>
    /// <param name="password">The plain-text password to hash.</param>
    /// <returns>A BCrypt-hashed password string safe for database storage.</returns>
    public string HashPassword(string password);

    /// <summary>
    /// Verifies that a plain-text password matches a stored BCrypt hash.
    /// </summary>
    /// <param name="password">The plain-text password entered by the user.</param>
    /// <param name="hashedPassword">The stored BCrypt hash to compare against.</param>
    /// <returns>True if the password matches; otherwise false.</returns>
    public bool VerifyPassword(string password, string hashedPassword);
}
