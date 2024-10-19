namespace Aegis.Models.Utils;

public class LicenseValidationResult<T>(bool isValid, T? license, Exception? exception = null) where T : BaseLicense
{
    public bool IsValid { get; } = isValid;
    public T? License { get; } = license;
    public Exception? Exception { get; } = exception;
}