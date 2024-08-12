namespace Aegis.Server.Entities;

public class Feature
{
    public Guid FeatureId { get; set; } = Guid.NewGuid();
    public string FeatureName { get; set; } = string.Empty;

    // Navigation property
    public ICollection<LicenseFeature> LicenseFeatures { get; set; } = [];
}