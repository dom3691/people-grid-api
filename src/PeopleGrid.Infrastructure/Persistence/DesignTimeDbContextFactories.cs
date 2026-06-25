using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using PeopleGrid.Application.Abstractions;

namespace PeopleGrid.Infrastructure.Persistence;

public sealed class PlatformDbContextFactory : IDesignTimeDbContextFactory<PlatformDbContext>
{
    public PlatformDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<PlatformDbContext>()
            .UseSqlServer("Server=localhost;Database=PeopleGrid_PlatformDb;Trusted_Connection=True;TrustServerCertificate=True;")
            .Options;

        return new PlatformDbContext(options);
    }
}

public sealed class ApplicationDbContextDesignTimeFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=localhost;Database=PeopleGrid_TenantTemplateDb;Trusted_Connection=True;TrustServerCertificate=True;")
            .Options;

        return new ApplicationDbContext(options, new DesignTimeCurrentUserService());
    }

    private sealed class DesignTimeCurrentUserService : ICurrentUserService
    {
        public string? UserId => "DESIGN_TIME";
        public string? Email => "design-time@peoplegrid.local";
        public IReadOnlyCollection<string> Roles => [];
        public IReadOnlyCollection<string> Permissions => [];
        public bool IsAuthenticated => false;
    }
}
