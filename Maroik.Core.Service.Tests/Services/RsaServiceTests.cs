using Maroik.Core.Contract.Utilities;
using Maroik.Core.Service.Services;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;

namespace Maroik.Core.Service.Tests.Services;

/// <summary>
/// Unit tests for <see cref="RsaService"/>.
///
/// Keys are generated in-test using <see cref="System.Security.Cryptography.RSA"/> .NET APIs:
/// <list type="bullet">
///   <item><see cref="RSA.ExportRSAPrivateKey"/> → PKCS#1 DER → Base64 (matches <c>Rsa.CreateRsaProviderFromPrivateKey</c>)</item>
///   <item><see cref="RSA.ExportSubjectPublicKeyInfo"/> → X.509 SubjectPublicKeyInfo DER → Base64 (matches <c>Rsa.CreateRsaProviderFromPublicKey</c>)</item>
/// </list>
/// A 2048-bit key pair is generated once per test class (class-level static) to keep individual tests fast.
/// </summary>
public class RsaServiceTests
{
    // -- Shared 2048-bit key pair generated once per test class ---------------

    private static readonly string PrivateKeyBase64;
    private static readonly string PublicKeyBase64;

    static RsaServiceTests()
    {
        using var rsa = RSA.Create(2048);
        PrivateKeyBase64 = Convert.ToBase64String(rsa.ExportRSAPrivateKey());
        PublicKeyBase64 = Convert.ToBase64String(rsa.ExportSubjectPublicKeyInfo());
    }

    // -- Helpers --------------------------------------------------------------

    private static RsaService CreateSut(RsaType algorithm = RsaType.Rsa2) =>
        new(Options.Create(new ServerSetting
        {
            RsaAlgorithm = algorithm,
            RsaPrivateKey = PrivateKeyBase64,
            RsaPublicKey = PublicKeyBase64
        }));

    // -- Encrypt / Decrypt ----------------------------------------------------

    /// <summary>Verifies that <c>Encrypt</c> then decrypt when returns original plaintext.</summary>
    [Fact]
    public void Encrypt_ThenDecrypt_ReturnsOriginalPlaintext()
    {
        var sut = CreateSut();
        const string plaintext = "hello-maroik-rsa";

        string cipher = sut.Encrypt(plaintext);
        string recovered = sut.Decrypt(cipher);

        Assert.Equal(plaintext, recovered);
    }

    /// <summary>Verifies that <c>Encrypt</c> then decrypt with sha1 algorithm returns original plaintext.</summary>
    [Fact]
    public void Encrypt_ThenDecrypt_WithSha1Algorithm_ReturnsOriginalPlaintext()
    {
        var sut = CreateSut(RsaType.Rsa);
        const string plaintext = "sha1-roundtrip";

        string cipher = sut.Encrypt(plaintext);
        string recovered = sut.Decrypt(cipher);

        Assert.Equal(plaintext, recovered);
    }

    /// <summary>Verifies that <c>Encrypt</c> produces base64 output.</summary>
    [Fact]
    public void Encrypt_ProducesBase64Output()
    {
        var sut = CreateSut();

        string cipher = sut.Encrypt("test");

        // Must be valid Base64
        var decoded = Convert.FromBase64String(cipher);
        Assert.NotEmpty(decoded);
    }

    /// <summary>Verifies that <c>Encrypt</c> produces different ciphertext each call when due to oaep padding.</summary>
    [Fact]
    public void Encrypt_ProducesDifferentCiphertextEachCall_DueToOaepPadding()
    {
        var sut = CreateSut();
        const string plaintext = "same-plaintext";

        string c1 = sut.Encrypt(plaintext);
        string c2 = sut.Encrypt(plaintext);

        // PKCS#1 v1.5 uses random padding → different ciphertext per call
        Assert.NotEqual(c1, c2);
    }

    /// <summary>Verifies that <c>Encrypt</c> then decrypt when round trips various strings.</summary>
    [Theory]
    [InlineData("short")]
    [InlineData("a longer test string with spaces and punctuation!")]
    [InlineData("unicode: 안녕하세요 🎉")]
    public void Encrypt_ThenDecrypt_RoundTrips_VariousStrings(string plaintext)
    {
        var sut = CreateSut();

        string cipher = sut.Encrypt(plaintext);
        string recovered = sut.Decrypt(cipher);

        Assert.Equal(plaintext, recovered);
    }

