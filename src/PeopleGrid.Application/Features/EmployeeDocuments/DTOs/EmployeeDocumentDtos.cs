namespace PeopleGrid.Application.Features.EmployeeDocuments.DTOs;

public sealed record UploadEmployeeDocumentRequest(
    Guid DocumentTypeId,
    string Title,
    DateOnly? IssueDate,
    DateTime? ExpiryDate,
    bool ConfidentialFlag);

public sealed record VerifyDocumentRequest(string? Comments);
public sealed record RejectDocumentRequest(string Comments);

public sealed record ExpiringDocumentsQuery(int Days = 30, Guid? EmployeeId = null, Guid? DocumentTypeId = null);

public sealed record EmployeeDocumentDto(
    Guid Id,
    Guid EmployeeId,
    Guid? DocumentTypeId,
    string? DocumentType,
    string Title,
    string FileName,
    DateOnly? IssueDate,
    DateTime? ExpiryDate,
    string VerificationStatus,
    bool ConfidentialFlag,
    bool IsArchived,
    long? FileSize,
    string? ContentType,
    DateTime? UploadedAt);

public sealed record DocumentTypeDto(
    Guid Id,
    string Code,
    string Name,
    string AllowedExtensions,
    long MaxFileSize,
    bool RequiresExpiry,
    bool RequiresVerification,
    string ConfidentialityLevel,
    string Status);

public sealed record DocumentDownloadDto(Stream Content, string FileName, string ContentType);
