using Aegis.Exceptions;
using Aegis.Models;

namespace Aegis;

public static class LicenseBuilder
{
    /// <summary>
    /// Sets the expiry date for the license.
    /// </summary>
    /// <param name="baseLicense">The license object to modify.</param>
    /// <param name="expiryDate">The expiry date to set.</param>
    /// <returns>The modified license object.</returns>
    /// <exception cref="LicenseGenerationException">Thrown if attempting to set an expiry date on a TrialLicense.</exception>
    public static BaseLicense WithExpiryDate(this BaseLicense baseLicense, DateTime? expiryDate)
    {
        if (baseLicense is TrialLicense)
            throw new LicenseGenerationException("Trial licenses have a predefined expiry date.");
        
        baseLicense.ExpirationDate = expiryDate;
        return baseLicense;
    }
    
    /// <summary>
    /// Adds or updates a feature flag for the license.
    /// </summary>
    /// <param name="baseLicense">The license object to modify.</param>
    /// <param name="featureName">The name of the feature.</param>
    /// <param name="enabled">Whether the feature is enabled.</param>
    /// <returns>The modified license object.</returns>
    public static BaseLicense WithFeature(this BaseLicense baseLicense, string featureName, bool enabled)
    {
        if (!baseLicense.Features.TryAdd(featureName, enabled))
            baseLicense.Features[featureName] = enabled;
        return baseLicense;
    }

    /// <summary>
    /// Sets the feature flags for the license.
    /// </summary>
    /// <param name="baseLicense">The license object to modify.</param>
    /// <param name="features">A dictionary of feature names and their enabled status.</param>
    /// <returns>The modified license object.</returns>
    public static BaseLicense WithFeatures(this BaseLicense baseLicense, Dictionary<string, bool> features)
    {
        baseLicense.Features = features;
        return baseLicense;
    }

    /// <summary>
    /// Sets the issuer for the license.
    /// </summary>
    /// <param name="baseLicense">The license object to modify.</param>
    /// <param name="issuer">The issuer to set.</param>
    /// <returns>The modified license object.</returns>
    public static BaseLicense WithIssuer(this BaseLicense baseLicense, string issuer)
    {
        baseLicense.Issuer = issuer;
        return baseLicense;
    }
    
    /// <summary>
    /// Sets the license key for the license.
    /// </summary>
    /// <param name="baseLicense">The license object to modify.</param>
    /// <param name="licenseKey">The license key to set.</param>
    /// <returns>The modified license object.</returns>
    public static BaseLicense WithLicenseKey(this BaseLicense baseLicense, string licenseKey)
    {
        baseLicense.LicenseKey = licenseKey;
        return baseLicense;
    }
    
    /// <summary>
    /// Saves the license to a file.
    /// </summary>
    /// <param name="baseLicense">The license object to save.</param>
    /// <param name="filePath">The path to the file to save the license to.</param>
    /// <returns>The license object that was saved.</returns>
    public static BaseLicense SaveLicense(this BaseLicense baseLicense, string filePath)
    {
        LicenseManager.SaveLicense(baseLicense, filePath);
        return baseLicense;
    }
}