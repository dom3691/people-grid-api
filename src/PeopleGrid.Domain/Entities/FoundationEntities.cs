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
    public ICollection<DocumentAccessRule> DocumentAccessRules { get; set; } = new List<DocumentAccessRule>();
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
    public ICollection<PublicHoliday> PublicHolidays { get; set; } = new List<PublicHoliday>();
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

public sealed class CompanyProfile : SoftDeleteEntity
{
    public string Name { get; set; } = string.Empty;
    public string? RegistrationNumber { get; set; }
    public string? LogoPath { get; set; }
    public string? Address { get; set; }
    public string? ContactEmail { get; set; }
    public string? Phone { get; set; }
}

public sealed class ApprovalLevel : SoftDeleteEntity
{
    public string Name { get; set; } = string.Empty;
    public int SequenceOrder { get; set; }
    public string? Description { get; set; }
    public string Status { get; set; } = "Active";
    public bool IsActive { get; set; } = true;
}

public sealed class LeaveType : SoftDeleteEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal DefaultDays { get; set; }
    public bool RequiresApproval { get; set; } = true;
    public bool PaidFlag { get; set; } = true;
    public bool RequiresAttachment { get; set; }
    public bool ExcludesWeekends { get; set; } = true;
    public bool ExcludesPublicHolidays { get; set; } = true;
    public string Status { get; set; } = "Active";
    public bool IsActive { get; set; } = true;
    public ICollection<LeaveEntitlement> Entitlements { get; set; } = new List<LeaveEntitlement>();
    public ICollection<LeaveBalance> Balances { get; set; } = new List<LeaveBalance>();
}

public sealed class PublicHoliday : SoftDeleteEntity
{
    public string Name { get; set; } = string.Empty;
    public DateOnly HolidayDate { get; set; }
    public int Year { get; set; }
    public Guid? BranchId { get; set; }
    public Branch? Branch { get; set; }
    public string? Country { get; set; }
    public string? LocationScope { get; set; }
    public string Status { get; set; } = "Active";
    public bool IsActive { get; set; } = true;
}

public sealed class SystemParameter : SoftDeleteEntity
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string DataType { get; set; } = "String";
    public string? Description { get; set; }
    public bool IsSensitive { get; set; }
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
    public DateTime? DeactivatedAt { get; set; }
    public string? DeactivationReason { get; set; }
    public EmployeePersonalInfo? PersonalInfo { get; set; }
    public EmployeeContactInfo? ContactInfo { get; set; }
    public EmployeeEmploymentInfo? EmploymentInfo { get; set; }
    public EmployeeBankInfo? BankInfo { get; set; }
    public EmployeeNextOfKin? NextOfKin { get; set; }
    public ICollection<EmployeeEmergencyContact> EmergencyContacts { get; set; } = new List<EmployeeEmergencyContact>();
    public ICollection<EmployeeJobHistory> JobHistory { get; set; } = new List<EmployeeJobHistory>();
    public ICollection<EmployeeStatusHistory> StatusHistory { get; set; } = new List<EmployeeStatusHistory>();
}

