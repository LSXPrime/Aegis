using System.Security.Cryptography;
using System.Text.Json;
using Aegis.Models;
using Aegis.Utilities;

namespace Aegis.Tests;

public class LicenseValidatorTests
{
    private static RSA CreateRsaKey()
    {
        var rsa = RSA.Create();
        rsa.KeySize = 2048;
        return rsa;
    }

    private static (byte[] EncryptedLicenseData, byte[] Signature) GenerateEncryptedLicenseData(BaseLicense license, string? privateKey = null)
    {
        var licenseData = JsonSerializer.SerializeToUtf8Bytes(license);

        var encryptedLicenseData = SecurityUtils.EncryptData(licenseData, privateKey ?? LicenseUtils.GetLicensingSecrets().PrivateKey);
        var signature = SecurityUtils.SignData(encryptedLicenseData, privateKey ?? LicenseUtils.GetLicensingSecrets().PrivateKey);

        return (encryptedLicenseData, signature);
    }
        
    private static bool VerifySignatureAndDecrypt(byte[] encryptedLicenseData, byte[] signature, out object? license)
    {
        license = null;
        if (!SecurityUtils.VerifySignature(encryptedLicenseData, signature, LicenseUtils.GetLicensingSecrets().PublicKey))
            return false;

        var decryptedData = SecurityUtils.DecryptData(encryptedLicenseData, LicenseUtils.GetLicensingSecrets().PrivateKey);
        license = JsonSerializer.Deserialize<BaseLicense>(decryptedData);

        return license != null;
    }
    
    private void LoadSecretKeys()
    {
        var secretPath = Path.GetTempFileName();
        LicenseUtils.GenerateLicensingSecrets("MySecretTestKey", secretPath, "12345678-90ab-cdef-ghij-klmnopqrst");
        LicenseUtils.LoadLicensingSecrets("MySecretTestKey", secretPath);
    }


    // Helper methods to generate various licenses
    private StandardLicense GenerateStandardLicense()
    {
        return (StandardLicense)new StandardLicense("TestUser")
            .WithLicenseKey("SD2D-35G9-1502-X3DG-16VI-ELN2")
            .WithIssuer("Aegis Software")
            .WithExpiryDate(DateTime.UtcNow.AddDays(30));
    }

    private TrialLicense GenerateTrialLicense(TimeSpan trialPeriod)
    {
        return new TrialLicense(trialPeriod);
    }

    private NodeLockedLicense GenerateNodeLockedLicense(string hardwareId)
    {
        return new NodeLockedLicense(hardwareId);
    }

    private SubscriptionLicense GenerateSubscriptionLicense()
    {
        return new SubscriptionLicense("TestUser", TimeSpan.FromDays(365));
    }

    private FloatingLicense GenerateFloatingLicense()
    {
        return new FloatingLicense("TestUser", 10);
    }

