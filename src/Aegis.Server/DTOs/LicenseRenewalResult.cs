namespace Aegis.Server.DTOs;

public class LicenseRenewalResult(bool isSuccessful, string message, byte[]? licenseFile = null)
{
    public bool IsSuccessful { get; set; } = isSuccessful;
    public string Message { get; set; } = message;
    public byte[]? LicenseFile { get; set; } = licenseFile;
}