using Microsoft.EntityFrameworkCore;
using PeopleGrid.Application.Abstractions;

namespace PeopleGrid.Infrastructure.Persistence;

public sealed class ApplicationDbContextFactory(ICurrentUserService currentUser) : IApplicationDbContextFactory
{
    public IApplicationDbContext CreateDbContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new ApplicationDbContext(options, currentUser);
    }
}
