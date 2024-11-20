namespace Aegis.Exceptions;

public class InvalidLicenseFormatException : LicenseValidationException
{
    public InvalidLicenseFormatException(string message) : base(message)
    {
    }

    public InvalidLicenseFormatException(string message, Exception innerException) : base(message, innerException)
    {
    }
}