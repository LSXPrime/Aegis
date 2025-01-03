namespace Aegis.Server.DTOs;

public class LicenseRenewalResult(bool isSuccessful, string message, byte[]? licenseFile = null)
{
    public bool IsSuccessful { get; } = isSuccessful;
    public string Message { get; } = message;
    public byte[]? LicenseFile { get; } = licenseFile;
}