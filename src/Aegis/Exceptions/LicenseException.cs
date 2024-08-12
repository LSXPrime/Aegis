namespace Aegis.Exceptions;

public class LicenseException : Exception
{
    protected LicenseException(string message) : base(message) { }
    protected LicenseException(string message, Exception innerException) : base(message, innerException) { }
}