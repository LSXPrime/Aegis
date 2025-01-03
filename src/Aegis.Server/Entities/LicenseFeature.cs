using Aegis.Models;

namespace Aegis.Server.Entities;

public class LicenseFeature : Aegis.Models.Feature
{
    public bool IsEnabled { get; set; }
    
    // Overriding properties to allow mutability
    public new FeatureValueType Type { get; set; } = FeatureValueType.Boolean;
    public new byte[]? Data { get; set; }

    // Navigation properties
    public Guid ProductId { get; set; }
    public Product Product { get; set; }

    public Guid FeatureId { get; set; }
    public Feature Feature { get; set; }

    public Guid LicenseId { get; set; }
    public License License { get; set; }
}