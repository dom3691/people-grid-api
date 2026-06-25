using System.Globalization;
using Microsoft.EntityFrameworkCore;
using PeopleGrid.Application.Abstractions;
using PeopleGrid.Application.Features.Settings.DTOs;
using PeopleGrid.Application.Features.Settings.Interfaces;
using PeopleGrid.Domain.Entities;
using PeopleGrid.Shared.Exceptions;
using PeopleGrid.Shared.Responses;

namespace PeopleGrid.Infrastructure.Services;

public sealed class SettingsService(IApplicationDbContext dbContext, ICurrentUserService currentUser) : ISettingsService
{
    public async Task<CompanyProfileDto?> GetCompanyProfileAsync(CancellationToken cancellationToken = default)
    {
        var profile = await dbContext.CompanyProfiles.AsNoTracking().OrderBy(x => x.CreatedAt).FirstOrDefaultAsync(cancellationToken);
        return profile is null ? null : MapCompanyProfile(profile);
    }

    public async Task<CompanyProfileDto> UpdateCompanyProfileAsync(CompanyProfileRequest request, CancellationToken cancellationToken = default)
    {
        var profile = await dbContext.CompanyProfiles.OrderBy(x => x.CreatedAt).FirstOrDefaultAsync(cancellationToken);
        if (profile is null)
        {
            profile = new CompanyProfile();
            dbContext.CompanyProfiles.Add(profile);
        }

        profile.Name = request.Name.Trim();
        profile.RegistrationNumber = request.RegistrationNumber?.Trim();
        profile.LogoPath = request.LogoPath?.Trim();
        profile.Address = request.Address?.Trim();
        profile.ContactEmail = request.ContactEmail?.Trim().ToLowerInvariant();
        profile.Phone = request.Phone?.Trim();

        AddAudit("Settings", "UpdateCompanyProfile", "CompanyProfile", profile.Id.ToString());
        await dbContext.SaveChangesAsync(cancellationToken);
        return MapCompanyProfile(profile);
    }

    public async Task<GradeLevelDto> CreateGradeLevelAsync(GradeLevelRequest request, CancellationToken cancellationToken = default)
    {
        var code = NormalizeCode(request.Code);
        await EnsureGradeLevelCodeUniqueAsync(null, code, cancellationToken);
        var gradeLevel = new GradeLevel { Code = code, Name = request.Name.Trim(), RankOrder = request.RankOrder, Status = request.Status, IsActive = IsActive(request.Status) };
        dbContext.GradeLevels.Add(gradeLevel);
        AddAudit("Settings", "CreateGradeLevel", "GradeLevel", gradeLevel.Id.ToString());
        await dbContext.SaveChangesAsync(cancellationToken);
        return MapGradeLevel(gradeLevel);
    }

    public async Task<PaginatedResponse<GradeLevelDto>> ListGradeLevelsAsync(SettingsListQuery query, CancellationToken cancellationToken = default)
    {
        var source = ApplyGradeLevelFilters(dbContext.GradeLevels.AsNoTracking(), query.Search, query.Status).OrderBy(x => x.RankOrder).ThenBy(x => x.Name);
        return await PaginateAsync(source, query, MapGradeLevel, cancellationToken);
    }

    public async Task<GradeLevelDto> UpdateGradeLevelAsync(Guid id, GradeLevelRequest request, CancellationToken cancellationToken = default)
    {
        var gradeLevel = await dbContext.GradeLevels.FirstOrDefaultAsync(x => x.Id == id, cancellationToken) ?? throw new NotFoundException("Grade level was not found.");
        var code = NormalizeCode(request.Code);
        await EnsureGradeLevelCodeUniqueAsync(id, code, cancellationToken);
        await EnsureCanDeactivateAsync(request.Status, dbContext.JobTitles.AnyAsync(x => x.GradeLevelId == id && x.IsActive, cancellationToken), "Grade level cannot be deactivated while active job titles use it.");

        gradeLevel.Code = code;
        gradeLevel.Name = request.Name.Trim();
        gradeLevel.RankOrder = request.RankOrder;
        gradeLevel.Status = request.Status;
        gradeLevel.IsActive = IsActive(request.Status);
        AddAudit("Settings", "UpdateGradeLevel", "GradeLevel", gradeLevel.Id.ToString());
        await dbContext.SaveChangesAsync(cancellationToken);
        return MapGradeLevel(gradeLevel);
    }

