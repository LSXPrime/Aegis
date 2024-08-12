namespace Aegis.Exceptions;

public class KeyManagementException : LicenseException
{
    public KeyManagementException(string message) : base(message) { }
    public KeyManagementException(string message, Exception innerException) : base(message, innerException) { }
}