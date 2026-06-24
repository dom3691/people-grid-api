using PeopleGrid.Application.Abstractions;
using PeopleGrid.Domain.Entities;

namespace PeopleGrid.Infrastructure.Services;

public sealed class AuditService(IApplicationDbContext dbContext, ICurrentUserService currentUser) : IAuditService
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
