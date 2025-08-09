using System.Security.Cryptography;
using System.Text;

namespace Maroik.Common.Miscellaneous.Utilities;

#region Code Example
//string plaintext = "plaintext";

//var rsa = new RSA(RSAType.RSA2, Encoding.UTF8, RSA.privateKey, RSA.publicKey);

//var aa = rsa.Encrypt(plaintext);
//var bb = rsa.Decrypt(aa);
//var cc = rsa.Sign(plaintext);
//var dd = rsa.Verify(plaintext, cc);
#endregion
/// <summary>
/// RSA加解密 使用OpenSSL的公钥加密/私钥解密
/// 
/// 公私钥请使用openssl生成  ssh-keygen -t rsa 命令生成的公钥私钥是不行的
/// 
/// 作者：李志强
/// 时间：2017年10月30日15:50:14
/// QQ:501232752
/// </summary>
public class Rsa
{
    public static readonly string PublicKey = """
                                              MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAsl8ck2CrGSBElRMNBa8l
                                              yE1mTPl6nY53L3V6b2GMJysT3cV+ISsCPvC3mjIQExFRLtNpx79uT9t6huKdi8Sr
                                              BeHdQLwwF+qwbpFLMWgz5yJ4YmUjamXaOU9Q6NsfTzkhqLzzE3zv678hdogtAcqW
                                              a72n7QERAjDKBij6COILmBptJtHwgOnMgZpyKk2CcRgzRJjLPrCU3LfiyLYXz7hS
                                              IjrKUUJkDrik/yt+mNVNLQsqeQB7jTwDmrMCdHFAoRH0R44lMcW6Bu3gqUVhdsWM
                                              3VGjmpRAIYm/gtBYlZhQ1ckS4uxrAmSgMM6iZ39yf7vPIVUHAwQV/W84NQlocwwX
                                              oQIDAQAB
                                              """;
    public static readonly string PrivateKey = """
                                               MIIEpQIBAAKCAQEAsl8ck2CrGSBElRMNBa8lyE1mTPl6nY53L3V6b2GMJysT3cV+
                                               ISsCPvC3mjIQExFRLtNpx79uT9t6huKdi8SrBeHdQLwwF+qwbpFLMWgz5yJ4YmUj
                                               amXaOU9Q6NsfTzkhqLzzE3zv678hdogtAcqWa72n7QERAjDKBij6COILmBptJtHw
                                               gOnMgZpyKk2CcRgzRJjLPrCU3LfiyLYXz7hSIjrKUUJkDrik/yt+mNVNLQsqeQB7
                                               jTwDmrMCdHFAoRH0R44lMcW6Bu3gqUVhdsWM3VGjmpRAIYm/gtBYlZhQ1ckS4uxr
                                               AmSgMM6iZ39yf7vPIVUHAwQV/W84NQlocwwXoQIDAQABAoIBAQCHbFjIuAMPL4WC
                                               XRVs9F5OJh1+YYcragepO5s6RyhecIFDxfaN/hZVbPMomcvEfkhRsedE5V0+Ohdb
                                               o2ATxrxF2gbeaEJnViKfv6YEG+YUHQKjScBKvouhKUv3jq3Fu02Hy/UNHbUWGmWw
                                               lZD+JC5yHGsLMN0V+a7YYHH9HonI7zrgmzG8ud7Zjw5WRnPWRefutJx/lCML8vAD
                                               5tvhkII+JWiifrSF3F1tK4Kq49egLUYlI7xByaJED/58Xknx8FLysKwAGpEY6UTB
                                               gsyKBCbFgFHX9mawrNQ2yxYh7RG60m8emOiQKmvfwaRdWfsY9YloqAUVUAzFE29d
                                               EMAS/qv1AoGBANhi2ETvoRxHnZAWnFu4FTth3B3MJPFRsVA5eXfIJPEcDZbRV8lv
                                               Qujdgs3fhC5Em/LU0wSa4S/z85MuUwVxaoQLLIK09G9vlo25YTnRCpCRmKVaVYc3
                                               SrobRiWoTpY1Dq+vq6GYup8DRIcpWo//n8g2R6wxn8KXAl1oHWyQuA23AoGBANMG
                                               rLwzpEgtR7e9+4GcUbjKEv/WrJ13ZHVvOamnWhCMOJ568dE0DakGe70/+seeIXwm
                                               azp25/bZku7FOLC6Ti73XPRQm9bBC200Fd2bSFHlICi2Exwi1AlVADcmoMd0dFf9
                                               NGRBpXTKVgM7Jd16raD2uVaSVDIdzWtFBrYlFgVnAoGBAM+FXGwgdJ6ugAmj5aiA
                                               zrv4iiV5lyRoeGi79iZOKD+j69RsSIjCz6SMa9oNNrM40Wb9fLK55qUajMrmbnCV
                                               6NVh8foYmSQWHIkm9HuCY2Nqfr519/iJqnz5kziiRVwA3mjXTP7ZfoDpnVsS6euX
                                               +CEHDY2yucjhEj+Kjk2ELT/ZAoGAf03XhvHgeWpXiZEi2N3krwW40H5BQ/1yrSWc
                                               M6yxuKnevjyNBRfR5dXzcAfY0fOjX7INngd9iH1ZFpID0RmTcOHpQbofST7MFeUG
                                               C9pRlpYTAHxWqvKX18lfh+XVuH2YcjxeDt1LY1ls6iYUgbgwZppw1LkR8enosn3B
                                               vsBAuKECgYEAzHTJNiXihMzbxk9GLNhALNNVsc8aS1wsVpThCK7Eqz8hUgEHoDKL
                                               RqoMA0zpFiGuqxAcXk8FpAzUMdxgXKNwWYxSAPEmBgUdfJQk63f+icq2I96kVnWr
                                               N3rZi5QlO2mf69+Tv8/GMec0/c4ORS2oT+Wm2yV0nfJaLptlbxUgIEg=
                                               """;
    private readonly RSA? _privateKeyRsaProvider;
    private readonly RSA? _publicKeyRsaProvider;
    private readonly HashAlgorithmName _hashAlgorithmName;
    private readonly Encoding _encoding;