    /// <summary>Verifies that <c>Decrypt</c> throws when ciphertext is invalid.</summary>
    [Fact]
    public void Decrypt_Throws_WhenCiphertextIsInvalid()
    {
        var sut = CreateSut();
        string badCipher = Convert.ToBase64String(new byte[256]); // zeros ?? not a valid RSA ciphertext

        Assert.Throws<CryptographicException>(() => sut.Decrypt(badCipher));
    }

    // -- Sign / Verify --------------------------------------------------------

    /// <summary>Verifies that <c>Sign</c> then verify when returns true.</summary>
    [Fact]
    public void Sign_ThenVerify_ReturnsTrue()
    {
        var sut = CreateSut();
        const string data = "data-to-sign";

        string signature = sut.Sign(data);
        bool valid = sut.Verify(data, signature);

        Assert.True(valid);
    }

    /// <summary>Verifies that <c>Sign</c> then verify with sha1 algorithm returns true.</summary>
    [Fact]
    public void Sign_ThenVerify_WithSha1Algorithm_ReturnsTrue()
    {
        var sut = CreateSut(RsaType.Rsa);
        const string data = "sha1-sign-verify";

        string signature = sut.Sign(data);
        bool valid = sut.Verify(data, signature);

        Assert.True(valid);
    }

    /// <summary>Verifies that <c>Verify</c> returns false when data is tampered.</summary>
    [Fact]
    public void Verify_ReturnsFalse_WhenDataIsTampered()
    {
        var sut = CreateSut();
        string signature = sut.Sign("original");

        bool valid = sut.Verify("tampered", signature);

        Assert.False(valid);
    }

    /// <summary>Verifies that <c>Verify</c> returns false when signature is corrupted.</summary>
    [Fact]
    public void Verify_ReturnsFalse_WhenSignatureIsCorrupted()
    {
        var sut = CreateSut();
        string signature = sut.Sign("data");

        // Flip the last byte of the Base64-decoded signature
        byte[] sigBytes = Convert.FromBase64String(signature);
        sigBytes[^1] ^= 0xFF;
        string corruptedSignature = Convert.ToBase64String(sigBytes);

        bool valid = sut.Verify("data", corruptedSignature);

        Assert.False(valid);
    }

    /// <summary>Verifies that <c>Sign</c> produces non empty base64 string.</summary>
    [Fact]
    public void Sign_ProducesNonEmptyBase64String()
    {
        var sut = CreateSut();

        string signature = sut.Sign("some data");

        Assert.False(string.IsNullOrEmpty(signature));
        var bytes = Convert.FromBase64String(signature); // must be valid Base64
        Assert.NotEmpty(bytes);
    }

    // -- No-key edge cases ----------------------------------------------------

    /// <summary>Verifies that <c>Sign</c> returns empty when private key is not configured.</summary>
    [Fact]
    public void Sign_ReturnsEmpty_WhenPrivateKeyIsNotConfigured()
    {
        // RsaService with no private key → _rsa has null private key provider
        var sut = new RsaService(Options.Create(new ServerSetting
        {
            RsaAlgorithm = RsaType.Rsa2,
            RsaPrivateKey = string.Empty,
            RsaPublicKey = PublicKeyBase64
        }));

        string result = sut.Sign("data");

        Assert.Equal(string.Empty, result);
    }

    /// <summary>Verifies that <c>Verify</c> returns false when public key is not configured.</summary>
    [Fact]
    public void Verify_ReturnsFalse_WhenPublicKeyIsNotConfigured()
    {
        var sut = new RsaService(Options.Create(new ServerSetting
        {
            RsaAlgorithm = RsaType.Rsa2,
            RsaPrivateKey = PrivateKeyBase64,
            RsaPublicKey = null
        }));

        // No public key → Verify returns false (provider is null)
        bool result = sut.Verify("data", Convert.ToBase64String(new byte[256]));

        Assert.False(result);
    }
}
