using System.Security.Cryptography;
using System.Text;
using Maroik.Core.Contract.Enums;

namespace Maroik.Core.Service.Services;

/// <summary>
/// RSA encryption/decryption using OpenSSL public key encryption / private key decryption.
///
/// Public/private keys must be generated with openssl. Keys generated via ssh-keygen -t rsa are NOT compatible.
///
/// Author: Li Zhiqiang
/// Date: 2017-10-30 15:50:14
/// QQ: 501232752
/// </summary>
internal sealed class Rsa
{
    private readonly RSA? _privateKeyRsaProvider;
    private readonly RSA? _publicKeyRsaProvider;
    private readonly HashAlgorithmName _hashAlgorithmName;
    private readonly Encoding _encoding;

    /// <summary>
    /// Instantiate RSAHelper.
    /// </summary>
    /// <param name="rsaType">Encryption algorithm type: Rsa=SHA1, Rsa2=SHA256. Key length must be at least 2048 bits.</param>
    /// <param name="encoding">Encoding type.</param>
    /// <param name="privateKey">Private key.</param>
    /// <param name="publicKey">Public key.</param>
    public Rsa(RsaType rsaType, Encoding encoding, string privateKey, string? publicKey = null)
    {
        _encoding = encoding;
        if (!string.IsNullOrEmpty(privateKey))
        {
            _privateKeyRsaProvider = CreateRsaProviderFromPrivateKey(privateKey);
        }

        if (!string.IsNullOrEmpty(publicKey))
        {
            _publicKeyRsaProvider = CreateRsaProviderFromPublicKey(publicKey);
        }

        _hashAlgorithmName = rsaType == RsaType.Rsa ? HashAlgorithmName.SHA1 : HashAlgorithmName.SHA256;
    }

    #region Sign with private key

    /// <summary>
    /// Sign with private key.
    /// </summary>
    /// <param name="data">Original data.</param>
    /// <returns></returns>
    public string Sign(string data)
    {
        byte[] dataBytes = _encoding.GetBytes(data);

        byte[]? signatureBytes = _privateKeyRsaProvider?.SignData(dataBytes, _hashAlgorithmName, RSASignaturePadding.Pkcs1);

        return signatureBytes != null ? Convert.ToBase64String(signatureBytes) : string.Empty;
    }

    #endregion

    #region Verify signature with public key

    /// <summary>
    /// Verify signature with public key.
    /// </summary>
    /// <param name="data">Original data.</param>
    /// <param name="sign">Signature.</param>
    /// <returns></returns>
    public bool Verify(string data, string sign)
    {
        byte[] dataBytes = _encoding.GetBytes(data);
        byte[] signBytes = Convert.FromBase64String(sign);

        bool verify = _publicKeyRsaProvider?.VerifyData(dataBytes, signBytes, _hashAlgorithmName, RSASignaturePadding.Pkcs1) ?? false;

        return verify;
    }

    #endregion

    #region Decrypt

    /// <summary>Decrypts a Base64-encoded ciphertext using the private key (PKCS#1 padding).</summary>
    public string Decrypt(string cipherText)
    {
        return _privateKeyRsaProvider == null
            ? throw new Exception("_privateKeyRsaProvider is null")
            : Encoding.UTF8.GetString(_privateKeyRsaProvider.Decrypt(Convert.FromBase64String(cipherText), RSAEncryptionPadding.Pkcs1));
    }

    #endregion

    #region Encrypt

    /// <summary>Encrypts a UTF-8 string using the public key (PKCS#1 padding) and returns a Base64-encoded ciphertext.</summary>
    public string Encrypt(string text)
    {
        return _publicKeyRsaProvider == null
            ? throw new Exception("_publicKeyRsaProvider is null")
            : Convert.ToBase64String(_publicKeyRsaProvider.Encrypt(Encoding.UTF8.GetBytes(text), RSAEncryptionPadding.Pkcs1));
    }

    #endregion

    #region Create RSA instance from private key

    /// <summary>Parses a DER-encoded PKCS#1 private key (Base64) and returns an <see cref="System.Security.Cryptography.RSA"/> instance.</summary>
    private static RSA CreateRsaProviderFromPrivateKey(string privateKey)
    {
        byte[] privateKeyBits = Convert.FromBase64String(privateKey);

        RSA rsa = RSA.Create();
        RSAParameters rsaParameters = new();

        using (BinaryReader binr = new(new MemoryStream(privateKeyBits)))
        {
            var twobytes = binr.ReadUInt16();
            _ = twobytes switch
            {
                0x8130 => binr.ReadByte(),
                0x8230 => binr.ReadInt16(),
                _ => throw new Exception("Unexpected value read binr.ReadUInt16()")
            };

            twobytes = binr.ReadUInt16();
            if (twobytes != 0x0102)
            {
                throw new Exception("Unexpected version");
            }

            var bt = binr.ReadByte();
            if (bt != 0x00)
            {
                throw new Exception("Unexpected value read binr.ReadByte()");
            }

            rsaParameters.Modulus = binr.ReadBytes(GetIntegerSize(binr));
            rsaParameters.Exponent = binr.ReadBytes(GetIntegerSize(binr));
            rsaParameters.D = binr.ReadBytes(GetIntegerSize(binr));
            rsaParameters.P = binr.ReadBytes(GetIntegerSize(binr));
            rsaParameters.Q = binr.ReadBytes(GetIntegerSize(binr));
            rsaParameters.DP = binr.ReadBytes(GetIntegerSize(binr));
            rsaParameters.DQ = binr.ReadBytes(GetIntegerSize(binr));
            rsaParameters.InverseQ = binr.ReadBytes(GetIntegerSize(binr));
        }

        rsa.ImportParameters(rsaParameters);
        return rsa;
    }

