using Aegis.Enums;
using Aegis.Exceptions;
using Aegis.Models;
using Aegis.Server.Data;
using Aegis.Server.DTOs;
using Aegis.Server.Entities;
using Aegis.Server.Enums;
using Aegis.Server.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Aegis.Server.Services;

public class LicenseService(ApplicationDbContext dbContext)
{
    /// <summary>
    /// Generates a license file asynchronously.
    /// </summary>
    /// <param name="request">The license generation request.</param>
    /// <returns>A byte array containing the generated license file.</returns>
    /// <exception cref="NotFoundException">Thrown if the product or feature is not found.</exception>
    /// <exception cref="BadRequestException">Thrown if the expiration date is in the past.</exception>
    public async Task<byte[]> GenerateLicenseAsync(LicenseGenerationRequest request)
    {
        // 1. Validate the request
        await ValidateLicenseGenerationRequestAsync(request);

        // 2. Create the License entity
        var license = CreateLicenseEntity(request);

        // 3. Add features to the license
        await AddFeaturesToLicenseAsync(license, request);

        // 4. Save to the database 
        dbContext.Licenses.Add(license);
        await dbContext.SaveChangesAsync();


        return GenerateLicenseFile(license);
    }


    /// <summary>
    /// Validates a license asynchronously.
    /// </summary>
    /// <param name="licenseKey">The license key to validate.</param>
    /// <param name="licenseFile">The license file to validate.</param>
    /// <param name="validationParams">Optional validation parameters.</param>
    /// <returns>A <see cref="LicenseValidationResult"/> object containing the validation result.</returns>
    public async Task<LicenseValidationResult> ValidateLicenseAsync(string licenseKey, byte[]? licenseFile,
        Dictionary<string, string?>? validationParams = null)
    {
        var license = await dbContext.Licenses
            .Include(l => l.Product)
            .Include(l => l.LicenseFeatures)
            .ThenInclude(lf => lf.Feature)
            .FirstOrDefaultAsync(l => l.LicenseKey == licenseKey);

        if (license == null)
            return new LicenseValidationResult(false, null, new NotFoundException("License not found."));

        if (license.ExpirationDate.HasValue && license.ExpirationDate.Value < DateTime.UtcNow ||
            license.Status == LicenseStatus.Expired)
        {
            license.Status = LicenseStatus.Expired;
            dbContext.Licenses.Update(license);
            await dbContext.SaveChangesAsync();
            return new LicenseValidationResult(false, null, new ExpiredLicenseException("License expired."));
        }

        if (license.Status == LicenseStatus.Revoked)
            return new LicenseValidationResult(false, null, new LicenseValidationException("License Revoked."));

        if (licenseFile != null)
        {
            BaseLicense? loadedLicense;

            try
            {
                loadedLicense =
                    await LicenseManager.LoadLicenseAsync(licenseFile, ValidationMode.Offline, validationParams);
            }
            catch (Exception e)
            {
                return new LicenseValidationResult(false, null, e);
            }

            if (loadedLicense == null)
                return new LicenseValidationResult(false, null,
                    new InvalidLicenseFormatException("License file is invalid."));

            // Basic license information validation
            if (loadedLicense.Type != license.Type ||
                loadedLicense.LicenseId != license.LicenseId ||
                loadedLicense.IssuedOn != license.IssuedOn)
            {
                return new LicenseValidationResult(false, null,
                    new LicenseValidationException("License file does not match the stored license."));
            }

            switch (loadedLicense)
            {
                case NodeLockedLicense nl:
                    if (validationParams != null && validationParams.TryGetValue("HardwareId", out var hardwareId) &&
                        nl.HardwareId != hardwareId)
                    {
                        return new LicenseValidationResult(false, null,
                            new HardwareMismatchException(
                                "License hardware ID does not match the requested hardware ID."));
                    }

                    break;
                case StandardLicense sl:
                    if (validationParams != null &&
                        ((validationParams.TryGetValue("SerialNumber", out var serialNumber) &&
                          sl.LicenseKey != serialNumber) ||
                         (validationParams.TryGetValue("UserName", out var userName) && sl.UserName != userName)))
                    {
                        var message = validationParams.ContainsKey("SerialNumber")
                            ? "License serial number does not match the requested serial number."
                            : "License user name does not match the requested user name.";

                        return new LicenseValidationResult(false, null, new UserMismatchException(message));
                    }

                    break;
                case SubscriptionLicense subscriptionLicense:
                    if (subscriptionLicense.SubscriptionStartDate.Add(subscriptionLicense.SubscriptionDuration) <=
                        license.SubscriptionExpiryDate!.Value)
                    {
                        return new LicenseValidationResult(false, null,
                            new ExpiredLicenseException("License expired."));
                    }

                    break;
                case FloatingLicense floatingLicense:
                    if (floatingLicense.MaxActiveUsersCount != license.MaxActiveUsersCount ||
                        floatingLicense.UserName != license.IssuedTo)
                    {
                        return new LicenseValidationResult(false, null,
                            new InvalidLicenseFormatException("License file does not match the stored license."));
                    }

                    break;
            }
        }

        return new LicenseValidationResult(true, license);
    }

