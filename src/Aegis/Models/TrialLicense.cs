using System.Text.Json.Serialization;
using Aegis.Enums;

namespace Aegis.Models;

[JsonDerivedType(typeof(TrialLicense), "Trial")]
public class TrialLicense : BaseLicense
{
    [JsonInclude]
    public TimeSpan TrialPeriod { get; protected init; }
    
    [JsonConstructor]
    protected TrialLicense()
    {
        Type = LicenseType.Trial;
    }

    public TrialLicense(TimeSpan trialPeriod)
    {
        TrialPeriod = trialPeriod;
        ExpirationDate = DateTime.UtcNow.Add(trialPeriod);
        Type = LicenseType.Trial;
    }
    
    public TrialLicense(BaseLicense license, TimeSpan trialPeriod)
    {
        TrialPeriod = trialPeriod;
        ExpirationDate = DateTime.UtcNow.Add(trialPeriod);
        Type = LicenseType.Trial;
        Features = license.Features;
        Issuer = license.Issuer;
        LicenseId = license.LicenseId;
        LicenseKey = license.LicenseKey;
        Type = license.Type;
        IssuedOn = license.IssuedOn;
    }
}