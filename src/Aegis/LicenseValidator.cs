using System.Text.Json;
using Aegis.Models;
using Aegis.Utilities;

namespace Aegis;

public static class LicenseValidator
{
    /// <summary>
    ///     Validates a standard license.
    /// </summary>
    /// <param name="licenseData">The raw license data.</param>
    /// <param name="userName">The username for the license.</param>
    /// <param name="serialNumber">The serial number for the license.</param>
    /// <returns>True if the license is valid, false otherwise.</returns>
    public static bool ValidateStandardLicense(byte[] licenseData, string userName, string serialNumber)
    {
        if (!VerifyLicenseData(licenseData, out var licenseObj))
            return false;

        if (licenseObj is not StandardLicense license ||
            (license.ExpirationDate.HasValue && license.ExpirationDate < DateTime.UtcNow))
            return false;

        return license.UserName == userName && license.LicenseKey == serialNumber;
    }

    /// <summary>
    ///     Validates a trial license.
    /// </summary>
    /// <param name="licenseData">The raw license data.</param>
    /// <returns>True if the license is valid, false otherwise.</returns>
    public static bool ValidateTrialLicense(byte[] licenseData)
    {
        if (!VerifyLicenseData(licenseData, out var licenseObj))
            return false;

        return licenseObj is TrialLicense license && license.ExpirationDate > DateTime.UtcNow &&
               license.TrialPeriod > TimeSpan.Zero &&
               license.IssuedOn + license.TrialPeriod > DateTime.UtcNow;
    }

    /// <summary>
    ///     Validates a node-locked license.
    /// </summary>
    /// <param name="licenseData">The raw license data.</param>
    /// <param name="hardwareId">
    ///     The hardware ID to validate against. If null, the hardware ID embedded in the license will be
    ///     used.
    /// </param>
    /// <returns>True if the license is valid, false otherwise.</returns>
    public static bool ValidateNodeLockedLicense(byte[] licenseData, string? hardwareId = null)
    {
        if (!VerifyLicenseData(licenseData, out var licenseObj))
            return false;

        return licenseObj is NodeLockedLicense license &&
               (!license.ExpirationDate.HasValue || !(license.ExpirationDate < DateTime.UtcNow)) &&
               HardwareUtils.ValidateHardwareId(hardwareId ?? license.HardwareId);
    }

    /// <summary>
    ///     Validates a subscription license.
    /// </summary>
    /// <param name="licenseData">The raw license data.</param>
    /// <returns>True if the license is valid, false otherwise.</returns>
    public static bool ValidateSubscriptionLicense(byte[] licenseData)
    {
        if (!VerifyLicenseData(licenseData, out var licenseObj))
            return false;

        return licenseObj is SubscriptionLicense license &&
               license.SubscriptionStartDate + license.SubscriptionDuration > DateTime.UtcNow &&
               license.ExpirationDate == license.SubscriptionStartDate + license.SubscriptionDuration;
    }

    /// <summary>
    ///     Validates a floating license.
    /// </summary>
    /// <param name="licenseData">The raw license data.</param>
    /// <param name="userName">The username for the license.</param>
    /// <param name="maxActiveUsersCount">The maximum number of concurrent users allowed.</param>
    /// <returns>True if the license is valid, false otherwise.</returns>
    public static bool ValidateFloatingLicense(byte[] licenseData, string userName, int maxActiveUsersCount)
    {
        if (!VerifyLicenseData(licenseData, out var licenseObj))
            return false;

        return licenseObj is FloatingLicense license &&
               license.UserName == userName &&
               license.MaxActiveUsersCount == maxActiveUsersCount;
    }

    /// <summary>
    ///     Validates a concurrent license.
    /// </summary>
    /// <param name="licenseData">The raw license data.</param>
    /// <param name="userName">The username for the license.</param>
    /// <param name="maxActiveUsersCount">The maximum number of concurrent users allowed.</param>
    /// <returns>True if the license is valid, false otherwise.</returns>
    public static bool ValidateConcurrentLicense(byte[] licenseData, string userName, int maxActiveUsersCount)
    {
        if (!VerifyLicenseData(licenseData, out var licenseObj))
            return false;

        return licenseObj is ConcurrentLicense license &&
               license.UserName == userName &&
               license.MaxActiveUsersCount == maxActiveUsersCount;
    }


    /// <summary>
    ///     Verifies the license data integrity and signature.
    /// </summary>
    /// <param name="licenseData">The raw license data.</param>
    /// <param name="license">The deserialized license object if verification succeeds.</param>
    /// <returns>True if the verification is successful, false otherwise.</returns>
    internal static bool VerifyLicenseData(byte[] licenseData, out object? license)
    {
        license = null;

        // Split the license data into its components
        var (hash, signature, encryptedData, aesKey) = LicenseManager.SplitLicenseData(licenseData);

        // Verify the RSA signature
        if (!SecurityUtils.VerifySignature(hash, signature, LicenseUtils.GetLicensingSecrets().PublicKey))
            return false;

        // Calculate the SHA256 hash of the encrypted data and compare with the provided hash
        var calculatedHash = SecurityUtils.CalculateSha256Hash(encryptedData);
        if (!hash.SequenceEqual(calculatedHash))
            return false;

        // Decrypt the license data using AES
        var decryptedData = SecurityUtils.DecryptData(encryptedData, aesKey);

        // Deserialize the license object
        license = JsonSerializer.Deserialize<BaseLicense>(decryptedData);

        return license != null;
    }
}