    #endregion

    #region Create RSA instance from public key

    /// <summary>Parses a DER-encoded X.509 SubjectPublicKeyInfo public key (Base64) and returns an <see cref="System.Security.Cryptography.RSA"/> instance.</summary>
    private static RSA? CreateRsaProviderFromPublicKey(string publicKeyString)
    {
        // encoded OID sequence for  PKCS #1 rsaEncryption szOID_RSA_RSA = "1.2.840.113549.1.1.1"
        byte[] seqOid = [0x30, 0x0D, 0x06, 0x09, 0x2A, 0x86, 0x48, 0x86, 0xF7, 0x0D, 0x01, 0x01, 0x01, 0x05, 0x00];
        _ = new byte[15];

        byte[] x509Key = Convert.FromBase64String(publicKeyString);

        // ---------  Set up stream to read the asn.1 encoded SubjectPublicKeyInfo blob  ------
        using MemoryStream mem = new(x509Key);
        using BinaryReader binr = new(mem);  //wrap Memory Stream with BinaryReader for easy reading

        var twobytes = binr.ReadUInt16();
        switch (twobytes)
        {
            //data read as little endian order (actual data order for Sequence is 30 81)
            case 0x8130:
                _ = binr.ReadByte();    //advance 1 byte
                break;
            case 0x8230:
                _ = binr.ReadInt16();   //advance 2 bytes
                break;
            default:
                return null;
        }

        byte[] seq = binr.ReadBytes(15);
        if (!CompareBytearrays(seq, seqOid))    //make sure Sequence for OID is correct
        {
            return null;
        }

        twobytes = binr.ReadUInt16();
        switch (twobytes)
        {
            //data read as little endian order (actual data order for Bit String is 03 81)
            case 0x8103:
                _ = binr.ReadByte();    //advance 1 byte
                break;
            case 0x8203:
                _ = binr.ReadInt16();   //advance 2 bytes
                break;
            default:
                return null;
        }

        var bt = binr.ReadByte();
        if (bt != 0x00)     //expect null byte next
        {
            return null;
        }

        twobytes = binr.ReadUInt16();
        switch (twobytes)
        {
            //data read as little endian order (actual data order for Sequence is 30 81)
            case 0x8130:
                _ = binr.ReadByte();    //advance 1 byte
                break;
            case 0x8230:
                _ = binr.ReadInt16();   //advance 2 bytes
                break;
            default:
                return null;
        }

        twobytes = binr.ReadUInt16();
        byte highbyte = 0x00;

        switch (twobytes)
        {
            //data read as little endian order (actual data order for Integer is 02 81)
            case 0x8102:
                // read next bytes which is bytes in modulus
                break;
            case 0x8202:
                highbyte = binr.ReadByte(); //advance 2 bytes
                break;
            default:
                return null;
        }

        var lowbyte = binr.ReadByte(); // read next bytes which is bytes in modulus

        byte[] modint = [lowbyte, highbyte, 0x00, 0x00];   //reverse byte order since asn.1 key uses big endian order
        int modsize = BitConverter.ToInt32(modint, 0);

        int firstbyte = binr.PeekChar();
        if (firstbyte == 0x00)
        {   //if first byte (highest order) of modulus is zero, don't include it
            _ = binr.ReadByte();    //skip this null byte
            modsize -= 1;   //reduce modulus buffer size by 1
        }

        byte[] modulus = binr.ReadBytes(modsize);   //read the modulus bytes

        if (binr.ReadByte() != 0x02)            //expect an Integer for the exponent data
        {
            return null;
        }

        int expbytes = binr.ReadByte();        // should only need one byte for actual exponent data (for all useful values)
        byte[] exponent = binr.ReadBytes(expbytes);

        // ------- create RSACryptoServiceProvider instance and initialize with public key -----
        RSA rsa = RSA.Create();
        RSAParameters rsaKeyInfo = new()
        {
            Modulus = modulus,
            Exponent = exponent
        };
        rsa.ImportParameters(rsaKeyInfo);

        return rsa;
    }

    #endregion

    #region Key parsing helpers

    /// <summary>
    /// Reads a DER-encoded INTEGER size from <paramref name="binr"/>.
    /// Handles single-byte (0x81) and two-byte (0x82) length encodings and
    /// skips any leading zero padding bytes included by DER for sign-safety.
    /// </summary>
    private static int GetIntegerSize(BinaryReader binr)
    {
        byte bt = binr.ReadByte();
        if (bt != 0x02)
        {
            return 0;
        }

        bt = binr.ReadByte();
        int count;
        switch (bt)
        {
            case 0x81:
                count = binr.ReadByte();
                break;
            case 0x82:
            {
                byte highbyte = binr.ReadByte();
                byte lowbyte = binr.ReadByte();
                byte[] modint = [lowbyte, highbyte, 0x00, 0x00];
                count = BitConverter.ToInt32(modint, 0);
                break;
            }
            default:
                count = bt;
                break;
        }

        while (binr.ReadByte() == 0x00)
        {
            count -= 1;
        }
        _ = binr.BaseStream.Seek(-1, SeekOrigin.Current);
        return count;
    }

    /// <summary>Returns <see langword="true"/> when arrays <paramref name="a"/> and <paramref name="b"/> are equal in length and content.</summary>
    private static bool CompareBytearrays(byte[] a, byte[] b)
    {
        if (a.Length != b.Length)
        {
            return false;
        }

        int i = 0;
        foreach (byte c in a)
        {
            if (c != b[i])
            {
                return false;
            }

            i++;
        }
        return true;
    }

    #endregion
}
