using Microsoft.EntityFrameworkCore;
using PeopleGrid.Application.Abstractions;
using PeopleGrid.Domain.Common;
using PeopleGrid.Domain.Entities;
using System.Linq.Expressions;

namespace PeopleGrid.Infrastructure.Persistence;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ICurrentUserService currentUser) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Unit> Units => Set<Unit>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<JobTitle> JobTitles => Set<JobTitle>();
    public DbSet<GradeLevel> GradeLevels => Set<GradeLevel>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<EmployeeDocument> EmployeeDocuments => Set<EmployeeDocument>();
    public DbSet<HRRequest> HRRequests => Set<HRRequest>();
    public DbSet<ApprovalFlow> ApprovalFlows => Set<ApprovalFlow>();
    public DbSet<ApprovalStep> ApprovalSteps => Set<ApprovalStep>();
    public DbSet<ApprovalRequest> ApprovalRequests => Set<ApprovalRequest>();
    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditFields();
        return base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        modelBuilder.Entity<User>().HasIndex(x => x.Email).IsUnique();
        modelBuilder.Entity<User>().HasIndex(x => x.UserName).IsUnique();
        modelBuilder.Entity<Role>().HasIndex(x => x.Code).IsUnique();
        modelBuilder.Entity<Permission>().HasIndex(x => x.Code).IsUnique();
        modelBuilder.Entity<Department>().HasIndex(x => x.Code).IsUnique();
        modelBuilder.Entity<Employee>().HasIndex(x => x.EmployeeNumber).IsUnique();
        modelBuilder.Entity<Employee>().HasIndex(x => x.WorkEmail).IsUnique();
        modelBuilder.Entity<UserRole>().HasIndex(x => new { x.UserId, x.RoleId }).IsUnique();
        modelBuilder.Entity<RolePermission>().HasIndex(x => new { x.RoleId, x.PermissionId }).IsUnique();

        foreach (var entityType in modelBuilder.Model.GetEntityTypes().Where(t => typeof(SoftDeleteEntity).IsAssignableFrom(t.ClrType)))
        {
            var parameter = Expression.Parameter(entityType.ClrType, "e");
            var property = Expression.Property(parameter, nameof(SoftDeleteEntity.IsDeleted));
            var compare = Expression.Equal(property, System.Linq.Expressions.Expression.Constant(false));
            var lambda = Expression.Lambda(compare, parameter);
            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
        }
    }

    private void ApplyAuditFields()
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.CreatedBy = currentUser.UserId;
            }
            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
                entry.Entity.UpdatedBy = currentUser.UserId;
            }
        }
    }
}
