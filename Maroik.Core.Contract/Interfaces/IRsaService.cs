namespace Maroik.Core.Contract.Interfaces;

public interface IRsaService
{
    string Sign(string data);
    bool Verify(string data, string sign);
    string Decrypt(string cipherText);
    string Encrypt(string text);
}
