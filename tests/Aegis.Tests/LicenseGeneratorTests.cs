using Aegis.Enums;
using Aegis.Models;

namespace Aegis.Tests;

public class LicenseGeneratorTests
{
    [Fact]
    public void GenerateStandardLicense_CreatesValidLicense()
    {
        // Arrange
        const string userName = "TestUser";

        // Act
        var license = LicenseGenerator.GenerateStandardLicense(userName);

        // Assert
        Assert.NotNull(license);
        Assert.IsType<StandardLicense>(license);
        Assert.Equal(userName, license.UserName);
        Assert.Equal(LicenseType.Standard, license.Type);
        Assert.Equal(DateTime.UtcNow.Date, license.IssuedOn.Date);
    }

    [Fact]
    public void GenerateTrialLicense_CreatesValidLicense()
    {
        // Arrange
        var trialPeriod = TimeSpan.FromDays(14);

        // Act
        var license = LicenseGenerator.GenerateTrialLicense(trialPeriod);

        // Assert
        Assert.NotNull(license);
        Assert.IsType<TrialLicense>(license);
        Assert.Equal(trialPeriod, license.TrialPeriod);
        Assert.Equal(LicenseType.Trial, license.Type);
        Assert.Equal(DateTime.UtcNow.Date, license.IssuedOn.Date);
        Assert.Equal(DateTime.UtcNow.Add(trialPeriod).Date, license.ExpirationDate!.Value.Date);
    }

    [Fact]
    public void GenerateNodeLockedLicense_CreatesValidLicense_WithGeneratedHardwareId()
    {
        // Arrange & Act
        var license = LicenseGenerator.GenerateNodeLockedLicense(); // No hardwareId provided

        // Assert
        Assert.NotNull(license);
        Assert.IsType<NodeLockedLicense>(license);
        Assert.NotNull(license.HardwareId); 
        Assert.Equal(LicenseType.NodeLocked, license.Type);
        Assert.Equal(DateTime.UtcNow.Date, license.IssuedOn.Date);
    }

    [Fact]
    public void GenerateNodeLockedLicense_CreatesValidLicense_WithProvidedHardwareId()
    {
        // Arrange
        const string hardwareId = "test-hardware-id";

        // Act
        var license = LicenseGenerator.GenerateNodeLockedLicense(hardwareId);

        // Assert
        Assert.NotNull(license);
        Assert.IsType<NodeLockedLicense>(license);
        Assert.Equal(hardwareId, license.HardwareId); 
        Assert.Equal(LicenseType.NodeLocked, license.Type);
        Assert.Equal(DateTime.UtcNow.Date, license.IssuedOn.Date);
    }

    [Fact]
    public void GenerateSubscriptionLicense_CreatesValidLicense()
    {
        // Arrange
        const string userName = "TestUser";
        var subscriptionDuration = TimeSpan.FromDays(365);

        // Act
        var license = LicenseGenerator.GenerateSubscriptionLicense(userName, subscriptionDuration);

        // Assert
        Assert.NotNull(license);
        Assert.IsType<SubscriptionLicense>(license);
        Assert.Equal(userName, license.UserName);
        Assert.Equal(subscriptionDuration, license.SubscriptionDuration);
        Assert.Equal(LicenseType.Subscription, license.Type);
        Assert.Equal(DateTime.UtcNow.Date, license.IssuedOn.Date);
        Assert.Equal(DateTime.UtcNow.Date, license.SubscriptionStartDate.Date);
    }

    [Fact]
    public void GenerateFloatingLicense_CreatesValidLicense()
    {
        // Arrange
        const string userName = "TestUser";
        const int maxActiveUsersCount = 10;

        // Act
        var license = LicenseGenerator.GenerateFloatingLicense(userName, maxActiveUsersCount);

        // Assert
        Assert.NotNull(license);
        Assert.IsType<FloatingLicense>(license);
        Assert.Equal(userName, license.UserName);
        Assert.Equal(maxActiveUsersCount, license.MaxActiveUsersCount);
        Assert.Equal(LicenseType.Floating, license.Type);
        Assert.Equal(DateTime.UtcNow.Date, license.IssuedOn.Date);
    }
    
    [Fact]
    public void GenerateConcurrentLicense_CreatesValidLicense()
    {
        // Arrange
        const string userName = "TestUser";
        const int maxActiveUsersCount = 4;

        // Act
        var license = LicenseGenerator.GenerateConcurrentLicense(userName, maxActiveUsersCount);

        // Assert
        Assert.NotNull(license);
        Assert.IsType<ConcurrentLicense>(license);
        Assert.Equal(userName, license.UserName);
        Assert.Equal(maxActiveUsersCount, license.MaxActiveUsersCount);
        Assert.Equal(LicenseType.Concurrent, license.Type);
        Assert.Equal(DateTime.UtcNow.Date, license.IssuedOn.Date);
    }
}