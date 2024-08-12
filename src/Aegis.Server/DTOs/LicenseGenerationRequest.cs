using Aegis.Enums;

namespace Aegis.Server.DTOs;

public class LicenseGenerationRequest
{
    public LicenseType LicenseType { get; set; } 
    public DateTime? ExpirationDate { get; set; }
    public Guid ProductId { get; init; }
    public string IssuedTo { get; init; } = string.Empty;
    public int? MaxActiveUsersCount { get; } 
    public string? HardwareId { get; }
    public TimeSpan? SubscriptionDuration { get; }
    public Guid[] FeatureIds { get; init; } = [];
}