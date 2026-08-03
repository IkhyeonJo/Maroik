using System.Security.Cryptography;
using System.Text;
using Maroik.Core.Contract.Enums;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Contract.Settings;
using Microsoft.Extensions.Options;

namespace Maroik.Core.Service.Services;

/// <summary>
/// Singleton implementation of <see cref="IRsaService"/> backed by <see cref="RSA"/> from
/// <c>System.Security.Cryptography</c>. Keys are OpenSSL-generated (PKCS#1 private key, X.509
/// SubjectPublicKeyInfo public key, both Base64-encoded DER) and imported once at construction
/// time from <see cref="ServerSetting"/> (appsettings.json), which is why this service is
/// registered as a singleton. The algorithm (SHA-1 vs SHA-256) is driven by
/// <see cref="ServerSetting.RsaAlgorithm"/>.
/// </summary>
public class RsaService : IRsaService
{
    private readonly RSA? _privateKeyRsa;
    private readonly RSA? _publicKeyRsa;
    private readonly HashAlgorithmName _hashAlgorithmName;

    /// <summary>Initializes a new instance of <see cref="RsaService"/> with the supplied dependencies.</summary>
    public RsaService(IOptions<ServerSetting> settings)
    {
        ServerSetting setting = settings.Value;

        if (!string.IsNullOrEmpty(setting.RsaPrivateKey))
        {
            _privateKeyRsa = RSA.Create();
            _privateKeyRsa.ImportRSAPrivateKey(Convert.FromBase64String(setting.RsaPrivateKey), out _);
        }

        if (!string.IsNullOrEmpty(setting.RsaPublicKey))
        {
            _publicKeyRsa = RSA.Create();
            _publicKeyRsa.ImportSubjectPublicKeyInfo(Convert.FromBase64String(setting.RsaPublicKey), out _);
        }

        _hashAlgorithmName = setting.RsaAlgorithm == RsaType.Rsa ? HashAlgorithmName.SHA1 : HashAlgorithmName.SHA256;
    }

    /// <inheritdoc cref="data" />
    public string Sign(string data)
    {
        byte[] dataBytes = Encoding.UTF8.GetBytes(data);

        byte[]? signatureBytes = _privateKeyRsa?.SignData(dataBytes, _hashAlgorithmName, RSASignaturePadding.Pkcs1);

        return signatureBytes != null ? Convert.ToBase64String(signatureBytes) : "";
    }

    /// <inheritdoc cref="data" />
    public bool Verify(string data, string sign)
    {
        byte[] dataBytes = Encoding.UTF8.GetBytes(data);
        byte[] signBytes = Convert.FromBase64String(sign);

        return _publicKeyRsa?.VerifyData(dataBytes, signBytes, _hashAlgorithmName, RSASignaturePadding.Pkcs1) ?? false;
    }

    /// <inheritdoc />
    public string Decrypt(string cipherText)
    {
        // OAEP, not PKCS#1 v1.5: the latter is vulnerable to Bleichenbacher padding-oracle attacks
        // when a decrypt failure is distinguishable (by response or timing) from a decrypt success,
        // which is the case here since this decrypts attacker-suppliable email-confirmation/
        // password-reset tokens.
        return _privateKeyRsa == null
            ? throw new InvalidOperationException("RSA private key is not configured.")
            : Encoding.UTF8.GetString(_privateKeyRsa.Decrypt(Convert.FromBase64String(cipherText), RSAEncryptionPadding.CreateOaep(_hashAlgorithmName)));
    }

    /// <inheritdoc />
    public string Encrypt(string text)
    {
        return _publicKeyRsa == null
            ? throw new InvalidOperationException("RSA public key is not configured.")
            : Convert.ToBase64String(_publicKeyRsa.Encrypt(Encoding.UTF8.GetBytes(text), RSAEncryptionPadding.CreateOaep(_hashAlgorithmName)));
    }
}
