using System.Security.Cryptography;
using Aegis.Utilities;

namespace Aegis.Tests;

public class SecurityUtilsTests
{
    // Helper method to generate test data
    private static byte[] GenerateTestData(int length)
    {
        var data = new byte[length];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(data);
        return data;
    }

    [Fact]
    public void GenerateAesKey_ReturnsKeyOfCorrectSize()
    {
        // Act
        var key = SecurityUtils.GenerateAesKey();

        // Assert
        Assert.Equal(32, key.Length); // AES-256 key size is 32 bytes
    }

    [Fact]
    public void EncryptData_DecryptsDataCorrectly()
    {
        // Arrange
        var key = SecurityUtils.GenerateAesKey();
        var testData = GenerateTestData(512);

        // Act
        var encryptedData = SecurityUtils.EncryptData(testData, key);
        var decryptedData = SecurityUtils.DecryptData(encryptedData, key);

        // Assert
        Assert.NotEqual(testData, encryptedData);
        Assert.Equal(testData, decryptedData);
    }

    [Fact]
    public void EncryptData_ThrowsException_ForNullData()
    {
        // Arrange
        var key = SecurityUtils.GenerateAesKey();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => SecurityUtils.EncryptData(null!, key));
    }

    [Fact]
    public void EncryptData_ThrowsException_ForNullKey()
    {
        // Arrange
        var testData = GenerateTestData(64);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => SecurityUtils.EncryptData(testData, null!));
    }

    [Fact]
    public void DecryptData_ThrowsException_ForNullData()
    {
        // Arrange
        var key = SecurityUtils.GenerateAesKey();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => SecurityUtils.DecryptData(null!, key));
    }

    [Fact]
    public void DecryptData_ThrowsException_ForNullKey()
    {
        // Arrange
        var testData = GenerateTestData(64);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => SecurityUtils.DecryptData(testData, null!));
    }

    [Fact]
    public void SignData_VerifiesSignatureCorrectly()
    {
        // Arrange
        var rsa = RSA.Create();
        var privateKey = Convert.ToBase64String(rsa.ExportRSAPrivateKey());
        var publicKey = Convert.ToBase64String(rsa.ExportRSAPublicKey());
        var testData = GenerateTestData(256);

        // Act
        var signature = SecurityUtils.SignData(testData, privateKey);
        var isValid = SecurityUtils.VerifySignature(testData, signature, publicKey);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void SignData_ThrowsException_ForNullData()
    {
        // Arrange
        var rsa = RSA.Create();
        var privateKey = Convert.ToBase64String(rsa.ExportRSAPrivateKey());

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => SecurityUtils.SignData(null!, privateKey));
    }

    [Fact]
    public void SignData_ThrowsException_ForNullPrivateKey()
    {
        // Arrange
        var testData = GenerateTestData(256);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => SecurityUtils.SignData(testData, null!));
    }

    [Fact]
    public void VerifySignature_ThrowsException_ForNullData()
    {
        // Arrange
        var rsa = RSA.Create();
        var publicKey = Convert.ToBase64String(rsa.ExportRSAPublicKey());
        var signature = GenerateTestData(128);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            SecurityUtils.VerifySignature(null!, signature, publicKey));
    }

    [Fact]
    public void VerifySignature_ThrowsException_ForNullSignature()
    {
        // Arrange
        var rsa = RSA.Create();
        var publicKey = Convert.ToBase64String(rsa.ExportRSAPublicKey());
        var testData = GenerateTestData(256);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            SecurityUtils.VerifySignature(testData, null!, publicKey));
    }

    [Fact]
    public void VerifySignature_ThrowsException_ForNullPublicKey()
    {
        // Arrange
        var testData = GenerateTestData(256);
        var signature = GenerateTestData(128);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            SecurityUtils.VerifySignature(testData, signature, null!));
    }

    [Fact]
    public void CalculateChecksum_ReturnsCorrectChecksum()
    {
        // Arrange
        var testData = GenerateTestData(256);

        // Act
        var checksum = SecurityUtils.CalculateChecksum(testData);

        // Assert
        Assert.NotEmpty(checksum);
    }

    [Fact]
    public void CalculateChecksum_ThrowsException_ForNullData()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => SecurityUtils.CalculateChecksum(null!));
    }
    
    [Fact]
    public void CalculateSha256Hash_ReturnsCorrectHash()
    {
        // Arrange
        var testData = GenerateTestData(256);

        // Act
        var hash = SecurityUtils.CalculateSha256Hash(testData);

        // Assert
        Assert.Equal(32, hash.Length); // SHA256 hash size is 32 bytes
    }

    [Fact]
    public void CalculateSha256Hash_ThrowsException_ForNullData()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => SecurityUtils.CalculateSha256Hash(null!));
    }

    [Fact]
    public void VerifyChecksum_ReturnsTrue_ForMatchingChecksum()
    {
        // Arrange
        var testData = GenerateTestData(256);
        var checksum = SecurityUtils.CalculateChecksum(testData);

        // Act
        var isValid = SecurityUtils.VerifyChecksum(testData, checksum);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void VerifyChecksum_ReturnsFalse_ForMismatchedChecksum()
    {
        // Arrange
        var testData = GenerateTestData(256);
        var checksum2 = SecurityUtils.CalculateChecksum(GenerateTestData(256)); // Different data

        // Act
        var isValid = SecurityUtils.VerifyChecksum(testData, checksum2);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void VerifyChecksum_ThrowsException_ForNullData()
    {
        // Arrange
        var checksum = SecurityUtils.CalculateChecksum(GenerateTestData(256));

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => SecurityUtils.VerifyChecksum(null!, checksum));
    }

    [Fact]
    public void VerifyChecksum_ThrowsException_ForNullChecksum()
    {
        // Arrange
        var testData = GenerateTestData(256);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => SecurityUtils.VerifyChecksum(testData, null!));
    }
}