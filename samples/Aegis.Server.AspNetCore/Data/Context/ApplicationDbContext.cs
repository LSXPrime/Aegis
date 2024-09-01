using Aegis.Server.AspNetCore.Entities;
using Aegis.Server.Data;
using Microsoft.EntityFrameworkCore;

namespace Aegis.Server.AspNetCore.Data.Context;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : AegisDbContext(options)
{
    public DbSet<User> Users { get; init; }
    public DbSet<Role> Roles { get; init; }
    public DbSet<RefreshToken> RefreshTokens { get; init; }

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
            .WithOne()
            .HasForeignKey(l => l.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        base.OnModelCreating(modelBuilder);
    }
}