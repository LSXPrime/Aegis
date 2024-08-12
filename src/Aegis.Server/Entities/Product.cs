namespace Aegis.Server.Entities;

public class Product
{
    public Guid ProductId { get; set; } = Guid.NewGuid();
    public string ProductName { get; set; } = string.Empty;

    // Navigation property
    public ICollection<License> Licenses { get; set; } = []; 
    public ICollection<LicenseFeature> LicenseFeatures { get; set; } = [];
}