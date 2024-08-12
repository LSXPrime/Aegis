using System.Security.Cryptography;
using System.Text;

namespace Aegis.Utilities;

public static class SecurityUtils
{
    /// <summary>
    /// Encrypts data using AES encryption.
    /// </summary>
    /// <param name="data">The data to encrypt.</param>
    /// <param name="key">The encryption key.</param>
    /// <returns>The encrypted data.</returns>
    /// <exception cref="ArgumentNullException">Thrown if data or key is null.</exception>
    public static byte[] EncryptData(byte[] data, string key)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(key);
        
        using var aes = Aes.Create();
        aes.Key = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        aes.IV = new byte[aes.BlockSize / 8];

        using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream();
        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        {
            cs.Write(data, 0, data.Length);
        }

        return ms.ToArray();
    }

    /// <summary>
    /// Decrypts data using AES decryption.
    /// </summary>
    /// <param name="data">The data to decrypt.</param>
    /// <param name="key">The decryption key.</param>
    /// <returns>The decrypted data.</returns>
    /// <exception cref="ArgumentNullException">Thrown if data or key is null.</exception>
    public static byte[] DecryptData(byte[] data, string key)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(key);

        using var aes = Aes.Create();
        aes.Key = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        aes.IV = new byte[aes.BlockSize / 8];

        using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream();
        using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Write))
        {
            cs.Write(data, 0, data.Length);
        }

        return ms.ToArray();
    }

    /// <summary>
    /// Signs data using RSA signature.
    /// </summary>
    /// <param name="data">The data to sign.</param>
    /// <param name="privateKey">The private key for signing.</param>
    /// <returns>The signature of the data.</returns>
    public static byte[] SignData(byte[] data, string privateKey)
    {
        using var rsa = RSA.Create();
        rsa.FromXmlString(privateKey);
        return rsa.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
    }

    /// <summary>
    /// Verifies the signature of data using RSA signature.
    /// </summary>
    /// <param name="data">The data to verify.</param>
    /// <param name="signature">The signature to verify.</param>
    /// <param name="publicKey">The public key for verification.</param>
    /// <returns>True if the signature is valid, false otherwise.</returns>
    public static bool VerifySignature(byte[] data, byte[] signature, string publicKey)
    {
        using var rsa = RSA.Create();
        rsa.FromXmlString(publicKey);

        return rsa.VerifyData(data, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
    }

    /// <summary>
    /// Calculates the SHA256 checksum of data.
    /// </summary>
    /// <param name="data">The data to calculate the checksum for.</param>
    /// <returns>The checksum as a base64 encoded string.</returns>
    /// <exception cref="ArgumentNullException">Thrown if data is null.</exception>
    public static string CalculateChecksum(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        
        var hash = SHA256.HashData(data);
        return Convert.ToBase64String(hash);
    }

    /// <summary>
    /// Verifies the checksum of data.
    /// </summary>
    /// <param name="data">The data to verify.</param>
    /// <param name="checksum">The checksum to verify against.</param>
    /// <returns>True if the checksum is valid, false otherwise.</returns>
    /// <exception cref="ArgumentNullException">Thrown if data or checksum is null.</exception>
    public static bool VerifyChecksum(byte[] data, string checksum)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(checksum);
        
        var calculatedChecksum = CalculateChecksum(data);
        return calculatedChecksum == checksum;
    }
}