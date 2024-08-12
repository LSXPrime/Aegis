using System.Text.Json.Serialization;
using Aegis.Enums;

namespace Aegis.Models;

[JsonDerivedType(typeof(ConcurrentLicense), "Concurrent")]
public class ConcurrentLicense : BaseLicense
{
    [JsonInclude]
    public string UserName { get; protected set; }
    [JsonInclude]
    public int MaxActiveUsersCount { get; protected set; }
    
    [JsonConstructor]
    protected ConcurrentLicense()
    {
        Type = LicenseType.Concurrent;
    }
    
    public ConcurrentLicense(string userName, int maxActiveUsersCount)
    {
        UserName = userName;
        MaxActiveUsersCount = maxActiveUsersCount;
        Type = LicenseType.Concurrent;
    }
    
    public ConcurrentLicense(BaseLicense license, string userName, int maxActiveUsersCount)
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