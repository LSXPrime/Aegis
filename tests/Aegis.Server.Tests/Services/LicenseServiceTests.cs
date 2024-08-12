using Aegis.Enums;
using Aegis.Exceptions;
using Aegis.Models;
using Aegis.Server.Data;
using Aegis.Server.DTOs;
using Aegis.Server.Entities;
using Aegis.Server.Enums;
using Aegis.Server.Exceptions;
using Aegis.Server.Services;
using Aegis.Utilities;
using Microsoft.EntityFrameworkCore;

namespace Aegis.Server.Tests.Services;

public class LicenseServiceTests
{
    private readonly ApplicationDbContext _dbContext;
    private readonly LicenseService _licenseService;

    public LicenseServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        _dbContext = new ApplicationDbContext(options);
        _licenseService = new LicenseService(_dbContext);
        
        // Initialize test data
        SeedDatabase();
        LoadSecretKeys();
    }

    private void SeedDatabase()
    {
        _dbContext.Products.Add(new Product { ProductId = Guid.NewGuid(), ProductName = "Test Product" });
        _dbContext.Features.Add(new Feature { FeatureId = Guid.NewGuid(), FeatureName = "Feature 1" });
        _dbContext.Features.Add(new Feature { FeatureId = Guid.NewGuid(), FeatureName = "Feature 2" }); 
        _dbContext.SaveChanges();
        _dbContext.LicenseFeatures.Add(new LicenseFeature() { ProductId = _dbContext.Products.First().ProductId, FeatureId = _dbContext.Features.First().FeatureId });
        _dbContext.LicenseFeatures.Add(new LicenseFeature() { ProductId = _dbContext.Products.First().ProductId, FeatureId = _dbContext.Features.Last().FeatureId });
        _dbContext.SaveChanges();
    }
    
    private void LoadSecretKeys()
    {
        var secretPath = Path.GetTempFileName();
        LicenseUtils.GenerateLicensingSecrets("MySecretTestKey", secretPath, "12345678-90ab-cdef-ghij-klmnopqrst");
        LicenseUtils.LoadLicensingSecrets("MySecretTestKey", secretPath);
    }

    #region GenerateLicenseAsync Tests

    [Fact]
    public async Task GenerateLicenseAsync_ValidRequest_CreatesLicenseAndReturnsFile()
    {
        // Arrange
        var productId = _dbContext.Products.First().ProductId;
        var featureId = _dbContext.Features.First().FeatureId;
        var request = new LicenseGenerationRequest
        {
            LicenseType = LicenseType.Standard,
            ExpirationDate = DateTime.UtcNow.AddDays(30),
            ProductId = productId,
            IssuedTo = "Test User",
            FeatureIds = [featureId]
        };

        // Act
        var licenseFile = await _licenseService.GenerateLicenseAsync(request);

        // Assert 
        Assert.NotNull(licenseFile);
        Assert.NotEmpty(licenseFile);

        var license = await LicenseManager.LoadLicenseAsync(licenseFile);
        Assert.NotNull(license);
        Assert.Equal(request.LicenseType, license.Type);
        Assert.Equal(request.ExpirationDate!.Value, license.ExpirationDate); 
    }

    [Fact]
    public async Task GenerateLicenseAsync_InvalidProductId_ThrowsNotFoundException()
    {
        // Arrange
        var request = new LicenseGenerationRequest
        {
            LicenseType = LicenseType.Standard,
            ExpirationDate = DateTime.UtcNow.AddDays(30),
            ProductId = Guid.NewGuid(), // Invalid ProductId
            IssuedTo = "Test User"
        };

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(async () =>
        {
            await _licenseService.GenerateLicenseAsync(request);
        });
    }

    [Fact]
    public async Task GenerateLicenseAsync_InvalidFeatureIds_ThrowsNotFoundException()
    {
        // Arrange
        var productId = _dbContext.Products.First().ProductId;
        var request = new LicenseGenerationRequest
        {
            LicenseType = LicenseType.Standard,
            ExpirationDate = DateTime.UtcNow.AddDays(30),
            ProductId = productId,
            IssuedTo = "Test User",
            FeatureIds = [Guid.NewGuid()] // Invalid FeatureId
        };

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(async () =>
        {
            await _licenseService.GenerateLicenseAsync(request);
        });
    }
    
    [Fact]
    public async Task GenerateLicenseAsync_ExpirationDateInThePast_ThrowsBadRequestException()
    {
        // Arrange
        var productId = _dbContext.Products.First().ProductId;
        var request = new LicenseGenerationRequest
        {
            LicenseType = LicenseType.Standard,
            ExpirationDate = DateTime.UtcNow.AddDays(-30), // Past Expiration Date
            ProductId = productId,
            IssuedTo = "Test User"
        };

        // Act & Assert
        await Assert.ThrowsAsync<BadRequestException>(async () =>
        {
            await _licenseService.GenerateLicenseAsync(request);
        });
    }

    #endregion

    #region ValidateLicenseAsync Tests

    [Fact]
    public async Task ValidateLicenseAsync_ValidLicense_ReturnsValidResult()
    {
        // Arrange
        var license = CreateAndSaveLicense(LicenseType.Standard);
        var licenseFile = GenerateLicenseFile(license); 

        // Act
        var result = await _licenseService.ValidateLicenseAsync(license.LicenseKey, licenseFile);
        
        // Assert
        Assert.True(result.IsValid);
        Assert.NotNull(result.License);
        Assert.Null(result.Exception);
    }

    [Fact]
    public async Task ValidateLicenseAsync_MissingLicenseKey_ReturnsInvalidResult()
    {
        // Arrange
        var license = CreateAndSaveLicense(LicenseType.Standard);
        var licenseFile = GenerateLicenseFile(license);

        // Act
        var result = await _licenseService.ValidateLicenseAsync(string.Empty, licenseFile);

        // Assert
        Assert.False(result.IsValid);
        Assert.Null(result.License);
        Assert.IsType<NotFoundException>(result.Exception);
    }
    
    [Fact]
    public async Task ValidateLicenseAsync_ExpiredLicense_ReturnsInvalidResult()
    {
        // Arrange
        var license = CreateAndSaveLicense(LicenseType.Standard, DateTime.UtcNow.AddDays(-30));
        var licenseFile = GenerateLicenseFile(license);

        // Act
        var result = await _licenseService.ValidateLicenseAsync(license.LicenseKey, licenseFile);

        // Assert
        Assert.False(result.IsValid);
        Assert.Null(result.License);
        Assert.IsType<ExpiredLicenseException>(result.Exception); 
    }

    [Fact]
    public async Task ValidateLicenseAsync_RevokedLicense_ReturnsInvalidResult()
    {
        // Arrange
        var license = CreateAndSaveLicense(LicenseType.Standard);
        license.Status = LicenseStatus.Revoked; 
        _dbContext.Licenses.Update(license);
        await _dbContext.SaveChangesAsync();
        var licenseFile = GenerateLicenseFile(license);

        // Act
        var result = await _licenseService.ValidateLicenseAsync(license.LicenseKey, licenseFile);

        // Assert
        Assert.False(result.IsValid);
        Assert.Null(result.License);
        Assert.IsType<LicenseValidationException>(result.Exception);
    }
    
    [Fact]
    public async Task ValidateLicenseAsync_TamperedLicense_ThrowsException()
    {
        // Arrange
        var license = CreateAndSaveLicense(LicenseType.Standard);
        var licenseFile = GenerateLicenseFile(license);
        licenseFile[0] = (byte)'X';
        
        // Act
        var result = await _licenseService.ValidateLicenseAsync(license.LicenseKey, licenseFile);
        
        // Assert
        Assert.False(result.IsValid);
        Assert.Null(result.License);
        Assert.IsType<InvalidLicenseSignatureException>(result.Exception);
    }
    
    [Fact]
    public async Task ValidateLicenseAsync_NodeLockedLicense_HardwareMismatch_ThrowException()
    {
        // Arrange
        var license = CreateAndSaveLicense(LicenseType.NodeLocked, hardwareId: "12345678");
        var licenseFile = GenerateLicenseFile(license);
        var validationParams = new Dictionary<string, string?> { { "HardwareId", "87654321" } };


        // Act
        var result = await _licenseService.ValidateLicenseAsync(license.LicenseKey, licenseFile, validationParams);
        
        // Assert
        Assert.False(result.IsValid);
        Assert.Null(result.License);
        Assert.IsType<LicenseValidationException>(result.Exception);
    }

    #endregion

    #region ActivateLicenseAsync Tests

    [Fact]
    public async Task ActivateLicenseAsync_StandardLicense_ReturnsSuccessfulResult()
    {
        // Arrange
        var license = CreateAndSaveLicense(LicenseType.Standard);

        // Act
        var result = await _licenseService.ActivateLicenseAsync(license.LicenseKey);

        // Assert
        Assert.True(result.IsSuccessful);
        Assert.Null(result.Exception);

        var updatedLicense = await _dbContext.Licenses.FindAsync(license.LicenseId);
        Assert.Equal(LicenseStatus.Active, updatedLicense!.Status);
    }

    [Fact]
    public async Task ActivateLicenseAsync_TrialLicense_ReturnsSuccessfulResult()
    {
        // Arrange
        var license = CreateAndSaveLicense(LicenseType.Trial);

        // Act
        var result = await _licenseService.ActivateLicenseAsync(license.LicenseKey);

        // Assert
        Assert.True(result.IsSuccessful);
        Assert.Null(result.Exception);

        var updatedLicense = await _dbContext.Licenses.FindAsync(license.LicenseId);
        Assert.Equal(LicenseStatus.Active, updatedLicense!.Status);
    }
    
    [Fact]
    public async Task ActivateLicenseAsync_NodeLockedLicense_ReturnsSuccessfulResult()
    {
        // Arrange
        const string hardwareId = "12345678";
        var license = CreateAndSaveLicense(LicenseType.NodeLocked); 

        // Act
        var result = await _licenseService.ActivateLicenseAsync(license.LicenseKey, hardwareId);

        // Assert
        Assert.True(result.IsSuccessful);
        Assert.Null(result.Exception); 

        var updatedLicense = await _dbContext.Licenses.FindAsync(license.LicenseId);
        Assert.Equal(LicenseStatus.Active, updatedLicense!.Status);
        Assert.Equal(hardwareId, updatedLicense.HardwareId);
    }
    
    [Fact]
    public async Task ActivateLicenseAsync_ConcurrentLicense_BelowLimit_ReturnsSuccessfulResult()
    {
        // Arrange
        const string hardwareId = "12345678";
        var license = CreateAndSaveLicense(LicenseType.Concurrent, maxActivations: 5); 

        // Act
        var result = await _licenseService.ActivateLicenseAsync(license.LicenseKey, hardwareId);

        // Assert
        Assert.True(result.IsSuccessful);
        Assert.Null(result.Exception);

        var updatedLicense = await _dbContext.Licenses.FindAsync(license.LicenseId);
        Assert.Equal(LicenseStatus.Active, updatedLicense!.Status);
        Assert.Equal(1, updatedLicense.ActiveUsersCount);

        var activation = await _dbContext.Activations.FirstOrDefaultAsync(a => a.LicenseId == license.LicenseId);
        Assert.NotNull(activation);
        Assert.Equal(hardwareId, activation.MachineId); 
    }
    
    [Fact]
    public async Task ActivateLicenseAsync_ConcurrentLicense_AtLimit_ReturnsUnsuccessfulResult()
    {
        // Arrange
        const string hardwareId = "12345678";
        var license = CreateAndSaveLicense(LicenseType.Concurrent, maxActivations: 1);
        await _licenseService.ActivateLicenseAsync(license.LicenseKey, hardwareId);

        // Act
        var result = await _licenseService.ActivateLicenseAsync(license.LicenseKey, hardwareId);

        // Assert
        Assert.False(result.IsSuccessful);
        Assert.IsType<MaximumActivationsReachedException>(result.Exception);

        var updatedLicense = await _dbContext.Licenses.FindAsync(license.LicenseId);
        Assert.Equal(1, updatedLicense!.ActiveUsersCount); 
    }
    
    [Fact]
    public async Task ActivateLicenseAsync_FloatingLicense_BelowLimit_ReturnsSuccessfulResult()
    {
        // Arrange
        const string hardwareId = "12345678";
        var license = CreateAndSaveLicense(LicenseType.Floating, maxActivations: 5);

        // Act
        var result = await _licenseService.ActivateLicenseAsync(license.LicenseKey, hardwareId);

        // Assert
        Assert.True(result.IsSuccessful);
        Assert.Null(result.Exception);

        var updatedLicense = await _dbContext.Licenses.FindAsync(license.LicenseId);
        Assert.Equal(LicenseStatus.Active, updatedLicense!.Status);
        Assert.Equal(1, updatedLicense.ActiveUsersCount);

        var activation = await _dbContext.Activations.FirstOrDefaultAsync(a => a.LicenseId == license.LicenseId);
        Assert.NotNull(activation);
        Assert.Equal(hardwareId, activation.MachineId);
    }

    [Fact]
    public async Task ActivateLicenseAsync_FloatingLicense_AtLimit_ReturnsUnsuccessfulResult()
    {
        // Arrange
        const string hardwareId = "12345678";
        var license = CreateAndSaveLicense(LicenseType.Floating, maxActivations: 1);
        await _licenseService.ActivateLicenseAsync(license.LicenseKey, hardwareId); 

        // Act
        var result = await _licenseService.ActivateLicenseAsync(license.LicenseKey, hardwareId);

        // Assert
        Assert.False(result.IsSuccessful);
        Assert.IsType<MaximumActivationsReachedException>(result.Exception);

        var updatedLicense = await _dbContext.Licenses.FindAsync(license.LicenseId);
        Assert.Equal(1, updatedLicense!.ActiveUsersCount);
    }

    [Fact]
    public async Task ActivateLicenseAsync_SubscriptionLicense_ValidDate_ReturnsSuccessfulResult()
    {
        // Arrange
        var license = CreateAndSaveLicense(LicenseType.Subscription, DateTime.UtcNow.AddDays(30));

        // Act
        var result = await _licenseService.ActivateLicenseAsync(license.LicenseKey);

        // Assert
        Assert.True(result.IsSuccessful);
        Assert.Null(result.Exception);

        var updatedLicense = await _dbContext.Licenses.FindAsync(license.LicenseId);
        Assert.Equal(LicenseStatus.Active, updatedLicense!.Status);
    }

    [Fact]
    public async Task ActivateLicenseAsync_SubscriptionLicense_Expired_ReturnsUnsuccessfulResult()
    {
        // Arrange
        var license = CreateAndSaveLicense(LicenseType.Subscription, DateTime.UtcNow.AddDays(-30));

        // Act
        var result = await _licenseService.ActivateLicenseAsync(license.LicenseKey);

        // Assert
        Assert.False(result.IsSuccessful);
        Assert.IsType<ExpiredLicenseException>(result.Exception); 
    }

    [Fact]
    public async Task ActivateLicenseAsync_InvalidLicenseKey_ReturnsUnsuccessfulResult()
    {
        // Act
        var result = await _licenseService.ActivateLicenseAsync("invalidkey");

        // Assert
        Assert.False(result.IsSuccessful);
        Assert.IsType<NotFoundException>(result.Exception); 
    }

    #endregion

    #region RevokeLicenseAsync Tests

    [Fact]
    public async Task RevokeLicenseAsync_StandardLicense_ReturnsSuccessfulResult()
    {
        // Arrange
        var license = CreateAndSaveLicense(LicenseType.Standard);

        // Act
        var result = await _licenseService.RevokeLicenseAsync(license.LicenseKey);

        // Assert
        Assert.True(result.IsSuccessful);
        Assert.Null(result.Exception);

        var updatedLicense = await _dbContext.Licenses.FindAsync(license.LicenseId);
        Assert.Equal(LicenseStatus.Revoked, updatedLicense!.Status); 
    }

    [Fact]
    public async Task RevokeLicenseAsync_TrialLicense_ReturnsSuccessfulResult()
    {
        // Arrange
        var license = CreateAndSaveLicense(LicenseType.Trial);

        // Act
        var result = await _licenseService.RevokeLicenseAsync(license.LicenseKey);

        // Assert
        Assert.True(result.IsSuccessful);
        Assert.Null(result.Exception);

        var updatedLicense = await _dbContext.Licenses.FindAsync(license.LicenseId);
        Assert.Equal(LicenseStatus.Revoked, updatedLicense!.Status);
    }

    [Fact]
    public async Task RevokeLicenseAsync_NodeLockedLicense_ReturnsSuccessfulResult()
    {
        // Arrange
        const string hardwareId = "12345678";
        var license = CreateAndSaveLicense(LicenseType.NodeLocked, hardwareId: hardwareId);

        // Act
        var result = await _licenseService.RevokeLicenseAsync(license.LicenseKey, hardwareId);

        // Assert
        Assert.True(result.IsSuccessful);
        Assert.Null(result.Exception);

        var updatedLicense = await _dbContext.Licenses.FindAsync(license.LicenseId);
        Assert.Equal(LicenseStatus.Revoked, updatedLicense!.Status);
        Assert.Null(updatedLicense.HardwareId); 
    }
    
    [Fact]
    public async Task RevokeLicenseAsync_ConcurrentLicense_ReturnsSuccessfulResult()
    {
        // Arrange
        const string hardwareId = "12345678";
        var license = CreateAndSaveLicense(LicenseType.Concurrent, maxActivations: 5);
        await _licenseService.ActivateLicenseAsync(license.LicenseKey, hardwareId); // Activate the license

        // Act
        var result = await _licenseService.RevokeLicenseAsync(license.LicenseKey, hardwareId);

        // Assert
        Assert.True(result.IsSuccessful);
        Assert.Null(result.Exception);

        var updatedLicense = await _dbContext.Licenses.FindAsync(license.LicenseId);
        Assert.Equal(0, updatedLicense!.ActiveUsersCount); 

        var activation = await _dbContext.Activations.FirstOrDefaultAsync(a => a.LicenseId == license.LicenseId);
        Assert.Null(activation); 
    }

    [Fact]
    public async Task RevokeLicenseAsync_FloatingLicense_ReturnsSuccessfulResult()
    {
        // Arrange
        const string hardwareId = "12345678";
        var license = CreateAndSaveLicense(LicenseType.Floating, maxActivations: 5);
        await _licenseService.ActivateLicenseAsync(license.LicenseKey, hardwareId); 

        // Act
        var result = await _licenseService.RevokeLicenseAsync(license.LicenseKey, hardwareId);

        // Assert
        Assert.True(result.IsSuccessful);
        Assert.Null(result.Exception);

        var updatedLicense = await _dbContext.Licenses.FindAsync(license.LicenseId);
        Assert.Equal(0, updatedLicense!.ActiveUsersCount);

        var activation = await _dbContext.Activations.FirstOrDefaultAsync(a => a.LicenseId == license.LicenseId);
        Assert.Null(activation); 
    }
    
    [Fact]
    public async Task RevokeLicenseAsync_SubscriptionLicense_ReturnsSuccessfulResult()
    {
        // Arrange
        var license = CreateAndSaveLicense(LicenseType.Subscription, DateTime.UtcNow.AddDays(30));

        // Act
        var result = await _licenseService.RevokeLicenseAsync(license.LicenseKey);

        // Assert
        Assert.True(result.IsSuccessful);
        Assert.Null(result.Exception);

        var updatedLicense = await _dbContext.Licenses.FindAsync(license.LicenseId);
        Assert.Equal(LicenseStatus.Revoked, updatedLicense!.Status);
    }
    
    [Fact]
    public async Task RevokeLicenseAsync_InvalidLicenseKey_ReturnsUnsuccessfulResult()
    {
        // Act
        var result = await _licenseService.RevokeLicenseAsync("invalidkey");

        // Assert
        Assert.False(result.IsSuccessful);
        Assert.IsType<NotFoundException>(result.Exception); 
    }

    #endregion

    #region DisconnectConcurrentLicenseUser Tests
    // ... (Implementation in progress)

    [Fact]
    public async Task DisconnectConcurrentLicenseUser_ValidRequest_ReturnsSuccessfulResult()
    {
        // Arrange
        const string hardwareId = "12345678";
        var license = CreateAndSaveLicense(LicenseType.Concurrent, maxActivations: 5);
        await _licenseService.ActivateLicenseAsync(license.LicenseKey, hardwareId); // Activate the license

        // Act
        var result = await _licenseService.DisconnectConcurrentLicenseUser(license.LicenseKey, hardwareId);

        // Assert
        Assert.True(result.IsSuccessful);
        Assert.Null(result.Exception);

        var updatedLicense = await _dbContext.Licenses.FindAsync(license.LicenseId);
        Assert.Equal(0, updatedLicense!.ActiveUsersCount);

        var activation = await _dbContext.Activations.FirstOrDefaultAsync(a => a.LicenseId == license.LicenseId);
        Assert.Null(activation);
    }

    [Fact]
    public async Task DisconnectConcurrentLicenseUser_InvalidLicenseType_ReturnsUnsuccessfulResult()
    {
        // Arrange
        const string hardwareId = "12345678";
        var license = CreateAndSaveLicense(LicenseType.NodeLocked, hardwareId: hardwareId);

        // Act
        var result = await _licenseService.DisconnectConcurrentLicenseUser(license.LicenseKey, hardwareId);

        // Assert
        Assert.False(result.IsSuccessful);
        Assert.IsType<InvalidLicenseFormatException>(result.Exception);
    }

    #endregion

    #region RenewLicenseAsync Tests

    [Fact]
    public async Task RenewLicenseAsync_SubscriptionLicense_ValidDate_ReturnsSuccessfulResult()
    {
        // Arrange
        var license = CreateAndSaveLicense(LicenseType.Subscription, DateTime.UtcNow.AddDays(30));
        var newExpirationDate = DateTime.UtcNow.AddDays(60);

        // Act
        var result = await _licenseService.RenewLicenseAsync(license.LicenseKey, newExpirationDate);

        // Assert
        Assert.True(result.IsSuccessful);
        Assert.Equal("License renewed successfully.", result.Message);
        Assert.NotNull(result.LicenseFile);
    }
    
    [Fact]
    public async Task RenewLicenseAsync_NonSubscriptionLicense_ReturnsUnsuccessfulResult()
    {
        // Arrange
        var license = CreateAndSaveLicense(LicenseType.Standard);
        var newExpirationDate = DateTime.UtcNow.AddDays(60); 

        // Act
        var result = await _licenseService.RenewLicenseAsync(license.LicenseKey, newExpirationDate);

        // Assert
        Assert.False(result.IsSuccessful);
        Assert.Equal("Invalid license type. Only subscription licenses can be renewed.", result.Message);
    }

    [Fact]
    public async Task RenewLicenseAsync_RevokedLicense_ReturnsUnsuccessfulResult()
    {
        // Arrange
        var license = CreateAndSaveLicense(LicenseType.Subscription, DateTime.UtcNow.AddDays(30));
        license.Status = LicenseStatus.Revoked;
        _dbContext.Licenses.Update(license);
        await _dbContext.SaveChangesAsync();
        var newExpirationDate = DateTime.UtcNow.AddDays(60);

        // Act
        var result = await _licenseService.RenewLicenseAsync(license.LicenseKey, newExpirationDate);

        // Assert
        Assert.False(result.IsSuccessful);
        Assert.Equal("License revoked.", result.Message); 
    }
    
    [Fact]
    public async Task RenewLicenseAsync_InvalidExpirationDate_ReturnsUnsuccessfulResult()
    {
        // Arrange
        var license = CreateAndSaveLicense(LicenseType.Subscription, DateTime.UtcNow.AddDays(30));
        var newExpirationDate = DateTime.UtcNow.AddDays(-30);

        // Act
        var result = await _licenseService.RenewLicenseAsync(license.LicenseKey, newExpirationDate);

        // Assert
        Assert.False(result.IsSuccessful);
        Assert.Equal("New expiration date cannot be in the past or before the current expiration date.", result.Message);
    }

    #endregion

    #region HeartbeatAsync Tests

    [Fact]
    public async Task HeartbeatAsync_ValidRequest_UpdatesLastHeartbeat()
    {
        // Arrange
        var license = CreateAndSaveLicense(LicenseType.Concurrent, maxActivations: 5);
        const string hardwareId = "12345678";
        await _licenseService.ActivateLicenseAsync(license.LicenseKey, hardwareId);
        var activation = await _dbContext.Activations.FirstAsync(a => a.LicenseId == license.LicenseId); 
        var initialHeartbeat = activation.LastHeartbeat;
        await Task.Delay(100);

        // Act
        var result = await _licenseService.HeartbeatAsync(license.LicenseKey, hardwareId);

        // Assert
        Assert.True(result);

        var updatedActivation = await _dbContext.Activations.FirstAsync(a => a.LicenseId == license.LicenseId);
        Assert.True(updatedActivation.LastHeartbeat > initialHeartbeat);
    }

    [Fact]
    public async Task HeartbeatAsync_NonExistentActivation_ReturnsFalse()
    {
        // Arrange
        const string licenseKey = "NonExistentKey";
        const string hardwareId = "12345678";

        // Act
        var result = await _licenseService.HeartbeatAsync(licenseKey, hardwareId);

        // Assert
        Assert.False(result); 
    }

    #endregion

    // Helper Methods for Tests

    private License CreateAndSaveLicense(LicenseType licenseType, DateTime? expirationDate = null, string? hardwareId = null, int? maxActivations = null)
    {
        var productId = _dbContext.Products.First().ProductId;
        var licenseFeature = _dbContext.LicenseFeatures.First();
        var license = new License
        {
            Type = licenseType,
            ProductId = productId,
            IssuedTo = "Test User",
            HardwareId = hardwareId,
            MaxActiveUsersCount = maxActivations,
            IssuedOn = DateTime.UtcNow,
            ExpirationDate = expirationDate,
            SubscriptionExpiryDate = licenseType == LicenseType.Subscription ? expirationDate : null,
            LicenseFeatures = [licenseFeature]
        };

        _dbContext.Licenses.Add(license);
        _dbContext.SaveChanges();
        
        return license; 
    }

    private byte[] GenerateLicenseFile(License license)
    {
        var baseLicense = new BaseLicense
        {
            LicenseId = license.LicenseId,
            LicenseKey = license.LicenseKey,
            Type = license.Type,
            IssuedOn = license.IssuedOn,
            ExpirationDate = license.ExpirationDate,
            Features = license.LicenseFeatures.ToDictionary(lf => lf.Feature.FeatureName, lf => lf.IsEnabled),
            Issuer = license.Issuer
        };
        return license.Type switch
        {
            LicenseType.Standard => LicenseManager.SaveLicense(new StandardLicense(baseLicense, license.IssuedTo)),
            LicenseType.Trial => LicenseManager.SaveLicense(new TrialLicense(baseLicense,
                license.ExpirationDate!.Value - DateTime.UtcNow)),
            LicenseType.NodeLocked => LicenseManager.SaveLicense(new NodeLockedLicense(baseLicense, license.HardwareId!)),
            LicenseType.Subscription => LicenseManager.SaveLicense(new SubscriptionLicense(baseLicense, license.IssuedTo,
                license.ExpirationDate!.Value - DateTime.UtcNow)),
            LicenseType.Floating => LicenseManager.SaveLicense(new FloatingLicense(baseLicense, license.IssuedTo,
                license.MaxActiveUsersCount!.Value)),
            LicenseType.Concurrent => LicenseManager.SaveLicense(new ConcurrentLicense(baseLicense, license.IssuedTo,
                license.MaxActiveUsersCount!.Value)),
            _ => throw new InvalidLicenseFormatException("Invalid license type.")
        };
    }
}