public sealed class EmployeePersonalInfo : SoftDeleteEntity
{
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public string LastName { get; set; } = string.Empty;
    public DateOnly DateOfBirth { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string? MaritalStatus { get; set; }
    public string? Nationality { get; set; }
}

public sealed class EmployeeContactInfo : SoftDeleteEntity
{
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public string WorkEmail { get; set; } = string.Empty;
    public string? PersonalEmail { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Country { get; set; }
}

public sealed class EmployeeEmploymentInfo : SoftDeleteEntity
{
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public Guid? DepartmentId { get; set; }
    public Department? Department { get; set; }
    public Guid? UnitId { get; set; }
    public Unit? Unit { get; set; }
    public Guid? BranchId { get; set; }
    public Branch? Branch { get; set; }
    public Guid? JobTitleId { get; set; }
    public JobTitle? JobTitle { get; set; }
    public Guid? GradeLevelId { get; set; }
    public GradeLevel? GradeLevel { get; set; }
    public Guid? EmploymentTypeId { get; set; }
    public EmploymentType? EmploymentType { get; set; }
    public Guid? LineManagerId { get; set; }
    public User? LineManager { get; set; }
    public DateOnly HireDate { get; set; }
    public DateOnly? ConfirmationDate { get; set; }
}

public sealed class EmployeeBankInfo : SoftDeleteEntity
{
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public string BankName { get; set; } = string.Empty;
    public string? BankCode { get; set; }
    public string AccountNumberEncrypted { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
}

public sealed class EmployeeNextOfKin : SoftDeleteEntity
{
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Relationship { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Address { get; set; }
}

public sealed class EmployeeEmergencyContact : SoftDeleteEntity
{
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Relationship { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Address { get; set; }
    public int Priority { get; set; }
}

public sealed class EmployeeJobHistory : SoftDeleteEntity
{
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public Guid? FromJobTitleId { get; set; }
    public JobTitle? FromJobTitle { get; set; }
    public Guid? ToJobTitleId { get; set; }
    public JobTitle? ToJobTitle { get; set; }
    public Guid? FromDepartmentId { get; set; }
    public Department? FromDepartment { get; set; }
    public Guid? ToDepartmentId { get; set; }
    public Department? ToDepartment { get; set; }
    public DateOnly EffectiveDate { get; set; }
    public string? Reason { get; set; }
}

public sealed class EmployeeStatusHistory : SoftDeleteEntity
{
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public string OldStatus { get; set; } = string.Empty;
    public string NewStatus { get; set; } = string.Empty;
    public DateOnly EffectiveDate { get; set; }
    public string? Reason { get; set; }
    public string? ChangedBy { get; set; }
}

public sealed class EmployeeDocument : SoftDeleteEntity
{
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public Guid? DocumentTypeId { get; set; }
    public DocumentType? DocumentType { get; set; }
    public string LegacyDocumentType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string StorageKey { get; set; } = string.Empty;
    public DateOnly? IssueDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string VerificationStatus { get; set; } = "Pending";
    public bool ConfidentialFlag { get; set; }
    public bool IsArchived { get; set; }
    public ICollection<DocumentVerificationHistory> VerificationHistory { get; set; } = new List<DocumentVerificationHistory>();
    public DocumentStorageReference? StorageReference { get; set; }
}

public sealed class DocumentType : SoftDeleteEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string AllowedExtensions { get; set; } = ".pdf,.doc,.docx,.jpg,.jpeg,.png";
    public long MaxFileSize { get; set; } = 5 * 1024 * 1024;
    public bool RequiresExpiry { get; set; }
    public bool RequiresVerification { get; set; } = true;
    public string ConfidentialityLevel { get; set; } = "Internal";
    public string Status { get; set; } = "Active";
    public bool IsActive { get; set; } = true;
    public ICollection<EmployeeDocument> EmployeeDocuments { get; set; } = new List<EmployeeDocument>();
    public ICollection<DocumentAccessRule> AccessRules { get; set; } = new List<DocumentAccessRule>();
}

public sealed class DocumentAccessRule : SoftDeleteEntity
{
    public Guid DocumentTypeId { get; set; }
    public DocumentType? DocumentType { get; set; }
    public Guid RoleId { get; set; }
    public Role? Role { get; set; }
    public string AccessLevel { get; set; } = "View";
}

public sealed class DocumentVerificationHistory : BaseEntity
{
    public Guid DocumentId { get; set; }
    public EmployeeDocument? Document { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Comments { get; set; }
    public Guid? VerifiedBy { get; set; }
    public User? VerifiedByUser { get; set; }
    public DateTime VerifiedAt { get; set; } = DateTime.UtcNow;
}

public sealed class DocumentStorageReference : SoftDeleteEntity
{
    public Guid DocumentId { get; set; }
    public EmployeeDocument? Document { get; set; }
    public string StorageProvider { get; set; } = "Local";
    public string? ContainerName { get; set; }
    public string BlobKey { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}

public sealed class HRRequest : SoftDeleteEntity
{
    public string RequestNumber { get; set; } = string.Empty;
    public Guid? RequestTypeId { get; set; }
    public HRRequestType? RequestTypeDefinition { get; set; }
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public string RequestType { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Status { get; set; } = "Draft";
    public string Priority { get; set; } = "Normal";
    public string? Description { get; set; }
    public string? RequestDataJson { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public ICollection<HRRequestAttachment> Attachments { get; set; } = new List<HRRequestAttachment>();
    public ICollection<HRRequestStatusHistory> StatusHistory { get; set; } = new List<HRRequestStatusHistory>();
    public ApprovalInstance? ApprovalInstance { get; set; }
}

public sealed class HRRequestType : SoftDeleteEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool RequiresApproval { get; set; } = true;
    public string? RequiredFieldsJson { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<HRRequest> Requests { get; set; } = new List<HRRequest>();
    public ICollection<ApprovalFlow> ApprovalFlows { get; set; } = new List<ApprovalFlow>();
}

public sealed class HRRequestAttachment : SoftDeleteEntity
{
    public Guid RequestId { get; set; }
    public HRRequest? Request { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string StorageKey { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public Guid? UploadedBy { get; set; }
    public User? UploadedByUser { get; set; }
}

public sealed class HRRequestStatusHistory : BaseEntity
{
    public Guid RequestId { get; set; }
    public HRRequest? Request { get; set; }
    public string OldStatus { get; set; } = string.Empty;
    public string NewStatus { get; set; } = string.Empty;
    public string? Comments { get; set; }
    public Guid? ChangedBy { get; set; }
    public User? ChangedByUser { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
}

public sealed class ApprovalFlow : SoftDeleteEntity
{
    public string Name { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string RequestType { get; set; } = string.Empty;
    public Guid? RequestTypeId { get; set; }
    public HRRequestType? RequestTypeDefinition { get; set; }
    public Guid? DepartmentId { get; set; }
    public Department? Department { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<ApprovalStep> Steps { get; set; } = new List<ApprovalStep>();
    public ICollection<ApprovalRule> Rules { get; set; } = new List<ApprovalRule>();
    public ICollection<ApprovalInstance> Instances { get; set; } = new List<ApprovalInstance>();
}

public sealed class ApprovalStep : SoftDeleteEntity
{
    public Guid ApprovalFlowId { get; set; }
    public ApprovalFlow? ApprovalFlow { get; set; }
    public int Sequence { get; set; }
    public string ApproverType { get; set; } = "Role";
    public Guid? ApproverRoleId { get; set; }
    public Role? ApproverRole { get; set; }
    public Guid? ApproverUserId { get; set; }
    public User? ApproverUser { get; set; }
    public int? SlaHours { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class ApprovalRequest : SoftDeleteEntity
{
    public Guid ApprovalFlowId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string Status { get; set; } = "Pending";
    public Guid? CurrentApproverId { get; set; }
}

public sealed class ApprovalRule : SoftDeleteEntity
{
    public Guid ApprovalFlowId { get; set; }
    public ApprovalFlow? ApprovalFlow { get; set; }
    public Guid? RequestTypeId { get; set; }
    public HRRequestType? RequestType { get; set; }
    public Guid? DepartmentId { get; set; }
    public Department? Department { get; set; }
    public Guid? RoleId { get; set; }
    public Role? Role { get; set; }
    public int Priority { get; set; } = 100;
    public bool IsActive { get; set; } = true;
}

public sealed class ApprovalInstance : SoftDeleteEntity
{
    public Guid RequestId { get; set; }
    public HRRequest? Request { get; set; }
    public Guid ApprovalFlowId { get; set; }
    public ApprovalFlow? ApprovalFlow { get; set; }
    public string Status { get; set; } = "Pending";
    public Guid? CurrentStepId { get; set; }
    public ApprovalStep? CurrentStep { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public ICollection<ApprovalInstanceStep> Steps { get; set; } = new List<ApprovalInstanceStep>();
    public ICollection<ApprovalAction> Actions { get; set; } = new List<ApprovalAction>();
}

public sealed class ApprovalInstanceStep : SoftDeleteEntity
{
    public Guid InstanceId { get; set; }
    public ApprovalInstance? Instance { get; set; }
    public Guid StepId { get; set; }
    public ApprovalStep? Step { get; set; }
    public Guid? AssignedUserId { get; set; }
    public User? AssignedUser { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public ICollection<ApprovalEscalation> Escalations { get; set; } = new List<ApprovalEscalation>();
}

public sealed class ApprovalAction : BaseEntity
{
    public Guid ApprovalInstanceId { get; set; }
    public ApprovalInstance? ApprovalInstance { get; set; }
    public Guid StepId { get; set; }
    public ApprovalStep? Step { get; set; }
    public Guid ActorUserId { get; set; }
    public User? ActorUser { get; set; }
    public string Decision { get; set; } = string.Empty;
    public string? Comments { get; set; }
    public DateTime DecidedAt { get; set; } = DateTime.UtcNow;
}

public sealed class ApprovalEscalation : SoftDeleteEntity
{
    public Guid ApprovalInstanceStepId { get; set; }
    public ApprovalInstanceStep? ApprovalInstanceStep { get; set; }
    public Guid EscalatedToUserId { get; set; }
    public User? EscalatedToUser { get; set; }
    public DateTime EscalatedAt { get; set; } = DateTime.UtcNow;
    public string Reason { get; set; } = string.Empty;
}

public sealed class LeaveRequest : SoftDeleteEntity
{
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public Guid? LeaveTypeId { get; set; }
    public LeaveType? LeaveTypeDefinition { get; set; }
    public string LeaveType { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal Days { get; set; }
    public string? Reason { get; set; }
    public string Status { get; set; } = "Draft";
    public Guid? CurrentApproverId { get; set; }
    public User? CurrentApprover { get; set; }
    public ICollection<LeaveRequestDate> RequestDates { get; set; } = new List<LeaveRequestDate>();
    public ICollection<LeaveApprovalAction> ApprovalActions { get; set; } = new List<LeaveApprovalAction>();
}

public sealed class LeaveEntitlement : SoftDeleteEntity
{
    public Guid LeaveTypeId { get; set; }
    public LeaveType? LeaveType { get; set; }
    public string? PolicyGroup { get; set; }
    public Guid? EmploymentTypeId { get; set; }
    public EmploymentType? EmploymentType { get; set; }
    public Guid? GradeLevelId { get; set; }
    public GradeLevel? GradeLevel { get; set; }
    public decimal EntitlementDays { get; set; }
    public string? AccrualRule { get; set; }
    public int Year { get; set; }
}

public sealed class LeaveBalance : SoftDeleteEntity
{
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public Guid LeaveTypeId { get; set; }
    public LeaveType? LeaveType { get; set; }
    public int Year { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal Accrued { get; set; }
    public decimal Used { get; set; }
    public decimal Adjusted { get; set; }
    public decimal Remaining { get; set; }
}

public sealed class LeaveRequestDate : SoftDeleteEntity
{
    public Guid LeaveRequestId { get; set; }
    public LeaveRequest? LeaveRequest { get; set; }
    public DateOnly Date { get; set; }
    public bool IsHalfDay { get; set; }
    public bool Excluded { get; set; }
    public string? ExclusionReason { get; set; }
}

public sealed class LeaveApprovalAction : BaseEntity
{
    public Guid LeaveRequestId { get; set; }
    public LeaveRequest? LeaveRequest { get; set; }
    public int Step { get; set; }
    public Guid ActorUserId { get; set; }
    public User? ActorUser { get; set; }
    public string Decision { get; set; } = string.Empty;
    public string? Comments { get; set; }
    public DateTime DecidedAt { get; set; } = DateTime.UtcNow;
}

public sealed class LeaveBalanceAdjustment : SoftDeleteEntity
{
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public Guid LeaveTypeId { get; set; }
    public LeaveType? LeaveType { get; set; }
    public int Year { get; set; }
    public decimal AdjustmentDays { get; set; }
    public string Reason { get; set; } = string.Empty;
    public Guid AdjustedBy { get; set; }
    public User? AdjustedByUser { get; set; }
    public DateTime AdjustedAt { get; set; } = DateTime.UtcNow;
}

public sealed class WorkCalendar : SoftDeleteEntity
{
    public string Name { get; set; } = string.Empty;
    public string WeekendDays { get; set; } = "Saturday,Sunday";
    public Guid? ApplicableBranchId { get; set; }
    public Branch? ApplicableBranch { get; set; }
    public Guid? ApplicableDepartmentId { get; set; }
    public Department? ApplicableDepartment { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class AttendanceRecord : SoftDeleteEntity
{
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public DateOnly AttendanceDate { get; set; }
    public DateTime? ClockInAt { get; set; }
    public DateTime? ClockOutAt { get; set; }
    public string Status { get; set; } = "Present";
    public int LateMinutes { get; set; }
    public int OvertimeMinutes { get; set; }
    public string Source { get; set; } = "Manual";
}

public sealed class AttendanceEvent : SoftDeleteEntity
{
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public DateTime EventTime { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string? DeviceId { get; set; }
    public string? AccessSystemRef { get; set; }
    public decimal? GpsLatitude { get; set; }
    public decimal? GpsLongitude { get; set; }
}

public sealed class AttendanceCorrectionRequest : SoftDeleteEntity
{
    public Guid AttendanceRecordId { get; set; }
    public AttendanceRecord? AttendanceRecord { get; set; }
    public DateTime? RequestedClockIn { get; set; }
    public DateTime? RequestedClockOut { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
}

public sealed class AttendanceApprovalAction : BaseEntity
{
    public Guid CorrectionRequestId { get; set; }
    public AttendanceCorrectionRequest? CorrectionRequest { get; set; }
    public Guid ActorUserId { get; set; }
    public User? ActorUser { get; set; }
    public string Decision { get; set; } = string.Empty;
    public string? Comments { get; set; }
    public DateTime DecidedAt { get; set; } = DateTime.UtcNow;
}

public sealed class WorkSchedule : SoftDeleteEntity
{
    public string Name { get; set; } = string.Empty;
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public int GraceMinutes { get; set; }
    public int OvertimeThresholdMinutes { get; set; }
    public Guid? ApplicableDepartmentId { get; set; }
    public Department? ApplicableDepartment { get; set; }
    public Guid? ApplicableBranchId { get; set; }
    public Branch? ApplicableBranch { get; set; }
}

public sealed class Shift : SoftDeleteEntity
{
    public string Name { get; set; } = string.Empty;
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public int BreakMinutes { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class AttendanceSource : SoftDeleteEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "Manual";
}

public sealed class OvertimeRecord : SoftDeleteEntity
{
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public DateOnly Date { get; set; }
    public int OvertimeMinutes { get; set; }
    public string ApprovalStatus { get; set; } = "Pending";
    public Guid? ApprovedBy { get; set; }
    public User? ApprovedByUser { get; set; }
}

public sealed class AbsenceRecord : SoftDeleteEntity
{
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public DateOnly Date { get; set; }
    public string AbsenceType { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public bool IsExcused { get; set; }
}

public sealed class OnboardingTemplate : SoftDeleteEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<OnboardingTemplateItem> Items { get; set; } = new List<OnboardingTemplateItem>();
}

public sealed class OnboardingTemplateItem : SoftDeleteEntity
{
    public Guid TemplateId { get; set; }
    public OnboardingTemplate? Template { get; set; }
    public string ChecklistItem { get; set; } = string.Empty;
    public string OwnerType { get; set; } = "HR";
    public int DefaultDueDays { get; set; }
    public bool IsMandatory { get; set; } = true;
    public int Sequence { get; set; }
}

public sealed class OnboardingPlan : SoftDeleteEntity
{
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public Guid TemplateId { get; set; }
    public OnboardingTemplate? Template { get; set; }
    public DateOnly StartDate { get; set; }
    public string Status { get; set; } = "Open";
    public DateTime? CompletedAt { get; set; }
    public ICollection<OnboardingTask> Tasks { get; set; } = new List<OnboardingTask>();
}

public sealed class OnboardingTask : SoftDeleteEntity
{
    public Guid PlanId { get; set; }
    public OnboardingPlan? Plan { get; set; }
    public string ChecklistItem { get; set; } = string.Empty;
    public string OwnerType { get; set; } = "HR";
    public Guid? OwnerUserId { get; set; }
    public User? OwnerUser { get; set; }
    public DateOnly DueDate { get; set; }
    public string Status { get; set; } = "Open";
    public DateTime? CompletedAt { get; set; }
    public Guid? CompletedBy { get; set; }
    public User? CompletedByUser { get; set; }
    public bool IsMandatory { get; set; }
}

public sealed class OnboardingDocument : SoftDeleteEntity
{
    public Guid PlanId { get; set; }
    public OnboardingPlan? Plan { get; set; }
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public Guid? DocumentTypeId { get; set; }
    public DocumentType? DocumentType { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string StorageKey { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}

public sealed class PolicyAcknowledgement : BaseEntity
{
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public Guid PolicyId { get; set; }
    public DateTime AcknowledgedAt { get; set; } = DateTime.UtcNow;
    public string? IpAddress { get; set; }
}

public sealed class EmployeeAssetAssignment : SoftDeleteEntity
{
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public string AssetType { get; set; } = string.Empty;
    public string AssetTag { get; set; } = string.Empty;
    public DateOnly AssignedDate { get; set; }
    public DateOnly? ReturnedDate { get; set; }
    public string? Condition { get; set; }
}

public sealed class ProbationRecord : SoftDeleteEntity
{
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public Guid? ReviewerUserId { get; set; }
    public User? ReviewerUser { get; set; }
    public string Status { get; set; } = "Open";
    public string? Outcome { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public sealed class DisciplinaryCase : SoftDeleteEntity
{
    public string CaseNumber { get; set; } = string.Empty;
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public DateOnly IncidentDate { get; set; }
    public string Category { get; set; } = string.Empty;
    public string QueryDetails { get; set; } = string.Empty;
    public string Status { get; set; } = "Open";
    public DateOnly ResponseDueDate { get; set; }
    public Guid IssuedBy { get; set; }
    public User? IssuedByUser { get; set; }
    public DateOnly IssueDate { get; set; }
    public ICollection<DisciplinaryResponse> Responses { get; set; } = new List<DisciplinaryResponse>();
}

public sealed class DisciplinaryResponse : BaseEntity
{
    public Guid CaseId { get; set; }
    public DisciplinaryCase? Case { get; set; }
    public string ResponseText { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public Guid SubmittedBy { get; set; }
    public User? SubmittedByUser { get; set; }
}

public sealed class DisciplinaryReview : BaseEntity
{
    public Guid CaseId { get; set; }
    public DisciplinaryCase? Case { get; set; }
    public string ReviewComments { get; set; } = string.Empty;
    public Guid ReviewedBy { get; set; }
    public User? ReviewedByUser { get; set; }
    public DateTime ReviewedAt { get; set; } = DateTime.UtcNow;
    public string Outcome { get; set; } = string.Empty;
}

public sealed class DisciplinaryAction : SoftDeleteEntity
{
    public Guid CaseId { get; set; }
    public DisciplinaryCase? Case { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public DateOnly EffectiveDate { get; set; }
    public string Details { get; set; } = string.Empty;
    public Guid CreatedByUserId { get; set; }
    public User? CreatedByUser { get; set; }
}

public sealed class WarningLetter : SoftDeleteEntity
{
    public Guid CaseId { get; set; }
    public DisciplinaryCase? Case { get; set; }
    public string WarningLevel { get; set; } = string.Empty;
    public string LetterContent { get; set; } = string.Empty;
    public Guid IssuedBy { get; set; }
    public User? IssuedByUser { get; set; }
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
}

public sealed class SuspensionRecord : SoftDeleteEntity
{
    public Guid CaseId { get; set; }
    public DisciplinaryCase? Case { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string Reason { get; set; } = string.Empty;
    public Guid ApprovedBy { get; set; }
    public User? ApprovedByUser { get; set; }
}

public sealed class DisciplinaryEscalation : SoftDeleteEntity
{
    public Guid CaseId { get; set; }
    public DisciplinaryCase? Case { get; set; }
    public Guid EscalatedTo { get; set; }
    public User? EscalatedToUser { get; set; }
    public Guid EscalatedBy { get; set; }
    public User? EscalatedByUser { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime EscalatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class DisciplinaryAttachment : SoftDeleteEntity
{
    public Guid CaseId { get; set; }
    public DisciplinaryCase? Case { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string StorageKey { get; set; } = string.Empty;
    public Guid? UploadedBy { get; set; }
    public User? UploadedByUser { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}

public sealed class ExitCase : SoftDeleteEntity
{
    public string CaseNumber { get; set; } = string.Empty;
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public DateOnly ResignationDate { get; set; }
    public DateOnly LastWorkingDay { get; set; }
    public string? Reason { get; set; }
    public int NoticePeriod { get; set; }
    public string Status { get; set; } = "Draft";
}

public sealed class ResignationRequest : SoftDeleteEntity
{
    public Guid ExitCaseId { get; set; }
    public ExitCase? ExitCase { get; set; }
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public DateOnly ProposedLastWorkingDay { get; set; }
    public string? ReasonForLeaving { get; set; }
}

public sealed class ExitApprovalAction : BaseEntity
{
    public Guid ExitCaseId { get; set; }
    public ExitCase? ExitCase { get; set; }
    public int Step { get; set; }
    public Guid ActorUserId { get; set; }
    public User? ActorUser { get; set; }
    public string Decision { get; set; } = string.Empty;
    public string? Comments { get; set; }
    public DateTime DecidedAt { get; set; } = DateTime.UtcNow;
}

public sealed class ExitClearanceItem : SoftDeleteEntity
{
    public Guid ExitCaseId { get; set; }
    public ExitCase? ExitCase { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public Guid? OwnerUserId { get; set; }
    public User? OwnerUser { get; set; }
    public bool IsMandatory { get; set; } = true;
    public string Status { get; set; } = "Pending";
    public DateTime? CompletedAt { get; set; }
}

public sealed class ExitHandoverRecord : SoftDeleteEntity
{
    public Guid ExitCaseId { get; set; }
    public ExitCase? ExitCase { get; set; }
    public Guid? HandoverToUserId { get; set; }
    public User? HandoverToUser { get; set; }
    public string? Notes { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public sealed class ExitInterviewResponse : BaseEntity
{
    public Guid ExitCaseId { get; set; }
    public ExitCase? ExitCase { get; set; }
    public string Question { get; set; } = string.Empty;
    public string? Response { get; set; }
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
}

public sealed class ExitAssetReturn : SoftDeleteEntity
{
    public Guid ExitCaseId { get; set; }
    public ExitCase? ExitCase { get; set; }
    public string AssetType { get; set; } = string.Empty;
    public string AssetTag { get; set; } = string.Empty;
    public DateOnly? ReturnedDate { get; set; }
    public string? Condition { get; set; }
    public Guid? ReceivedBy { get; set; }
    public User? ReceivedByUser { get; set; }
}

public sealed class FinalSettlementStatus : SoftDeleteEntity
{
    public Guid ExitCaseId { get; set; }
    public ExitCase? ExitCase { get; set; }
    public string Status { get; set; } = "Pending";
    public Guid? StatusUpdatedBy { get; set; }
    public User? StatusUpdatedByUser { get; set; }
    public DateTime StatusUpdatedAt { get; set; } = DateTime.UtcNow;
    public string? Comments { get; set; }
}

public sealed class SalaryStructure : SoftDeleteEntity
{
    public Guid? EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public Guid? GradeLevelId { get; set; }
    public GradeLevel? GradeLevel { get; set; }
    public decimal BasicSalary { get; set; }
    public DateOnly EffectiveDate { get; set; }
    public string Status { get; set; } = "Active";
}

public sealed class PayrollItem : SoftDeleteEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "Earning";
    public bool Taxable { get; set; }
    public bool Pensionable { get; set; }
    public bool Recurring { get; set; }
    public string CalculationMethod { get; set; } = "Fixed";
    public decimal? Amount { get; set; }
    public decimal? Percentage { get; set; }
    public string Status { get; set; } = "Active";
}

public sealed class EmployeePayrollItem : SoftDeleteEntity
{
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public Guid PayrollItemId { get; set; }
    public PayrollItem? PayrollItem { get; set; }
    public decimal? Amount { get; set; }
    public decimal? Percentage { get; set; }
    public DateOnly EffectiveDate { get; set; }
    public string Status { get; set; } = "Active";
}

public sealed class PayrollRun : SoftDeleteEntity
{
    public string Period { get; set; } = string.Empty;
    public string Status { get; set; } = "Draft";
    public Guid? PreparedBy { get; set; }
    public User? PreparedByUser { get; set; }
    public Guid? ReviewedBy { get; set; }
    public User? ReviewedByUser { get; set; }
    public Guid? ApprovedBy { get; set; }
    public User? ApprovedByUser { get; set; }
    public DateTime? FinalizedAt { get; set; }
    public ICollection<PayrollRunEmployee> Employees { get; set; } = new List<PayrollRunEmployee>();
}

public sealed class PayrollRunEmployee : SoftDeleteEntity
{
    public Guid PayrollRunId { get; set; }
    public PayrollRun? PayrollRun { get; set; }
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public decimal GrossSalary { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal NetSalary { get; set; }
    public ICollection<PayrollEarning> Earnings { get; set; } = new List<PayrollEarning>();
    public ICollection<PayrollDeduction> Deductions { get; set; } = new List<PayrollDeduction>();
}

public sealed class PayrollEarning : BaseEntity
{
    public Guid PayrollRunEmployeeId { get; set; }
    public PayrollRunEmployee? PayrollRunEmployee { get; set; }
    public Guid ItemId { get; set; }
    public PayrollItem? Item { get; set; }
    public decimal Amount { get; set; }
    public string? CalculationBasis { get; set; }
}

public sealed class PayrollDeduction : BaseEntity
{
    public Guid PayrollRunEmployeeId { get; set; }
    public PayrollRunEmployee? PayrollRunEmployee { get; set; }
    public Guid ItemId { get; set; }
    public PayrollItem? Item { get; set; }
    public decimal Amount { get; set; }
    public string? CalculationBasis { get; set; }
}

public sealed class TaxRule : SoftDeleteEntity
{
    public string RuleCode { get; set; } = string.Empty;
    public decimal Rate { get; set; }
    public decimal Threshold { get; set; }
    public DateOnly EffectiveDate { get; set; }
    public string Status { get; set; } = "Active";
}

public sealed class PensionRule : SoftDeleteEntity
{
    public string RuleCode { get; set; } = string.Empty;
    public decimal EmployeeRate { get; set; }
    public decimal EmployerRate { get; set; }
    public decimal Threshold { get; set; }
    public DateOnly EffectiveDate { get; set; }
    public string Status { get; set; } = "Active";
}

public sealed class EmployeeLoan : SoftDeleteEntity
{
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public decimal LoanAmount { get; set; }
    public decimal InterestFee { get; set; }
    public DateOnly RepaymentStartDate { get; set; }
    public decimal MonthlyRepayment { get; set; }
    public decimal OutstandingBalance { get; set; }
    public string LoanStatus { get; set; } = "Active";
}

public sealed class LoanRepayment : BaseEntity
{
    public Guid LoanId { get; set; }
    public EmployeeLoan? Loan { get; set; }
    public Guid PayrollRunId { get; set; }
    public PayrollRun? PayrollRun { get; set; }
    public decimal Amount { get; set; }
    public DateOnly RepaymentDate { get; set; }
}

public sealed class Payslip : SoftDeleteEntity
{
    public Guid PayrollRunEmployeeId { get; set; }
    public PayrollRunEmployee? PayrollRunEmployee { get; set; }
    public string PayslipNumber { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public string? FileReference { get; set; }
}

public sealed class PayrollApprovalAction : BaseEntity
{
    public Guid PayrollRunId { get; set; }
    public PayrollRun? PayrollRun { get; set; }
    public int Step { get; set; }
    public Guid ActorUserId { get; set; }
    public User? ActorUser { get; set; }
    public string Decision { get; set; } = string.Empty;
    public string? Comments { get; set; }
    public DateTime DecidedAt { get; set; } = DateTime.UtcNow;
}

public sealed class PayrollAuditHistory : BaseEntity
{
    public Guid PayrollRunId { get; set; }
    public PayrollRun? PayrollRun { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Actor { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? SnapshotData { get; set; }
}

public sealed class PerformanceCycle : SoftDeleteEntity
{
    public string Name { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string Status { get; set; } = "Draft";
    public bool IsActive { get; set; }
}

public sealed class PerformanceTemplate : SoftDeleteEntity
{
    public Guid CycleId { get; set; }
    public PerformanceCycle? Cycle { get; set; }
    public string Name { get; set; } = string.Empty;
    public string RatingScale { get; set; } = "Excellent,Very Good,Good,Average,Poor";
    public decimal WeightRule { get; set; } = 100;
    public bool IsActive { get; set; } = true;
}

public sealed class EmployeeGoal : SoftDeleteEntity
{
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public Guid CycleId { get; set; }
    public PerformanceCycle? Cycle { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Target { get; set; } = string.Empty;
    public decimal Weight { get; set; }
    public string? Achievement { get; set; }
    public string Status { get; set; } = "Draft";
}

public sealed class EmployeeKpi : SoftDeleteEntity
{
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public Guid CycleId { get; set; }
    public PerformanceCycle? Cycle { get; set; }
    public string Metric { get; set; } = string.Empty;
    public string Target { get; set; } = string.Empty;
    public string? Actual { get; set; }
    public decimal Weight { get; set; }
    public string Status { get; set; } = "Draft";
}

public sealed class SelfAssessment : BaseEntity
{
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public Guid CycleId { get; set; }
    public PerformanceCycle? Cycle { get; set; }
    public Guid? GoalId { get; set; }
    public EmployeeGoal? Goal { get; set; }
    public Guid? KpiId { get; set; }
    public EmployeeKpi? Kpi { get; set; }
    public string SelfRating { get; set; } = string.Empty;
    public string? Comments { get; set; }
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
}

public sealed class ManagerAssessment : BaseEntity
{
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public Guid CycleId { get; set; }
    public PerformanceCycle? Cycle { get; set; }
    public Guid? GoalId { get; set; }
    public EmployeeGoal? Goal { get; set; }
    public Guid? KpiId { get; set; }
    public EmployeeKpi? Kpi { get; set; }
    public string ManagerRating { get; set; } = string.Empty;
    public string? Comments { get; set; }
    public Guid SubmittedBy { get; set; }
    public User? SubmittedByUser { get; set; }
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
}

public sealed class HrPerformanceReview : BaseEntity
{
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public Guid CycleId { get; set; }
    public PerformanceCycle? Cycle { get; set; }
    public string ReviewComments { get; set; } = string.Empty;
    public Guid ReviewedBy { get; set; }
    public User? ReviewedByUser { get; set; }
    public DateTime ReviewedAt { get; set; } = DateTime.UtcNow;
}

public sealed class PerformanceRating : SoftDeleteEntity
{
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public Guid CycleId { get; set; }
    public PerformanceCycle? Cycle { get; set; }
    public string FinalRating { get; set; } = "Good";
    public Guid? ReleasedBy { get; set; }
    public User? ReleasedByUser { get; set; }
    public DateTime? ReleasedAt { get; set; }
    public bool IsReleased { get; set; }
}

public sealed class PromotionRecommendation : SoftDeleteEntity
{
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public Guid CycleId { get; set; }
    public PerformanceCycle? Cycle { get; set; }
    public string Recommendation { get; set; } = string.Empty;
    public string Justification { get; set; } = string.Empty;
    public Guid RecommendedBy { get; set; }
    public User? RecommendedByUser { get; set; }
    public string Status { get; set; } = "Pending";
}

public sealed class PerformanceImprovementPlan : SoftDeleteEntity
{
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public Guid CycleId { get; set; }
    public PerformanceCycle? Cycle { get; set; }
    public string Objective { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string Status { get; set; } = "Open";
    public string? Outcome { get; set; }
}

public sealed class PerformanceHistory : BaseEntity
{
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public Guid CycleId { get; set; }
    public PerformanceCycle? Cycle { get; set; }
    public string FinalRating { get; set; } = string.Empty;
    public bool PromotionRecommended { get; set; }
    public bool PipTriggered { get; set; }
    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
}

public sealed class JobOpening : SoftDeleteEntity
{
    public string Title { get; set; } = string.Empty;
    public Guid DepartmentId { get; set; }
    public Department? Department { get; set; }
    public Guid? BranchId { get; set; }
    public Branch? Branch { get; set; }
    public Guid? HiringManagerId { get; set; }
    public User? HiringManager { get; set; }
    public int Vacancies { get; set; }
    public string EmploymentType { get; set; } = string.Empty;
    public Guid? GradeLevelId { get; set; }
    public GradeLevel? GradeLevel { get; set; }
    public string JobDescription { get; set; } = string.Empty;
    public string Requirements { get; set; } = string.Empty;
    public DateOnly? PublicationDate { get; set; }
    public DateOnly ClosingDate { get; set; }
    public string Status { get; set; } = "Draft";
}

public sealed class VacancyPublication : SoftDeleteEntity
{
    public Guid JobOpeningId { get; set; }
    public JobOpening? JobOpening { get; set; }
    public string Channel { get; set; } = "Internal";
    public DateTime PublishedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
}

public sealed class Candidate : SoftDeleteEntity
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Source { get; set; }
}

public sealed class CandidateApplication : SoftDeleteEntity
{
    public Guid CandidateId { get; set; }
    public Candidate? Candidate { get; set; }
    public Guid JobOpeningId { get; set; }
    public JobOpening? JobOpening { get; set; }
    public string Status { get; set; } = "Applied";
    public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
}

public sealed class CandidateDocument : SoftDeleteEntity
{
    public Guid CandidateId { get; set; }
    public Candidate? Candidate { get; set; }
    public string DocumentType { get; set; } = "CV";
    public string FileName { get; set; } = string.Empty;
    public string StorageKey { get; set; } = string.Empty;
}

public sealed class InterviewSchedule : SoftDeleteEntity
{
    public Guid ApplicationId { get; set; }
    public CandidateApplication? Application { get; set; }
    public string InterviewStage { get; set; } = string.Empty;
    public DateTime DateTime { get; set; }
    public string? VenueOrMode { get; set; }
    public string Status { get; set; } = "Scheduled";
}

public sealed class InterviewPanelMember : SoftDeleteEntity
{
    public Guid InterviewScheduleId { get; set; }
    public InterviewSchedule? InterviewSchedule { get; set; }
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public string Role { get; set; } = "Interviewer";
}

public sealed class InterviewFeedback : BaseEntity
{
    public Guid InterviewScheduleId { get; set; }
    public InterviewSchedule? InterviewSchedule { get; set; }
    public Guid PanelMemberId { get; set; }
    public InterviewPanelMember? PanelMember { get; set; }
    public decimal Score { get; set; }
    public string? Comments { get; set; }
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
}

public sealed class OfferLetter : SoftDeleteEntity
{
    public Guid ApplicationId { get; set; }
    public CandidateApplication? Application { get; set; }
    public string OfferDetails { get; set; } = string.Empty;
    public decimal Salary { get; set; }
    public string Status { get; set; } = "Draft";
    public DateTime? SentAt { get; set; }
    public DateTime? AcceptedAt { get; set; }
}

public sealed class RecruitmentStatusHistory : BaseEntity
{
    public Guid ApplicationId { get; set; }
    public CandidateApplication? Application { get; set; }
    public string? OldStatus { get; set; }
    public string NewStatus { get; set; } = string.Empty;
    public Guid ChangedBy { get; set; }
    public User? ChangedByUser { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    public string? Comments { get; set; }
}

public sealed class TrainingProgram : SoftDeleteEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ProviderId { get; set; }
    public string? Venue { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public decimal Cost { get; set; }
    public int Capacity { get; set; }
    public Guid? TargetDepartmentId { get; set; }
    public Department? TargetDepartment { get; set; }
    public Guid? TargetGradeLevelId { get; set; }
    public GradeLevel? TargetGradeLevel { get; set; }
    public Guid? TargetSkillId { get; set; }
    public Skill? TargetSkill { get; set; }
    public string Status { get; set; } = "Draft";
}

public sealed class TrainingSchedule : SoftDeleteEntity
{
    public Guid ProgramId { get; set; }
    public TrainingProgram? Program { get; set; }
    public DateOnly SessionDate { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public string? Venue { get; set; }
}

public sealed class TrainingNomination : SoftDeleteEntity
{
    public Guid ProgramId { get; set; }
    public TrainingProgram? Program { get; set; }
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public Guid NominatedBy { get; set; }
    public User? NominatedByUser { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime NominatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class TrainingApprovalAction : BaseEntity
{
    public Guid NominationId { get; set; }
    public TrainingNomination? Nomination { get; set; }
    public Guid ActorUserId { get; set; }
    public User? ActorUser { get; set; }
    public string Decision { get; set; } = string.Empty;
    public string? Comments { get; set; }
    public DateTime DecidedAt { get; set; } = DateTime.UtcNow;
}

public sealed class TrainingAttendance : BaseEntity
{
    public Guid ProgramId { get; set; }
    public TrainingProgram? Program { get; set; }
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public DateOnly SessionDate { get; set; }
    public bool Attended { get; set; }
    public Guid MarkedBy { get; set; }
    public User? MarkedByUser { get; set; }
}

public sealed class TrainingFeedback : BaseEntity
{
    public Guid ProgramId { get; set; }
    public TrainingProgram? Program { get; set; }
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public int Score { get; set; }
    public string? Comments { get; set; }
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
}

public sealed class TrainingCertificate : SoftDeleteEntity
{
    public Guid ProgramId { get; set; }
    public TrainingProgram? Program { get; set; }
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string StorageKey { get; set; } = string.Empty;
    public DateOnly IssuedDate { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}

public sealed class Skill : SoftDeleteEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? Description { get; set; }
}

public sealed class EmployeeSkill : SoftDeleteEntity
{
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public Guid SkillId { get; set; }
    public Skill? Skill { get; set; }
    public string ProficiencyLevel { get; set; } = "Beginner";
    public DateOnly? AcquiredDate { get; set; }
    public string Source { get; set; } = "Training";
}

public sealed class TrainingCost : SoftDeleteEntity
{
    public Guid ProgramId { get; set; }
    public TrainingProgram? Program { get; set; }
    public string CostCategory { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? Provider { get; set; }
    public string? InvoiceRef { get; set; }
}

public sealed class TrainingHistory : BaseEntity
{
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public Guid ProgramId { get; set; }
    public TrainingProgram? Program { get; set; }
    public string Status { get; set; } = "Completed";
    public DateTime? CompletedAt { get; set; }
}

public sealed class Notification : SoftDeleteEntity
{
    public Guid RecipientUserId { get; set; }
    public User? RecipientUser { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = "Info";
    public bool IsRead { get; set; }
    public string? RelatedEntityType { get; set; }
    public Guid? RelatedEntityId { get; set; }
    public ICollection<NotificationDeliveryLog> DeliveryLogs { get; set; } = new List<NotificationDeliveryLog>();
}

public sealed class NotificationTemplate : SoftDeleteEntity
{
    public string TemplateKey { get; set; } = string.Empty;
    public string Channel { get; set; } = "InApp";
    public string? Subject { get; set; }
    public string Body { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public sealed class NotificationDeliveryLog : BaseEntity
{
    public Guid NotificationId { get; set; }
    public Notification? Notification { get; set; }
    public string Channel { get; set; } = string.Empty;
    public string RecipientAddress { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? ProviderMessageId { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime AttemptedAt { get; set; } = DateTime.UtcNow;
}

public sealed class NotificationPreference : SoftDeleteEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public string NotificationType { get; set; } = string.Empty;
    public string Channel { get; set; } = "InApp";
    public bool Enabled { get; set; } = true;
}

public sealed class AuditLog : BaseEntity
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? ActorUserId { get; set; }
    public string Module { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string Outcome { get; set; } = "Success";
    public string Severity { get; set; } = "Information";
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? CorrelationId { get; set; }
    public DateTime? RetentionUntil { get; set; }
    public DateTime? ArchivedAt { get; set; }
    public int PartitionKey { get; set; }
    public AuditLogDetail? Detail { get; set; }
}

public sealed class AuditLogDetail
{
    public Guid AuditLogId { get; set; }
    public AuditLog? AuditLog { get; set; }
    public string? OldValuesJson { get; set; }
    public string? NewValuesJson { get; set; }
    public string? ChangedFieldsJson { get; set; }
}

public sealed class SystemSetting : SoftDeleteEntity
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string DataType { get; set; } = "String";
    public bool IsSensitive { get; set; }
}
