namespace Maroik.Core.Service.Contracts;

public interface IPasswordService
{
    /// <summary>
    /// BCrypt를 사용하여 패스워드를 해싱합니다.
    /// </summary>
    /// <param name="password">원본 패스워드</param>
    /// <returns>BCrypt 해싱된 패스워드</returns>
    public string HashPassword(string password);
    /// <summary>
    /// BCrypt를 사용하여 패스워드를 검증합니다.
    /// </summary>
    /// <param name="password">입력된 패스워드</param>
    /// <param name="hashedPassword">저장된 해싱된 패스워드</param>
    /// <returns>패스워드 일치 여부</returns>
    public bool VerifyPassword(string password, string hashedPassword);
}