namespace Aegis.Server.Entities;

public class LicenseFeature
{
    public bool IsEnabled { get; set; } 

    // Navigation properties
    public Guid ProductId { get; set; }
    public Product Product { get; set; }

    public Guid FeatureId { get; set; }
    public Feature Feature { get; set; } 
    
    public Guid LicenseId { get; set; }
    public License License { get; set; }
}