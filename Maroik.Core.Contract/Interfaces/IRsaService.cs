namespace Maroik.Core.Contract.Interfaces;

/// <summary>
/// Service interface for RSA cryptographic operations used to protect sensitive data
/// (e.g. passwords transmitted from the browser before HTTPS termination).
/// The algorithm variant (RSA / RSA2) is configured via <see cref="Maroik.Core.Contract.Utilities.ServerSetting.RsaAlgorithm"/>.
/// </summary>
public interface IRsaService
{
    /// <summary>Signs <paramref name="data"/> with the private key and returns a Base64 signature.</summary>
    string Sign(string data);

    /// <summary>Verifies that <paramref name="sign"/> is a valid signature for <paramref name="data"/> using the public key.</summary>
    bool Verify(string data, string sign);

    /// <summary>Decrypts a Base64-encoded RSA cipher text using the private key.</summary>
    string Decrypt(string cipherText);

    /// <summary>Encrypts plain text using the public key and returns a Base64-encoded cipher text.</summary>
    string Encrypt(string text);
}
