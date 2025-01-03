using Aegis.Exceptions;
using Aegis.Models;
using Aegis.Models.License;

namespace Aegis;

public static class FeatureManager
{
    private static readonly ThreadLocal<BaseLicense?> CurrentLicense = new();

    /// <summary>
    /// Sets the current license.
    /// </summary>
    /// <param name="license">The license to set.</param>
    internal static void SetLicense(BaseLicense? license)
    {
        CurrentLicense.Value = license;
    }

    /// <summary>
    /// Clears the current license.
    /// </summary>
    internal static void ClearLicense()
    {
        CurrentLicense.Value = null;
    }

    /// <summary>
    /// Checks if a feature is enabled based on the current license.
    /// </summary>
    /// <param name="featureName">The name of the feature to check.</param>
    /// <returns>True if the feature is enabled, false otherwise.</returns>
    public static bool IsFeatureEnabled(string featureName)
    {
        if (CurrentLicense.Value == null ||
            CurrentLicense.Value.Features.TryGetValue(featureName, out var featureValue) == false)
            return false;

        return featureValue.Type switch
        {
            FeatureValueType.Boolean => featureValue.AsBool(),
            FeatureValueType.Integer => featureValue.AsInt() != 0,
            FeatureValueType.Float => featureValue.AsFloat() != 0,
            FeatureValueType.String => !string.IsNullOrEmpty(featureValue.AsString()),
            FeatureValueType.DateTime => featureValue.AsDateTime() != default,
            FeatureValueType.ByteArray => featureValue.AsByteArray().Length != 0,
            _ => false
        };
    }

    /// <summary>
    /// Retrieves the integer value of a feature.
    /// </summary>
    /// <param name="featureName">The name of the feature to retrieve.</param>
    /// <returns>The integer value of the feature, or default if the feature is not found or not an integer.</returns>
    public static int GetFeatureInt(string featureName)
    {
        return CurrentLicense.Value?.Features.TryGetValue(featureName, out var featureValue) == true &&
               featureValue.Type == FeatureValueType.Integer
            ? featureValue.AsInt()
            : default;
    }

    /// <summary>
    /// Retrieves the float value of a feature.
    /// </summary>
    /// <param name="featureName">The name of the feature to retrieve.</param>
    /// <returns>The float value of the feature, or default if the feature is not found or not a float.</returns>
    public static float GetFeatureFloat(string featureName)
    {
        return CurrentLicense.Value?.Features.TryGetValue(featureName, out var featureValue) == true &&
               featureValue.Type == FeatureValueType.Float
            ? featureValue.AsFloat()
            : default;
    }

    /// <summary>
    /// Retrieves the string value of a feature.
    /// </summary>
    /// <param name="featureName">The name of the feature to retrieve.</param>
    /// <returns>The string value of the feature, or default if the feature is not found or not a string.</returns>
    public static string GetFeatureString(string featureName)
    {
        return CurrentLicense.Value?.Features.TryGetValue(featureName, out var featureValue) == true &&
               featureValue.Type == FeatureValueType.String
            ? featureValue.AsString()
            : default!;
    }

    /// <summary>
    /// Retrieves the DateTime value of a feature.
    /// </summary>
    /// <param name="featureName">The name of the feature to retrieve.</param>
    /// <returns>The DateTime value of the feature, or default if the feature is not found or not a DateTime.</returns>
    public static DateTime GetFeatureDateTime(string featureName)
    {
        return CurrentLicense.Value?.Features.TryGetValue(featureName, out var featureValue) == true &&
               featureValue.Type == FeatureValueType.DateTime
            ? featureValue.AsDateTime()
            : default;
    }

    /// <summary>
    /// Retrieves the byte array value of a feature.
    /// </summary>
    /// <param name="featureName">The name of the feature to retrieve.</param>
    /// <returns>The byte array value of the feature, or default if the feature is not found or not a byte array.</returns>
    public static byte[] GetFeatureByteArray(string featureName)
    {
        return CurrentLicense.Value?.Features.TryGetValue(featureName, out var featureValue) == true &&
               featureValue.Type == FeatureValueType.ByteArray
            ? featureValue.AsByteArray()
            : default!;
    }

    /// <summary>
    /// Throws a FeatureNotLicensedException if the specified feature is not enabled.
    /// </summary>
    /// <param name="featureName">The name of the feature to check.</param>
    public static void ThrowIfNotAllowed(string featureName)
    {
        if (!IsFeatureEnabled(featureName))
            throw new FeatureNotLicensedException(featureName);
    }
}