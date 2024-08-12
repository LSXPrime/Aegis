using System.Reflection;
using System.Text.Json;
using Aegis.Enums;
using Aegis.Exceptions;
using Aegis.Models;
using Aegis.Utilities;

namespace Aegis.Tests;

public class LicenseManagerTests
{
    // Helper method to generate a license for testing
    private BaseLicense GenerateLicense(LicenseType type = LicenseType.Standard)
    {
        return type switch
        {
            LicenseType.Standard => LicenseGenerator.GenerateStandardLicense("TestUser"),
            LicenseType.Trial => LicenseGenerator.GenerateTrialLicense(TimeSpan.FromDays(7)),
            LicenseType.NodeLocked => LicenseGenerator.GenerateNodeLockedLicense("test-hardware-id"),
            LicenseType.Subscription => LicenseGenerator.GenerateSubscriptionLicense("TestUser", TimeSpan.FromDays(30)),
            LicenseType.Concurrent => LicenseGenerator.GenerateConcurrentLicense("TestUser", 6),
            LicenseType.Floating => LicenseGenerator.GenerateFloatingLicense("TestUser", 5),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }

    private void LoadSecretKeys()
    {
        var secretPath = Path.GetTempFileName();
        LicenseUtils.GenerateLicensingSecrets("MySecretTestKey", secretPath, "12345678-90ab-cdef-ghij-klmnopqrst");
        LicenseUtils.LoadLicensingSecrets("MySecretTestKey", secretPath);
    }

    [Fact]
    public void SaveLicense_SavesLicenseToFileCorrectly()
    {
        // Arrange
        LoadSecretKeys();
        var license = GenerateLicense();
        var filePath = Path.GetTempFileName(); // Use a temporary file

        // Act
        LicenseManager.SaveLicense(license, filePath);

        // Assert
        Assert.True(File.Exists(filePath));

        // Clean up
        File.Delete(filePath);
    }

    [Fact]
    public void SaveLicense_ThrowsExceptionForNullLicense()
    {
        // Arrange
        var filePath = Path.GetTempFileName();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => LicenseManager.SaveLicense<BaseLicense>(null!, filePath));

        // Clean up
        File.Delete(filePath);
    }

