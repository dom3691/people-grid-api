using PeopleGrid.Application.Abstractions;
using PeopleGrid.Domain.Entities;
using PeopleGrid.Infrastructure.Persistence;

namespace PeopleGrid.Infrastructure.Services;

public sealed class AuditService(ApplicationDbContext dbContext, ICurrentUserService currentUser) : IAuditService
{
    public async Task TrackAsync(string module, string action, string entityType, string? entityId = null, string outcome = "Success", CancellationToken cancellationToken = default)
    {
        dbContext.AuditLogs.Add(new AuditLog
        {
            ActorUserId = currentUser.UserId,
            Module = module,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Outcome = outcome
        });
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
