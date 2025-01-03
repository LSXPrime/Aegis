using System.Reflection;
using System.Text.Json;
using Aegis.Enums;
using Aegis.Exceptions;
using Aegis.Models.License;
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
        LicenseManager.SaveLicenseToPath(license, filePath);

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
        Assert.Throws<ArgumentNullException>(() => LicenseManager.SaveLicenseToPath(license, null!));
    }

    [Fact]
    public void SaveLicense_ThrowsExceptionForEmptyFilePath()
    {
        // Arrange
        LoadSecretKeys();
        var license = GenerateLicense();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => LicenseManager.SaveLicenseToPath(license, ""));
    }

    [Fact]
    public void SaveLicense_ThrowsExceptionForInvalidFilePath()
    {
        // Arrange
        LoadSecretKeys();
        var license = GenerateLicense();
        const string filePath = "Invalid/File/Path"; // This should be an invalid path on most systems

        // Act & Assert
        Assert.Throws<ArgumentException>(() => LicenseManager.SaveLicenseToPath(license, filePath));
    }

    [Fact]
    public async Task LoadLicenseAsync_LoadsLicenseFromFileCorrectly()
    {
        // Arrange
        LoadSecretKeys();
        var license = GenerateLicense();
        var filePath = Path.GetTempFileName();
        LicenseManager.SaveLicenseToPath(license, filePath);

        // Act
        var loadedLicense = await LicenseManager.LoadLicenseAsync(filePath);

        // Assert
        Assert.Equal(LicenseStatus.Valid, loadedLicense.Status);
        Assert.NotNull(loadedLicense.License);
        Assert.Equal(license.LicenseKey, loadedLicense.License.LicenseKey);
        Assert.Equal(license.Type, loadedLicense.License.Type);

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
        var (hash, signature, _, _) = LicenseValidator.SplitLicenseData(licenseData);
        var isValid = SecurityUtils.VerifySignature(hash, signature, publicKey); // Verify signature of hash

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
        var (hash, _, _, _) = LicenseValidator.SplitLicenseData(licenseData);

        var isValid = SecurityUtils.VerifySignature(hash, invalidSignature, publicKey);

        // Assert
        Assert.False(isValid);
    }

    private static void SetLicense(BaseLicense license)
    {
        var currentProperty = typeof(LicenseManager).GetProperty("Current", BindingFlags.Public | BindingFlags.Static);
        currentProperty!.SetValue(null, license);
    }
}