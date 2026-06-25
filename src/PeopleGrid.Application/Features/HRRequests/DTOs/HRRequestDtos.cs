using PeopleGrid.Shared.Pagination;

namespace PeopleGrid.Application.Features.HRRequests.DTOs;

public sealed record HRRequestListQuery(
    Guid? RequestTypeId,
    string? Status,
    Guid? EmployeeId,
    Guid? DepartmentId,
    DateTime? FromDate,
    DateTime? ToDate,
    string? Search,
    int PageNumber = 1,
    int PageSize = 10)
{
    public PaginationRequest ToPagination() => new(PageNumber, PageSize);
}

public sealed record CreateHRRequestRequest(Guid RequestTypeId, Guid EmployeeId, string? Subject, string? Description, string? Priority, string? RequestDataJson);
public sealed record UpdateHRRequestRequest(string? Subject, string? Description, string? Priority, string? RequestDataJson);
public sealed record TransitionHRRequestRequest(string? Comments);
public sealed record HRRequestAttachmentUploadRequest(string? Title);

public sealed record HRRequestDto(
    Guid Id,
    string RequestNumber,
    Guid? RequestTypeId,
    string? RequestType,
    Guid EmployeeId,
    string? EmployeeNumber,
    string? EmployeeName,
    string Subject,
    string? Description,
    string Status,
    string Priority,
    string? RequestDataJson,
    DateTime? SubmittedAt,
    DateTime? CompletedAt,
    DateTime CreatedAt,
    IReadOnlyCollection<HRRequestAttachmentDto> Attachments);

public sealed record HRRequestListItemDto(Guid Id, string RequestNumber, string? RequestType, Guid EmployeeId, string? EmployeeName, string Subject, string Status, string Priority, DateTime? SubmittedAt, DateTime CreatedAt);
public sealed record HRRequestTypeDto(Guid Id, string Code, string Name, bool RequiresApproval, string? RequiredFieldsJson, bool IsActive);
public sealed record HRRequestAttachmentDto(Guid Id, string FileName, long FileSize, string ContentType, Guid? UploadedBy, DateTime CreatedAt);
public sealed record HRRequestStatusHistoryDto(Guid Id, string OldStatus, string NewStatus, string? Comments, Guid? ChangedBy, DateTime ChangedAt);
