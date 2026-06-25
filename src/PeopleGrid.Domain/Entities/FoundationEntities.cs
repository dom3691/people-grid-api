using PeopleGrid.Domain.Common;

namespace PeopleGrid.Domain.Entities;

public sealed class Tenant : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Subdomain { get; set; }
    public string DatabaseName { get; set; } = string.Empty;
    public string ConnectionString { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed class User : SoftDeleteEntity
{
    public string EmployeeNumber { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime PasswordChangedAt { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "Active";
    public bool IsActive { get; set; } = true;
    public int FailedLoginCount { get; set; }
    public DateTime? LockoutEnd { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<UserSession> Sessions { get; set; } = new List<UserSession>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<PasswordResetToken> PasswordResetTokens { get; set; } = new List<PasswordResetToken>();
    public ICollection<PasswordHistory> PasswordHistories { get; set; } = new List<PasswordHistory>();
    public ICollection<Department> HeadedDepartments { get; set; } = new List<Department>();
    public ICollection<UserManagerAssignment> ManagerAssignments { get; set; } = new List<UserManagerAssignment>();
    public ICollection<UserManagerAssignment> DirectReportAssignments { get; set; } = new List<UserManagerAssignment>();
    public UserProfile? Profile { get; set; }
}

public sealed class UserProfile : SoftDeleteEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public Guid? DepartmentId { get; set; }
    public Department? Department { get; set; }
    public Guid? UnitId { get; set; }
    public Unit? Unit { get; set; }
    public Guid? BranchId { get; set; }
    public Branch? Branch { get; set; }
    public Guid? JobTitleId { get; set; }
    public JobTitle? JobTitle { get; set; }
    public Guid? EmploymentTypeId { get; set; }
    public EmploymentType? EmploymentType { get; set; }
}

public sealed class UserSession : SoftDeleteEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public string RefreshTokenHash { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAt { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class RefreshToken : SoftDeleteEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public Guid? ReplacedByTokenId { get; set; }
    public RefreshToken? ReplacedByToken { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public bool IsActive => RevokedAt is null && ExpiresAt > DateTime.UtcNow;
}

public sealed class PasswordResetToken : SoftDeleteEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }
    public string? RequestedIpAddress { get; set; }
    public bool IsActive => UsedAt is null && ExpiresAt > DateTime.UtcNow;
}

public sealed class PasswordHistory : AuditableEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
}

public sealed class LoginAttempt : BaseEntity
{
    public Guid? UserId { get; set; }
    public User? User { get; set; }
    public string EmailOrUserName { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? FailureReason { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}

public sealed class Role : SoftDeleteEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsSystemRole { get; set; }
    public string Status { get; set; } = "Active";
    public bool IsActive { get; set; } = true;
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}

public sealed class Permission : AuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}

public sealed class UserRole : AuditableEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public Guid RoleId { get; set; }
    public Role? Role { get; set; }
    public string? AssignedBy { get; set; }
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
}

public sealed class RolePermission : AuditableEntity
{
    public Guid RoleId { get; set; }
    public Role? Role { get; set; }
    public Guid PermissionId { get; set; }
    public Permission? Permission { get; set; }
    public string? AssignedBy { get; set; }
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
}

public sealed class Department : SoftDeleteEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Guid? HeadUserId { get; set; }
    public User? HeadUser { get; set; }
    public string Status { get; set; } = "Active";
    public bool IsActive { get; set; } = true;
    public ICollection<Unit> Units { get; set; } = new List<Unit>();
    public ICollection<UserProfile> UserProfiles { get; set; } = new List<UserProfile>();
}

public sealed class UserManagerAssignment : SoftDeleteEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public Guid ManagerUserId { get; set; }
    public User? ManagerUser { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsCurrent { get; set; } = true;
}

public sealed class Unit : SoftDeleteEntity
{
    public Guid DepartmentId { get; set; }
    public Department? Department { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = "Active";
    public bool IsActive { get; set; } = true;
    public ICollection<UserProfile> UserProfiles { get; set; } = new List<UserProfile>();
}

public sealed class Branch : SoftDeleteEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Country { get; set; }
    public string? StateRegion { get; set; }
    public string Status { get; set; } = "Active";
    public bool IsActive { get; set; } = true;
    public ICollection<UserProfile> UserProfiles { get; set; } = new List<UserProfile>();
}

