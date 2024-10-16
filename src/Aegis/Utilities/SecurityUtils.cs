using System.Security.Cryptography;

namespace Aegis.Utilities;

public static class SecurityUtils
{
    /// <summary>
    ///     Encrypts data using AES encryption.
    /// </summary>
    /// <param name="data">The data to encrypt.</param>
    /// <param name="publicKey">The encryption key.</param>
    /// <returns>The encrypted data.</returns>
    /// <exception cref="ArgumentNullException">Thrown if data or key is null.</exception>
    public static byte[] EncryptData(byte[] data, string publicKey)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentException.ThrowIfNullOrEmpty(publicKey);
        
        using var rsa = RSA.Create();
        rsa.ImportRSAPublicKey(Convert.FromBase64String(publicKey), out _);

        // Encrypt the data in chunks
        var keySizeBytes = rsa.KeySize / 16;
        var maxDataLength = keySizeBytes - 42; // Subtract padding overhead (OAEP SHA-256)
        var encryptedData = Array.Empty<byte>();

        for (var i = 0; i < data.Length; i += maxDataLength)
        {
            var bytesToEncrypt = Math.Min(maxDataLength, data.Length - i);
            var chunk = new byte[bytesToEncrypt];
            Array.Copy(data, i, chunk, 0, bytesToEncrypt);

            var encryptedChunk = rsa.Encrypt(chunk, RSAEncryptionPadding.OaepSHA256);
            encryptedData = CombineByteArrays(encryptedData, encryptedChunk);
        }

        return encryptedData;
    }

    /// <summary>
    ///     Decrypts data using AES decryption.
    /// </summary>
    /// <param name="data">The data to decrypt.</param>
    /// <param name="privateKey">The decryption key.</param>
    /// <returns>The decrypted data.</returns>
    /// <exception cref="ArgumentNullException">Thrown if data or key is null.</exception>
    public static byte[] DecryptData(byte[] data, string privateKey)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentException.ThrowIfNullOrEmpty(privateKey);

        using var rsa = RSA.Create();
        rsa.ImportRSAPrivateKey(Convert.FromBase64String(privateKey), out _);

        // Decrypt the data in chunks
        var keySizeBytes = rsa.KeySize / 8;
        var decryptedData = Array.Empty<byte>();

        for (var i = 0; i < data.Length; i += keySizeBytes)
        {
            var bytesToDecrypt = Math.Min(keySizeBytes, data.Length - i);
            var chunk = new byte[bytesToDecrypt];
            Array.Copy(data, i, chunk, 0, bytesToDecrypt);

            var decryptedChunk = rsa.Decrypt(chunk, RSAEncryptionPadding.OaepSHA256);
            decryptedData = CombineByteArrays(decryptedData, decryptedChunk);
        }

        return decryptedData;
    }
    
    // Helper function to combine byte arrays
    private static byte[] CombineByteArrays(byte[] array1, byte[] array2)
    {
        var combined = new byte[array1.Length + array2.Length];
        Array.Copy(array1, 0, combined, 0, array1.Length);
        Array.Copy(array2, 0, combined, array1.Length, array2.Length);
        return combined;
    }

    /// <summary>
    ///     Signs data using RSA signature.
    /// </summary>
    /// <param name="data">The data to sign.</param>
    /// <param name="privateKey">The private key for signing.</param>
    /// <returns>The signature of the data.</returns>
    public static byte[] SignData(byte[] data, string privateKey)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentException.ThrowIfNullOrEmpty(privateKey);

        using var rsa = RSA.Create();
        rsa.ImportRSAPrivateKey(Convert.FromBase64String(privateKey), out _);

        // Sign the data using SHA256 hash algorithm
        var signature = rsa.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return signature;
    }

    /// <summary>
    ///     Verifies the signature of data using RSA signature.
    /// </summary>
    /// <param name="data">The data to verify.</param>
    /// <param name="signature">The signature to verify.</param>
    /// <param name="publicKey">The public key for verification.</param>
    /// <returns>True if the signature is valid, false otherwise.</returns>
    public static bool VerifySignature(byte[] data, byte[] signature, string publicKey)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(signature);
        ArgumentException.ThrowIfNullOrEmpty(publicKey);

        using var rsa = RSA.Create();
        rsa.ImportRSAPublicKey(Convert.FromBase64String(publicKey), out _);

        // Verify the signature using SHA256 hash algorithm
        var verified = rsa.VerifyData(data, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return verified;
    }

    /// <summary>
    ///     Calculates the SHA256 checksum of data.
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
    ///     Verifies the checksum of data.
    /// </summary>
    /// <param name="data">The data to verify.</param>
    /// <param name="checksum">The checksum to verify against.</param>
    /// <returns>True if the checksum is valid, false otherwise.</returns>
    /// <exception cref="ArgumentNullException">Thrown if data or checksum is null.</exception>
    public static bool VerifyChecksum(byte[] data, string checksum)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentException.ThrowIfNullOrEmpty(checksum);

        var calculatedChecksum = CalculateChecksum(data);
        return calculatedChecksum == checksum;
    }
}