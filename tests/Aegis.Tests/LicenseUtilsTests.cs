using Aegis.Exceptions;
using Aegis.Models;
using Aegis.Utilities;
using Microsoft.Extensions.Configuration;

namespace Aegis.Tests;

public class LicenseUtilsTests
{
    private const string TestKey = "MySecretTestKey";
    private const string TestApiKey = "12345678-90ab-cdef-ghij-klmnopqrst";

    [Fact]
    public void LoadLicensingSecrets_LoadsKeysFromConfigurationSection()
    {
        // Arrange
        var config = new Dictionary<string, string>
        {
            { "LicensingSecrets:PublicKey", "TestPublicKey" },
            { "LicensingSecrets:PrivateKey", "TestPrivateKey" },
            { "LicensingSecrets:EncryptionKey", "TestEncryptionKey" },
            { "LicensingSecrets:ApiKey", TestApiKey }
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(config!)
            .Build();
        var section = configuration.GetSection("LicensingSecrets");

        // Act
        var keys = LicenseUtils.LoadLicensingSecrets(section);

        // Assert
        Assert.Equal("TestPublicKey", keys.PublicKey);
        Assert.Equal("TestPrivateKey", keys.PrivateKey);
        Assert.Equal("TestEncryptionKey", keys.EncryptionKey);
        Assert.Equal(TestApiKey, keys.ApiKey);
    }
        
    [Fact]
    public void LoadLicensingSecrets_ThrowsException_ForInvalidPath()
    {
        // Arrange
        const string invalidPath = "Invalid/Path";

        // Act & Assert
        Assert.Throws<KeyManagementException>(() =>
            LicenseUtils.LoadLicensingSecrets(TestKey, invalidPath));
    }

    [Fact]
    public void LoadLicensingSecrets_ThrowsException_ForInvalidData()
    {
        // Arrange
        var filePath = Path.GetTempFileName();
        File.WriteAllText(filePath, "Invalid JSON Data"); // Write invalid JSON

        // Act & Assert
        Assert.Throws<KeyManagementException>(() =>
            LicenseUtils.LoadLicensingSecrets(TestKey, filePath));

        // Clean up
        File.Delete(filePath);
    }
        
    [Fact]
    public void LoadLicensingSecrets_LoadsKeysFromFileCorrectly()
    {
        // Arrange
        var keys = GenerateLicensingSecrets(TestKey, out var filePath);

        // Act
        var loadedKeys = LicenseUtils.LoadLicensingSecrets(TestKey, filePath);

        // Assert
        Assert.Equal(keys.PublicKey, loadedKeys.PublicKey);
        Assert.Equal(keys.PrivateKey, loadedKeys.PrivateKey);
        Assert.Equal(keys.EncryptionKey, loadedKeys.EncryptionKey);
        Assert.Equal(keys.ApiKey, loadedKeys.ApiKey);

        // Clean up
        File.Delete(filePath);
    }

    [Fact]
    public void GenerateLicensingSecrets_GeneratesAndSavesKeysCorrectly()
    {
        // Arrange & Act
        var keys = GenerateLicensingSecrets(TestKey, out var filePath);

        // Assert
        Assert.NotNull(keys);
        Assert.NotEmpty(keys.PublicKey);
        Assert.NotEmpty(keys.PrivateKey);
        Assert.NotEmpty(keys.EncryptionKey);
        Assert.NotEmpty(keys.ApiKey);
        Assert.True(File.Exists(filePath));

        // Clean up
        File.Delete(filePath);
    }
        
    [Fact]
    public void GenerateLicensingSecrets_ThrowsException_ForInvalidPath()
    {
        // Arrange
        const string invalidPath = "Invalid/Path";

        // Act & Assert
        Assert.Throws<KeyManagementException>(() =>
            GenerateLicensingSecrets(TestKey, out _, invalidPath));
    }

    // Helper method to generate keys and save them to a file
    private LicensingSecrets GenerateLicensingSecrets(string key, out string filePath, string? overriddenFilePath = null)
    {
        filePath = Path.GetTempFileName();
        var keys = LicenseUtils.GenerateLicensingSecrets(key, overriddenFilePath ?? filePath, TestApiKey);
        return keys;
    }
}