using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Aegis.Exceptions;
using Aegis.Models;
using Microsoft.Extensions.Configuration;

namespace Aegis.Utilities;

public static class LicenseUtils
{
    private static LicensingSecrets? _licenseKeys;
    private static readonly object Lock = new();

    /// <summary>
    /// Gets the licensing secrets. This method should only be accessed from the Aegis assembly.
    /// </summary>
    /// <returns>The licensing secrets.</returns>
    internal static LicensingSecrets GetLicensingSecrets()
    {
        if (_licenseKeys == null)
        {
            lock (Lock)
            {
                var config = new ConfigurationBuilder()
                    .AddUserSecrets(typeof(LicenseUtils).Assembly)
                    .Build();

                _licenseKeys = LoadLicensingSecrets(config.GetSection("LicensingSecrets"));
            }
        }

        return _licenseKeys;
    }

    /// <summary>
    /// Loads the licensing secrets from a configuration section.
    /// </summary>
    /// <param name="config">The configuration section.</param>
    /// <returns>The licensing secrets.</returns>
    public static LicensingSecrets LoadLicensingSecrets(IConfigurationSection config)
    {
        _licenseKeys = new LicensingSecrets
        {
            PublicKey = config.GetSection("PublicKey").Value!,
            PrivateKey = config.GetSection("PrivateKey").Value!,
            EncryptionKey = config.GetSection("EncryptionKey").Value!,
            ApiKey = config.GetSection("ApiKey").Value!
        };

        return _licenseKeys;
    }

    /// <summary>
    /// Loads the encrypted licensing secrets from a file.
    /// </summary>
    /// <param name="secretKey">The secret key used to encrypt the secrets.</param>
    /// <param name="path">The path to the file containing the encrypted secrets.</param>
    /// <returns>The licensing secrets.</returns>
    /// <exception cref="KeyManagementException">Thrown if the secrets cannot be loaded.</exception>
    public static LicensingSecrets LoadLicensingSecrets(string secretKey, string path)
    {
        try
        {
            var bytes = File.ReadAllBytes(path);

            // Decrypt the JSON data using AES
            using var aes = Aes.Create();
            aes.Key = SHA256.HashData(Encoding.UTF8.GetBytes(secretKey));
            aes.IV = new byte[aes.BlockSize / 8];
            var decryptedData = aes.CreateDecryptor(aes.Key, aes.IV).TransformFinalBlock(bytes, 0, bytes.Length);
            var json = Encoding.UTF8.GetString(decryptedData);

            // Deserialize the decrypted JSON to LicenseSignatureKeys
            _licenseKeys = JsonSerializer.Deserialize<LicensingSecrets>(json);

            return _licenseKeys ?? throw new KeyManagementException("Invalid license keys. Failed to load from file.");
        }
        catch (Exception ex) when (ex is IOException or CryptographicException or JsonException)
        {
            throw new KeyManagementException("Failed to load license signature keys.", ex);
        }
    }

    /// <summary>
    /// Generates and saves the licensing secrets to a file.
    /// </summary>
    /// <param name="key">The secret key to use for encryption.</param>
    /// <param name="path">The path to the file where the secrets will be saved.</param>
    /// <param name="apiKey">The API key to use for online validation.</param>
    /// <returns>The generated licensing secrets.</returns>
    /// <exception cref="KeyManagementException">Thrown if the secrets cannot be generated or saved.</exception>
    public static LicensingSecrets GenerateLicensingSecrets(string key, string path, string apiKey = "")
    {
        try
        {
            var rsa = RSA.Create();
            var keys = new LicensingSecrets
            {
                PublicKey = rsa.ToXmlString(false),
                PrivateKey = rsa.ToXmlString(true),
                EncryptionKey = Encoding.UTF8.GetString(SHA256.HashData(Encoding.UTF8.GetBytes(key))),
                ApiKey = apiKey
            };
            var json = JsonSerializer.Serialize(keys);

            // Encrypt the JSON data using AES
            using var aes = Aes.Create();
            aes.Key = SHA256.HashData(Encoding.UTF8.GetBytes(key));
            aes.IV = new byte[aes.BlockSize / 8];
            var encryptedData = Encoding.UTF8.GetBytes(json);
            var decryptedData = aes.CreateEncryptor(aes.Key, aes.IV)
                .TransformFinalBlock(encryptedData, 0, encryptedData.Length);

            File.WriteAllBytes(path, decryptedData);
            return keys;
        }
        catch (Exception ex) when (ex is IOException or CryptographicException)
        {
            throw new KeyManagementException("Failed to generate and save license signature keys.", ex);
        }
    }
}