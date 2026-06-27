using Maroik.Core.Contract.Enums;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Contract.Settings;
using Microsoft.Extensions.Options;

namespace Maroik.Core.Service.Services;

/// <summary>
/// Singleton implementation of <see cref="IRsaService"/> that wraps the internal <see cref="Rsa"/> helper.
/// RSA key material is loaded once from <see cref="ServerSetting"/> (appsettings.json) at construction time
/// and reused across all requests, which is why this service is registered as a singleton.
/// The algorithm (SHA-1 vs SHA-256) is driven by <see cref="ServerSetting.RsaAlgorithm"/>.
/// </summary>
public class RsaService : IRsaService
{
    private readonly Rsa _rsa;

    /// <summary>Initializes a new instance of <see cref="RsaService"/> with the supplied dependencies.</summary>
    public RsaService(IOptions<ServerSetting> settings)
    {
        _rsa = new Rsa(
            settings.Value.RsaAlgorithm,
            System.Text.Encoding.UTF8,
            settings.Value.RsaPrivateKey ?? string.Empty,
            settings.Value.RsaPublicKey);
    }

    /// <inheritdoc />
    public string Sign(string data) => _rsa.Sign(data);
    /// <inheritdoc />
    public bool Verify(string data, string sign) => _rsa.Verify(data, sign);
    /// <inheritdoc />
    public string Decrypt(string cipherText) => _rsa.Decrypt(cipherText);
    /// <inheritdoc />
    public string Encrypt(string text) => _rsa.Encrypt(text);
}