    /// <summary>
    /// 实例化RSAHelper
    /// </summary>
    /// <param name="rsaType">加密算法类型 RSA SHA1;RSA2 SHA256 密钥长度至少为2048</param>
    /// <param name="encoding">编码类型</param>
    /// <param name="privateKey">私钥</param>
    /// <param name="publicKey">公钥</param>
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

    #region 使用私钥签名

    /// <summary>
    /// 使用私钥签名
    /// </summary>
    /// <param name="data">原始数据</param>
    /// <returns></returns>
    public string Sign(string data)
    {
        var dataBytes = _encoding.GetBytes(data);

        var signatureBytes = _privateKeyRsaProvider?.SignData(dataBytes, _hashAlgorithmName, RSASignaturePadding.Pkcs1);

        return signatureBytes != null ? Convert.ToBase64String(signatureBytes) : string.Empty;
    }

    #endregion

    #region 使用公钥验证签名

    /// <summary>
    /// 使用公钥验证签名
    /// </summary>
    /// <param name="data">原始数据</param>
    /// <param name="sign">签名</param>
    /// <returns></returns>
    public bool Verify(string data, string sign)
    {
        var dataBytes = _encoding.GetBytes(data);
        var signBytes = Convert.FromBase64String(sign);

        var verify = _publicKeyRsaProvider?.VerifyData(dataBytes, signBytes, _hashAlgorithmName, RSASignaturePadding.Pkcs1) ?? false;

        return verify;
    }

    #endregion

    #region 解密

    public string Decrypt(string cipherText)
    {
        return _privateKeyRsaProvider == null
            ? throw new Exception("_privateKeyRsaProvider is null")
            : Encoding.UTF8.GetString(_privateKeyRsaProvider.Decrypt(Convert.FromBase64String(cipherText), RSAEncryptionPadding.Pkcs1));
    }

