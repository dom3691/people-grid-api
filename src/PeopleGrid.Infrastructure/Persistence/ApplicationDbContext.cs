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
    public DbSet<UserManagerAssignment> UserManagerAssignments { get; set; }
    public DbSet<Unit> Units { get; set; }
    public DbSet<Branch> Branches { get; set; }
    public DbSet<JobTitle> JobTitles { get; set; }
    public DbSet<GradeLevel> GradeLevels { get; set; }
    public DbSet<EmploymentType> EmploymentTypes { get; set; }
    public DbSet<CompanyProfile> CompanyProfiles { get; set; }
    public DbSet<ApprovalLevel> ApprovalLevels { get; set; }
    public DbSet<LeaveType> LeaveTypes { get; set; }
    public DbSet<PublicHoliday> PublicHolidays { get; set; }
    public DbSet<SystemParameter> SystemParameters { get; set; }
    public DbSet<Employee> Employees { get; set; }
    public DbSet<EmployeePersonalInfo> EmployeePersonalInfos { get; set; }
    public DbSet<EmployeeContactInfo> EmployeeContactInfos { get; set; }
    public DbSet<EmployeeEmploymentInfo> EmployeeEmploymentInfos { get; set; }
    public DbSet<EmployeeBankInfo> EmployeeBankInfos { get; set; }
    public DbSet<EmployeeNextOfKin> EmployeeNextOfKin { get; set; }
    public DbSet<EmployeeEmergencyContact> EmployeeEmergencyContacts { get; set; }
    public DbSet<EmployeeJobHistory> EmployeeJobHistories { get; set; }
    public DbSet<EmployeeStatusHistory> EmployeeStatusHistories { get; set; }
    public DbSet<EmployeeDocument> EmployeeDocuments { get; set; }
    public DbSet<DocumentType> DocumentTypes { get; set; }
    public DbSet<DocumentAccessRule> DocumentAccessRules { get; set; }
    public DbSet<DocumentVerificationHistory> DocumentVerificationHistories { get; set; }
    public DbSet<DocumentStorageReference> DocumentStorageReferences { get; set; }
    public DbSet<HRRequest> HRRequests { get; set; }
    public DbSet<ApprovalFlow> ApprovalFlows { get; set; }
    public DbSet<ApprovalStep> ApprovalSteps { get; set; }
    public DbSet<ApprovalRequest> ApprovalRequests { get; set; }
    public DbSet<LeaveRequest> LeaveRequests { get; set; }
    public DbSet<AttendanceRecord> AttendanceRecords { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<AuditLogDetail> AuditLogDetails { get; set; }
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
        modelBuilder.Entity<LoginAttempt>().HasIndex(x => new { x.OccurredAt, x.Success });
        modelBuilder.Entity<LoginAttempt>().HasIndex(x => new { x.EmailOrUserName, x.OccurredAt });
        modelBuilder.Entity<LoginAttempt>().Property(x => x.EmailOrUserName).HasMaxLength(256);
        modelBuilder.Entity<LoginAttempt>().Property(x => x.FailureReason).HasMaxLength(200);
        modelBuilder.Entity<LoginAttempt>().Property(x => x.IpAddress).HasMaxLength(100);
        modelBuilder.Entity<LoginAttempt>().Property(x => x.UserAgent).HasMaxLength(500);
        modelBuilder.Entity<AuditLog>().HasIndex(x => x.Timestamp);
        modelBuilder.Entity<AuditLog>().HasIndex(x => x.ActorUserId);
        modelBuilder.Entity<AuditLog>().HasIndex(x => x.Module);
        modelBuilder.Entity<AuditLog>().HasIndex(x => x.Action);
        modelBuilder.Entity<AuditLog>().HasIndex(x => x.EntityType);
        modelBuilder.Entity<AuditLog>().HasIndex(x => x.Outcome);
        modelBuilder.Entity<AuditLog>().HasIndex(x => x.Severity);
        modelBuilder.Entity<AuditLog>().HasIndex(x => x.CorrelationId);
        modelBuilder.Entity<AuditLog>().HasIndex(x => x.PartitionKey);
        modelBuilder.Entity<AuditLog>().HasIndex(x => x.RetentionUntil);
        modelBuilder.Entity<AuditLog>().HasIndex(x => x.ArchivedAt);
        modelBuilder.Entity<AuditLog>().HasIndex(x => new { x.Timestamp, x.Module, x.Action });
        modelBuilder.Entity<AuditLog>().HasIndex(x => new { x.ActorUserId, x.Timestamp });
        modelBuilder.Entity<AuditLog>().HasIndex(x => new { x.EntityType, x.EntityId, x.Timestamp });
        modelBuilder.Entity<AuditLog>().HasIndex(x => new { x.Module, x.EntityType, x.Timestamp });
        modelBuilder.Entity<AuditLog>().Property(x => x.ActorUserId).HasMaxLength(100);
        modelBuilder.Entity<AuditLog>().Property(x => x.Module).HasMaxLength(100);
        modelBuilder.Entity<AuditLog>().Property(x => x.Action).HasMaxLength(100);
        modelBuilder.Entity<AuditLog>().Property(x => x.EntityType).HasMaxLength(150);
        modelBuilder.Entity<AuditLog>().Property(x => x.EntityId).HasMaxLength(100);
        modelBuilder.Entity<AuditLog>().Property(x => x.Outcome).HasMaxLength(50);
        modelBuilder.Entity<AuditLog>().Property(x => x.Severity).HasMaxLength(50);
        modelBuilder.Entity<AuditLog>().Property(x => x.IpAddress).HasMaxLength(100);
        modelBuilder.Entity<AuditLog>().Property(x => x.UserAgent).HasMaxLength(500);
        modelBuilder.Entity<AuditLog>().Property(x => x.CorrelationId).HasMaxLength(100);
        modelBuilder.Entity<AuditLogDetail>().HasKey(x => x.AuditLogId);
        modelBuilder.Entity<AuditLogDetail>().Property(x => x.OldValuesJson).HasColumnType("nvarchar(max)");
        modelBuilder.Entity<AuditLogDetail>().Property(x => x.NewValuesJson).HasColumnType("nvarchar(max)");
        modelBuilder.Entity<AuditLogDetail>().Property(x => x.ChangedFieldsJson).HasColumnType("nvarchar(max)");
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
        modelBuilder.Entity<Department>().HasIndex(x => x.HeadUserId);
        modelBuilder.Entity<Department>().HasIndex(x => x.Status);
        modelBuilder.Entity<Department>().Property(x => x.Code).HasMaxLength(50);
        modelBuilder.Entity<Department>().Property(x => x.Name).HasMaxLength(150);
        modelBuilder.Entity<Department>().Property(x => x.Status).HasMaxLength(50);
        modelBuilder.Entity<UserManagerAssignment>().HasIndex(x => x.UserId);
        modelBuilder.Entity<UserManagerAssignment>().HasIndex(x => x.ManagerUserId);
        modelBuilder.Entity<UserManagerAssignment>().HasIndex(x => x.IsCurrent);
        modelBuilder.Entity<UserManagerAssignment>().HasIndex(x => new { x.UserId, x.IsCurrent });
        modelBuilder.Entity<UserManagerAssignment>()
            .HasIndex(x => new { x.UserId, x.ManagerUserId, x.EffectiveFrom })
            .IsUnique();
        modelBuilder.Entity<UserManagerAssignment>()
            .HasIndex(x => x.UserId)
            .IsUnique()
            .HasFilter("[IsCurrent] = 1 AND [IsDeleted] = 0");
        modelBuilder.Entity<Unit>().HasIndex(x => x.Code).IsUnique();
        modelBuilder.Entity<Unit>().HasIndex(x => x.DepartmentId);
        modelBuilder.Entity<Unit>().HasIndex(x => x.Status);
        modelBuilder.Entity<Unit>().Property(x => x.Code).HasMaxLength(50);
        modelBuilder.Entity<Unit>().Property(x => x.Name).HasMaxLength(150);
        modelBuilder.Entity<Unit>().Property(x => x.Status).HasMaxLength(50);
        modelBuilder.Entity<Branch>().HasIndex(x => x.Code).IsUnique();
        modelBuilder.Entity<Branch>().HasIndex(x => x.Status);
        modelBuilder.Entity<Branch>().Property(x => x.Code).HasMaxLength(50);
        modelBuilder.Entity<Branch>().Property(x => x.Name).HasMaxLength(150);
        modelBuilder.Entity<Branch>().Property(x => x.Address).HasMaxLength(500);
        modelBuilder.Entity<Branch>().Property(x => x.Country).HasMaxLength(100);
        modelBuilder.Entity<Branch>().Property(x => x.StateRegion).HasMaxLength(100);
        modelBuilder.Entity<Branch>().Property(x => x.Status).HasMaxLength(50);
        modelBuilder.Entity<JobTitle>().HasIndex(x => x.Code).IsUnique();
        modelBuilder.Entity<JobTitle>().HasIndex(x => x.GradeLevelId);
        modelBuilder.Entity<JobTitle>().HasIndex(x => x.Status);
        modelBuilder.Entity<JobTitle>().Property(x => x.Code).HasMaxLength(50);
        modelBuilder.Entity<JobTitle>().Property(x => x.Name).HasMaxLength(150);
        modelBuilder.Entity<JobTitle>().Property(x => x.Status).HasMaxLength(50);
        modelBuilder.Entity<GradeLevel>().HasIndex(x => x.Code).IsUnique();
        modelBuilder.Entity<GradeLevel>().HasIndex(x => x.Status);
        modelBuilder.Entity<GradeLevel>().Property(x => x.Code).HasMaxLength(50);
        modelBuilder.Entity<GradeLevel>().Property(x => x.Name).HasMaxLength(150);
        modelBuilder.Entity<GradeLevel>().Property(x => x.Status).HasMaxLength(50);
        modelBuilder.Entity<EmploymentType>().HasIndex(x => x.Code).IsUnique();
        modelBuilder.Entity<EmploymentType>().HasIndex(x => x.Status);
        modelBuilder.Entity<EmploymentType>().Property(x => x.Code).HasMaxLength(50);
        modelBuilder.Entity<EmploymentType>().Property(x => x.Name).HasMaxLength(150);
        modelBuilder.Entity<EmploymentType>().Property(x => x.Status).HasMaxLength(50);
        modelBuilder.Entity<CompanyProfile>().Property(x => x.Name).HasMaxLength(200);
        modelBuilder.Entity<CompanyProfile>().Property(x => x.RegistrationNumber).HasMaxLength(100);
        modelBuilder.Entity<CompanyProfile>().Property(x => x.LogoPath).HasMaxLength(500);
        modelBuilder.Entity<CompanyProfile>().Property(x => x.Address).HasMaxLength(500);
        modelBuilder.Entity<CompanyProfile>().Property(x => x.ContactEmail).HasMaxLength(256);
        modelBuilder.Entity<CompanyProfile>().Property(x => x.Phone).HasMaxLength(50);
        modelBuilder.Entity<ApprovalLevel>().HasIndex(x => x.SequenceOrder).IsUnique();
        modelBuilder.Entity<ApprovalLevel>().HasIndex(x => x.Status);
        modelBuilder.Entity<ApprovalLevel>().Property(x => x.Name).HasMaxLength(150);
        modelBuilder.Entity<ApprovalLevel>().Property(x => x.Description).HasMaxLength(500);
        modelBuilder.Entity<ApprovalLevel>().Property(x => x.Status).HasMaxLength(50);
        modelBuilder.Entity<LeaveType>().HasIndex(x => x.Code).IsUnique();
        modelBuilder.Entity<LeaveType>().HasIndex(x => x.Status);
        modelBuilder.Entity<LeaveType>().Property(x => x.Code).HasMaxLength(50);
        modelBuilder.Entity<LeaveType>().Property(x => x.Name).HasMaxLength(150);
        modelBuilder.Entity<LeaveType>().Property(x => x.Status).HasMaxLength(50);
        modelBuilder.Entity<PublicHoliday>()
            .HasIndex(x => new { x.Name, x.HolidayDate, x.BranchId, x.LocationScope })
            .IsUnique()
            .HasFilter(null);
        modelBuilder.Entity<PublicHoliday>().HasIndex(x => x.BranchId);
        modelBuilder.Entity<PublicHoliday>().HasIndex(x => x.HolidayDate);
        modelBuilder.Entity<PublicHoliday>().HasIndex(x => x.Status);
        modelBuilder.Entity<PublicHoliday>().Property(x => x.Name).HasMaxLength(150);
        modelBuilder.Entity<PublicHoliday>().Property(x => x.LocationScope).HasMaxLength(100);
        modelBuilder.Entity<PublicHoliday>().Property(x => x.Status).HasMaxLength(50);
        modelBuilder.Entity<SystemParameter>().HasIndex(x => x.Key).IsUnique();
        modelBuilder.Entity<SystemParameter>().Property(x => x.Key).HasMaxLength(150);
        modelBuilder.Entity<SystemParameter>().Property(x => x.Value).HasMaxLength(2000);
        modelBuilder.Entity<SystemParameter>().Property(x => x.DataType).HasMaxLength(50);
        modelBuilder.Entity<SystemParameter>().Property(x => x.Description).HasMaxLength(500);
        modelBuilder.Entity<Employee>().HasIndex(x => x.EmployeeNumber).IsUnique();
        modelBuilder.Entity<Employee>().HasIndex(x => x.WorkEmail).IsUnique();
        modelBuilder.Entity<Employee>()
            .HasIndex(x => x.UserId)
            .IsUnique()
            .HasFilter("[UserId] IS NOT NULL AND [IsDeleted] = 0");
        modelBuilder.Entity<Employee>().HasIndex(x => x.Status);
        modelBuilder.Entity<Employee>().HasIndex(x => x.DepartmentId);
        modelBuilder.Entity<Employee>().HasIndex(x => x.UnitId);
        modelBuilder.Entity<Employee>().HasIndex(x => x.BranchId);
        modelBuilder.Entity<Employee>().HasIndex(x => x.JobTitleId);
        modelBuilder.Entity<Employee>().HasIndex(x => x.GradeLevelId);
        modelBuilder.Entity<Employee>().HasIndex(x => x.LineManagerId);
        modelBuilder.Entity<Employee>().HasIndex(x => x.DeactivatedAt);
        modelBuilder.Entity<Employee>().Property(x => x.EmployeeNumber).HasMaxLength(50);
        modelBuilder.Entity<Employee>().Property(x => x.FirstName).HasMaxLength(100);
        modelBuilder.Entity<Employee>().Property(x => x.LastName).HasMaxLength(100);
        modelBuilder.Entity<Employee>().Property(x => x.WorkEmail).HasMaxLength(256);
        modelBuilder.Entity<Employee>().Property(x => x.Status).HasMaxLength(50);
        modelBuilder.Entity<Employee>().Property(x => x.DeactivationReason).HasMaxLength(500);

        modelBuilder.Entity<EmployeePersonalInfo>().Ignore(x => x.Id);
        modelBuilder.Entity<EmployeePersonalInfo>().HasKey(x => x.EmployeeId);
        modelBuilder.Entity<EmployeePersonalInfo>().Property(x => x.FirstName).HasMaxLength(100);
        modelBuilder.Entity<EmployeePersonalInfo>().Property(x => x.MiddleName).HasMaxLength(100);
        modelBuilder.Entity<EmployeePersonalInfo>().Property(x => x.LastName).HasMaxLength(100);
        modelBuilder.Entity<EmployeePersonalInfo>().Property(x => x.Gender).HasMaxLength(50);
        modelBuilder.Entity<EmployeePersonalInfo>().Property(x => x.MaritalStatus).HasMaxLength(50);
        modelBuilder.Entity<EmployeePersonalInfo>().Property(x => x.Nationality).HasMaxLength(100);

        modelBuilder.Entity<EmployeeContactInfo>().Ignore(x => x.Id);
        modelBuilder.Entity<EmployeeContactInfo>().HasKey(x => x.EmployeeId);
        modelBuilder.Entity<EmployeeContactInfo>().HasIndex(x => x.WorkEmail).IsUnique();
        modelBuilder.Entity<EmployeeContactInfo>().Property(x => x.WorkEmail).HasMaxLength(256);
        modelBuilder.Entity<EmployeeContactInfo>().Property(x => x.PersonalEmail).HasMaxLength(256);
        modelBuilder.Entity<EmployeeContactInfo>().Property(x => x.Phone).HasMaxLength(50);
        modelBuilder.Entity<EmployeeContactInfo>().Property(x => x.Address).HasMaxLength(500);
        modelBuilder.Entity<EmployeeContactInfo>().Property(x => x.City).HasMaxLength(100);
        modelBuilder.Entity<EmployeeContactInfo>().Property(x => x.State).HasMaxLength(100);
        modelBuilder.Entity<EmployeeContactInfo>().Property(x => x.Country).HasMaxLength(100);

        modelBuilder.Entity<EmployeeEmploymentInfo>().Ignore(x => x.Id);
        modelBuilder.Entity<EmployeeEmploymentInfo>().HasKey(x => x.EmployeeId);
        modelBuilder.Entity<EmployeeEmploymentInfo>().HasIndex(x => x.DepartmentId);
        modelBuilder.Entity<EmployeeEmploymentInfo>().HasIndex(x => x.UnitId);
        modelBuilder.Entity<EmployeeEmploymentInfo>().HasIndex(x => x.BranchId);
        modelBuilder.Entity<EmployeeEmploymentInfo>().HasIndex(x => x.JobTitleId);
        modelBuilder.Entity<EmployeeEmploymentInfo>().HasIndex(x => x.GradeLevelId);
        modelBuilder.Entity<EmployeeEmploymentInfo>().HasIndex(x => x.EmploymentTypeId);
        modelBuilder.Entity<EmployeeEmploymentInfo>().HasIndex(x => x.LineManagerId);
        modelBuilder.Entity<EmployeeEmploymentInfo>().HasIndex(x => x.HireDate);
        modelBuilder.Entity<EmployeeEmploymentInfo>().HasIndex(x => x.ConfirmationDate);
        modelBuilder.Entity<EmployeeEmploymentInfo>()
            .ToTable(t => t.HasCheckConstraint("CK_EmployeeEmploymentInfos_HireDate_ConfirmationDate", "[ConfirmationDate] IS NULL OR [HireDate] <= [ConfirmationDate]"));

        modelBuilder.Entity<EmployeeBankInfo>().Ignore(x => x.Id);
        modelBuilder.Entity<EmployeeBankInfo>().HasKey(x => x.EmployeeId);
        modelBuilder.Entity<EmployeeBankInfo>().Property(x => x.BankName).HasMaxLength(150);
        modelBuilder.Entity<EmployeeBankInfo>().Property(x => x.BankCode).HasMaxLength(50);
        modelBuilder.Entity<EmployeeBankInfo>().Property(x => x.AccountNumberEncrypted).HasMaxLength(1000);
        modelBuilder.Entity<EmployeeBankInfo>().Property(x => x.AccountName).HasMaxLength(150);

        modelBuilder.Entity<EmployeeNextOfKin>().Ignore(x => x.Id);
        modelBuilder.Entity<EmployeeNextOfKin>().HasKey(x => x.EmployeeId);
        modelBuilder.Entity<EmployeeNextOfKin>().Property(x => x.Name).HasMaxLength(150);
        modelBuilder.Entity<EmployeeNextOfKin>().Property(x => x.Relationship).HasMaxLength(100);
        modelBuilder.Entity<EmployeeNextOfKin>().Property(x => x.Phone).HasMaxLength(50);
        modelBuilder.Entity<EmployeeNextOfKin>().Property(x => x.Email).HasMaxLength(256);
        modelBuilder.Entity<EmployeeNextOfKin>().Property(x => x.Address).HasMaxLength(500);

        modelBuilder.Entity<EmployeeEmergencyContact>().HasIndex(x => x.EmployeeId);
        modelBuilder.Entity<EmployeeEmergencyContact>()
            .HasIndex(x => new { x.EmployeeId, x.Priority })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");
        modelBuilder.Entity<EmployeeEmergencyContact>().Property(x => x.Name).HasMaxLength(150);
        modelBuilder.Entity<EmployeeEmergencyContact>().Property(x => x.Relationship).HasMaxLength(100);
        modelBuilder.Entity<EmployeeEmergencyContact>().Property(x => x.Phone).HasMaxLength(50);
        modelBuilder.Entity<EmployeeEmergencyContact>().Property(x => x.Email).HasMaxLength(256);
        modelBuilder.Entity<EmployeeEmergencyContact>().Property(x => x.Address).HasMaxLength(500);

        modelBuilder.Entity<EmployeeJobHistory>().HasIndex(x => x.EmployeeId);
        modelBuilder.Entity<EmployeeJobHistory>().HasIndex(x => x.EffectiveDate);
        modelBuilder.Entity<EmployeeJobHistory>().HasIndex(x => new { x.EmployeeId, x.EffectiveDate });
        modelBuilder.Entity<EmployeeJobHistory>().HasIndex(x => x.FromJobTitleId);
        modelBuilder.Entity<EmployeeJobHistory>().HasIndex(x => x.ToJobTitleId);
        modelBuilder.Entity<EmployeeJobHistory>().HasIndex(x => x.FromDepartmentId);
        modelBuilder.Entity<EmployeeJobHistory>().HasIndex(x => x.ToDepartmentId);
        modelBuilder.Entity<EmployeeJobHistory>().Property(x => x.Reason).HasMaxLength(500);

        modelBuilder.Entity<EmployeeStatusHistory>().HasIndex(x => x.EmployeeId);
        modelBuilder.Entity<EmployeeStatusHistory>().HasIndex(x => x.EffectiveDate);
        modelBuilder.Entity<EmployeeStatusHistory>().HasIndex(x => new { x.EmployeeId, x.EffectiveDate });
        modelBuilder.Entity<EmployeeStatusHistory>().HasIndex(x => x.NewStatus);
        modelBuilder.Entity<EmployeeStatusHistory>().Property(x => x.OldStatus).HasMaxLength(50);
        modelBuilder.Entity<EmployeeStatusHistory>().Property(x => x.NewStatus).HasMaxLength(50);
        modelBuilder.Entity<EmployeeStatusHistory>().Property(x => x.Reason).HasMaxLength(500);
        modelBuilder.Entity<EmployeeStatusHistory>().Property(x => x.ChangedBy).HasMaxLength(100);
        modelBuilder.Entity<EmployeeDocument>().HasIndex(x => x.EmployeeId);
        modelBuilder.Entity<EmployeeDocument>().HasIndex(x => x.DocumentTypeId);
        modelBuilder.Entity<EmployeeDocument>().HasIndex(x => x.VerificationStatus);
        modelBuilder.Entity<EmployeeDocument>().HasIndex(x => x.ExpiryDate);
        modelBuilder.Entity<EmployeeDocument>().HasIndex(x => x.IsArchived);
        modelBuilder.Entity<EmployeeDocument>().Property(x => x.LegacyDocumentType).HasColumnName("DocumentType").HasMaxLength(100);
        modelBuilder.Entity<EmployeeDocument>().Property(x => x.Title).HasMaxLength(200);
        modelBuilder.Entity<EmployeeDocument>().Property(x => x.FileName).HasMaxLength(255);
        modelBuilder.Entity<EmployeeDocument>().Property(x => x.StorageKey).HasMaxLength(500);
        modelBuilder.Entity<EmployeeDocument>().Property(x => x.VerificationStatus).HasMaxLength(50);
        modelBuilder.Entity<EmployeeDocument>()
            .ToTable(t => t.HasCheckConstraint("CK_EmployeeDocuments_IssueDate_ExpiryDate", "[IssueDate] IS NULL OR [ExpiryDate] IS NULL OR [IssueDate] <= CONVERT(date, [ExpiryDate])"));

        modelBuilder.Entity<DocumentType>().HasIndex(x => x.Code).IsUnique();
        modelBuilder.Entity<DocumentType>().HasIndex(x => x.Status);
        modelBuilder.Entity<DocumentType>().Property(x => x.Code).HasMaxLength(50);
        modelBuilder.Entity<DocumentType>().Property(x => x.Name).HasMaxLength(150);
        modelBuilder.Entity<DocumentType>().Property(x => x.AllowedExtensions).HasMaxLength(500);
        modelBuilder.Entity<DocumentType>().Property(x => x.ConfidentialityLevel).HasMaxLength(50);
        modelBuilder.Entity<DocumentType>().Property(x => x.Status).HasMaxLength(50);

        modelBuilder.Entity<DocumentAccessRule>()
            .HasIndex(x => new { x.DocumentTypeId, x.RoleId })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");
        modelBuilder.Entity<DocumentAccessRule>().Property(x => x.AccessLevel).HasMaxLength(50);

        modelBuilder.Entity<DocumentVerificationHistory>().HasIndex(x => x.DocumentId);
        modelBuilder.Entity<DocumentVerificationHistory>().HasIndex(x => x.VerifiedBy);
        modelBuilder.Entity<DocumentVerificationHistory>().HasIndex(x => x.VerifiedAt);
        modelBuilder.Entity<DocumentVerificationHistory>().HasIndex(x => x.Status);
        modelBuilder.Entity<DocumentVerificationHistory>().Property(x => x.Status).HasMaxLength(50);
        modelBuilder.Entity<DocumentVerificationHistory>().Property(x => x.Comments).HasMaxLength(1000);

        modelBuilder.Entity<DocumentStorageReference>().Ignore(x => x.Id);
        modelBuilder.Entity<DocumentStorageReference>().HasKey(x => x.DocumentId);
        modelBuilder.Entity<DocumentStorageReference>().HasIndex(x => x.BlobKey);
        modelBuilder.Entity<DocumentStorageReference>().HasIndex(x => x.UploadedAt);
        modelBuilder.Entity<DocumentStorageReference>().Property(x => x.StorageProvider).HasMaxLength(50);
        modelBuilder.Entity<DocumentStorageReference>().Property(x => x.ContainerName).HasMaxLength(150);
        modelBuilder.Entity<DocumentStorageReference>().Property(x => x.BlobKey).HasMaxLength(500);
        modelBuilder.Entity<DocumentStorageReference>().Property(x => x.ContentType).HasMaxLength(150);
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

        modelBuilder.Entity<Department>()
            .HasOne(x => x.HeadUser)
            .WithMany(x => x.HeadedDepartments)
            .HasForeignKey(x => x.HeadUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<UserManagerAssignment>()
            .HasOne(x => x.User)
            .WithMany(x => x.ManagerAssignments)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<UserManagerAssignment>()
            .HasOne(x => x.ManagerUser)
            .WithMany(x => x.DirectReportAssignments)
            .HasForeignKey(x => x.ManagerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Unit>()
            .HasOne(x => x.Department)
            .WithMany(x => x.Units)
            .HasForeignKey(x => x.DepartmentId)
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

        modelBuilder.Entity<PublicHoliday>()
            .HasOne(x => x.Branch)
            .WithMany(x => x.PublicHolidays)
            .HasForeignKey(x => x.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AuditLog>()
            .HasOne(x => x.Detail)
            .WithOne(x => x.AuditLog)
            .HasForeignKey<AuditLogDetail>(x => x.AuditLogId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Employee>()
            .HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Employee>()
            .HasOne<Department>()
            .WithMany()
            .HasForeignKey(x => x.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Employee>()
            .HasOne<Unit>()
            .WithMany()
            .HasForeignKey(x => x.UnitId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Employee>()
            .HasOne<Branch>()
            .WithMany()
            .HasForeignKey(x => x.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Employee>()
            .HasOne<JobTitle>()
            .WithMany()
            .HasForeignKey(x => x.JobTitleId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Employee>()
            .HasOne<GradeLevel>()
            .WithMany()
            .HasForeignKey(x => x.GradeLevelId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Employee>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.LineManagerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Employee>()
            .HasOne(x => x.PersonalInfo)
            .WithOne(x => x.Employee)
            .HasForeignKey<EmployeePersonalInfo>(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Employee>()
            .HasOne(x => x.ContactInfo)
            .WithOne(x => x.Employee)
            .HasForeignKey<EmployeeContactInfo>(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Employee>()
            .HasOne(x => x.EmploymentInfo)
            .WithOne(x => x.Employee)
            .HasForeignKey<EmployeeEmploymentInfo>(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Employee>()
            .HasOne(x => x.BankInfo)
            .WithOne(x => x.Employee)
            .HasForeignKey<EmployeeBankInfo>(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Employee>()
            .HasOne(x => x.NextOfKin)
            .WithOne(x => x.Employee)
            .HasForeignKey<EmployeeNextOfKin>(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Employee>()
            .HasMany(x => x.EmergencyContacts)
            .WithOne(x => x.Employee)
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Employee>()
            .HasMany(x => x.JobHistory)
            .WithOne(x => x.Employee)
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Employee>()
            .HasMany(x => x.StatusHistory)
            .WithOne(x => x.Employee)
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<EmployeeEmploymentInfo>()
            .HasOne(x => x.Department)
            .WithMany()
            .HasForeignKey(x => x.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<EmployeeEmploymentInfo>()
            .HasOne(x => x.Unit)
            .WithMany()
            .HasForeignKey(x => x.UnitId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<EmployeeEmploymentInfo>()
            .HasOne(x => x.Branch)
            .WithMany()
            .HasForeignKey(x => x.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<EmployeeEmploymentInfo>()
            .HasOne(x => x.JobTitle)
            .WithMany()
            .HasForeignKey(x => x.JobTitleId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<EmployeeEmploymentInfo>()
            .HasOne(x => x.GradeLevel)
            .WithMany()
            .HasForeignKey(x => x.GradeLevelId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<EmployeeEmploymentInfo>()
            .HasOne(x => x.EmploymentType)
            .WithMany()
            .HasForeignKey(x => x.EmploymentTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<EmployeeEmploymentInfo>()
            .HasOne(x => x.LineManager)
            .WithMany()
            .HasForeignKey(x => x.LineManagerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<EmployeeJobHistory>()
            .HasOne(x => x.FromJobTitle)
            .WithMany()
            .HasForeignKey(x => x.FromJobTitleId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<EmployeeJobHistory>()
            .HasOne(x => x.ToJobTitle)
            .WithMany()
            .HasForeignKey(x => x.ToJobTitleId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<EmployeeJobHistory>()
            .HasOne(x => x.FromDepartment)
            .WithMany()
            .HasForeignKey(x => x.FromDepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<EmployeeJobHistory>()
            .HasOne(x => x.ToDepartment)
            .WithMany()
            .HasForeignKey(x => x.ToDepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<EmployeeDocument>()
            .HasOne(x => x.Employee)
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<EmployeeDocument>()
            .HasOne(x => x.DocumentType)
            .WithMany(x => x.EmployeeDocuments)
            .HasForeignKey(x => x.DocumentTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<DocumentAccessRule>()
            .HasOne(x => x.DocumentType)
            .WithMany(x => x.AccessRules)
            .HasForeignKey(x => x.DocumentTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<DocumentAccessRule>()
            .HasOne(x => x.Role)
            .WithMany(x => x.DocumentAccessRules)
            .HasForeignKey(x => x.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<DocumentVerificationHistory>()
            .HasOne(x => x.Document)
            .WithMany(x => x.VerificationHistory)
            .HasForeignKey(x => x.DocumentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<DocumentVerificationHistory>()
            .HasOne(x => x.VerifiedByUser)
            .WithMany()
            .HasForeignKey(x => x.VerifiedBy)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<EmployeeDocument>()
            .HasOne(x => x.StorageReference)
            .WithOne(x => x.Document)
            .HasForeignKey<DocumentStorageReference>(x => x.DocumentId)
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
        EnforceAppendOnlyLogs();
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries<AuditLog>().Where(x => x.State == EntityState.Added))
        {
            entry.Entity.Timestamp = entry.Entity.Timestamp == default ? now : entry.Entity.Timestamp;
            entry.Entity.PartitionKey = entry.Entity.Timestamp.Year * 100 + entry.Entity.Timestamp.Month;
        }

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

    private void EnforceAppendOnlyLogs()
    {
        var immutableEntries = ChangeTracker.Entries()
            .Where(x => (x.Entity is AuditLog || x.Entity is AuditLogDetail || x.Entity is LoginAttempt || x.Entity is DocumentVerificationHistory) &&
                        (x.State == EntityState.Modified || x.State == EntityState.Deleted));

        foreach (var entry in immutableEntries)
        {
            throw new InvalidOperationException($"{entry.Entity.GetType().Name} is append-only and cannot be updated or deleted.");
        }
    }
}
