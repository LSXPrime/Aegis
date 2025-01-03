using Aegis.Enums;
using Aegis.Models.License;

namespace Aegis.Models.Utils;

public class LicenseLoadResult<T>(LicenseStatus status, T? license, Exception? exception = null) where T : BaseLicense
{
    public LicenseStatus Status { get; internal set; } = status;
    public T? License { get; internal set; } = license;
    public Exception? Exception { get; set; } = exception;
}