    [Fact]
    public void SaveLicense_ThrowsExceptionForNullFilePath()
    {
        // Arrange
        var license = GenerateLicense();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => LicenseManager.SaveLicense(license, null!));
    }

    [Fact]
    public void SaveLicense_ThrowsExceptionForEmptyFilePath()
    {
        // Arrange
        LoadSecretKeys();
        var license = GenerateLicense();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => LicenseManager.SaveLicense(license, ""));
    }

    [Fact]
    public void SaveLicense_ThrowsExceptionForInvalidFilePath()
    {
        // Arrange
        LoadSecretKeys();
        var license = GenerateLicense();
        const string filePath = "Invalid/File/Path"; // This should be an invalid path on most systems

        // Act & Assert
        Assert.Throws<DirectoryNotFoundException>(() => LicenseManager.SaveLicense(license, filePath));
    }

    [Fact]
    public async Task LoadLicenseAsync_LoadsLicenseFromFileCorrectly()
    {
        // Arrange
        LoadSecretKeys();
        var license = GenerateLicense();
        var filePath = Path.GetTempFileName();
        LicenseManager.SaveLicense(license, filePath);

        // Act
        var loadedLicense = await LicenseManager.LoadLicenseAsync(filePath);

        // Assert
        Assert.NotNull(loadedLicense);
        Assert.Equal(license.LicenseKey, loadedLicense.LicenseKey);
        Assert.Equal(license.Type, loadedLicense.Type);

        // Clean up
        File.Delete(filePath);
    }

    [Fact]
    public async Task LoadLicenseAsync_ThrowsExceptionForInvalidLicenseFile()
    {
        // Arrange
        var filePath = Path.GetTempFileName();
        await File.WriteAllTextAsync(filePath, "Invalid License Data"); // Corrupt the file

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(async () =>
            await LicenseManager.LoadLicenseAsync(filePath));

        // Clean up
        File.Delete(filePath);
    }

    [Fact]
    public async Task LoadLicenseAsync_ThrowsExceptionForNullFilePath()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await LicenseManager.LoadLicenseAsync(null!, ValidationMode.Offline));
    }

    [Fact]
    public async Task LoadLicenseAsync_ThrowsExceptionForEmptyFilePath()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await LicenseManager.LoadLicenseAsync(""));
    }

    [Fact]
    public async Task LoadLicenseAsync_ThrowsExceptionForInvalidFilePath()
    {
        // Arrange
        const string filePath = "Invalid/File/Path"; // This should be an invalid path

        // Act & Assert
        await Assert.ThrowsAsync<DirectoryNotFoundException>(async () =>
            await LicenseManager.LoadLicenseAsync(filePath));
    }
    
    // IsFeatureEnabled Tests

    [Fact]
    public void IsFeatureEnabled_ReturnsCorrectValue()
    {
        // Arrange
        var license = GenerateLicense();
        license.Features.Add("Feature1", true);
        LicenseManager.Current = license;

        // Act
        var isEnabled = LicenseManager.IsFeatureEnabled("Feature1");

        // Assert
        Assert.True(isEnabled);
    }

    [Fact]
    public void IsFeatureEnabled_ReturnsFalseForNonExistingFeature()
    {
        // Arrange
        var license = GenerateLicense();
        LicenseManager.Current = license;

        // Act
        var isEnabled = LicenseManager.IsFeatureEnabled("NonExistingFeature");

        // Assert
        Assert.False(isEnabled);
    }

    [Fact]
    public void IsFeatureEnabled_ReturnsFalseForDisabledFeature()
    {
        // Arrange
        var license = GenerateLicense();
        license.Features.Add("Feature1", false);
        LicenseManager.Current = license;

        // Act
        var isEnabled = LicenseManager.IsFeatureEnabled("Feature1");

        // Assert
        Assert.False(isEnabled);
    }

    [Fact]
    public void ThrowIfNotAllowed_ThrowsExceptionForDisabledFeature()
    {
        // Arrange
        var license = GenerateLicense();
        license.Features.Add("Feature1", false);
        LicenseManager.Current = license;

        // Act & Assert
        Assert.Throws<FeatureNotLicensedException>(() => LicenseManager.ThrowIfNotAllowed("Feature1"));
    }

    [Fact]
    public void ThrowIfNotAllowed_DoesNotThrowExceptionForEnabledFeature()
    {
        // Arrange
        var license = GenerateLicense();
        license.Features.Add("Feature1", true);
        LicenseManager.Current = license;

        // Act & Assert (no exception should be thrown)
        LicenseManager.ThrowIfNotAllowed("Feature1");
    }

    // SetServerBaseEndpoint Tests
    
    [Fact]
    public void SetServerBaseEndpoint_SetsEndpointCorrectly()
    {
        // Arrange
        const string newEndpoint = "https://new-api-endpoint.com";

        // Act
        LicenseManager.SetServerBaseEndpoint(newEndpoint);

        // Assert
        var serverBaseEndpointField = typeof(LicenseManager).GetField("_serverBaseEndpoint",
            BindingFlags.NonPublic | BindingFlags.Static);
        var serverBaseEndpointValue = serverBaseEndpointField!.GetValue(null);
        Assert.Equal(newEndpoint, serverBaseEndpointValue);
    }

    [Fact]
    public void SetServerBaseEndpoint_ThrowsExceptionForNullEndpoint()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => LicenseManager.SetServerBaseEndpoint(null!));
    }

    [Fact]
    public void SetServerBaseEndpoint_ThrowsExceptionForEmptyEndpoint()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => LicenseManager.SetServerBaseEndpoint(""));
    }

    // SetHeartbeatInterval Tests

    [Fact]
    public void SetHeartbeatInterval_SetsIntervalCorrectly()
    {
        // Arrange
        var newInterval = TimeSpan.FromMinutes(15);

        // Act
        LicenseManager.SetHeartbeatInterval(newInterval);

        // Assert
        var heartbeatIntervalField = typeof(LicenseManager).GetField("_heartbeatInterval",
            BindingFlags.NonPublic | BindingFlags.Static);
        var heartbeatIntervalValue = heartbeatIntervalField!.GetValue(null);
        Assert.Equal(newInterval, heartbeatIntervalValue);
    }

    [Fact]
    public void SetHeartbeatInterval_ThrowsExceptionForNegativeInterval()
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => LicenseManager.SetHeartbeatInterval(TimeSpan.FromMinutes(-1)));
    }

    // CalculateChecksum Tests

    [Fact]
    public void VerifyChecksum_ReturnsTrueForMatchingChecksum()
    {
        // Arrange
        var license = GenerateLicense();
        var licenseData = JsonSerializer.SerializeToUtf8Bytes(license);
        var checksum = SecurityUtils.CalculateChecksum(licenseData);

        // Act
        var isValid = SecurityUtils.VerifyChecksum(licenseData, checksum);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void VerifyChecksum_ReturnsFalseForMismatchedChecksum()
    {
        // Arrange
        var license = GenerateLicense();
        var licenseData = JsonSerializer.SerializeToUtf8Bytes(license);
        const string incorrectChecksum = "invalid-checksum"; // Incorrect checksum

        // Act
        var isValid = SecurityUtils.VerifyChecksum(licenseData, incorrectChecksum);

        // Assert
        Assert.False(isValid);
    }

    // Signature Verification Tests

    [Fact]
    public void VerifySignature_ReturnsTrueForValidSignature()
    {
        // Arrange
        LoadSecretKeys();
        var license = GenerateLicense();
        var licenseData = LicenseManager.SaveLicense(license); // Encrypted and signed
        var publicKey = LicenseUtils.GetLicensingSecrets().PublicKey; // Get the public key

        // Act
        var (encryptedLicenseData, signature, _) = (ValueTuple<byte[], byte[], byte[]>)typeof(LicenseManager)
            .GetMethod("SplitEncryptedDataAndSignature", BindingFlags.Static | BindingFlags.NonPublic)!
            .Invoke(null, [licenseData])!;
        var isValid = SecurityUtils.VerifySignature(encryptedLicenseData, signature, publicKey);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void VerifySignature_ReturnsFalseForInvalidSignature()
    {
        // Arrange
        LoadSecretKeys();
        var license = GenerateLicense();
        var licenseData = LicenseManager.SaveLicense(license); // Encrypted and signed
        var publicKey = LicenseUtils.GetLicensingSecrets().PublicKey; // Get the public key
        var invalidSignature = new byte[16]; // Create a fake, invalid signature

        // Act
        var (encryptedLicenseData, _, _) = (ValueTuple<byte[], byte[], byte[]>)typeof(LicenseManager)
            .GetMethod("SplitEncryptedDataAndSignature", BindingFlags.Static | BindingFlags.NonPublic)!
            .Invoke(null, [licenseData])!;
        var isValid = SecurityUtils.VerifySignature(encryptedLicenseData, invalidSignature, publicKey);

        // Assert
        Assert.False(isValid);
    }

    // Data Encryption and Decryption Tests

    [Fact]
    public void EncryptData_DecryptsDataCorrectly()
    {
        // Arrange
        LoadSecretKeys();
        var privateKey = LicenseUtils.GetLicensingSecrets().PrivateKey; // Get private key
        var data = "This is a secret message"u8.ToArray();

        // Act
        var encryptedData = SecurityUtils.EncryptData(data, privateKey);
        var decryptedData = SecurityUtils.DecryptData(encryptedData, privateKey);

        // Assert
        Assert.NotEqual(data, encryptedData); // Ensure encryption
        Assert.Equal(data, decryptedData); // Ensure decryption
    }
}