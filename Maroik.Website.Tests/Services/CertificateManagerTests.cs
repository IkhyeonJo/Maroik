using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Maroik.Website.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace Maroik.Website.Tests.Services;

/// <summary>
/// Unit tests for <see cref="CertificateManager"/>. Certificates and keys are real, self-signed
/// PEM files written to a temp directory - <see cref="CertificateManager"/> only ever touches the
/// filesystem and the .NET X509/ECDsa APIs, so no mocking is needed to exercise it end-to-end.
/// </summary>
public class CertificateManagerTests : IDisposable
{
    private readonly string _tempDir = Directory.CreateTempSubdirectory(nameof(CertificateManagerTests)).FullName;
    private string CertPath => Path.Combine(_tempDir, "cert.pem");
    private string KeyPath => Path.Combine(_tempDir, "privkey.pem");

    private static (string CertPem, string Thumbprint) GenerateCert(ECDsa key, string subject)
    {
        var request = new CertificateRequest(subject, key, HashAlgorithmName.SHA256);
        using var cert = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(30));
        return (cert.ExportCertificatePem(), cert.Thumbprint);
    }

    private void WriteCertFiles(string certPem, string keyPem)
    {
        File.WriteAllText(CertPath, certPem);
        File.WriteAllText(KeyPath, keyPem);
    }

    /// <summary>Verifies the constructor loads the certificate that is on disk at startup.</summary>
    [Fact]
    public void Constructor_LoadsCertificateFromDisk()
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var (certPem, thumbprint) = GenerateCert(key, "CN=initial");
        WriteCertFiles(certPem, key.ExportECPrivateKeyPem());

        using var sut = new CertificateManager(CertPath, KeyPath, NullLogger.Instance);

        Assert.Equal(thumbprint, sut.SelectCertificate().Thumbprint);
    }

    /// <summary>Verifies that polling with no file changes never swaps the in-memory certificate.</summary>
    [Fact]
    public void TryReload_NoFileChange_KeepsSameCertificateInstance()
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var (certPem, _) = GenerateCert(key, "CN=initial");
        WriteCertFiles(certPem, key.ExportECPrivateKeyPem());
        using var sut = new CertificateManager(CertPath, KeyPath, NullLogger.Instance);
        var before = sut.SelectCertificate();

        sut.TryReload();

        Assert.Same(before, sut.SelectCertificate());
    }

    /// <summary>
    /// Regression test: certificate renewal commonly reuses the same private key, so only the cert
    /// file's hash changes while the key file's hash stays identical. Reload must trigger on either
    /// file changing, not only when both change.
    /// </summary>
    [Fact]
    public void TryReload_CertFileChangesButKeyFileDoesNot_StillReloads()
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var (initialCertPem, initialThumbprint) = GenerateCert(key, "CN=initial");
        WriteCertFiles(initialCertPem, key.ExportECPrivateKeyPem());
        using var sut = new CertificateManager(CertPath, KeyPath, NullLogger.Instance);

        var (renewedCertPem, renewedThumbprint) = GenerateCert(key, "CN=renewed");
        File.WriteAllText(CertPath, renewedCertPem); // key file is left untouched

        sut.TryReload();

        var current = sut.SelectCertificate();
        Assert.NotEqual(initialThumbprint, current.Thumbprint);
        Assert.Equal(renewedThumbprint, current.Thumbprint);
    }

    /// <summary>Verifies a corrupt certificate file is logged and swallowed, keeping the previous certificate active.</summary>
    [Fact]
    public void TryReload_WhenNewCertificateFailsToLoad_KeepsPreviousCertificateAndDoesNotThrow()
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var (certPem, thumbprint) = GenerateCert(key, "CN=initial");
        WriteCertFiles(certPem, key.ExportECPrivateKeyPem());
        using var sut = new CertificateManager(CertPath, KeyPath, NullLogger.Instance);

        File.WriteAllText(CertPath, "not a valid certificate");

        var ex = Record.Exception(() => sut.TryReload());

        Assert.Null(ex);
        Assert.Equal(thumbprint, sut.SelectCertificate().Thumbprint);
    }

    public void Dispose()
    {
        Directory.Delete(_tempDir, recursive: true);
    }
}