    [Fact]
    public void ValidateStandardLicense_ReturnsTrue_ForValidLicense()
    {
        // Arrange
        LoadSecretKeys();
        var license = GenerateStandardLicense();
        var (encryptedLicenseData, signature) = GenerateEncryptedLicenseData(license);

        // Act
        var isValid = LicenseValidator.ValidateStandardLicense(encryptedLicenseData, signature,
            license.UserName, license.LicenseKey);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void ValidateStandardLicense_ReturnsFalse_ForIncorrectUserName()
    {
        // Arrange
        LoadSecretKeys();
        var license = GenerateStandardLicense();
        var (encryptedLicenseData, signature) = GenerateEncryptedLicenseData(license);
        const string incorrectUserName = "IncorrectUser";

        // Act
        var isValid = LicenseValidator.ValidateStandardLicense(encryptedLicenseData, signature,
            incorrectUserName, license.LicenseKey);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void ValidateStandardLicense_ReturnsFalse_ForIncorrectSerialNumber()
    {
        // Arrange
        LoadSecretKeys();
        var license = GenerateStandardLicense();
        var (encryptedLicenseData, signature) = GenerateEncryptedLicenseData(license);
        const string incorrectSerialNumber = "IncorrectSerialNumber";

        // Act
        var isValid = LicenseValidator.ValidateStandardLicense(encryptedLicenseData, signature,
            license.UserName, incorrectSerialNumber);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void ValidateStandardLicense_ReturnsFalse_ForExpiredLicense()
    {
        // Arrange
        LoadSecretKeys();
        var license = GenerateStandardLicense();
        license.ExpirationDate = DateTime.UtcNow.Subtract(TimeSpan.FromDays(1));
        var (encryptedLicenseData, signature) = GenerateEncryptedLicenseData(license);

        // Act
        var isValid = LicenseValidator.ValidateStandardLicense(encryptedLicenseData, signature,
            license.UserName, license.LicenseKey);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void ValidateTrialLicense_ReturnsTrue_ForValidLicense()
    {
        // Arrange
        LoadSecretKeys();
        var license = GenerateTrialLicense(TimeSpan.FromDays(7));
        var (encryptedLicenseData, signature) = GenerateEncryptedLicenseData(license);

        // Act
        var isValid = LicenseValidator.ValidateTrialLicense(encryptedLicenseData, signature);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void ValidateTrialLicense_ReturnsFalse_ForExpiredLicense()
    {
        // Arrange
        LoadSecretKeys();
        var license = GenerateTrialLicense(TimeSpan.FromDays(7));
        license.ExpirationDate = DateTime.UtcNow.Subtract(TimeSpan.FromDays(1));
        var (encryptedLicenseData, signature) = GenerateEncryptedLicenseData(license);

        // Act
        var isValid = LicenseValidator.ValidateTrialLicense(encryptedLicenseData, signature);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void ValidateTrialLicense_ReturnsFalse_ForZeroTrialPeriod()
    {
        // Arrange
        LoadSecretKeys();
        var license = GenerateTrialLicense(TimeSpan.Zero);
        var (encryptedLicenseData, signature) = GenerateEncryptedLicenseData(license);

        // Act
        var isValid = LicenseValidator.ValidateTrialLicense(encryptedLicenseData, signature);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void ValidateNodeLockedLicense_ReturnsTrue_ForValidLicense()
    {
        // Arrange
        LoadSecretKeys();
        var hardwareId = HardwareUtils.GetHardwareId(); // LicenseValidator checks against this device hardware id
        var license = GenerateNodeLockedLicense(hardwareId);
        var (encryptedLicenseData, signature) = GenerateEncryptedLicenseData(license);

        // Act
        var isValid = LicenseValidator.ValidateNodeLockedLicense(encryptedLicenseData, signature, hardwareId);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void ValidateNodeLockedLicense_ReturnsFalse_ForMismatchedHardwareId()
    {
        // Arrange
        LoadSecretKeys();
        const string hardwareId = "TestHardwareId";
        var license = GenerateNodeLockedLicense(hardwareId);
        var (encryptedLicenseData, signature) = GenerateEncryptedLicenseData(license);

        // Act
        var isValid = LicenseValidator.ValidateNodeLockedLicense(encryptedLicenseData, signature,
            "IncorrectHardwareId");

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void ValidateNodeLockedLicense_ReturnsFalse_ForExpiredLicense()
    {
        // Arrange
        LoadSecretKeys();
        const string hardwareId = "TestHardwareId";
        var license = GenerateNodeLockedLicense(hardwareId);
        license.ExpirationDate = DateTime.UtcNow.Subtract(TimeSpan.FromDays(1));
        var (encryptedLicenseData, signature) = GenerateEncryptedLicenseData(license);

        // Act
        var isValid = LicenseValidator.ValidateNodeLockedLicense(encryptedLicenseData, signature, hardwareId);

        // Assert
        Assert.False(isValid);
    }
        
    [Fact]
    public void ValidateSubscriptionLicense_ReturnsTrue_ForValidLicense()
    {
        // Arrange
        LoadSecretKeys();
        var license = GenerateSubscriptionLicense();
        var (encryptedLicenseData, signature) = GenerateEncryptedLicenseData(license);

        // Act
        var isValid = LicenseValidator.ValidateSubscriptionLicense(encryptedLicenseData, signature);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void ValidateSubscriptionLicense_ReturnsFalse_ForExpiredLicense()
    {
        // Arrange
        LoadSecretKeys();
        var license = GenerateSubscriptionLicense();
        license.ExpirationDate = DateTime.UtcNow.Subtract(TimeSpan.FromDays(1));
        var (encryptedLicenseData, signature) = GenerateEncryptedLicenseData(license);

        // Act
        var isValid = LicenseValidator.ValidateSubscriptionLicense(encryptedLicenseData, signature);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void ValidateFloatingLicense_ReturnsTrue_ForValidLicense()
    {
        // Arrange
        LoadSecretKeys();
        var license = GenerateFloatingLicense();
        var (encryptedLicenseData, signature) = GenerateEncryptedLicenseData(license);

        // Act
        var isValid = LicenseValidator.ValidateFloatingLicense(encryptedLicenseData, signature,
            license.UserName, license.MaxActiveUsersCount);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void ValidateFloatingLicense_ReturnsFalse_ForIncorrectUserName()
    {
        // Arrange
        LoadSecretKeys();
        var license = GenerateFloatingLicense();
        var (encryptedLicenseData, signature) = GenerateEncryptedLicenseData(license);

        // Act
        var isValid = LicenseValidator.ValidateFloatingLicense(encryptedLicenseData, signature,
            "IncorrectUser", license.MaxActiveUsersCount);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void ValidateFloatingLicense_ReturnsFalse_ForIncorrectMaxActiveUsersCount()
    {
        // Arrange
        LoadSecretKeys();
        var license = GenerateFloatingLicense();
        var (encryptedLicenseData, signature) = GenerateEncryptedLicenseData(license);

        // Act
        var isValid = LicenseValidator.ValidateFloatingLicense(encryptedLicenseData, signature,
            license.UserName, 15); // Incorrect count

        // Assert
        Assert.False(isValid);
    }


    [Fact]
    public void VerifySignatureAndDecrypt_ReturnsFalse_ForInvalidSignature()
    {
        // Arrange
        LoadSecretKeys();
        var license = GenerateStandardLicense();
        var (encryptedLicenseData, signature) = GenerateEncryptedLicenseData(license);

        // Act
        var isValid = VerifySignatureAndDecrypt(encryptedLicenseData,
            new byte[signature.Length], // Invalid signature
            out var licenseObj);

        // Assert
        Assert.False(isValid);
        Assert.Null(licenseObj);
    }

    [Fact]
    public void VerifySignatureAndDecrypt_ReturnsFalse_ForDecryptionFailure()
    {
        // Arrange
        LoadSecretKeys();
        var rsa = CreateRsaKey(); // New key to simulate decryption failure
        var incorrectPrivateKey = rsa.ToXmlString(true);
        var license = GenerateStandardLicense();
        var (encryptedLicenseData, signature) = GenerateEncryptedLicenseData(license, incorrectPrivateKey);
        

        // Act
        var isValid = VerifySignatureAndDecrypt(encryptedLicenseData, signature,
            out var licenseObj);

        // Assert
        Assert.False(isValid);
        Assert.Null(licenseObj);
    }
}