using Microsoft.EntityFrameworkCore;
using PeopleGrid.Domain.Entities;

namespace PeopleGrid.Infrastructure.Persistence;

public sealed class PlatformDbContext(DbContextOptions<PlatformDbContext> options) : DbContext(options)
{
    public DbSet<Tenant> Tenants => Set<Tenant>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.ToTable("Tenants");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Code).IsUnique();
            entity.HasIndex(x => x.Subdomain).IsUnique().HasFilter("[Subdomain] IS NOT NULL");
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Code).HasMaxLength(50).IsRequired();
            entity.Property(x => x.DatabaseName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.ConnectionString).HasMaxLength(2000).IsRequired();
        });
    }
}