    /// <summary>
    /// Activates a license asynchronously.
    /// </summary>
    /// <param name="licenseKey">The license key to activate.</param>
    /// <param name="hardwareId">The hardware ID of the machine activating the license (optional).</param>
    /// <returns>A <see cref="LicenseActivationResult"/> object containing the activation result.</returns>
    public async Task<LicenseActivationResult> ActivateLicenseAsync(string licenseKey, string? hardwareId = null)
    {
        var validationResult = await ValidateLicenseAsync(licenseKey, null,
            new Dictionary<string, string?>() { { "HardwareId", hardwareId } });
        if (!validationResult.IsValid)
        {
            return new LicenseActivationResult(false, validationResult.Exception);
        }

        var license = validationResult.License!;
        switch (license.Type)
        {
            case LicenseType.Trial:
            case LicenseType.Standard:
                break;
            case LicenseType.NodeLocked:
                license.Status = LicenseStatus.Active;
                license.HardwareId = hardwareId!;
                break;
            case LicenseType.Concurrent:
                if (license.ActiveUsersCount >= license.MaxActiveUsersCount)
                {
                    return new LicenseActivationResult(false,
                        new MaximumActivationsReachedException("Maximum activations reached."));
                }

                // Acquire a lock (using a database row lock)
                var lockObject = await dbContext.Licenses.FirstOrDefaultAsync(l => l.LicenseKey == licenseKey);
                if (lockObject == null)
                {
                    return new LicenseActivationResult(false, new NotFoundException("License not found."));
                }

                dbContext.Entry(lockObject).State = EntityState.Modified;

                try
                {
                    await dbContext.SaveChangesAsync(); // Acquire lock

                    var currentActiveUsers = await dbContext.Activations
                        .CountAsync(a => a.LicenseId == license.LicenseId);

                    if (currentActiveUsers < license.MaxActiveUsersCount)
                    {
                        dbContext.Activations.Add(new Activation
                        {
                            LicenseId = license.LicenseId,
                            MachineId = hardwareId!,
                            ActivationDate = DateTime.UtcNow,
                            LastHeartbeat = DateTime.UtcNow
                        });

                        license.ActiveUsersCount = currentActiveUsers + 1;
                        await dbContext.SaveChangesAsync();
                    }
                    else
                    {
                        return new LicenseActivationResult(false,
                            new MaximumActivationsReachedException(
                                "Maximum concurrent users reached for this license."));
                    }
                }
                finally
                {
                    dbContext.Entry(lockObject).State = EntityState.Detached;
                }

                break;
            case LicenseType.Subscription:
                if (license.SubscriptionExpiryDate < DateTime.UtcNow)
                {
                    return new LicenseActivationResult(false, new ExpiredLicenseException("Subscription has expired."));
                }

                break;
            case LicenseType.Floating:
                var activeActivations = await dbContext.Activations
                    .CountAsync(a => a.LicenseId == license.LicenseId);

                if (activeActivations < license.MaxActiveUsersCount)
                {
                    dbContext.Activations.Add(new Activation
                    {
                        LicenseId = license.LicenseId,
                        UserId = license.UserId,
                        MachineId = hardwareId!,
                        ActivationDate = DateTime.UtcNow
                    });

                    license.ActiveUsersCount = activeActivations + 1;
                }
                else
                {
                    return new LicenseActivationResult(false,
                        new MaximumActivationsReachedException("Maximum activations reached."));
                }

                break;
            default:
                return new LicenseActivationResult(false, new InvalidLicenseFormatException("Invalid license type."));
        }

        license.Status = LicenseStatus.Active;
        dbContext.Licenses.Update(license);
        await dbContext.SaveChangesAsync();
        return new LicenseActivationResult(true);
    }