    #endregion

    #region 加密

    public string Encrypt(string text)
    {
        return _publicKeyRsaProvider == null
            ? throw new Exception("_publicKeyRsaProvider is null")
            : Convert.ToBase64String(_publicKeyRsaProvider.Encrypt(Encoding.UTF8.GetBytes(text), RSAEncryptionPadding.Pkcs1));
    }

    #endregion

    #region 使用私钥创建RSA实例

    private RSA CreateRsaProviderFromPrivateKey(string privateKey)
    {
        var privateKeyBits = Convert.FromBase64String(privateKey);

        var rsa = RSA.Create();
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

    #region 使用公钥创建RSA实例

    private RSA? CreateRsaProviderFromPublicKey(string publicKeyString)
    {
        // encoded OID sequence for  PKCS #1 rsaEncryption szOID_RSA_RSA = "1.2.840.113549.1.1.1"
        byte[] seqOid = [0x30, 0x0D, 0x06, 0x09, 0x2A, 0x86, 0x48, 0x86, 0xF7, 0x0D, 0x01, 0x01, 0x01, 0x05, 0x00];
        _ = new byte[15];

        var x509Key = Convert.FromBase64String(publicKeyString);

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

        var seq = binr.ReadBytes(15);
        if (!CompareBytearrays(seq, seqOid))    //make sure Sequence for OID is correct
        {
            return null;
        }

        twobytes = binr.ReadUInt16();
        switch (twobytes)
        {
            //data read as a little endian order (actual data order for Bit String is 03 81)
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
                // read next bytes that are bytes in modulus
                break;
            case 0x8202:
                highbyte = binr.ReadByte(); //advance 2 bytes
                break;
            default:
                return null;
        }

        var lowbyte = binr.ReadByte(); // read next bytes that are bytes in modulus

        byte[] modInt = [lowbyte, highbyte, 0x00, 0x00];   //reverse byte order since asn.1 key uses big endian order
        var modSize = BitConverter.ToInt32(modInt, 0);

        var firstByte = binr.PeekChar();
        if (firstByte == 0x00)
        {   //if the first byte (the highest order) of modulus is zero, don't include it
            _ = binr.ReadByte();    //skip this null byte
            modSize -= 1;   //reduce modulus buffer size by 1
        }

        var modulus = binr.ReadBytes(modSize);   //read the modulus bytes

        if (binr.ReadByte() != 0x02)            //expect an Integer for the exponent data
        {
            return null;
        }

        int expBytes = binr.ReadByte();        // should only need one byte for actual exponent data (for all useful values)
        var exponent = binr.ReadBytes(expBytes);

        // ------- create RSACryptoServiceProvider instance and initialize with a public key -----
        var rsa = RSA.Create();
        RSAParameters rsaKeyInfo = new()
        {
            Modulus = modulus,
            Exponent = exponent
        };
        rsa.ImportParameters(rsaKeyInfo);

        return rsa;
    }

    #endregion

    #region 导入密钥算法

    private int GetIntegerSize(BinaryReader binr)
    {
        var bt = binr.ReadByte();
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
                var highbyte = binr.ReadByte();
                var lowbyte = binr.ReadByte();
                byte[] modInt = [lowbyte, highbyte, 0x00, 0x00];
                count = BitConverter.ToInt32(modInt, 0);
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

    private static bool CompareBytearrays(byte[] a, byte[] b)
    {
        if (a.Length != b.Length)
        {
            return false;
        }

        var i = 0;
        foreach (var c in a)
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

/// <summary>
/// RSA算法类型
/// </summary>
public enum RsaType
{
    /// <summary>
    /// SHA1
    /// </summary>
    Rsa = 0,
    /// <summary>
    /// RSA2 密钥长度至少为2048
    /// SHA256
    /// </summary>
    Rsa2
}