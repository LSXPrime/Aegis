using System.Text;
using Aegis.Enums;
using Aegis.Exceptions;
using Aegis.Interfaces;
using Aegis.Models.License;
using Aegis.Models.Utils;
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

    public static bool ValidateLicenseRules<T>(T license, Dictionary<string, string?>? validationParams = null)
        where T : BaseLicense
    {
        return ValidationRules.All(rule => rule.Validate(license, validationParams).Status == LicenseStatus.Valid) &&
               ValidationRuleGroups.All(ruleGroup =>
                   ruleGroup.Value.Validate(license, validationParams).Status == LicenseStatus.Valid);
    }

    /// <summary>
    ///     Validates a standard license.
    /// </summary>
    /// <param name="licenseData">The raw license data.</param>
    /// <param name="userName">The username for the license.</param>
    /// <param name="licenseKey">The serial number for the license.</param>
    /// <returns>True if the license is valid, false otherwise.</returns>
    public static LicenseStatus ValidateStandardLicense(byte[] licenseData, string userName, string licenseKey)
    {
        var verifiedLicense = VerifyLicenseData(licenseData);
        if (verifiedLicense.Status != LicenseStatus.Valid)
            return verifiedLicense.Status;

        if (verifiedLicense.License is not StandardLicense license ||
            (license.ExpirationDate.HasValue && license.ExpirationDate < DateTime.UtcNow))
            return LicenseStatus.Invalid;

        return license.UserName == userName && license.LicenseKey == licenseKey
            ? LicenseStatus.Valid
            : LicenseStatus.Invalid;
    }

    /// <summary>
    ///     Validates a trial license.
    /// </summary>
    /// <param name="licenseData">The raw license data.</param>
    /// <returns>True if the license is valid, false otherwise.</returns>
    public static LicenseStatus ValidateTrialLicense(byte[] licenseData)
    {
        var verifiedLicense = VerifyLicenseData(licenseData);
        if (verifiedLicense.Status != LicenseStatus.Valid)
            return verifiedLicense.Status;

        return verifiedLicense.License is TrialLicense license && license.ExpirationDate > DateTime.UtcNow &&
               license.TrialPeriod > TimeSpan.Zero &&
               license.IssuedOn + license.TrialPeriod > DateTime.UtcNow
            ? LicenseStatus.Valid
            : LicenseStatus.Expired;
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
    public static LicenseStatus ValidateNodeLockedLicense(byte[] licenseData, string? hardwareId = null)
    {
        var verifiedLicense = VerifyLicenseData(licenseData);
        if (verifiedLicense.Status != LicenseStatus.Valid)
            return verifiedLicense.Status;

        return verifiedLicense.License is NodeLockedLicense license &&
               (!license.ExpirationDate.HasValue || !(license.ExpirationDate < DateTime.UtcNow)) &&
               _hardwareIdentifier.ValidateHardwareIdentifier(hardwareId ?? license.HardwareId)
            ? LicenseStatus.Valid
            : LicenseStatus.Revoked;
    }

    /// <summary>
    ///     Validates a subscription license.
    /// </summary>
    /// <param name="licenseData">The raw license data.</param>
    /// <returns>True if the license is valid, false otherwise.</returns>
    public static LicenseStatus ValidateSubscriptionLicense(byte[] licenseData)
    {
        var verifiedLicense = VerifyLicenseData(licenseData);
        if (verifiedLicense.Status != LicenseStatus.Valid)
            return verifiedLicense.Status;

        return verifiedLicense.License is SubscriptionLicense license &&
               license.SubscriptionStartDate + license.SubscriptionDuration > DateTime.UtcNow &&
               license.ExpirationDate == license.SubscriptionStartDate + license.SubscriptionDuration
            ? LicenseStatus.Valid
            : LicenseStatus.Expired;
    }

    /// <summary>
    ///     Validates a floating license.
    /// </summary>
    /// <param name="licenseData">The raw license data.</param>
    /// <param name="userName">The username for the license.</param>
    /// <param name="maxActiveUsersCount">The maximum number of concurrent users allowed.</param>
    /// <returns>True if the license is valid, false otherwise.</returns>
    public static LicenseStatus ValidateFloatingLicense(byte[] licenseData, string userName, int maxActiveUsersCount)
    {
        var verifiedLicense = VerifyLicenseData(licenseData);
        if (verifiedLicense.Status != LicenseStatus.Valid)
            return verifiedLicense.Status;

        return verifiedLicense.License is FloatingLicense license &&
               license.UserName == userName &&
               license.MaxActiveUsersCount == maxActiveUsersCount
            ? LicenseStatus.Valid
            : LicenseStatus.Invalid;
    }

    /// <summary>
    ///     Validates a concurrent license.
    /// </summary>
    /// <param name="licenseData">The raw license data.</param>
    /// <param name="userName">The username for the license.</param>
    /// <param name="maxActiveUsersCount">The maximum number of concurrent users allowed.</param>
    /// <returns>True if the license is valid, false otherwise.</returns>
    public static LicenseStatus ValidateConcurrentLicense(byte[] licenseData, string userName, int maxActiveUsersCount)
    {
        var verifiedLicense = VerifyLicenseData(licenseData);
        if (verifiedLicense.Status != LicenseStatus.Valid)
            return verifiedLicense.Status;

        return verifiedLicense.License is ConcurrentLicense license &&
               license.UserName == userName &&
               license.MaxActiveUsersCount == maxActiveUsersCount
            ? LicenseStatus.Valid
            : LicenseStatus.Invalid;
    }


    /// <summary>
    ///     Verifies the license data integrity and signature.
    /// </summary>
    /// <param name="licenseData">The raw license data.</param>
    /// <returns><see cref="LicenseLoadResult{T}"/>object indicating the validation result.</returns>
    internal static LicenseLoadResult<BaseLicense> VerifyLicenseData(byte[] licenseData)
    {
        // Split the license data into its components
        var (hash, signature, encryptedData, aesKey) = SplitLicenseData(licenseData);

        // Verify the RSA signature
        if (!SecurityUtils.VerifySignature(hash, signature, LicenseUtils.GetLicensingSecrets().PublicKey))
            return new LicenseLoadResult<BaseLicense>(LicenseStatus.Invalid, null,
                new InvalidLicenseSignatureException("License signature verification failed."));


        // Calculate the SHA256 hash of the encrypted data and compare with the provided hash
        var calculatedHash = SecurityUtils.CalculateSha256Hash(encryptedData);
        if (!hash.SequenceEqual(calculatedHash))
            return new LicenseLoadResult<BaseLicense>(LicenseStatus.Invalid, null,
                new InvalidLicenseSignatureException("License data integrity check failed."));

        // Decrypt the license data using AES
        var decryptedData = SecurityUtils.DecryptData(encryptedData, aesKey);

        // Deserialize the license object
        var license = _serializer.Deserialize(Encoding.UTF8.GetString(decryptedData));
        return license is null
            ? new LicenseLoadResult<BaseLicense>(LicenseStatus.Invalid, null,
                new LicenseValidationException("Failed to deserialize license."))
            : new LicenseLoadResult<BaseLicense>(LicenseStatus.Valid, license);
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