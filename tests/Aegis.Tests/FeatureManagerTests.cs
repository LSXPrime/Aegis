using Aegis.Enums;
using Aegis.Exceptions;
using Aegis.Models;
using Aegis.Models.License;

namespace Aegis.Tests;

public class FeatureManagerTests
{
    // Helper method to set a license for testing
    private static void SetLicense(BaseLicense license)
    {
        FeatureManager.SetLicense(license);
    }

    // Helper method to clear the license after testing
    private static void ClearLicense()
    {
        FeatureManager.ClearLicense();
    }
    
    // Helper method to generate a license for testing
    private BaseLicense GenerateLicense(LicenseType type = LicenseType.Standard)
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

        return license.WithIssuer("Aegis Software").WithFeatures(new Dictionary<string, Feature>
            { { "Feature1", Feature.FromBool(true) }, { "Feature2", Feature.FromBool(false) } });
    }

    [Fact]
    public void IsFeatureEnabled_ReturnsCorrectValue()
    {
        // Arrange
        var license = GenerateLicense();
        license.Features.Add("Feature9", Feature.FromBool(true));
        SetLicense(license);

        // Act
        var isEnabled = FeatureManager.IsFeatureEnabled("Feature9");

        // Assert
        Assert.True(isEnabled);
        ClearLicense();
    }

    [Fact]
    public void IsFeatureEnabled_ReturnsFalseForNonExistingFeature()
    {
        // Arrange
        var license = GenerateLicense();
        SetLicense(license);

        // Act
        var isEnabled = FeatureManager.IsFeatureEnabled("NonExistingFeature");

        // Assert
        Assert.False(isEnabled);
        ClearLicense();
    }

    [Fact]
    public void IsFeatureEnabled_ReturnsFalseForDisabledFeature()
    {
        // Arrange
        var license = GenerateLicense();
        license.Features.Add("Feature9", Feature.FromBool(false));
        SetLicense(license);

        // Act
        var isEnabled = FeatureManager.IsFeatureEnabled("Feature9");

        // Assert
        Assert.False(isEnabled);
        ClearLicense();
    }

    [Fact]
    public void GetFeatureInt_ReturnsCorrectValue()
    {
        // Arrange
        var license = GenerateLicense();
        license.Features.Add("FeatureInt", Feature.FromInt(123));
        SetLicense(license);

        // Act
        var intValue = FeatureManager.GetFeatureInt("FeatureInt");

        // Assert
        Assert.Equal(123, intValue);
        ClearLicense();
    }

    [Fact]
    public void GetFeatureInt_ReturnsDefaultValueForNonExistingFeature()
    {
        // Arrange
        var license = GenerateLicense();
        SetLicense(license);

        // Act
        var intValue = FeatureManager.GetFeatureInt("NonExistingFeature");

        // Assert
        Assert.Equal(default, intValue); // Default value for int
        ClearLicense();
    }
    
    [Fact]
    public void GetFeatureInt_ReturnsDefaultValueForInvalidType()
    {
        // Arrange
        var license = GenerateLicense();
        license.Features.Add("FeatureString", Feature.FromString("NotAnInt"));
        SetLicense(license);

        // Act
        var intValue = FeatureManager.GetFeatureInt("FeatureString");

        // Assert
        Assert.Equal(default, intValue);
        ClearLicense();
    }

    [Fact]
    public void GetFeatureString_ReturnsCorrectValue()
    {
        // Arrange
        var license = GenerateLicense();
        license.Features.Add("FeatureString", Feature.FromString("Test String"));
        SetLicense(license);

        // Act
        var stringValue = FeatureManager.GetFeatureString("FeatureString");

        // Assert
        Assert.Equal("Test String", stringValue);
        ClearLicense();
    }

    [Fact]
    public void GetFeatureString_ReturnsDefaultValueForNonExistingFeature()
    {
        // Arrange
        var license = GenerateLicense();
        SetLicense(license);

        // Act
        var stringValue = FeatureManager.GetFeatureString("NonExistingFeature");

        // Assert
        Assert.Null(stringValue);
        ClearLicense();
    }
    
    [Fact]
    public void GetFeatureString_ReturnsDefaultValueForInvalidType()
    {
        // Arrange
        var license = GenerateLicense();
        license.Features.Add("FeatureInt", Feature.FromInt(123));
        SetLicense(license);

        // Act
        var stringValue = FeatureManager.GetFeatureString("FeatureInt");

        // Assert
        Assert.Null(stringValue);
        ClearLicense();
    }

    [Fact]
    public void GetFeatureDateTime_ReturnsCorrectValue()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var license = GenerateLicense();
        license.Features.Add("FeatureDateTime", Feature.FromDateTime(now));
        SetLicense(license);

        // Act
        var dateTimeValue = FeatureManager.GetFeatureDateTime("FeatureDateTime");

        // Assert
        Assert.Equal(now, dateTimeValue);
        ClearLicense();
    }

    [Fact]
    public void GetFeatureDateTime_ReturnsDefaultValueForNonExistingFeature()
    {
        // Arrange
        var license = GenerateLicense();
        SetLicense(license);

        // Act
        var dateTimeValue = FeatureManager.GetFeatureDateTime("NonExistingFeature");

        // Assert
        Assert.Equal(default, dateTimeValue);
        ClearLicense();
    }
    
    [Fact]
    public void GetFeatureDateTime_ReturnsDefaultValueForInvalidType()
    {
        // Arrange
        var license = GenerateLicense();
        license.Features.Add("FeatureString", Feature.FromString("NotADateTime"));
        SetLicense(license);

        // Act
        var dateTimeValue = FeatureManager.GetFeatureDateTime("FeatureString");

        // Assert
        Assert.Equal(default, dateTimeValue);
        ClearLicense();
    }

    [Fact]
    public void GetFeatureByteArray_ReturnsCorrectValue()
    {
        // Arrange
        var testData = new byte[] { 1, 2, 3, 4, 5 };
        var license = GenerateLicense();
        license.Features.Add("FeatureByteArray", Feature.FromByteArray(testData));
        SetLicense(license);

        // Act
        var byteArrayValue = FeatureManager.GetFeatureByteArray("FeatureByteArray");

        // Assert
        Assert.Equal(testData, byteArrayValue);
        ClearLicense();
    }
    
    [Fact]
    public void GetFeatureByteArray_ReturnsDefaultValueForNonExistingFeature()
    {
        // Arrange
        var license = GenerateLicense();
        SetLicense(license);

        // Act
        var byteArrayValue = FeatureManager.GetFeatureByteArray("NonExistingFeature");

        // Assert
        Assert.Null(byteArrayValue);
        ClearLicense();
    }
    
    [Fact]
    public void GetFeatureByteArray_ReturnsDefaultValueForInvalidType()
    {
        // Arrange
        var license = GenerateLicense();
        license.Features.Add("FeatureString", Feature.FromString("NotAByteArray"));
        SetLicense(license);

        // Act
        var byteArrayValue = FeatureManager.GetFeatureByteArray("FeatureString");

        // Assert
        Assert.Null(byteArrayValue);
        ClearLicense();
    }

    [Fact]
    public void ThrowIfNotAllowed_ThrowsExceptionForDisabledFeature()
    {
        // Arrange
        var license = GenerateLicense();
        license.Features.Add("Feature9", Feature.FromBool(false));
        SetLicense(license);

        // Act & Assert
        Assert.Throws<FeatureNotLicensedException>(() => FeatureManager.ThrowIfNotAllowed("Feature9"));
        ClearLicense();
    }

    [Fact]
    public void ThrowIfNotAllowed_DoesNotThrowExceptionForEnabledFeature()
    {
        // Arrange
        var license = GenerateLicense();
        license.Features.Add("Feature9", Feature.FromBool(true));
        SetLicense(license);

        // Act & Assert (no exception should be thrown)
        FeatureManager.ThrowIfNotAllowed("Feature9");
        ClearLicense();
    }
}