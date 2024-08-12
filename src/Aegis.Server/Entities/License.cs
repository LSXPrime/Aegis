using Aegis.Enums;
using Aegis.Server.Enums;

namespace Aegis.Server.Entities;

public class License
{
    public Guid LicenseId { get; init; } = Guid.NewGuid();
    public string LicenseKey { get;  set; } = Guid.NewGuid().ToString("D").ToUpper(); 
    public LicenseType Type { get; init; }
    public DateTime IssuedOn { get;  init; } = DateTime.UtcNow;
    public DateTime? ExpirationDate { get;  set; }
    public string Issuer { get;  set; } = string.Empty;
    public LicenseStatus Status { get; set; } = LicenseStatus.Active;
    public string IssuedTo { get; init; } = string.Empty;
    public int? MaxActiveUsersCount { get; init; }
    public int? ActiveUsersCount { get; set; }
    public string? HardwareId { get; set; } = string.Empty;
    public DateTime? SubscriptionExpiryDate { get; set; }
    
    // Navigation properties
    public Guid ProductId { get; init; } = Guid.Empty;
    public Product Product { get; init; } = null!;
    public ICollection<LicenseFeature> LicenseFeatures { get; init; } = [];
    public ICollection<Activation> Activations { get; init; } = [];
    public Guid UserId { get; init; } = Guid.Empty;
    public User User { get; init; }
}