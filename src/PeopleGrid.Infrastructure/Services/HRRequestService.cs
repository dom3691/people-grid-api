using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using PeopleGrid.Application.Abstractions;
using PeopleGrid.Application.Features.Approvals.DTOs;
using PeopleGrid.Application.Features.Approvals.Interfaces;
using PeopleGrid.Application.Features.HRRequests.DTOs;
using PeopleGrid.Application.Features.HRRequests.Interfaces;
using PeopleGrid.Domain.Entities;
using PeopleGrid.Shared.Exceptions;
using PeopleGrid.Shared.Responses;

namespace PeopleGrid.Infrastructure.Services;

public sealed class HRRequestService(IApplicationDbContext dbContext, ICurrentUserService currentUser, IFileStorageService fileStorage, IApprovalWorkflowService approvalWorkflow) : IHRRequestService
{
    private static readonly string[] AllowedAttachmentExtensions = [".pdf", ".doc", ".docx", ".jpg", ".jpeg", ".png"];

    public async Task<HRRequestDto> CreateAsync(CreateHRRequestRequest request, CancellationToken cancellationToken = default)
    {
        var employee = await dbContext.Employees.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.EmployeeId, cancellationToken)
            ?? throw new NotFoundException("Employee was not found.");
        EnsureCanActForEmployee(employee);
        var type = await dbContext.HRRequestTypes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.RequestTypeId && x.IsActive, cancellationToken)
            ?? throw new BusinessRuleException("Selected request type is invalid.");

        var hrRequest = new HRRequest
        {
            RequestNumber = await GenerateRequestNumberAsync(cancellationToken),
            RequestTypeId = type.Id,
            RequestType = type.Code,
            EmployeeId = employee.Id,
            Subject = string.IsNullOrWhiteSpace(request.Subject) ? type.Name : request.Subject.Trim(),
            Description = request.Description?.Trim(),
            Priority = string.IsNullOrWhiteSpace(request.Priority) ? "Normal" : request.Priority.Trim(),
            RequestDataJson = request.RequestDataJson,
            Status = "Draft"
        };
        dbContext.HRRequests.Add(hrRequest);
        AddHistory(hrRequest.Id, string.Empty, "Draft", "Request created");
        AddAudit("HRRequests", "Create", hrRequest.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(hrRequest.Id, cancellationToken);
    }

    public async Task<PaginatedResponse<HRRequestListItemDto>> ListAsync(HRRequestListQuery query, CancellationToken cancellationToken = default)
    {
        var source = ApplyFilters(LoadList(), query);
        var total = await source.CountAsync(cancellationToken);
        var page = query.ToPagination();
        var requests = await source.OrderByDescending(x => x.CreatedAt).Skip(page.Skip).Take(page.Take).ToListAsync(cancellationToken);
        return new PaginatedResponse<HRRequestListItemDto>(requests.Select(MapListItem).ToList(), page.PageNumber, page.Take, total);
    }

    public async Task<PaginatedResponse<HRRequestListItemDto>> ListMyAsync(HRRequestListQuery query, CancellationToken cancellationToken = default)
    {
        var userId = CurrentUserGuidOrNull();
        var employee = await dbContext.Employees.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken)
            ?? throw new NotFoundException("Employee profile was not found for current user.");
        query = query with { EmployeeId = employee.Id };
        return await ListAsync(query, cancellationToken);
    }

    public async Task<HRRequestDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var request = await LoadDetails().AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("HR request was not found.");
        EnsureCanView(request);
        return MapDetails(request);
    }

    public async Task<HRRequestDto> UpdateDraftAsync(Guid id, UpdateHRRequestRequest request, CancellationToken cancellationToken = default)
    {
        var hrRequest = await dbContext.HRRequests.Include(x => x.Employee).FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("HR request was not found.");
        if (hrRequest.Status != "Draft") throw new BusinessRuleException("Only draft requests can be edited.");
        EnsureOwner(hrRequest);
        hrRequest.Subject = string.IsNullOrWhiteSpace(request.Subject) ? hrRequest.Subject : request.Subject.Trim();
        hrRequest.Description = request.Description?.Trim();
        hrRequest.Priority = string.IsNullOrWhiteSpace(request.Priority) ? hrRequest.Priority : request.Priority.Trim();
        hrRequest.RequestDataJson = request.RequestDataJson;
        AddAudit("HRRequests", "UpdateDraft", hrRequest.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<HRRequestDto> SubmitAsync(Guid id, TransitionHRRequestRequest request, CancellationToken cancellationToken = default)
    {
        var hrRequest = await dbContext.HRRequests.Include(x => x.Employee).Include(x => x.RequestTypeDefinition).FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("HR request was not found.");
        if (hrRequest.Status != "Draft") throw new BusinessRuleException("Only draft requests can be submitted.");
        EnsureOwner(hrRequest);
        ValidateSubmission(hrRequest);

        await using var transaction = await BeginTransactionAsync(cancellationToken);
        var newStatus = hrRequest.RequestTypeDefinition!.RequiresApproval ? "Pending Approval" : "Submitted";
        ChangeStatus(hrRequest, newStatus, request.Comments);
        hrRequest.SubmittedAt = DateTime.UtcNow;
        AddAudit("HRRequests", "Submit", hrRequest.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        if (hrRequest.RequestTypeDefinition.RequiresApproval)
        {
            await approvalWorkflow.CreateInstanceAsync(new CreateApprovalInstanceRequest(hrRequest.Id), cancellationToken);
        }
        if (transaction is not null) await transaction.CommitAsync(cancellationToken);
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<HRRequestDto> CancelAsync(Guid id, TransitionHRRequestRequest request, CancellationToken cancellationToken = default)
    {
        var hrRequest = await dbContext.HRRequests.Include(x => x.Employee).FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("HR request was not found.");
        if (hrRequest.Status is "Approved" or "Completed" or "Cancelled") throw new BusinessRuleException("Request cannot be cancelled from its current status.");
        if (!IsHrAdmin()) EnsureOwner(hrRequest);
        ChangeStatus(hrRequest, "Cancelled", request.Comments);
        AddAudit("HRRequests", "Cancel", hrRequest.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<HRRequestDto> CompleteAsync(Guid id, TransitionHRRequestRequest request, CancellationToken cancellationToken = default)
    {
        if (!IsHrAdmin()) throw new ForbiddenException("Only HR administrators can complete requests.");
        var hrRequest = await dbContext.HRRequests.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("HR request was not found.");
        if (hrRequest.Status != "Approved" && hrRequest.Status != "Submitted") throw new BusinessRuleException("Only approved/submitted requests can be completed.");
        ChangeStatus(hrRequest, "Completed", request.Comments);
        hrRequest.CompletedAt = DateTime.UtcNow;
        AddAudit("HRRequests", "Complete", hrRequest.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<IReadOnlyCollection<HRRequestStatusHistoryDto>> GetHistoryAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (!await dbContext.HRRequests.AnyAsync(x => x.Id == id, cancellationToken)) throw new NotFoundException("HR request was not found.");
        return await dbContext.HRRequestStatusHistories.AsNoTracking()
            .Where(x => x.RequestId == id).OrderBy(x => x.ChangedAt)
            .Select(x => new HRRequestStatusHistoryDto(x.Id, x.OldStatus, x.NewStatus, x.Comments, x.ChangedBy, x.ChangedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<HRRequestAttachmentDto> UploadAttachmentAsync(Guid id, Stream fileStream, string fileName, string contentType, long fileSize, CancellationToken cancellationToken = default)
    {
        var hrRequest = await dbContext.HRRequests.Include(x => x.Employee).FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("HR request was not found.");
        EnsureCanView(hrRequest);
        ValidateAttachment(fileName, contentType, fileSize);
        var storageKey = await fileStorage.SaveAsync(fileStream, fileName, contentType, cancellationToken);
        var attachment = new HRRequestAttachment { RequestId = id, FileName = Path.GetFileName(fileName), StorageKey = storageKey, FileSize = fileSize, ContentType = contentType, UploadedBy = CurrentUserGuidOrNull() };
        dbContext.HRRequestAttachments.Add(attachment);
        AddAudit("HRRequests", "UploadAttachment", id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new HRRequestAttachmentDto(attachment.Id, attachment.FileName, attachment.FileSize, attachment.ContentType, attachment.UploadedBy, attachment.CreatedAt);
    }

    private void ValidateSubmission(HRRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Description)) throw new BusinessRuleException("Description is required before submission.");
        var requiredFields = ParseRequiredFields(request.RequestTypeDefinition?.RequiredFieldsJson);
        if (requiredFields.Count == 0) return;
        using var doc = string.IsNullOrWhiteSpace(request.RequestDataJson) ? null : JsonDocument.Parse(request.RequestDataJson);
        foreach (var field in requiredFields)
        {
            if (doc is null || !doc.RootElement.TryGetProperty(field, out var value) || value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined || (value.ValueKind == JsonValueKind.String && string.IsNullOrWhiteSpace(value.GetString())))
                throw new BusinessRuleException($"Required field '{field}' is missing.");
        }
    }

    private static IReadOnlyCollection<string> ParseRequiredFields(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try { return JsonSerializer.Deserialize<string[]>(json) ?? []; }
        catch { return []; }
    }

    private void ChangeStatus(HRRequest request, string newStatus, string? comments)
    {
        var allowed = (request.Status, newStatus) switch
        {
            ("Draft", "Submitted") or ("Draft", "Pending Approval") or ("Draft", "Cancelled") => true,
            ("Submitted", "Completed") or ("Submitted", "Cancelled") => true,
            ("Pending Approval", "Approved") or ("Pending Approval", "Rejected") or ("Pending Approval", "Cancelled") => true,
            ("Approved", "Completed") => true,
            _ => false
        };
        if (!allowed) throw new BusinessRuleException($"Status cannot transition from {request.Status} to {newStatus}.");
        AddHistory(request.Id, request.Status, newStatus, comments);
        request.Status = newStatus;
    }

    private IQueryable<HRRequest> ApplyFilters(IQueryable<HRRequest> source, HRRequestListQuery query)
    {
        if (query.RequestTypeId is not null) source = source.Where(x => x.RequestTypeId == query.RequestTypeId);
        if (!string.IsNullOrWhiteSpace(query.Status)) source = source.Where(x => x.Status == query.Status);
        if (query.EmployeeId is not null) source = source.Where(x => x.EmployeeId == query.EmployeeId);
        if (query.DepartmentId is not null) source = source.Where(x => x.Employee != null && x.Employee.EmploymentInfo != null && x.Employee.EmploymentInfo.DepartmentId == query.DepartmentId);
        if (query.FromDate is not null) source = source.Where(x => x.CreatedAt >= query.FromDate);
        if (query.ToDate is not null) source = source.Where(x => x.CreatedAt <= query.ToDate);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLowerInvariant();
            source = source.Where(x => x.RequestNumber.ToLower().Contains(search) || x.Subject.ToLower().Contains(search));
        }
        return source;
    }

    private IQueryable<HRRequest> LoadList() => dbContext.HRRequests.AsNoTracking()
        .Include(x => x.RequestTypeDefinition)
        .Include(x => x.Employee).ThenInclude(x => x!.PersonalInfo)
        .Include(x => x.Employee).ThenInclude(x => x!.EmploymentInfo);

    private IQueryable<HRRequest> LoadDetails() => LoadList().Include(x => x.Attachments);
    private void EnsureCanActForEmployee(Employee employee) { if (!IsHrAdmin() && employee.UserId != CurrentUserGuidOrNull()) throw new ForbiddenException("You cannot create requests for this employee."); }
    private void EnsureOwner(HRRequest request) { if (request.Employee?.UserId != CurrentUserGuidOrNull()) throw new ForbiddenException("Only the request owner can perform this action."); }
    private void EnsureCanView(HRRequest request) { if (!IsHrAdmin() && request.Employee?.UserId != CurrentUserGuidOrNull()) throw new ForbiddenException("You are not allowed to view this request."); }
    private bool IsHrAdmin() => currentUser.Permissions.Contains("HRRequest.Manage") || currentUser.Permissions.Contains("Approval.Manage") || currentUser.Roles.Contains("HR Admin");
    private Guid? CurrentUserGuidOrNull() => Guid.TryParse(currentUser.UserId, out var id) ? id : null;
    private void AddHistory(Guid requestId, string oldStatus, string newStatus, string? comments) => dbContext.HRRequestStatusHistories.Add(new HRRequestStatusHistory { RequestId = requestId, OldStatus = oldStatus, NewStatus = newStatus, Comments = comments?.Trim(), ChangedBy = CurrentUserGuidOrNull(), ChangedAt = DateTime.UtcNow });
    private void AddAudit(string module, string action, Guid requestId) => dbContext.AuditLogs.Add(new AuditLog { ActorUserId = currentUser.UserId, Module = module, Action = action, EntityType = "HRRequest", EntityId = requestId.ToString(), Outcome = "Success" });
    private async Task<IDbContextTransaction?> BeginTransactionAsync(CancellationToken cancellationToken) => dbContext is DbContext ef && ef.Database.CurrentTransaction is null ? await ef.Database.BeginTransactionAsync(cancellationToken) : null;
    private async Task<string> GenerateRequestNumberAsync(CancellationToken cancellationToken) { var count = await dbContext.HRRequests.IgnoreQueryFilters().CountAsync(cancellationToken) + 1; string number; do { number = $"HRR-{DateTime.UtcNow:yyyyMMdd}-{count:00000}"; count++; } while (await dbContext.HRRequests.IgnoreQueryFilters().AnyAsync(x => x.RequestNumber == number, cancellationToken)); return number; }
    private static void ValidateAttachment(string fileName, string contentType, long fileSize) { if (fileSize <= 0) throw new BusinessRuleException("File is required."); if (fileSize > 5 * 1024 * 1024) throw new BusinessRuleException("File size exceeds the allowed limit."); if (!AllowedAttachmentExtensions.Contains(Path.GetExtension(fileName).ToLowerInvariant())) throw new BusinessRuleException("File extension is not allowed."); if (string.IsNullOrWhiteSpace(contentType)) throw new BusinessRuleException("File content type is required."); }
    private static HRRequestListItemDto MapListItem(HRRequest x) => new(x.Id, x.RequestNumber, x.RequestTypeDefinition?.Name ?? x.RequestType, x.EmployeeId, x.Employee?.PersonalInfo is null ? null : $"{x.Employee.PersonalInfo.FirstName} {x.Employee.PersonalInfo.LastName}".Trim(), x.Subject, x.Status, x.Priority, x.SubmittedAt, x.CreatedAt);
    private static HRRequestDto MapDetails(HRRequest x) => new(x.Id, x.RequestNumber, x.RequestTypeId, x.RequestTypeDefinition?.Name ?? x.RequestType, x.EmployeeId, x.Employee?.EmployeeNumber, x.Employee?.PersonalInfo is null ? null : $"{x.Employee.PersonalInfo.FirstName} {x.Employee.PersonalInfo.LastName}".Trim(), x.Subject, x.Description, x.Status, x.Priority, x.RequestDataJson, x.SubmittedAt, x.CompletedAt, x.CreatedAt, x.Attachments.Select(a => new HRRequestAttachmentDto(a.Id, a.FileName, a.FileSize, a.ContentType, a.UploadedBy, a.CreatedAt)).ToList());
}