    /// <summary>
    /// Disconnects a concurrent license user asynchronously.
    /// </summary>
    /// <param name="licenseKey">The license key to disconnect.</param>
    /// <param name="hardwareId">The hardware ID of the machine to disconnect (optional).</param>
    /// <returns>A <see cref="LicenseDeactivationResult"/> object containing the deactivation result.</returns>
    public async Task<LicenseDeactivationResult> DisconnectConcurrentLicenseUser(string licenseKey,
        string? hardwareId = null)
    {
        var license = await dbContext.Licenses
            .FirstOrDefaultAsync(l => l.LicenseKey == licenseKey);

        return license is { Type: LicenseType.Concurrent }
            ? await RevokeLicenseAsync(licenseKey, hardwareId)
            : new LicenseDeactivationResult(false, new InvalidLicenseFormatException("Invalid license type."));
    }

    /// <summary>
    /// Revokes a license asynchronously.
    /// </summary>
    /// <param name="licenseKey">The license key to revoke.</param>
    /// <param name="hardwareId">The hardware ID of the machine to revoke the license from (optional).</param>
    /// <returns>A <see cref="LicenseDeactivationResult"/> object containing the deactivation result.</returns>
    public async Task<LicenseDeactivationResult> RevokeLicenseAsync(string licenseKey, string? hardwareId = null)
    {
        var license = await dbContext.Licenses
            .Include(l => l.Activations)
            .FirstOrDefaultAsync(l => l.LicenseKey == licenseKey);

        if (license == null)
        {
            return new LicenseDeactivationResult(false, new NotFoundException("License not found."));
        }

        switch (license.Type)
        {
            case LicenseType.Concurrent:
            case LicenseType.Floating:
                // Acquire a lock (using a database row lock for concurrent licenses)
                var lockObject = await dbContext.Licenses.FirstOrDefaultAsync(l => l.LicenseKey == licenseKey);
                if (lockObject == null)
                {
                    return new LicenseDeactivationResult(false, new NotFoundException("License not found."));
                }

                dbContext.Entry(lockObject).State = EntityState.Modified;

                try
                {
                    await dbContext.SaveChangesAsync(); // Acquire lock

                    var activation = license.Activations.FirstOrDefault(a => a.MachineId == hardwareId);
                    if (activation != null)
                    {
                        dbContext.Activations.Remove(activation);
                        license.ActiveUsersCount--;
                        await dbContext.SaveChangesAsync();
                        return new LicenseDeactivationResult(true);
                    }
                    else
                    {
                        return new LicenseDeactivationResult(false, new NotFoundException("Activation not found."));
                    }
                }
                finally
                {
                    dbContext.Entry(lockObject).State = EntityState.Detached; // Release lock
                }

            case LicenseType.NodeLocked:
                license.Status = LicenseStatus.Revoked;
                license.HardwareId = null;
                dbContext.Licenses.Update(license);
                await dbContext.SaveChangesAsync();
                return new LicenseDeactivationResult(true);
            case LicenseType.Trial:
            case LicenseType.Standard:
            case LicenseType.Subscription:
                license.Status = LicenseStatus.Revoked;
                dbContext.Licenses.Update(license);
                await dbContext.SaveChangesAsync();
                return new LicenseDeactivationResult(true);
            default:
                return new LicenseDeactivationResult(false, new InvalidLicenseFormatException("Invalid license type."));
        }
    }

    /// <summary>
    /// Renews a subscription license asynchronously.
    /// </summary>
    /// <param name="licenseKey">The license key to renew.</param>
    /// <param name="newExpirationDate">The new expiration date for the license.</param>
    /// <returns>A <see cref="LicenseRenewalResult"/> object containing the renewal result.</returns>
    public async Task<LicenseRenewalResult> RenewLicenseAsync(string licenseKey, DateTime newExpirationDate)
    {
        var license = await dbContext.Licenses.FirstOrDefaultAsync(l => l.LicenseKey == licenseKey);
        if (license == null)
        {
            return new LicenseRenewalResult(false, "License not found.");
        }

        if (license.Type != LicenseType.Subscription)
        {
            return new LicenseRenewalResult(false, "Invalid license type. Only subscription licenses can be renewed.");
        }

        if (license.Status == LicenseStatus.Revoked)
        {
            return new LicenseRenewalResult(false, "License revoked.");
        }

        if (newExpirationDate < DateTime.UtcNow || newExpirationDate < license.SubscriptionExpiryDate)
        {
            return new LicenseRenewalResult(false,
                "New expiration date cannot be in the past or before the current expiration date.");
        }


        license.SubscriptionExpiryDate = newExpirationDate;
        license.Status = LicenseStatus.Active;
        dbContext.Licenses.Update(license);
        await dbContext.SaveChangesAsync();

        return new LicenseRenewalResult(true, "License renewed successfully.", GenerateLicenseFile(license));
    }

