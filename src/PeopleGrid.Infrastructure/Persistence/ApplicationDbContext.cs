using Microsoft.EntityFrameworkCore;
using PeopleGrid.Application.Abstractions;
using PeopleGrid.Domain.Common;
using PeopleGrid.Domain.Entities;
using System.Linq.Expressions;

namespace PeopleGrid.Infrastructure.Persistence;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ICurrentUserService currentUser) : DbContext(options), IApplicationDbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<UserProfile> UserProfiles { get; set; }
    public DbSet<UserSession> UserSessions { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }
    public DbSet<PasswordHistory> PasswordHistories { get; set; }
    public DbSet<LoginAttempt> LoginAttempts { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<Permission> Permissions { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }
    public DbSet<RolePermission> RolePermissions { get; set; }
    public DbSet<Department> Departments { get; set; }
    public DbSet<Unit> Units { get; set; }
    public DbSet<Branch> Branches { get; set; }
    public DbSet<JobTitle> JobTitles { get; set; }
    public DbSet<GradeLevel> GradeLevels { get; set; }
    public DbSet<EmploymentType> EmploymentTypes { get; set; }
    public DbSet<Employee> Employees { get; set; }
    public DbSet<EmployeeDocument> EmployeeDocuments { get; set; }
    public DbSet<HRRequest> HRRequests { get; set; }
    public DbSet<ApprovalFlow> ApprovalFlows { get; set; }
    public DbSet<ApprovalStep> ApprovalSteps { get; set; }
    public DbSet<ApprovalRequest> ApprovalRequests { get; set; }
    public DbSet<LeaveRequest> LeaveRequests { get; set; }
    public DbSet<AttendanceRecord> AttendanceRecords { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<SystemSetting> SystemSettings { get; set; }

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
        modelBuilder.Entity<User>().HasIndex(x => x.EmployeeNumber).IsUnique();
        modelBuilder.Entity<User>().HasIndex(x => x.Status);
        modelBuilder.Entity<User>().Property(x => x.Email).HasMaxLength(256);
        modelBuilder.Entity<User>().Property(x => x.UserName).HasMaxLength(100);
        modelBuilder.Entity<User>().Property(x => x.EmployeeNumber).HasMaxLength(50);
        modelBuilder.Entity<User>().Property(x => x.Status).HasMaxLength(50);
        modelBuilder.Entity<UserProfile>().HasIndex(x => x.UserId).IsUnique();
        modelBuilder.Entity<UserProfile>().HasIndex(x => x.DepartmentId);
        modelBuilder.Entity<UserProfile>().HasIndex(x => x.UnitId);
        modelBuilder.Entity<UserProfile>().HasIndex(x => x.BranchId);
        modelBuilder.Entity<UserProfile>().HasIndex(x => x.JobTitleId);
        modelBuilder.Entity<UserProfile>().HasIndex(x => x.EmploymentTypeId);
        modelBuilder.Entity<RefreshToken>().HasIndex(x => x.TokenHash);
        modelBuilder.Entity<RefreshToken>().HasIndex(x => x.UserId);
        modelBuilder.Entity<PasswordResetToken>().HasIndex(x => x.TokenHash);
        modelBuilder.Entity<PasswordResetToken>().HasIndex(x => x.UserId);
        modelBuilder.Entity<PasswordHistory>().HasIndex(x => x.UserId);
        modelBuilder.Entity<PasswordHistory>().HasIndex(x => x.ChangedAt);
        modelBuilder.Entity<LoginAttempt>().HasIndex(x => x.EmailOrUserName);
        modelBuilder.Entity<LoginAttempt>().HasIndex(x => x.UserId);
        modelBuilder.Entity<LoginAttempt>().HasIndex(x => x.OccurredAt);
        modelBuilder.Entity<SystemSetting>().HasIndex(x => x.Key).IsUnique();
        modelBuilder.Entity<Role>().HasIndex(x => x.Code).IsUnique();
        modelBuilder.Entity<Role>().HasIndex(x => x.Status);
        modelBuilder.Entity<Role>().Property(x => x.Code).HasMaxLength(100);
        modelBuilder.Entity<Role>().Property(x => x.Name).HasMaxLength(150);
        modelBuilder.Entity<Role>().Property(x => x.Description).HasMaxLength(500);
        modelBuilder.Entity<Role>().Property(x => x.Status).HasMaxLength(50);
        modelBuilder.Entity<Permission>().HasIndex(x => x.Code).IsUnique();
        modelBuilder.Entity<Permission>().HasIndex(x => new { x.Module, x.Action });
        modelBuilder.Entity<Permission>().Property(x => x.Code).HasMaxLength(150);
        modelBuilder.Entity<Permission>().Property(x => x.Module).HasMaxLength(100);
        modelBuilder.Entity<Permission>().Property(x => x.Action).HasMaxLength(100);
        modelBuilder.Entity<Permission>().Property(x => x.Description).HasMaxLength(500);
        modelBuilder.Entity<Department>().HasIndex(x => x.Code).IsUnique();
        modelBuilder.Entity<Department>().HasIndex(x => x.Status);
        modelBuilder.Entity<Unit>().HasIndex(x => x.Code).IsUnique();
        modelBuilder.Entity<Unit>().HasIndex(x => x.DepartmentId);
        modelBuilder.Entity<Unit>().HasIndex(x => x.Status);
        modelBuilder.Entity<Branch>().HasIndex(x => x.Code).IsUnique();
        modelBuilder.Entity<Branch>().HasIndex(x => x.Status);
        modelBuilder.Entity<JobTitle>().HasIndex(x => x.Code).IsUnique();
        modelBuilder.Entity<JobTitle>().HasIndex(x => x.GradeLevelId);
        modelBuilder.Entity<JobTitle>().HasIndex(x => x.Status);
        modelBuilder.Entity<GradeLevel>().HasIndex(x => x.Code).IsUnique();
        modelBuilder.Entity<EmploymentType>().HasIndex(x => x.Code).IsUnique();
        modelBuilder.Entity<EmploymentType>().HasIndex(x => x.Status);
        modelBuilder.Entity<Employee>().HasIndex(x => x.EmployeeNumber).IsUnique();
        modelBuilder.Entity<Employee>().HasIndex(x => x.WorkEmail).IsUnique();
        modelBuilder.Entity<UserRole>().HasIndex(x => new { x.UserId, x.RoleId }).IsUnique();
        modelBuilder.Entity<RolePermission>().HasIndex(x => new { x.RoleId, x.PermissionId }).IsUnique();
        modelBuilder.Entity<UserRole>().Property(x => x.AssignedBy).HasMaxLength(100);
        modelBuilder.Entity<RolePermission>().Property(x => x.AssignedBy).HasMaxLength(100);
        modelBuilder.Entity<RefreshToken>()
            .HasOne(x => x.ReplacedByToken)
            .WithMany()
            .HasForeignKey(x => x.ReplacedByTokenId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<UserRole>()
            .HasOne(x => x.User)
            .WithMany(x => x.UserRoles)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<UserRole>()
            .HasOne(x => x.Role)
            .WithMany(x => x.UserRoles)
            .HasForeignKey(x => x.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<RolePermission>()
            .HasOne(x => x.Role)
            .WithMany(x => x.RolePermissions)
            .HasForeignKey(x => x.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<RolePermission>()
            .HasOne(x => x.Permission)
            .WithMany(x => x.RolePermissions)
            .HasForeignKey(x => x.PermissionId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<User>()
            .HasOne(x => x.Profile)
            .WithOne(x => x.User)
            .HasForeignKey<UserProfile>(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<UserProfile>()
            .HasOne(x => x.Department)
            .WithMany(x => x.UserProfiles)
            .HasForeignKey(x => x.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<UserProfile>()
            .HasOne(x => x.Unit)
            .WithMany(x => x.UserProfiles)
            .HasForeignKey(x => x.UnitId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<UserProfile>()
            .HasOne(x => x.Branch)
            .WithMany(x => x.UserProfiles)
            .HasForeignKey(x => x.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<UserProfile>()
            .HasOne(x => x.JobTitle)
            .WithMany(x => x.UserProfiles)
            .HasForeignKey(x => x.JobTitleId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<UserProfile>()
            .HasOne(x => x.EmploymentType)
            .WithMany(x => x.UserProfiles)
            .HasForeignKey(x => x.EmploymentTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<JobTitle>()
            .HasOne(x => x.GradeLevel)
            .WithMany(x => x.JobTitles)
            .HasForeignKey(x => x.GradeLevelId)
            .OnDelete(DeleteBehavior.Restrict);

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
