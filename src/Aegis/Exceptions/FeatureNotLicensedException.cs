namespace Aegis.Exceptions;

public class FeatureNotLicensedException(string featureName)
    : LicenseValidationException($"The feature '{featureName}' is not included in your license.");