    public async Task<EmploymentTypeDto> CreateEmploymentTypeAsync(EmploymentTypeRequest request, CancellationToken cancellationToken = default)
    {
        var code = NormalizeCode(request.Code);
        await EnsureEmploymentTypeCodeUniqueAsync(null, code, cancellationToken);
        var employmentType = new EmploymentType { Code = code, Name = request.Name.Trim(), Status = request.Status, IsActive = IsActive(request.Status) };
        dbContext.EmploymentTypes.Add(employmentType);
        AddAudit("Settings", "CreateEmploymentType", "EmploymentType", employmentType.Id.ToString());
        await dbContext.SaveChangesAsync(cancellationToken);
        return MapEmploymentType(employmentType);
    }

    public async Task<PaginatedResponse<EmploymentTypeDto>> ListEmploymentTypesAsync(SettingsListQuery query, CancellationToken cancellationToken = default)
    {
        var source = ApplyEmploymentTypeFilters(dbContext.EmploymentTypes.AsNoTracking(), query.Search, query.Status).OrderBy(x => x.Name);
        return await PaginateAsync(source, query, MapEmploymentType, cancellationToken);
    }

    public async Task<EmploymentTypeDto> UpdateEmploymentTypeAsync(Guid id, EmploymentTypeRequest request, CancellationToken cancellationToken = default)
    {
        var employmentType = await dbContext.EmploymentTypes.FirstOrDefaultAsync(x => x.Id == id, cancellationToken) ?? throw new NotFoundException("Employment type was not found.");
        var code = NormalizeCode(request.Code);
        await EnsureEmploymentTypeCodeUniqueAsync(id, code, cancellationToken);
        await EnsureCanDeactivateAsync(request.Status, dbContext.UserProfiles.AnyAsync(x => x.EmploymentTypeId == id && x.User != null && x.User.IsActive, cancellationToken), "Employment type cannot be deactivated while active users use it.");

        employmentType.Code = code;
        employmentType.Name = request.Name.Trim();
        employmentType.Status = request.Status;
        employmentType.IsActive = IsActive(request.Status);
        AddAudit("Settings", "UpdateEmploymentType", "EmploymentType", employmentType.Id.ToString());
        await dbContext.SaveChangesAsync(cancellationToken);
        return MapEmploymentType(employmentType);
    }

    public async Task<ApprovalLevelDto> CreateApprovalLevelAsync(ApprovalLevelRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureApprovalSequenceUniqueAsync(null, request.SequenceOrder, cancellationToken);
        var approvalLevel = new ApprovalLevel { Name = request.Name.Trim(), SequenceOrder = request.SequenceOrder, Description = request.Description?.Trim(), Status = request.Status, IsActive = IsActive(request.Status) };
        dbContext.ApprovalLevels.Add(approvalLevel);
        AddAudit("Settings", "CreateApprovalLevel", "ApprovalLevel", approvalLevel.Id.ToString());
        await dbContext.SaveChangesAsync(cancellationToken);
        return MapApprovalLevel(approvalLevel);
    }

