namespace Aegis.Exceptions;

public class MaximumActivationsReachedException(string message) : LicenseException(message);