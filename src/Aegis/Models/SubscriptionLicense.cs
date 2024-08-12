using System.Text.Json.Serialization;
using Aegis.Enums;

namespace Aegis.Models;

[JsonDerivedType(typeof(SubscriptionLicense), "Subscription")]
public class SubscriptionLicense : BaseLicense
{
    [JsonInclude]
    public string UserName { get; protected internal set; }
    [JsonInclude]
    public DateTime SubscriptionStartDate { get; protected internal set; }
    [JsonInclude]
    public TimeSpan SubscriptionDuration { get; protected internal set; }

    [JsonConstructor]
    protected SubscriptionLicense()
    {
        Type = LicenseType.Standard;
    }
    
    public SubscriptionLicense(string userName, TimeSpan subscriptionDuration)
    {
        UserName = userName;
        SubscriptionStartDate = DateTime.UtcNow;
        SubscriptionDuration = subscriptionDuration;
        ExpirationDate = SubscriptionStartDate + subscriptionDuration;
        Type = LicenseType.Subscription;
    }

    public SubscriptionLicense(BaseLicense license, string userName, TimeSpan subscriptionDuration)
    {
        UserName = userName;
        SubscriptionStartDate = DateTime.UtcNow;
        SubscriptionDuration = subscriptionDuration;
        Type = LicenseType.Subscription;
        ExpirationDate = license.ExpirationDate;
        Features = license.Features;
        Issuer = license.Issuer;
        LicenseId = license.LicenseId;
        LicenseKey = license.LicenseKey;
        Type = license.Type;
        IssuedOn = license.IssuedOn;
    }
}