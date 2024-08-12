using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Aegis.Enums;

[assembly: InternalsVisibleTo("Aegis.Server")]
[assembly: InternalsVisibleTo("Aegis.Server.Tests")]
namespace Aegis.Models;

[JsonDerivedType(typeof(StandardLicense), "Standard")]
[JsonDerivedType(typeof(TrialLicense), "Trial")]
[JsonDerivedType(typeof(NodeLockedLicense), "NodeLocked")]
[JsonDerivedType(typeof(SubscriptionLicense), "Subscription")]
[JsonDerivedType(typeof(FloatingLicense), "Floating")]
[JsonDerivedType(typeof(ConcurrentLicense), "Concurrent")]
public class BaseLicense
{
    [JsonInclude]
    public Guid LicenseId { get; internal init; } = Guid.NewGuid();
    [JsonInclude]
    public string LicenseKey { get; internal set; } = Guid.NewGuid().ToString("D").ToUpper();
    [JsonInclude]
    public LicenseType Type { get; init; }
    [JsonInclude]
    public DateTime IssuedOn { get; internal init; } = DateTime.UtcNow;
    [JsonInclude]
    public DateTime? ExpirationDate { get; protected internal set; }
    [JsonInclude]
    public Dictionary<string, bool> Features { get; protected internal set; } = new();
    [JsonInclude]
    public string Issuer { get; protected internal set; } = string.Empty;
}