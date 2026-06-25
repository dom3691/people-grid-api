using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using PeopleGrid.Application.Abstractions;
using PeopleGrid.Application.Features.Employees.DTOs;
using PeopleGrid.Application.Features.Employees.Interfaces;
using PeopleGrid.Domain.Entities;
using PeopleGrid.Shared.Exceptions;
using PeopleGrid.Shared.Responses;

namespace PeopleGrid.Infrastructure.Services;

public sealed class EmployeeService(IApplicationDbContext dbContext, ICurrentUserService currentUser, IConfiguration configuration) : IEmployeeService
{
    public async Task<EmployeeDetailsDto> CreateAsync(CreateEmployeeRequest request, CancellationToken cancellationToken = default)
    {
        var employeeNumber = string.IsNullOrWhiteSpace(request.EmployeeNumber)
            ? (await GenerateNumberAsync(new GenerateEmployeeNumberRequest(null), cancellationToken)).EmployeeNumber
            : request.EmployeeNumber.Trim();

        await EnsureUniqueAsync(null, employeeNumber, request.ContactInfo.WorkEmail, cancellationToken);
        await ValidateReferencesAsync(request.EmploymentInfo, cancellationToken);

        await using var transaction = await BeginTransactionAsync(cancellationToken);
        var employee = new Employee
        {
            EmployeeNumber = employeeNumber,
            UserId = request.UserId,
            FirstName = request.PersonalInfo.FirstName.Trim(),
            LastName = request.PersonalInfo.LastName.Trim(),
            WorkEmail = NormalizeEmail(request.ContactInfo.WorkEmail),
            DepartmentId = request.EmploymentInfo.DepartmentId,
            UnitId = request.EmploymentInfo.UnitId,
            BranchId = request.EmploymentInfo.BranchId,
            JobTitleId = request.EmploymentInfo.JobTitleId,
            GradeLevelId = request.EmploymentInfo.GradeLevelId,
            LineManagerId = request.EmploymentInfo.LineManagerId,
            Status = request.Status
        };

        dbContext.Employees.Add(employee);
        dbContext.EmployeePersonalInfos.Add(MapPersonal(employee.Id, request.PersonalInfo));
        dbContext.EmployeeContactInfos.Add(MapContact(employee.Id, request.ContactInfo));
        dbContext.EmployeeEmploymentInfos.Add(MapEmployment(employee.Id, request.EmploymentInfo));
        dbContext.EmployeeStatusHistories.Add(new EmployeeStatusHistory
        {
            EmployeeId = employee.Id,
            OldStatus = string.Empty,
            NewStatus = request.Status,
            EffectiveDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Reason = "Employee created",
            ChangedBy = currentUser.UserId
        });

        AddAudit("Employees", "Create", employee.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        return await GetByIdAsync(employee.Id, cancellationToken);
    }

    public async Task<PaginatedResponse<EmployeeListItemDto>> ListAsync(EmployeeListQuery query, CancellationToken cancellationToken = default)
    {
        var source = dbContext.Employees.AsNoTracking()
            .Include(x => x.PersonalInfo)
            .Include(x => x.ContactInfo)
            .Include(x => x.EmploymentInfo!).ThenInclude(x => x.Department)
            .Include(x => x.EmploymentInfo!).ThenInclude(x => x.Branch)
            .Include(x => x.EmploymentInfo!).ThenInclude(x => x.JobTitle)
            .Include(x => x.EmploymentInfo!).ThenInclude(x => x.GradeLevel)
            .Include(x => x.EmploymentInfo!).ThenInclude(x => x.LineManager)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLowerInvariant();
            source = source.Where(x =>
                x.EmployeeNumber.ToLower().Contains(search) ||
                x.WorkEmail.ToLower().Contains(search) ||
                x.FirstName.ToLower().Contains(search) ||
                x.LastName.ToLower().Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(query.Status)) source = source.Where(x => x.Status == query.Status);
        if (query.DepartmentId is not null) source = source.Where(x => x.EmploymentInfo != null && x.EmploymentInfo.DepartmentId == query.DepartmentId);
        if (query.BranchId is not null) source = source.Where(x => x.EmploymentInfo != null && x.EmploymentInfo.BranchId == query.BranchId);
        if (query.JobTitleId is not null) source = source.Where(x => x.EmploymentInfo != null && x.EmploymentInfo.JobTitleId == query.JobTitleId);
        if (query.GradeLevelId is not null) source = source.Where(x => x.EmploymentInfo != null && x.EmploymentInfo.GradeLevelId == query.GradeLevelId);
        if (query.LineManagerId is not null) source = source.Where(x => x.EmploymentInfo != null && x.EmploymentInfo.LineManagerId == query.LineManagerId);

        source = ApplySorting(source, query.SortBy, query.SortDirection);
        var totalCount = await source.CountAsync(cancellationToken);
        var pagination = query.ToPagination();
        var employees = await source.Skip(pagination.Skip).Take(pagination.Take).ToListAsync(cancellationToken);
        var items = employees.Select(MapListItem).ToList();
        return new PaginatedResponse<EmployeeListItemDto>(items, pagination.PageNumber, pagination.Take, totalCount);
    }

    public async Task<EmployeeDetailsDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var employee = await LoadDetails().AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Employee was not found.");

        return MapDetails(employee);
    }

