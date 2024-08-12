using Aegis.Server.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aegis.Server.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<Activation> Activations { get; set; }
    public DbSet<License> Licenses { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<Feature> Features { get; set; }
    public DbSet<LicenseFeature> LicenseFeatures { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // RefreshToken - User (One-to-One)
        modelBuilder.Entity<RefreshToken>()
            .HasOne(r => r.User)
            .WithOne(u => u.RefreshToken)
            .HasForeignKey<RefreshToken>(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // User - License (One-to-Many)
        modelBuilder.Entity<User>()
            .HasMany(u => u.Licenses)
            .WithOne(l => l.User)
            .HasForeignKey(l => l.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        
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
            .HasForeignKey(lf => lf.ProductId);
        
        // LicenseFeature - License (One-to-Many)
        modelBuilder.Entity<LicenseFeature>()
            .HasOne(lf => lf.License)
            .WithMany(l => l.LicenseFeatures)
            .HasForeignKey(lf => lf.LicenseId);

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