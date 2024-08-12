using Aegis.Server.Entities;

namespace Aegis.Server.DTOs;

public class LicenseValidationResult(bool isValid, License? license, Exception? exception = null)
{
    public bool IsValid { get; } = isValid;
    public License? License { get; } = license;
    public Exception? Exception { get; } = exception;
}