namespace Aegis.Exceptions;

public class ExpiredLicenseException(string message) : LicenseValidationException(message);