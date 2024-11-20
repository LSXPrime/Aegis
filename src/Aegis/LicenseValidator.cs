using System.Text;
using System.Text.Json;
using Aegis.Exceptions;
using Aegis.Interfaces;
using Aegis.Models;
using Aegis.Serialization;
using Aegis.Utilities;

namespace Aegis;

public static class LicenseValidator
{
    private static readonly List<IValidationRule> ValidationRules = [];
    private static readonly Dictionary<Type, IValidationRuleGroup> ValidationRuleGroups = new();
    private static ILicenseSerializer _serializer = new JsonLicenseSerializer();
    private static IHardwareIdentifier _hardwareIdentifier = new DefaultHardwareIdentifier();
    
    internal static void SetSerializer(ILicenseSerializer serializer)
    {
        ArgumentNullException.ThrowIfNull(serializer);
        _serializer = serializer;
    }
    
    internal static void SetHardwareIdentifier(IHardwareIdentifier hardwareIdentifier)
    {
        _hardwareIdentifier = hardwareIdentifier;
    }
    
    public static void AddValidationRule(IValidationRule rule)
    {
        ValidationRules.Add(rule);
    }

    public static void RemoveValidationRule(IValidationRule rule)
    {
        ValidationRules.Remove(rule);
    }

    public static void AddValidationRuleGroup<T>(IValidationRuleGroup ruleGroup) where T : BaseLicense
    {
        ValidationRuleGroups.Add(typeof(T), ruleGroup);
    }

    public static void RemoveValidationRuleGroup<T>() where T : BaseLicense
    {
        ValidationRuleGroups.Remove(typeof(T));
    }
    
    public static bool ValidateLicenseRules<T>(T license, Dictionary<string, string?>? validationParams = null) where T : BaseLicense
    {
        return ValidationRules.All(rule => rule.Validate(license, validationParams).IsValid) && ValidationRuleGroups.All(ruleGroup => ruleGroup.Value.Validate(license, validationParams).IsValid);
    }
    
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
               _hardwareIdentifier.ValidateHardwareIdentifier(hardwareId ?? license.HardwareId);
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
    /// <param name="throwOnFailure">True to throw an exception if verification fails.</param>
    /// <returns>True if the verification is successful, false otherwise.</returns>
    internal static bool VerifyLicenseData(byte[] licenseData, out BaseLicense? license, bool throwOnFailure = false)
    {
        license = null;

        // Split the license data into its components
        var (hash, signature, encryptedData, aesKey) = SplitLicenseData(licenseData);

        // Verify the RSA signature
        if (!SecurityUtils.VerifySignature(hash, signature, LicenseUtils.GetLicensingSecrets().PublicKey))
        {
            if (throwOnFailure)
                throw new InvalidLicenseSignatureException("License signature verification failed.");

            return false;
        }


        // Calculate the SHA256 hash of the encrypted data and compare with the provided hash
        var calculatedHash = SecurityUtils.CalculateSha256Hash(encryptedData);
        if (!hash.SequenceEqual(calculatedHash))
        {
            if (throwOnFailure)
                throw new InvalidLicenseSignatureException("License data integrity check failed.");

            return false;
        }

        // Decrypt the license data using AES
        var decryptedData = SecurityUtils.DecryptData(encryptedData, aesKey);

        // Deserialize the license object
        license = _serializer.Deserialize(Encoding.UTF8.GetString(decryptedData));

        return license != null;
    }
    
    internal static (byte[] hash, byte[] signature, byte[] encryptedData, byte[] aesKey) SplitLicenseData(
        byte[] licenseData)
    {
        var offset = 0;

        // Extract hash
        var hashLength = BitConverter.ToInt32(licenseData, offset);
        offset += 4;
        var hash = new byte[hashLength];
        Array.Copy(licenseData, offset, hash, 0, hashLength);
        offset += hashLength;

        // Extract signature
        var signatureLength = BitConverter.ToInt32(licenseData, offset);
        offset += 4;
        var signature = new byte[signatureLength];
        Array.Copy(licenseData, offset, signature, 0, signatureLength);
        offset += signatureLength;

        // Extract encrypted data
        var encryptedDataLength = BitConverter.ToInt32(licenseData, offset);
        offset += 4;
        var encryptedData = new byte[encryptedDataLength];
        Array.Copy(licenseData, offset, encryptedData, 0, encryptedDataLength);
        offset += encryptedDataLength;

        // Extract AES key
        var aesKeyLength = BitConverter.ToInt32(licenseData, offset);
        offset += 4;
        var aesKey = new byte[aesKeyLength];
        Array.Copy(licenseData, offset, aesKey, 0, aesKeyLength);

        return (hash, signature, encryptedData, aesKey);
    }
}