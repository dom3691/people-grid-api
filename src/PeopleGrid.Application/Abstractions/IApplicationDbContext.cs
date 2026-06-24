using Microsoft.EntityFrameworkCore;
using PeopleGrid.Domain.Entities;

namespace PeopleGrid.Application.Abstractions;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; set; }
    DbSet<Role> Roles { get; set; }
    DbSet<Permission> Permissions { get; set; }
    DbSet<UserRole> UserRoles { get; set; }
    DbSet<RolePermission> RolePermissions { get; set; }
    DbSet<Department> Departments { get; set; }
    DbSet<Unit> Units { get; set; }
    DbSet<Branch> Branches { get; set; }
    DbSet<JobTitle> JobTitles { get; set; }
    DbSet<GradeLevel> GradeLevels { get; set; }
    DbSet<Employee> Employees { get; set; }
    DbSet<EmployeeDocument> EmployeeDocuments { get; set; }
    DbSet<HRRequest> HRRequests { get; set; }
    DbSet<ApprovalFlow> ApprovalFlows { get; set; }
    DbSet<ApprovalStep> ApprovalSteps { get; set; }
    DbSet<ApprovalRequest> ApprovalRequests { get; set; }
    DbSet<LeaveRequest> LeaveRequests { get; set; }
    DbSet<AttendanceRecord> AttendanceRecords { get; set; }
    DbSet<Notification> Notifications { get; set; }
    DbSet<AuditLog> AuditLogs { get; set; }
    DbSet<SystemSetting> SystemSettings { get; set; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
