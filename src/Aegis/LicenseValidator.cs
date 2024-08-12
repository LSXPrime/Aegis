using System.Text.Json;
using Aegis.Models;
using Aegis.Utilities;

namespace Aegis;

public static class LicenseValidator
{
    /// <summary>
    /// Validates a standard license.
    /// </summary>
    /// <param name="encryptedLicenseData">The encrypted license data.</param>
    /// <param name="signature">The signature of the license data.</param>
    /// <param name="userName">The user name for the license.</param>
    /// <param name="serialNumber">The serial number for the license.</param>
    /// <returns>True if the license is valid, false otherwise.</returns>
    public static bool ValidateStandardLicense(byte[] encryptedLicenseData, byte[] signature,
        string userName, string serialNumber)
    {
        if (!VerifySignatureAndDecrypt(encryptedLicenseData, signature, out var licenseObj))
            return false;

        if (licenseObj is not StandardLicense license || license.ExpirationDate.HasValue && license.ExpirationDate < DateTime.UtcNow)
            return false;

        return license.UserName == userName && license.LicenseKey == serialNumber;
    }

    /// <summary>
    /// Validates a trial license.
    /// </summary>
    /// <param name="encryptedLicenseData">The encrypted license data.</param>
    /// <param name="signature">The signature of the license data.</param>
    /// <returns>True if the license is valid, false otherwise.</returns>
    public static bool ValidateTrialLicense(byte[] encryptedLicenseData, byte[] signature)
    {
        if (!VerifySignatureAndDecrypt(encryptedLicenseData, signature, out var licenseObj))
            return false;

        return licenseObj is TrialLicense license && license.ExpirationDate > DateTime.UtcNow &&
               license.TrialPeriod > TimeSpan.Zero &&
               license.IssuedOn + license.TrialPeriod > DateTime.UtcNow;
    }

    /// <summary>
    /// Validates a node-locked license.
    /// </summary>
    /// <param name="encryptedLicenseData">The encrypted license data.</param>
    /// <param name="signature">The signature of the license data.</param>
    /// <param name="hardwareId">The hardware ID to validate against. If null, the hardware ID embedded in the license will be used.</param>
    /// <returns>True if the license is valid, false otherwise.</returns>
    public static bool ValidateNodeLockedLicense(byte[] encryptedLicenseData, byte[] signature, string? hardwareId = null)
    {
        if (!VerifySignatureAndDecrypt(encryptedLicenseData, signature, out var licenseObj))
            return false;

        return licenseObj is NodeLockedLicense license &&
               (!license.ExpirationDate.HasValue || !(license.ExpirationDate < DateTime.UtcNow)) &&
               HardwareUtils.ValidateHardwareId(hardwareId ?? license.HardwareId);
    }
    
    /// <summary>
    /// Validates a subscription license.
    /// </summary>
    /// <param name="encryptedLicenseData">The encrypted license data.</param>
    /// <param name="signature">The signature of the license data.</param>
    /// <returns>True if the license is valid, false otherwise.</returns>
    public static bool ValidateSubscriptionLicense(byte[] encryptedLicenseData, byte[] signature)
    {
        if (!VerifySignatureAndDecrypt(encryptedLicenseData, signature, out var licenseObj))
            return false;

        return licenseObj is SubscriptionLicense license &&
               license.SubscriptionStartDate + license.SubscriptionDuration > DateTime.UtcNow && license.ExpirationDate == license.SubscriptionStartDate + license.SubscriptionDuration;
    }

    /// <summary>
    /// Validates a floating license.
    /// </summary>
    /// <param name="encryptedLicenseData">The encrypted license data.</param>
    /// <param name="signature">The signature of the license data.</param>
    /// <param name="userName">The user name for the license.</param>
    /// <param name="maxActiveUsersCount">The maximum number of concurrent users allowed.</param>
    /// <returns>True if the license is valid, false otherwise.</returns>
    public static bool ValidateFloatingLicense(byte[] encryptedLicenseData, byte[] signature, string userName,
        int maxActiveUsersCount)
    {
        if (!VerifySignatureAndDecrypt(encryptedLicenseData, signature, out var licenseObj))
            return false;
        
        return licenseObj is FloatingLicense license &&
               license.UserName == userName &&
               license.MaxActiveUsersCount == maxActiveUsersCount;
    }
    
    /// <summary>
    /// Validates a concurrent license.
    /// </summary>
    /// <param name="encryptedLicenseData">The encrypted license data.</param>
    /// <param name="signature">The signature of the license data.</param>
    /// <param name="userName">The user name for the license.</param>
    /// <param name="maxActiveUsersCount">The maximum number of concurrent users allowed.</param>
    /// <returns>True if the license is valid, false otherwise.</returns>
    public static bool ValidateConcurrentLicense(byte[] encryptedLicenseData, byte[] signature, string userName,
        int maxActiveUsersCount)
    {
        if (!VerifySignatureAndDecrypt(encryptedLicenseData, signature, out var licenseObj))
            return false;
        
        return licenseObj is ConcurrentLicense license &&
               license.UserName == userName &&
               license.MaxActiveUsersCount == maxActiveUsersCount;
    }

    /// <summary>
    /// Verifies the signature and decrypts the license data.
    /// </summary>
    /// <param name="encryptedLicenseData">The encrypted license data.</param>
    /// <param name="signature">The signature of the license data.</param>
    /// <param name="license">The deserialized license object.</param>
    /// <returns>True if the signature and decryption are successful, false otherwise.</returns>
    private static bool VerifySignatureAndDecrypt(byte[] encryptedLicenseData, byte[] signature, out object? license)
    {
        license = null;
        if (!SecurityUtils.VerifySignature(encryptedLicenseData, signature, LicenseUtils.GetLicensingSecrets().PublicKey))
            return false;

        var decryptedData = SecurityUtils.DecryptData(encryptedLicenseData, LicenseUtils.GetLicensingSecrets().PrivateKey);
        license = JsonSerializer.Deserialize<BaseLicense>(decryptedData);
        
        return license != null;
    }
}