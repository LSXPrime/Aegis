using System.Text.Json.Serialization;
using Aegis.Enums;

namespace Aegis.Models;

[JsonDerivedType(typeof(StandardLicense), "Standard")]
public class StandardLicense : BaseLicense
{
    [JsonInclude]
    public string UserName { get; protected internal set; }
    
    [JsonConstructor]
    protected StandardLicense()
    {
        Type = LicenseType.Standard;
    }

    public StandardLicense(string userName)
    {
        UserName = userName;
        Type = LicenseType.Standard;
    }

    public StandardLicense(BaseLicense license, string userName)
    {
        UserName = userName;
        Type = LicenseType.Standard;
        ExpirationDate = license.ExpirationDate;
        Features = license.Features;
        Issuer = license.Issuer;
        LicenseId = license.LicenseId;
        LicenseKey = license.LicenseKey;
        Type = license.Type;
        IssuedOn = license.IssuedOn;
    }
}