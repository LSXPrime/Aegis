namespace Aegis.Exceptions;

public class InvalidLicenseFormatException(string message) : LicenseValidationException(message);