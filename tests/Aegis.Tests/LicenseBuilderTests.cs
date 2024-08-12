using Aegis.Enums;
using Aegis.Exceptions;
using Aegis.Models;
using Aegis.Utilities;

namespace Aegis.Tests;

public class LicenseBuilderTests
{
    // A helper method to create a base license with default values
    private BaseLicense CreateBaseLicense(LicenseType type = LicenseType.Standard)
    {
        BaseLicense license = type switch
        {
            LicenseType.Standard => new StandardLicense("TestUser"),
            LicenseType.Trial => new TrialLicense(TimeSpan.FromDays(7)),
            LicenseType.NodeLocked => new NodeLockedLicense("TestHardwareId"),
            LicenseType.Subscription => new SubscriptionLicense("TestUser", TimeSpan.FromDays(30)),
            LicenseType.Floating => new FloatingLicense("TestUser", 5),
            LicenseType.Concurrent => new ConcurrentLicense("TestUser", 5),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
        
        if (license.Type != LicenseType.Trial)
            license.WithExpiryDate(DateTime.UtcNow.AddDays(10));
        
        return license.WithIssuer("Aegis Software").WithFeatures(new Dictionary<string, bool>() { { "Feature1", true }, { "Feature2", false } });
    }

    [Fact]
    public void WithExpiryDate_SetsExpirationDateCorrectly_ForStandardLicense()
    {
        // Arrange
        var baseLicense = CreateBaseLicense();
        var expectedExpiryDate = DateTime.UtcNow.AddDays(10).Date;

        // Act
        var license = baseLicense.WithExpiryDate(expectedExpiryDate);

        // Assert
        Assert.Equal(expectedExpiryDate, license.ExpirationDate!.Value.Date);
    }
    
    [Fact]
    public void WithExpiryDate_ThrowsExceptionForTrialLicense()
    {
        // Arrange
        var baseLicense = CreateBaseLicense(LicenseType.Trial);
        var expiryDate = DateTime.UtcNow.AddDays(10);

        // Act & Assert
        Assert.Throws<LicenseGenerationException>(() => baseLicense.WithExpiryDate(expiryDate));
    }

    [Fact]
    public void WithFeature_AddsNewFeatureCorrectly()
    {
        // Arrange
        var baseLicense = CreateBaseLicense();

        // Act
        var license = baseLicense.WithFeature("TestFeature", true);

        // Assert
        Assert.True(license.Features.ContainsKey("TestFeature"));
        Assert.True(license.Features["TestFeature"]);
    }

    [Fact]
    public void WithFeature_UpdatesExistingFeatureCorrectly()
    {
        // Arrange
        var baseLicense = CreateBaseLicense();
        baseLicense.Features.Add("TestFeature", false);

        // Act
        var license = baseLicense.WithFeature("TestFeature", true);

        // Assert
        Assert.True(license.Features.ContainsKey("TestFeature"));
        Assert.True(license.Features["TestFeature"]);
    }

    [Fact]
    public void WithFeatures_SetsFeaturesCorrectly()
    {
        // Arrange
        var baseLicense = CreateBaseLicense();
        var features = new Dictionary<string, bool>
        {
            { "Feature1", true },
            { "Feature2", false }
        };

        // Act
        var license = baseLicense.WithFeatures(features);

        // Assert
        Assert.Equal(features, license.Features);
    }

    [Fact]
    public void WithIssuer_SetsIssuerCorrectly()
    {
        // Arrange
        var baseLicense = CreateBaseLicense();
        const string issuer = "Aegis Software Inc.";

        // Act
        var license = baseLicense.WithIssuer(issuer);

        // Assert
        Assert.Equal(issuer, license.Issuer);
    }

    [Fact]
    public void WithLicenseKey_SetsLicenseKeyCorrectly()
    {
        // Arrange
        var baseLicense = CreateBaseLicense();
        const string licenseKey = "ABCD-EFGH-IJKL-MNOP";

        // Act
        var license = baseLicense.WithLicenseKey(licenseKey);

        // Assert
        Assert.Equal(licenseKey, license.LicenseKey);
    }

    [Fact]
    public async Task SaveLicense_CallsLicenseManagerMethodsWithCorrectArguments()
    {
        // Arrange
        var baseLicense = CreateBaseLicense();
        var filePath = $@"{Path.GetTempPath()}\license.lic";
        var secretPath = Path.GetTempFileName();
        LicenseUtils.GenerateLicensingSecrets("MySecretTestKey", secretPath, "12345678-90ab-cdef-ghij-klmnopqrst");
        LicenseUtils.LoadLicensingSecrets("MySecretTestKey", secretPath);

        // Act 
        baseLicense.SaveLicense(filePath);

        // Assert
        var licenseData = await File.ReadAllBytesAsync(filePath);
        Assert.NotEmpty(licenseData);
        var license = await LicenseManager.LoadLicenseAsync(licenseData);
        Assert.NotNull(license);
        Assert.Equal(baseLicense.Type, license.Type);
        Assert.Equal(baseLicense.Issuer, license.Issuer);
        Assert.Equal(baseLicense.LicenseKey, license.LicenseKey);
        Assert.Equal(baseLicense.Features, license.Features);
        Assert.Equal(baseLicense.ExpirationDate, license.ExpirationDate);
        Assert.Equal(baseLicense.IssuedOn, license.IssuedOn);
        Assert.Equal(baseLicense.LicenseId, license.LicenseId);

        // Clean up
        File.Delete(filePath);
    }
}