    public async Task<EmployeeDetailsDto> UpdateAsync(Guid id, UpdateEmployeeRequest request, CancellationToken cancellationToken = default)
    {
        var employee = await LoadForUpdateAsync(id, cancellationToken);
        await EnsureUniqueAsync(id, employee.EmployeeNumber, request.ContactInfo.WorkEmail, cancellationToken);
        await ValidateReferencesAsync(request.EmploymentInfo, cancellationToken);
        await EnsureNotOwnManagerAsync(employee.UserId, request.EmploymentInfo.LineManagerId, cancellationToken);

        ApplyProfile(employee, request.PersonalInfo, request.ContactInfo, request.EmploymentInfo);
        if (employee.Status != request.Status)
        {
            AddStatusHistory(employee, request.Status, "Employee updated");
            employee.Status = request.Status;
        }

        AddAudit("Employees", "Update", employee.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<EmployeeDetailsDto> ChangeStatusAsync(Guid id, ChangeEmployeeStatusRequest request, CancellationToken cancellationToken = default)
    {
        var employee = await dbContext.Employees.Include(x => x.StatusHistory).FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Employee was not found.");

        await using var transaction = await BeginTransactionAsync(cancellationToken);
        var oldStatus = employee.Status;
        employee.Status = request.Status;
        dbContext.EmployeeStatusHistories.Add(new EmployeeStatusHistory
        {
            EmployeeId = employee.Id,
            OldStatus = oldStatus,
            NewStatus = request.Status,
            EffectiveDate = request.EffectiveDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
            Reason = request.Reason?.Trim(),
            ChangedBy = currentUser.UserId
        });
        AddAudit("Employees", "ChangeStatus", employee.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        if (transaction is not null) await transaction.CommitAsync(cancellationToken);
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task DeactivateAsync(Guid id, DeactivateEmployeeRequest request, CancellationToken cancellationToken = default)
    {
        var employee = await dbContext.Employees.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Employee was not found.");

        await using var transaction = await BeginTransactionAsync(cancellationToken);
        var oldStatus = employee.Status;
        employee.Status = "Deactivated";
        employee.DeactivatedAt = DateTime.UtcNow;
        employee.DeactivationReason = request.Reason.Trim();
        employee.IsDeleted = true;
        employee.DeletedAt = DateTime.UtcNow;
        employee.DeletedBy = currentUser.UserId;
        dbContext.EmployeeStatusHistories.Add(new EmployeeStatusHistory
        {
            EmployeeId = employee.Id,
            OldStatus = oldStatus,
            NewStatus = "Deactivated",
            EffectiveDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Reason = request.Reason.Trim(),
            ChangedBy = currentUser.UserId
        });
        AddAudit("Employees", "Deactivate", employee.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        if (transaction is not null) await transaction.CommitAsync(cancellationToken);
    }

    public async Task<GenerateEmployeeNumberResponse> GenerateNumberAsync(GenerateEmployeeNumberRequest request, CancellationToken cancellationToken = default)
    {
        var prefix = string.IsNullOrWhiteSpace(request.Prefix) ? "PG" : request.Prefix.Trim().ToUpperInvariant();
        var count = await dbContext.Employees.IgnoreQueryFilters().CountAsync(cancellationToken) + 1;
        string employeeNumber;
        do
        {
            employeeNumber = $"{prefix}-{count:00000}";
            count++;
        }
        while (await dbContext.Employees.IgnoreQueryFilters().AnyAsync(x => x.EmployeeNumber == employeeNumber, cancellationToken));

        return new GenerateEmployeeNumberResponse(employeeNumber);
    }

    public async Task<EmployeeDetailsDto> UpdateEmploymentInfoAsync(Guid id, EmployeeEmploymentInfoRequest request, CancellationToken cancellationToken = default)
    {
        var employee = await LoadForUpdateAsync(id, cancellationToken);
        await ValidateReferencesAsync(request, cancellationToken);
        await EnsureNotOwnManagerAsync(employee.UserId, request.LineManagerId, cancellationToken);
        ApplyEmployment(employee, request);
        AddAudit("Employees", "UpdateEmploymentInfo", employee.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<EmployeeDetailsDto> UpdateBankInfoAsync(Guid id, EmployeeBankInfoRequest request, CancellationToken cancellationToken = default)
    {
        var employee = await dbContext.Employees.Include(x => x.BankInfo).FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Employee was not found.");

        employee.BankInfo ??= new EmployeeBankInfo { EmployeeId = employee.Id };
        employee.BankInfo.BankName = request.BankName.Trim();
        employee.BankInfo.BankCode = NormalizeNullable(request.BankCode);
        employee.BankInfo.AccountNumberEncrypted = Encrypt(request.AccountNumber.Trim());
        employee.BankInfo.AccountName = request.AccountName.Trim();
        AddAudit("Employees", "UpdateBankInfo", employee.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<EmployeeDetailsDto> UpdateNextOfKinAsync(Guid id, EmployeeNextOfKinRequest request, CancellationToken cancellationToken = default)
    {
        var employee = await dbContext.Employees.Include(x => x.NextOfKin).FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Employee was not found.");

        employee.NextOfKin ??= new EmployeeNextOfKin { EmployeeId = employee.Id };
        employee.NextOfKin.Name = request.Name.Trim();
        employee.NextOfKin.Relationship = request.Relationship.Trim();
        employee.NextOfKin.Phone = request.Phone.Trim();
        employee.NextOfKin.Email = NormalizeEmailNullable(request.Email);
        employee.NextOfKin.Address = NormalizeNullable(request.Address);
        AddAudit("Employees", "UpdateNextOfKin", employee.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<EmployeeEmergencyContactDto> UpsertEmergencyContactAsync(Guid id, EmployeeEmergencyContactRequest request, CancellationToken cancellationToken = default)
    {
        var employee = await dbContext.Employees.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Employee was not found.");

        var contact = await dbContext.EmployeeEmergencyContacts.FirstOrDefaultAsync(x => x.EmployeeId == id && x.Priority == request.Priority, cancellationToken);
        if (contact is null)
        {
            contact = new EmployeeEmergencyContact { EmployeeId = employee.Id, Priority = request.Priority };
            dbContext.EmployeeEmergencyContacts.Add(contact);
        }

        contact.Name = request.Name.Trim();
        contact.Relationship = request.Relationship.Trim();
        contact.Phone = request.Phone.Trim();
        contact.Email = NormalizeEmailNullable(request.Email);
        contact.Address = NormalizeNullable(request.Address);
        AddAudit("Employees", "UpsertEmergencyContact", employee.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return MapEmergencyContact(contact);
    }

    public async Task<IReadOnlyCollection<EmployeeJobHistoryDto>> GetJobHistoryAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (!await dbContext.Employees.AnyAsync(x => x.Id == id, cancellationToken))
        {
            throw new NotFoundException("Employee was not found.");
        }

        var history = await dbContext.EmployeeJobHistories.AsNoTracking()
            .Include(x => x.FromJobTitle)
            .Include(x => x.ToJobTitle)
            .Include(x => x.FromDepartment)
            .Include(x => x.ToDepartment)
            .Where(x => x.EmployeeId == id)
            .OrderByDescending(x => x.EffectiveDate)
            .ToListAsync(cancellationToken);

        return history.Select(MapJobHistory).ToList();
    }

    private IQueryable<Employee> LoadDetails() => dbContext.Employees
        .Include(x => x.PersonalInfo)
        .Include(x => x.ContactInfo)
        .Include(x => x.EmploymentInfo!).ThenInclude(x => x.Department)
        .Include(x => x.EmploymentInfo!).ThenInclude(x => x.Unit)
        .Include(x => x.EmploymentInfo!).ThenInclude(x => x.Branch)
        .Include(x => x.EmploymentInfo!).ThenInclude(x => x.JobTitle)
        .Include(x => x.EmploymentInfo!).ThenInclude(x => x.GradeLevel)
        .Include(x => x.EmploymentInfo!).ThenInclude(x => x.EmploymentType)
        .Include(x => x.EmploymentInfo!).ThenInclude(x => x.LineManager)
        .Include(x => x.BankInfo)
        .Include(x => x.NextOfKin)
        .Include(x => x.EmergencyContacts);

    private async Task<Employee> LoadForUpdateAsync(Guid id, CancellationToken cancellationToken)
    {
        var employee = await dbContext.Employees
            .Include(x => x.PersonalInfo)
            .Include(x => x.ContactInfo)
            .Include(x => x.EmploymentInfo)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Employee was not found.");

        employee.PersonalInfo ??= new EmployeePersonalInfo { EmployeeId = employee.Id };
        employee.ContactInfo ??= new EmployeeContactInfo { EmployeeId = employee.Id };
        employee.EmploymentInfo ??= new EmployeeEmploymentInfo { EmployeeId = employee.Id };
        return employee;
    }

    private void ApplyProfile(Employee employee, EmployeePersonalInfoRequest personal, EmployeeContactInfoRequest contact, EmployeeEmploymentInfoRequest employment)
    {
        employee.FirstName = personal.FirstName.Trim();
        employee.LastName = personal.LastName.Trim();
        employee.WorkEmail = NormalizeEmail(contact.WorkEmail);
        employee.DepartmentId = employment.DepartmentId;
        employee.UnitId = employment.UnitId;
        employee.BranchId = employment.BranchId;
        employee.JobTitleId = employment.JobTitleId;
        employee.GradeLevelId = employment.GradeLevelId;
        employee.LineManagerId = employment.LineManagerId;

        employee.PersonalInfo = MapPersonal(employee.Id, personal, employee.PersonalInfo);
        employee.ContactInfo = MapContact(employee.Id, contact, employee.ContactInfo);
        ApplyEmployment(employee, employment);
    }

    private void ApplyEmployment(Employee employee, EmployeeEmploymentInfoRequest request)
    {
        employee.EmploymentInfo ??= new EmployeeEmploymentInfo { EmployeeId = employee.Id };
        employee.EmploymentInfo.DepartmentId = request.DepartmentId;
        employee.EmploymentInfo.UnitId = request.UnitId;
        employee.EmploymentInfo.BranchId = request.BranchId;
        employee.EmploymentInfo.JobTitleId = request.JobTitleId;
        employee.EmploymentInfo.GradeLevelId = request.GradeLevelId;
        employee.EmploymentInfo.EmploymentTypeId = request.EmploymentTypeId;
        employee.EmploymentInfo.LineManagerId = request.LineManagerId;
        employee.EmploymentInfo.HireDate = request.HireDate;
        employee.EmploymentInfo.ConfirmationDate = request.ConfirmationDate;
        employee.DepartmentId = request.DepartmentId;
        employee.UnitId = request.UnitId;
        employee.BranchId = request.BranchId;
        employee.JobTitleId = request.JobTitleId;
        employee.GradeLevelId = request.GradeLevelId;
        employee.LineManagerId = request.LineManagerId;
    }

    private async Task EnsureUniqueAsync(Guid? employeeId, string employeeNumber, string workEmail, CancellationToken cancellationToken)
    {
        var email = NormalizeEmail(workEmail);
        if (await dbContext.Employees.IgnoreQueryFilters().AnyAsync(x => x.Id != employeeId && (x.EmployeeNumber == employeeNumber || x.WorkEmail == email), cancellationToken) ||
            await dbContext.EmployeeContactInfos.IgnoreQueryFilters().AnyAsync(x => x.EmployeeId != employeeId && x.WorkEmail == email, cancellationToken))
        {
            throw new BusinessRuleException("Employee number or work email already exists.");
        }
    }

    private async Task ValidateReferencesAsync(EmployeeEmploymentInfoRequest request, CancellationToken cancellationToken)
    {
        if (request.DepartmentId is not null && !await dbContext.Departments.AnyAsync(x => x.Id == request.DepartmentId && x.IsActive, cancellationToken)) throw new BusinessRuleException("Selected department is invalid.");
        if (request.UnitId is not null && !await dbContext.Units.AnyAsync(x => x.Id == request.UnitId && x.IsActive && (request.DepartmentId == null || x.DepartmentId == request.DepartmentId), cancellationToken)) throw new BusinessRuleException("Selected unit is invalid.");
        if (request.BranchId is not null && !await dbContext.Branches.AnyAsync(x => x.Id == request.BranchId && x.IsActive, cancellationToken)) throw new BusinessRuleException("Selected branch is invalid.");
        if (request.JobTitleId is not null && !await dbContext.JobTitles.AnyAsync(x => x.Id == request.JobTitleId && x.IsActive, cancellationToken)) throw new BusinessRuleException("Selected job title is invalid.");
        if (request.GradeLevelId is not null && !await dbContext.GradeLevels.AnyAsync(x => x.Id == request.GradeLevelId && x.IsActive, cancellationToken)) throw new BusinessRuleException("Selected grade level is invalid.");
        if (request.EmploymentTypeId is not null && !await dbContext.EmploymentTypes.AnyAsync(x => x.Id == request.EmploymentTypeId && x.IsActive, cancellationToken)) throw new BusinessRuleException("Selected employment type is invalid.");
        if (request.LineManagerId is not null && !await dbContext.Users.AnyAsync(x => x.Id == request.LineManagerId && x.IsActive, cancellationToken)) throw new BusinessRuleException("Selected line manager is invalid.");
    }

    private async Task EnsureNotOwnManagerAsync(Guid? employeeUserId, Guid? lineManagerId, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        if (employeeUserId is not null && lineManagerId == employeeUserId)
        {
            throw new BusinessRuleException("An employee cannot be assigned as their own line manager.");
        }
    }

    private void AddStatusHistory(Employee employee, string newStatus, string? reason)
    {
        dbContext.EmployeeStatusHistories.Add(new EmployeeStatusHistory { EmployeeId = employee.Id, OldStatus = employee.Status, NewStatus = newStatus, EffectiveDate = DateOnly.FromDateTime(DateTime.UtcNow), Reason = reason, ChangedBy = currentUser.UserId });
    }

    private void AddAudit(string module, string action, Guid employeeId) => dbContext.AuditLogs.Add(new AuditLog { ActorUserId = currentUser.UserId, Module = module, Action = action, EntityType = "Employee", EntityId = employeeId.ToString(), Outcome = "Success" });

    private async Task<IDbContextTransaction?> BeginTransactionAsync(CancellationToken cancellationToken) => dbContext is DbContext efContext ? await efContext.Database.BeginTransactionAsync(cancellationToken) : null;

    private static EmployeePersonalInfo MapPersonal(Guid employeeId, EmployeePersonalInfoRequest request, EmployeePersonalInfo? target = null)
    {
        target ??= new EmployeePersonalInfo { EmployeeId = employeeId };
        target.FirstName = request.FirstName.Trim();
        target.MiddleName = NormalizeNullable(request.MiddleName);
        target.LastName = request.LastName.Trim();
        target.DateOfBirth = request.DateOfBirth;
        target.Gender = request.Gender.Trim();
        target.MaritalStatus = NormalizeNullable(request.MaritalStatus);
        target.Nationality = NormalizeNullable(request.Nationality);
        return target;
    }

    private static EmployeeContactInfo MapContact(Guid employeeId, EmployeeContactInfoRequest request, EmployeeContactInfo? target = null)
    {
        target ??= new EmployeeContactInfo { EmployeeId = employeeId };
        target.WorkEmail = NormalizeEmail(request.WorkEmail);
        target.PersonalEmail = NormalizeEmailNullable(request.PersonalEmail);
        target.Phone = NormalizeNullable(request.Phone);
        target.Address = NormalizeNullable(request.Address);
        target.City = NormalizeNullable(request.City);
        target.State = NormalizeNullable(request.State);
        target.Country = NormalizeNullable(request.Country);
        return target;
    }

    private static EmployeeEmploymentInfo MapEmployment(Guid employeeId, EmployeeEmploymentInfoRequest request) => new()
    {
        EmployeeId = employeeId,
        DepartmentId = request.DepartmentId,
        UnitId = request.UnitId,
        BranchId = request.BranchId,
        JobTitleId = request.JobTitleId,
        GradeLevelId = request.GradeLevelId,
        EmploymentTypeId = request.EmploymentTypeId,
        LineManagerId = request.LineManagerId,
        HireDate = request.HireDate,
        ConfirmationDate = request.ConfirmationDate
    };

    private static EmployeeListItemDto MapListItem(Employee employee) => new(
        employee.Id,
        employee.EmployeeNumber,
        $"{employee.PersonalInfo?.FirstName ?? employee.FirstName} {employee.PersonalInfo?.LastName ?? employee.LastName}".Trim(),
        employee.ContactInfo?.WorkEmail ?? employee.WorkEmail,
        employee.Status,
        employee.EmploymentInfo?.Department?.Name,
        employee.EmploymentInfo?.Branch?.Name,
        employee.EmploymentInfo?.JobTitle?.Name,
        employee.EmploymentInfo?.GradeLevel?.Name,
        employee.EmploymentInfo?.LineManager is null ? null : $"{employee.EmploymentInfo.LineManager.FirstName} {employee.EmploymentInfo.LineManager.LastName}".Trim(),
        employee.CreatedAt);

    private static EmployeeDetailsDto MapDetails(Employee employee) => new(
        employee.Id,
        employee.EmployeeNumber,
        employee.UserId,
        employee.Status,
        employee.DeactivatedAt,
        employee.DeactivationReason,
        employee.PersonalInfo is null ? null : new EmployeePersonalInfoDto(employee.PersonalInfo.FirstName, employee.PersonalInfo.MiddleName, employee.PersonalInfo.LastName, employee.PersonalInfo.DateOfBirth, employee.PersonalInfo.Gender, employee.PersonalInfo.MaritalStatus, employee.PersonalInfo.Nationality),
        employee.ContactInfo is null ? null : new EmployeeContactInfoDto(employee.ContactInfo.WorkEmail, employee.ContactInfo.PersonalEmail, employee.ContactInfo.Phone, employee.ContactInfo.Address, employee.ContactInfo.City, employee.ContactInfo.State, employee.ContactInfo.Country),
        employee.EmploymentInfo is null ? null : new EmployeeEmploymentInfoDto(employee.EmploymentInfo.DepartmentId, employee.EmploymentInfo.Department?.Name, employee.EmploymentInfo.UnitId, employee.EmploymentInfo.Unit?.Name, employee.EmploymentInfo.BranchId, employee.EmploymentInfo.Branch?.Name, employee.EmploymentInfo.JobTitleId, employee.EmploymentInfo.JobTitle?.Name, employee.EmploymentInfo.GradeLevelId, employee.EmploymentInfo.GradeLevel?.Name, employee.EmploymentInfo.EmploymentTypeId, employee.EmploymentInfo.EmploymentType?.Name, employee.EmploymentInfo.LineManagerId, employee.EmploymentInfo.LineManager is null ? null : $"{employee.EmploymentInfo.LineManager.FirstName} {employee.EmploymentInfo.LineManager.LastName}".Trim(), employee.EmploymentInfo.HireDate, employee.EmploymentInfo.ConfirmationDate),
        employee.BankInfo is null ? null : new EmployeeBankInfoDto(employee.BankInfo.BankName, employee.BankInfo.BankCode, MaskEncryptedAccount(employee.BankInfo.AccountNumberEncrypted), employee.BankInfo.AccountName),
        employee.NextOfKin is null ? null : new EmployeeNextOfKinDto(employee.NextOfKin.Name, employee.NextOfKin.Relationship, employee.NextOfKin.Phone, employee.NextOfKin.Email, employee.NextOfKin.Address),
        employee.EmergencyContacts.OrderBy(x => x.Priority).Select(MapEmergencyContact).ToList());

    private static EmployeeEmergencyContactDto MapEmergencyContact(EmployeeEmergencyContact x) => new(x.Id, x.Name, x.Relationship, x.Phone, x.Email, x.Address, x.Priority);

    private static EmployeeJobHistoryDto MapJobHistory(EmployeeJobHistory x) => new(x.Id, x.EffectiveDate, x.FromJobTitleId, x.FromJobTitle?.Name, x.ToJobTitleId, x.ToJobTitle?.Name, x.FromDepartmentId, x.FromDepartment?.Name, x.ToDepartmentId, x.ToDepartment?.Name, x.Reason);

    private static IQueryable<Employee> ApplySorting(IQueryable<Employee> source, string? sortBy, string? sortDirection)
    {
        var desc = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);
        return (sortBy ?? "createdAt").Trim().ToLowerInvariant() switch
        {
            "employeenumber" => desc ? source.OrderByDescending(x => x.EmployeeNumber) : source.OrderBy(x => x.EmployeeNumber),
            "name" => desc ? source.OrderByDescending(x => x.FirstName).ThenByDescending(x => x.LastName) : source.OrderBy(x => x.FirstName).ThenBy(x => x.LastName),
            "status" => desc ? source.OrderByDescending(x => x.Status) : source.OrderBy(x => x.Status),
            _ => desc ? source.OrderByDescending(x => x.CreatedAt) : source.OrderBy(x => x.CreatedAt)
        };
    }

    private string Encrypt(string value)
    {
        var keyMaterial = configuration["Security:DataProtectionKey"] ?? "PeopleGrid.Default.Development.DataProtection.Key.ChangeMe";
        var key = SHA256.HashData(Encoding.UTF8.GetBytes(keyMaterial));
        using var aes = Aes.Create();
        aes.Key = key;
        aes.GenerateIV();
        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(value);
        var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
        var protectedBytes = new byte[aes.IV.Length + cipherBytes.Length];
        Buffer.BlockCopy(aes.IV, 0, protectedBytes, 0, aes.IV.Length);
        Buffer.BlockCopy(cipherBytes, 0, protectedBytes, aes.IV.Length, cipherBytes.Length);
        return $"v1:{Convert.ToBase64String(protectedBytes)}";
    }
    private static string MaskEncryptedAccount(string encrypted) => string.IsNullOrWhiteSpace(encrypted) ? string.Empty : "********";
    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
    private static string? NormalizeEmailNullable(string? email) => string.IsNullOrWhiteSpace(email) ? null : NormalizeEmail(email);
    private static string? NormalizeNullable(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
