namespace Aegis.Models;

public class LicensingSecrets
{
    public string PrivateKey { get; init; } = string.Empty;
    public string PublicKey { get; init; } = string.Empty;
    public string EncryptionKey { get; init; } = string.Empty;
    public string ApiKey { get; init; } = string.Empty;
}