namespace Aegis.Exceptions;

public class UserMismatchException(string message) : LicenseValidationException(message);