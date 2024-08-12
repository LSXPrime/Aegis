namespace Aegis.Exceptions;

public class LicenseGenerationException : LicenseException
{
    public LicenseGenerationException(string message) : base(message) { }
    public LicenseGenerationException(string message, Exception innerException) : base(message, innerException) { }
}