namespace Aegis.Server.DTOs;

public class LicenseActivationResult(bool isSuccessful, Exception? exception = null)
{
    public bool IsSuccessful { get; } = isSuccessful;
    public Exception? Exception { get; } = exception;
}