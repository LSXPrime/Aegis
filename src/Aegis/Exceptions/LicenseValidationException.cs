namespace Aegis.Exceptions;

public class LicenseValidationException : LicenseException
{
    public LicenseValidationException(string message) : base(message) { }
    public LicenseValidationException(string message, Exception innerException) : base(message, innerException) { }
}