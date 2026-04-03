using Maroik.Core.Contract.Interfaces;

namespace Maroik.Core.Service.Services;

public class PasswordService : IPasswordService
{
    private const int WorkFactor = 13; // BCrypt work factor

    /// <summary>
    /// Hashes the password using BCrypt.
    /// </summary>
    /// <param name="password">Original password</param>
    /// <returns>BCrypt hashed password</returns>
    public string HashPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password cannot be null or empty.", nameof(password));

        return BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
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
}