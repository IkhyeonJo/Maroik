using Maroik.WebSite.Contracts;

namespace Maroik.WebSite.Services;

public class PasswordService : IPasswordService
{
    private const int WorkFactor = 13; // BCrypt work factor

    /// <summary>
    /// BCrypt를 사용하여 패스워드를 해싱합니다.
    /// </summary>
    /// <param name="password">원본 패스워드</param>
    /// <returns>BCrypt 해싱된 패스워드</returns>
    public string HashPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password cannot be null or empty.", nameof(password));

        return BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
    }

    /// <summary>
    /// BCrypt를 사용하여 패스워드를 검증합니다.
    /// </summary>
    /// <param name="password">입력된 패스워드</param>
    /// <param name="hashedPassword">저장된 해싱된 패스워드</param>
    /// <returns>패스워드 일치 여부</returns>
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