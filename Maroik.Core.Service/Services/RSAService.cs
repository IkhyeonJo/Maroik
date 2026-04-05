using Maroik.Core.Client.Utilities;
using Maroik.Core.Contract.Interfaces;
using Microsoft.Extensions.Options;

namespace Maroik.Core.Service.Services;

public class RSAService : IRSAService
{
    private readonly RSA _rsa;

    public RSAService(IOptions<ServerSetting> settings)
    {
        _rsa = new RSA(
            settings.Value.RSAAlgorithm,
            System.Text.Encoding.UTF8,
            settings.Value.RSAPrivateKey ?? string.Empty,
            settings.Value.RSAPublicKey);
    }

    public string Sign(string data) => _rsa.Sign(data);
    public bool Verify(string data, string sign) => _rsa.Verify(data, sign);
    public string Decrypt(string cipherText) => _rsa.Decrypt(cipherText);
    public string Encrypt(string text) => _rsa.Encrypt(text);
}