    /// <summary>
    /// Processes a heartbeat for a concurrent license asynchronously.
    /// </summary>
    /// <param name="licenseKey">The license key.</param>
    /// <param name="machineId">The machine ID sending the heartbeat.</param>
    /// <returns>True if the heartbeat was processed successfully, false otherwise.</returns>
    public async Task<bool> HeartbeatAsync(string licenseKey, string machineId)
    {
        var activation = await dbContext.Activations
            .FirstOrDefaultAsync(a => a.License.LicenseKey == licenseKey && a.MachineId == machineId);

        if (activation == null)
            return false;

        activation.LastHeartbeat = DateTime.UtcNow;

        dbContext.Activations.Update(activation);
        await dbContext.SaveChangesAsync();

        return true;
    }

    #region Private Helper Methods

    private async Task ValidateLicenseGenerationRequestAsync(LicenseGenerationRequest request)
    {
        if (!await dbContext.Products.AnyAsync(p => p.ProductId == request.ProductId))
            throw new NotFoundException("Product not found");

        if (request.FeatureIds.Length != 0 &&
            !await dbContext.Features.AnyAsync(f => request.FeatureIds.Contains(f.FeatureId)))
            throw new NotFoundException("Feature not found");

        if (request.ExpirationDate.HasValue && request.ExpirationDate.Value < DateTime.UtcNow)
            throw new BadRequestException("Expiration date cannot be in the past");
    }

    private License CreateLicenseEntity(LicenseGenerationRequest request)
    {
        return new License
        {
            Type = request.LicenseType,
            ProductId = request.ProductId,
            IssuedTo = request.IssuedTo,
            MaxActiveUsersCount = request.MaxActiveUsersCount,
            ExpirationDate = request.ExpirationDate,
            SubscriptionExpiryDate = request.SubscriptionDuration != null
                ? DateTime.UtcNow.Add(request.SubscriptionDuration!.Value)
                : null,
            HardwareId = request.HardwareId
        };
    }

    private async Task AddFeaturesToLicenseAsync(License license, LicenseGenerationRequest request)
    {
        if (request.FeatureIds.Length == 0)
            return;

        foreach (var featureId in request.FeatureIds)
        {
            var licenseFeature = await dbContext.LicenseFeatures
                .FirstOrDefaultAsync(lf => lf.ProductId == request.ProductId && lf.FeatureId == featureId);

            if (licenseFeature == null)
            {
                var product = await dbContext.Products.FirstAsync(p => p.ProductId == request.ProductId);
                var feature = await dbContext.Features.FirstAsync(f => f.FeatureId == featureId);

                licenseFeature = new LicenseFeature
                {
                    Product = product,
                    Feature = feature,
                    License = license,
                    IsEnabled = true
                };

                dbContext.LicenseFeatures.Add(licenseFeature);
            }
            else
            {
                licenseFeature.IsEnabled = true;
                licenseFeature.License = license;
            }
        }
    }

    private byte[] GenerateLicenseFile(License license)
    {
        var baseLicense = MapLicenseToBaseLicense(license);
        return license.Type switch
        {
            LicenseType.Standard => LicenseManager.SaveLicense(new StandardLicense(baseLicense, license.IssuedTo)),
            LicenseType.Trial => LicenseManager.SaveLicense(new TrialLicense(baseLicense,
                license.ExpirationDate!.Value - DateTime.UtcNow)),
            LicenseType.NodeLocked => LicenseManager.SaveLicense(
                new NodeLockedLicense(baseLicense, license.HardwareId!)),
            LicenseType.Subscription => LicenseManager.SaveLicense(new SubscriptionLicense(baseLicense,
                license.IssuedTo,
                license.ExpirationDate!.Value - DateTime.UtcNow)),
            LicenseType.Floating => LicenseManager.SaveLicense(new FloatingLicense(baseLicense, license.IssuedTo,
                license.MaxActiveUsersCount!.Value)),
            LicenseType.Concurrent => LicenseManager.SaveLicense(new ConcurrentLicense(baseLicense, license.IssuedTo,
                license.MaxActiveUsersCount!.Value)),
            _ => throw new InvalidLicenseFormatException("Invalid license type.")
        };
    }

    private BaseLicense MapLicenseToBaseLicense(License license)
    {
        return new BaseLicense()
        {
            LicenseId = license.LicenseId,
            LicenseKey = license.LicenseKey,
            Type = license.Type,
            IssuedOn = license.IssuedOn,
            ExpirationDate = license.ExpirationDate,
            Features = license.LicenseFeatures.ToDictionary(lf => lf.Feature.FeatureName, lf => lf.IsEnabled),
            Issuer = license.Issuer
        };
    }

    #endregion
}