public sealed class JobTitle : SoftDeleteEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Guid? GradeLevelId { get; set; }
    public GradeLevel? GradeLevel { get; set; }
    public string Status { get; set; } = "Active";
    public bool IsActive { get; set; } = true;
    public ICollection<UserProfile> UserProfiles { get; set; } = new List<UserProfile>();
}

public sealed class GradeLevel : SoftDeleteEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int RankOrder { get; set; }
    public string Status { get; set; } = "Active";
    public bool IsActive { get; set; } = true;
    public ICollection<JobTitle> JobTitles { get; set; } = new List<JobTitle>();
}

public sealed class EmploymentType : SoftDeleteEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = "Active";
    public bool IsActive { get; set; } = true;
    public ICollection<UserProfile> UserProfiles { get; set; } = new List<UserProfile>();
}

public sealed class Employee : SoftDeleteEntity
{
    public Guid? UserId { get; set; }
    public User? User { get; set; }
    public string EmployeeNumber { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string WorkEmail { get; set; } = string.Empty;
    public Guid? DepartmentId { get; set; }
    public Guid? UnitId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? JobTitleId { get; set; }
    public Guid? GradeLevelId { get; set; }
    public Guid? LineManagerId { get; set; }
    public string Status { get; set; } = "Active";
}

public sealed class EmployeeDocument : SoftDeleteEntity
{
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string StorageKey { get; set; } = string.Empty;
    public DateTime? ExpiryDate { get; set; }
    public string VerificationStatus { get; set; } = "Pending";
}

public sealed class HRRequest : SoftDeleteEntity
{
    public string RequestNumber { get; set; } = string.Empty;
    public Guid EmployeeId { get; set; }
    public string RequestType { get; set; } = string.Empty;
    public string Status { get; set; } = "Draft";
    public string? Description { get; set; }
}

public sealed class ApprovalFlow : SoftDeleteEntity
{
    public string Name { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string RequestType { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public ICollection<ApprovalStep> Steps { get; set; } = new List<ApprovalStep>();
}

public sealed class ApprovalStep : SoftDeleteEntity
{
    public Guid ApprovalFlowId { get; set; }
    public ApprovalFlow? ApprovalFlow { get; set; }
    public int Sequence { get; set; }
    public string ApproverType { get; set; } = "Role";
    public Guid? ApproverRoleId { get; set; }
    public Guid? ApproverUserId { get; set; }
}

public sealed class ApprovalRequest : SoftDeleteEntity
{
    public Guid ApprovalFlowId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string Status { get; set; } = "Pending";
    public Guid? CurrentApproverId { get; set; }
}

public sealed class LeaveRequest : SoftDeleteEntity
{
    public Guid EmployeeId { get; set; }
    public string LeaveType { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal Days { get; set; }
    public string Status { get; set; } = "Draft";
}

public sealed class AttendanceRecord : SoftDeleteEntity
{
    public Guid EmployeeId { get; set; }
    public DateOnly AttendanceDate { get; set; }
    public DateTime? ClockInAt { get; set; }
    public DateTime? ClockOutAt { get; set; }
    public string Status { get; set; } = "Present";
}

public sealed class Notification : SoftDeleteEntity
{
    public Guid RecipientUserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = "Info";
    public bool IsRead { get; set; }
    public string? RelatedEntityType { get; set; }
    public Guid? RelatedEntityId { get; set; }
}

public sealed class AuditLog : BaseEntity
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? ActorUserId { get; set; }
    public string Module { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string? OldValuesJson { get; set; }
    public string? NewValuesJson { get; set; }
    public string Outcome { get; set; } = "Success";
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? CorrelationId { get; set; }
}

public sealed class SystemSetting : SoftDeleteEntity
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string DataType { get; set; } = "String";
    public bool IsSensitive { get; set; }
}
