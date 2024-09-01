using System.Text.Json.Serialization;
using Aegis.Enums;

namespace Aegis.Models;

[JsonDerivedType(typeof(NodeLockedLicense), "NodeLocked")]
public class NodeLockedLicense : BaseLicense
{
    [JsonConstructor]
    public NodeLockedLicense()
    {
        Type = LicenseType.NodeLocked;
    }

    public NodeLockedLicense(string hardwareId)
    {
        HardwareId = hardwareId;
        Type = LicenseType.NodeLocked;
    }

    public NodeLockedLicense(BaseLicense license, string hardwareId)
    {
        HardwareId = hardwareId;
        Type = LicenseType.NodeLocked;
        ExpirationDate = license.ExpirationDate;
        Features = license.Features;
        Issuer = license.Issuer;
        LicenseId = license.LicenseId;
        LicenseKey = license.LicenseKey;
        Type = license.Type;
        IssuedOn = license.IssuedOn;
    }

    [JsonInclude] public string HardwareId { get; protected internal set; }
}