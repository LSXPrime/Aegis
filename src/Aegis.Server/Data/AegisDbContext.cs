using Aegis.Server.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aegis.Server.Data;

public class AegisDbContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<Activation> Activations { get; set; }
    public DbSet<License> Licenses { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<Feature> Features { get; set; }
    public DbSet<LicenseFeature> LicenseFeatures { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // License - Product (One-to-Many)
        modelBuilder.Entity<License>()
            .HasOne(l => l.Product)
            .WithMany(p => p.Licenses)
            .HasForeignKey(l => l.ProductId);

        // LicenseFeature (Many-to-Many) - Configure composite key
        modelBuilder.Entity<LicenseFeature>()
            .HasKey(lf => new { lf.ProductId, lf.FeatureId });

        // LicenseFeature - Product (One-to-Many)
        modelBuilder.Entity<LicenseFeature>()
            .HasOne(lf => lf.Product)
            .WithMany(l => l.LicenseFeatures)
            .HasForeignKey(lf => lf.ProductId)
            .OnDelete(DeleteBehavior.NoAction);

        // LicenseFeature - License (One-to-Many)
        modelBuilder.Entity<LicenseFeature>()
            .HasOne(lf => lf.License)
            .WithMany(l => l.LicenseFeatures)
            .HasForeignKey(lf => lf.LicenseId)
            .OnDelete(DeleteBehavior.NoAction);

        // LicenseFeature - Feature (One-to-Many)
        modelBuilder.Entity<LicenseFeature>()
            .HasOne(lf => lf.Feature)
            .WithMany(f => f.LicenseFeatures)
            .HasForeignKey(lf => lf.FeatureId);

        // Activation - License (One-to-Many)
        modelBuilder.Entity<Activation>()
            .HasOne(a => a.License)
            .WithMany(l => l.Activations)
            .HasForeignKey(a => a.LicenseId);
    }
}