    public async Task<PaginatedResponse<ApprovalLevelDto>> ListApprovalLevelsAsync(SettingsListQuery query, CancellationToken cancellationToken = default)
    {
        var source = dbContext.ApprovalLevels.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLowerInvariant();
            source = source.Where(x => x.Name.ToLower().Contains(search));
        }
        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            source = source.Where(x => x.Status == query.Status);
        }
        return await PaginateAsync(source.OrderBy(x => x.SequenceOrder), query, MapApprovalLevel, cancellationToken);
    }

    public async Task<ApprovalLevelDto> UpdateApprovalLevelAsync(Guid id, ApprovalLevelRequest request, CancellationToken cancellationToken = default)
    {
        var approvalLevel = await dbContext.ApprovalLevels.FirstOrDefaultAsync(x => x.Id == id, cancellationToken) ?? throw new NotFoundException("Approval level was not found.");
        await EnsureApprovalSequenceUniqueAsync(id, request.SequenceOrder, cancellationToken);
        approvalLevel.Name = request.Name.Trim();
        approvalLevel.SequenceOrder = request.SequenceOrder;
        approvalLevel.Description = request.Description?.Trim();
        approvalLevel.Status = request.Status;
        approvalLevel.IsActive = IsActive(request.Status);
        AddAudit("Settings", "UpdateApprovalLevel", "ApprovalLevel", approvalLevel.Id.ToString());
        await dbContext.SaveChangesAsync(cancellationToken);
        return MapApprovalLevel(approvalLevel);
    }

    public async Task<LeaveTypeDto> CreateLeaveTypeAsync(LeaveTypeRequest request, CancellationToken cancellationToken = default)
    {
        var code = NormalizeCode(request.Code);
        await EnsureLeaveTypeCodeUniqueAsync(null, code, cancellationToken);
        var leaveType = new LeaveType { Code = code, Name = request.Name.Trim(), DefaultDays = request.DefaultDays, RequiresApproval = request.RequiresApproval, Status = request.Status, IsActive = IsActive(request.Status) };
        dbContext.LeaveTypes.Add(leaveType);
        AddAudit("Settings", "CreateLeaveType", "LeaveType", leaveType.Id.ToString());
        await dbContext.SaveChangesAsync(cancellationToken);
        return MapLeaveType(leaveType);
    }

    public async Task<PaginatedResponse<LeaveTypeDto>> ListLeaveTypesAsync(SettingsListQuery query, CancellationToken cancellationToken = default)
    {
        var source = ApplyLeaveTypeFilters(dbContext.LeaveTypes.AsNoTracking(), query.Search, query.Status).OrderBy(x => x.Name);
        return await PaginateAsync(source, query, MapLeaveType, cancellationToken);
    }

    public async Task<LeaveTypeDto> UpdateLeaveTypeAsync(Guid id, LeaveTypeRequest request, CancellationToken cancellationToken = default)
    {
        var leaveType = await dbContext.LeaveTypes.FirstOrDefaultAsync(x => x.Id == id, cancellationToken) ?? throw new NotFoundException("Leave type was not found.");
        var code = NormalizeCode(request.Code);
        await EnsureLeaveTypeCodeUniqueAsync(id, code, cancellationToken);
        leaveType.Code = code;
        leaveType.Name = request.Name.Trim();
        leaveType.DefaultDays = request.DefaultDays;
        leaveType.RequiresApproval = request.RequiresApproval;
        leaveType.Status = request.Status;
        leaveType.IsActive = IsActive(request.Status);
        AddAudit("Settings", "UpdateLeaveType", "LeaveType", leaveType.Id.ToString());
        await dbContext.SaveChangesAsync(cancellationToken);
        return MapLeaveType(leaveType);
    }

    public async Task<PublicHolidayDto> CreatePublicHolidayAsync(PublicHolidayRequest request, CancellationToken cancellationToken = default)
    {
        await ValidateBranchAsync(request.BranchId, cancellationToken);
        await EnsureHolidayUniqueAsync(null, request, cancellationToken);
        var holiday = new PublicHoliday { Name = request.Name.Trim(), HolidayDate = request.HolidayDate, BranchId = request.BranchId, LocationScope = NormalizeNullable(request.LocationScope), Status = request.Status, IsActive = IsActive(request.Status) };
        dbContext.PublicHolidays.Add(holiday);
        AddAudit("Settings", "CreatePublicHoliday", "PublicHoliday", holiday.Id.ToString());
        await dbContext.SaveChangesAsync(cancellationToken);
        return await LoadHolidayDtoAsync(holiday.Id, cancellationToken);
    }

    public async Task<PaginatedResponse<PublicHolidayDto>> ListPublicHolidaysAsync(SettingsListQuery query, CancellationToken cancellationToken = default)
    {
        var source = dbContext.PublicHolidays.AsNoTracking().Include(x => x.Branch).AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLowerInvariant();
            source = source.Where(x => x.Name.ToLower().Contains(search) || (x.LocationScope != null && x.LocationScope.ToLower().Contains(search)));
        }
        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            source = source.Where(x => x.Status == query.Status);
        }
        return await PaginateAsync(source.OrderBy(x => x.HolidayDate).ThenBy(x => x.Name), query, MapPublicHoliday, cancellationToken);
    }

    public async Task<PublicHolidayDto> UpdatePublicHolidayAsync(Guid id, PublicHolidayRequest request, CancellationToken cancellationToken = default)
    {
        var holiday = await dbContext.PublicHolidays.FirstOrDefaultAsync(x => x.Id == id, cancellationToken) ?? throw new NotFoundException("Public holiday was not found.");
        await ValidateBranchAsync(request.BranchId, cancellationToken);
        await EnsureHolidayUniqueAsync(id, request, cancellationToken);
        holiday.Name = request.Name.Trim();
        holiday.HolidayDate = request.HolidayDate;
        holiday.BranchId = request.BranchId;
        holiday.LocationScope = NormalizeNullable(request.LocationScope);
        holiday.Status = request.Status;
        holiday.IsActive = IsActive(request.Status);
        AddAudit("Settings", "UpdatePublicHoliday", "PublicHoliday", holiday.Id.ToString());
        await dbContext.SaveChangesAsync(cancellationToken);
        return await LoadHolidayDtoAsync(holiday.Id, cancellationToken);
    }

    public async Task<PaginatedResponse<SystemParameterDto>> ListSystemParametersAsync(SettingsListQuery query, CancellationToken cancellationToken = default)
    {
        var source = dbContext.SystemParameters.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLowerInvariant();
            source = source.Where(x => x.Key.ToLower().Contains(search) || (x.Description != null && x.Description.ToLower().Contains(search)));
        }
        var totalCount = await source.CountAsync(cancellationToken);
        var pagination = query.ToPagination();
        var entities = await source.OrderBy(x => x.Key).Skip(pagination.Skip).Take(pagination.Take).ToListAsync(cancellationToken);
        var items = entities.Select(MapSystemParameter).ToList();
        return new PaginatedResponse<SystemParameterDto>(items, pagination.PageNumber, pagination.Take, totalCount);
    }

    public async Task<SystemParameterDto> UpdateSystemParameterAsync(string key, SystemParameterRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedKey = key.Trim();
        var parameter = await dbContext.SystemParameters.FirstOrDefaultAsync(x => x.Key == normalizedKey, cancellationToken)
            ?? throw new NotFoundException("System parameter was not found.");
        ValidateParameterValue(parameter.DataType, request.Value);
        parameter.Value = request.Value.Trim();
        AddAudit("Settings", "UpdateSystemParameter", "SystemParameter", parameter.Id.ToString());
        await dbContext.SaveChangesAsync(cancellationToken);
        return MapSystemParameter(parameter);
    }

    private async Task<PublicHolidayDto> LoadHolidayDtoAsync(Guid id, CancellationToken cancellationToken)
    {
        var holiday = await dbContext.PublicHolidays.AsNoTracking().Include(x => x.Branch).FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Public holiday was not found.");
        return MapPublicHoliday(holiday);
    }

    private static async Task<PaginatedResponse<TDto>> PaginateAsync<TEntity, TDto>(IQueryable<TEntity> source, SettingsListQuery query, Func<TEntity, TDto> mapper, CancellationToken cancellationToken)
    {
        var totalCount = await source.CountAsync(cancellationToken);
        var pagination = query.ToPagination();
        var entities = await source.Skip(pagination.Skip).Take(pagination.Take).ToListAsync(cancellationToken);
        var items = entities.Select(mapper).ToList();
        return new PaginatedResponse<TDto>(items, pagination.PageNumber, pagination.Take, totalCount);
    }

    private static IQueryable<GradeLevel> ApplyGradeLevelFilters(IQueryable<GradeLevel> source, string? search, string? status)
    {
        if (!string.IsNullOrWhiteSpace(search))
        {
            var value = search.Trim().ToLowerInvariant();
            source = source.Where(x => x.Code.ToLower().Contains(value) || x.Name.ToLower().Contains(value));
        }
        if (!string.IsNullOrWhiteSpace(status))
        {
            source = source.Where(x => x.Status == status);
        }
        return source;
    }

    private static IQueryable<EmploymentType> ApplyEmploymentTypeFilters(IQueryable<EmploymentType> source, string? search, string? status)
    {
        if (!string.IsNullOrWhiteSpace(search))
        {
            var value = search.Trim().ToLowerInvariant();
            source = source.Where(x => x.Code.ToLower().Contains(value) || x.Name.ToLower().Contains(value));
        }
        if (!string.IsNullOrWhiteSpace(status))
        {
            source = source.Where(x => x.Status == status);
        }
        return source;
    }

    private static IQueryable<LeaveType> ApplyLeaveTypeFilters(IQueryable<LeaveType> source, string? search, string? status)
    {
        if (!string.IsNullOrWhiteSpace(search))
        {
            var value = search.Trim().ToLowerInvariant();
            source = source.Where(x => x.Code.ToLower().Contains(value) || x.Name.ToLower().Contains(value));
        }
        if (!string.IsNullOrWhiteSpace(status))
        {
            source = source.Where(x => x.Status == status);
        }
        return source;
    }

    private async Task EnsureGradeLevelCodeUniqueAsync(Guid? currentId, string code, CancellationToken cancellationToken)
    {
        if (await dbContext.GradeLevels.AnyAsync(x => x.Id != currentId && x.Code == code, cancellationToken))
        {
            throw new BusinessRuleException("Grade level code already exists.");
        }
    }

    private async Task EnsureEmploymentTypeCodeUniqueAsync(Guid? currentId, string code, CancellationToken cancellationToken)
    {
        if (await dbContext.EmploymentTypes.AnyAsync(x => x.Id != currentId && x.Code == code, cancellationToken))
        {
            throw new BusinessRuleException("Employment type code already exists.");
        }
    }

    private async Task EnsureLeaveTypeCodeUniqueAsync(Guid? currentId, string code, CancellationToken cancellationToken)
    {
        if (await dbContext.LeaveTypes.AnyAsync(x => x.Id != currentId && x.Code == code, cancellationToken))
        {
            throw new BusinessRuleException("Leave type code already exists.");
        }
    }

    private async Task EnsureApprovalSequenceUniqueAsync(Guid? currentId, int sequenceOrder, CancellationToken cancellationToken)
    {
        if (await dbContext.ApprovalLevels.AnyAsync(x => x.Id != currentId && x.SequenceOrder == sequenceOrder, cancellationToken))
        {
            throw new BusinessRuleException("Approval level sequence order already exists.");
        }
    }

    private async Task EnsureHolidayUniqueAsync(Guid? currentId, PublicHolidayRequest request, CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        var scope = NormalizeNullable(request.LocationScope);
        if (await dbContext.PublicHolidays.AnyAsync(x =>
                x.Id != currentId &&
                x.Name == name &&
                x.HolidayDate == request.HolidayDate &&
                x.BranchId == request.BranchId &&
                x.LocationScope == scope,
                cancellationToken))
        {
            throw new BusinessRuleException("A public holiday already exists for the selected name, date, and location.");
        }
    }

    private async Task ValidateBranchAsync(Guid? branchId, CancellationToken cancellationToken)
    {
        if (branchId is not null && !await dbContext.Branches.AnyAsync(x => x.Id == branchId && x.IsActive, cancellationToken))
        {
            throw new BusinessRuleException("Selected branch is invalid.");
        }
    }

    private static async Task EnsureCanDeactivateAsync(string status, Task<bool> isUsedTask, string message)
    {
        if (status == "Inactive" && await isUsedTask)
        {
            throw new BusinessRuleException(message);
        }
    }

    private static void ValidateParameterValue(string dataType, string value)
    {
        var normalized = dataType.Trim().ToLowerInvariant();
        var isValid = normalized switch
        {
            "string" => true,
            "int" or "integer" => int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _),
            "decimal" => decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out _),
            "bool" or "boolean" => bool.TryParse(value, out _),
            "date" => DateOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out _),
            "datetime" => DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out _),
            _ => true
        };
        if (!isValid)
        {
            throw new BusinessRuleException($"Value is invalid for data type '{dataType}'.");
        }
    }

    private void AddAudit(string module, string action, string entityType, string? entityId)
    {
        dbContext.AuditLogs.Add(new AuditLog { ActorUserId = currentUser.UserId, Module = module, Action = action, EntityType = entityType, EntityId = entityId, Outcome = "Success" });
    }

    private static string NormalizeCode(string code) => code.Trim().ToUpperInvariant();
    private static string? NormalizeNullable(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static bool IsActive(string status) => status == "Active";

    private static CompanyProfileDto MapCompanyProfile(CompanyProfile x) => new(x.Id, x.Name, x.RegistrationNumber, x.LogoPath, x.Address, x.ContactEmail, x.Phone, x.CreatedAt, x.UpdatedAt);
    private static GradeLevelDto MapGradeLevel(GradeLevel x) => new(x.Id, x.Code, x.Name, x.RankOrder, x.Status, x.IsActive, x.CreatedAt, x.UpdatedAt);
    private static EmploymentTypeDto MapEmploymentType(EmploymentType x) => new(x.Id, x.Code, x.Name, x.Status, x.IsActive, x.CreatedAt, x.UpdatedAt);
    private static ApprovalLevelDto MapApprovalLevel(ApprovalLevel x) => new(x.Id, x.Name, x.SequenceOrder, x.Description, x.Status, x.IsActive, x.CreatedAt, x.UpdatedAt);
    private static LeaveTypeDto MapLeaveType(LeaveType x) => new(x.Id, x.Code, x.Name, x.DefaultDays, x.RequiresApproval, x.Status, x.IsActive, x.CreatedAt, x.UpdatedAt);
    private static PublicHolidayDto MapPublicHoliday(PublicHoliday x) => new(x.Id, x.Name, x.HolidayDate, x.BranchId, x.Branch?.Name, x.LocationScope, x.Status, x.IsActive, x.CreatedAt, x.UpdatedAt);
    private static SystemParameterDto MapSystemParameter(SystemParameter x) => new(x.Id, x.Key, x.IsSensitive ? "********" : x.Value, x.DataType, x.Description, x.IsSensitive, x.CreatedAt, x.UpdatedAt);
}
