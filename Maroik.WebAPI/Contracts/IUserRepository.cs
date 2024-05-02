namespace Maroik.WebAPI.Contracts
{
    public interface IUserRepository
    {
        Task<bool> IsValidUserCredentials(string email, string password);
    }
}