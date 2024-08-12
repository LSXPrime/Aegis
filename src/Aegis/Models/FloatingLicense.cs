using System.Text.Json.Serialization;
using Aegis.Enums;

namespace Aegis.Models;

[JsonDerivedType(typeof(FloatingLicense), "Floating")]
public class FloatingLicense : BaseLicense
{
    [JsonInclude]
    public string UserName { get; protected set; }
    [JsonInclude]
    public int MaxActiveUsersCount { get; protected set; }
    
    [JsonConstructor]
    protected FloatingLicense()
    {
        Type = LicenseType.Floating;
    }
    
    public FloatingLicense(string userName, int maxActiveUsersCount)
    {
        UserName = userName;
        MaxActiveUsersCount = maxActiveUsersCount;
        Type = LicenseType.Floating;
    }
    
    public FloatingLicense(BaseLicense license, string userName, int maxActiveUsersCount)
    {
        UserName = userName;
        MaxActiveUsersCount = maxActiveUsersCount;
        Type = LicenseType.Floating;
        ExpirationDate = license.ExpirationDate;
        Features = license.Features;
        Issuer = license.Issuer;
        LicenseId = license.LicenseId;
        LicenseKey = license.LicenseKey;
        Type = license.Type;
        IssuedOn = license.IssuedOn;
    }
}