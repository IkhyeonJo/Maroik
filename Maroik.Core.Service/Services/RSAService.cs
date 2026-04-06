using Maroik.Core.Contract.Utilities;
using Maroik.Core.Contract.Interfaces;
using Microsoft.Extensions.Options;

namespace Maroik.Core.Service.Services;

public class RsaService : IRsaService
{
    private readonly Rsa _rsa;

    public RsaService(IOptions<ServerSetting> settings)
    {
        _rsa = new Rsa(
            settings.Value.RsaAlgorithm,
            System.Text.Encoding.UTF8,
            settings.Value.RsaPrivateKey ?? string.Empty,
            settings.Value.RsaPublicKey);
    }

    public string Sign(string data) => _rsa.Sign(data);
    public bool Verify(string data, string sign) => _rsa.Verify(data, sign);
    public string Decrypt(string cipherText) => _rsa.Decrypt(cipherText);
    public string Encrypt(string text) => _rsa.Encrypt(text);
}
