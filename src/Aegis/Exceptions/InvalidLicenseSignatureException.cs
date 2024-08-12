namespace Aegis.Exceptions;

public class InvalidLicenseSignatureException(string message) : LicenseValidationException(message);