using Microsoft.EntityFrameworkCore;
using PeopleGrid.Domain.Entities;

namespace PeopleGrid.Application.Abstractions;

public interface IApplicationDbContext : IAsyncDisposable
{
    DbSet<User> Users { get; set; }
    DbSet<UserProfile> UserProfiles { get; set; }
    DbSet<UserSession> UserSessions { get; set; }
    DbSet<RefreshToken> RefreshTokens { get; set; }
    DbSet<PasswordResetToken> PasswordResetTokens { get; set; }
    DbSet<PasswordHistory> PasswordHistories { get; set; }
    DbSet<LoginAttempt> LoginAttempts { get; set; }
    DbSet<Role> Roles { get; set; }
    DbSet<Permission> Permissions { get; set; }
    DbSet<UserRole> UserRoles { get; set; }
    DbSet<RolePermission> RolePermissions { get; set; }
    DbSet<Department> Departments { get; set; }
    DbSet<UserManagerAssignment> UserManagerAssignments { get; set; }
    DbSet<Unit> Units { get; set; }
    DbSet<Branch> Branches { get; set; }
    DbSet<JobTitle> JobTitles { get; set; }
    DbSet<GradeLevel> GradeLevels { get; set; }
    DbSet<EmploymentType> EmploymentTypes { get; set; }
    DbSet<CompanyProfile> CompanyProfiles { get; set; }
    DbSet<ApprovalLevel> ApprovalLevels { get; set; }
    DbSet<LeaveType> LeaveTypes { get; set; }
    DbSet<PublicHoliday> PublicHolidays { get; set; }
    DbSet<SystemParameter> SystemParameters { get; set; }
    DbSet<Employee> Employees { get; set; }
    DbSet<EmployeePersonalInfo> EmployeePersonalInfos { get; set; }
    DbSet<EmployeeContactInfo> EmployeeContactInfos { get; set; }
    DbSet<EmployeeEmploymentInfo> EmployeeEmploymentInfos { get; set; }
    DbSet<EmployeeBankInfo> EmployeeBankInfos { get; set; }
    DbSet<EmployeeNextOfKin> EmployeeNextOfKin { get; set; }
    DbSet<EmployeeEmergencyContact> EmployeeEmergencyContacts { get; set; }
    DbSet<EmployeeJobHistory> EmployeeJobHistories { get; set; }
    DbSet<EmployeeStatusHistory> EmployeeStatusHistories { get; set; }
    DbSet<EmployeeDocument> EmployeeDocuments { get; set; }
    DbSet<DocumentType> DocumentTypes { get; set; }
    DbSet<DocumentAccessRule> DocumentAccessRules { get; set; }
    DbSet<DocumentVerificationHistory> DocumentVerificationHistories { get; set; }
    DbSet<DocumentStorageReference> DocumentStorageReferences { get; set; }
    DbSet<HRRequest> HRRequests { get; set; }
    DbSet<ApprovalFlow> ApprovalFlows { get; set; }
    DbSet<ApprovalStep> ApprovalSteps { get; set; }
    DbSet<ApprovalRequest> ApprovalRequests { get; set; }
    DbSet<LeaveRequest> LeaveRequests { get; set; }
    DbSet<AttendanceRecord> AttendanceRecords { get; set; }
    DbSet<Notification> Notifications { get; set; }
    DbSet<AuditLog> AuditLogs { get; set; }
    DbSet<AuditLogDetail> AuditLogDetails { get; set; }
    DbSet<SystemSetting> SystemSettings { get; set; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public interface IApplicationDbContextFactory
{
    IApplicationDbContext CreateDbContext(string connectionString);
}
