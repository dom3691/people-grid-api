using Microsoft.EntityFrameworkCore;
using PeopleGrid.Application.Abstractions;
using PeopleGrid.Application.Features.EmployeeDocuments.DTOs;
using PeopleGrid.Application.Features.EmployeeDocuments.Interfaces;
using PeopleGrid.Domain.Entities;
using PeopleGrid.Shared.Exceptions;

namespace PeopleGrid.Infrastructure.Services;

public sealed class EmployeeDocumentService(IApplicationDbContext dbContext, ICurrentUserService currentUser, IFileStorageService fileStorage) : IEmployeeDocumentService
{
    public async Task<EmployeeDocumentDto> UploadAsync(Guid employeeId, UploadEmployeeDocumentRequest request, Stream fileStream, string fileName, string contentType, long fileSize, CancellationToken cancellationToken = default)
    {
        var employee = await dbContext.Employees.AsNoTracking().FirstOrDefaultAsync(x => x.Id == employeeId, cancellationToken)
            ?? throw new NotFoundException("Employee was not found.");
        var documentType = await dbContext.DocumentTypes.Include(x => x.AccessRules).ThenInclude(x => x.Role).FirstOrDefaultAsync(x => x.Id == request.DocumentTypeId && x.IsActive, cancellationToken)
            ?? throw new BusinessRuleException("Selected document type is invalid.");

        EnsureCanAccessEmployee(employee);
        ValidateFilePolicy(documentType, fileName, contentType, fileSize);
        if (documentType.RequiresExpiry && request.ExpiryDate is null)
        {
            throw new BusinessRuleException("Expiry date is required for this document type.");
        }

        var storageKey = await fileStorage.SaveAsync(fileStream, fileName, contentType, cancellationToken);
        var document = new EmployeeDocument
        {
            EmployeeId = employeeId,
            DocumentTypeId = documentType.Id,
            LegacyDocumentType = documentType.Code,
            Title = request.Title.Trim(),
            FileName = Path.GetFileName(fileName),
            StorageKey = storageKey,
            IssueDate = request.IssueDate,
            ExpiryDate = request.ExpiryDate,
            VerificationStatus = documentType.RequiresVerification ? "Pending" : "Not Required",
            ConfidentialFlag = request.ConfidentialFlag || documentType.ConfidentialityLevel != "Internal",
            IsArchived = false
        };

        dbContext.EmployeeDocuments.Add(document);
        dbContext.DocumentStorageReferences.Add(new DocumentStorageReference
        {
            DocumentId = document.Id,
            StorageProvider = "Local",
            ContainerName = "employee-documents",
            BlobKey = storageKey,
            FileSize = fileSize,
            ContentType = contentType,
            UploadedAt = DateTime.UtcNow
        });
        AddAudit("EmployeeDocuments", "Upload", document.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(document.Id, cancellationToken);
    }

    public async Task<IReadOnlyCollection<EmployeeDocumentDto>> ListEmployeeDocumentsAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        var employee = await dbContext.Employees.AsNoTracking().FirstOrDefaultAsync(x => x.Id == employeeId, cancellationToken)
            ?? throw new NotFoundException("Employee was not found.");
        EnsureCanAccessEmployee(employee);

        var documents = await LoadDocuments()
            .Where(x => x.EmployeeId == employeeId && !x.IsArchived)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
        return documents.Select(MapDocument).ToList();
    }

    public async Task<EmployeeDocumentDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var document = await LoadDocuments().FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Document was not found.");
        EnsureCanAccessDocument(document);
        return MapDocument(document);
    }

    public async Task<DocumentDownloadDto> DownloadAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var document = await LoadDocuments().FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Document was not found.");
        EnsureCanAccessDocument(document);
        var storageKey = document.StorageReference?.BlobKey ?? document.StorageKey;
        var stream = await fileStorage.OpenReadAsync(storageKey, cancellationToken);
        AddAudit("EmployeeDocuments", "Download", document.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new DocumentDownloadDto(stream, document.FileName, document.StorageReference?.ContentType ?? "application/octet-stream");
    }

    public async Task ArchiveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var document = await dbContext.EmployeeDocuments.Include(x => x.Employee).Include(x => x.DocumentType).FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Document was not found.");
        EnsureCanAccessDocument(document);
        document.IsArchived = true;
        document.IsDeleted = true;
        document.DeletedAt = DateTime.UtcNow;
        document.DeletedBy = currentUser.UserId;
        AddAudit("EmployeeDocuments", "Archive", document.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<EmployeeDocumentDto> VerifyAsync(Guid id, VerifyDocumentRequest request, CancellationToken cancellationToken = default)
    {
        return await SetVerificationStatusAsync(id, "Verified", request.Comments, cancellationToken);
    }

    public async Task<EmployeeDocumentDto> RejectAsync(Guid id, RejectDocumentRequest request, CancellationToken cancellationToken = default)
    {
        return await SetVerificationStatusAsync(id, "Rejected", request.Comments, cancellationToken);
    }

    public async Task<IReadOnlyCollection<EmployeeDocumentDto>> ListExpiringAsync(ExpiringDocumentsQuery query, CancellationToken cancellationToken = default)
    {
        var endDate = DateTime.UtcNow.Date.AddDays(Math.Clamp(query.Days, 1, 365));
        var documents = await LoadDocuments()
            .Where(x => !x.IsArchived && x.ExpiryDate != null && x.ExpiryDate.Value.Date <= endDate)
            .Where(x => query.EmployeeId == null || x.EmployeeId == query.EmployeeId)
            .Where(x => query.DocumentTypeId == null || x.DocumentTypeId == query.DocumentTypeId)
            .OrderBy(x => x.ExpiryDate)
            .ToListAsync(cancellationToken);

        return documents.Where(CanAccessDocument).Select(MapDocument).ToList();
    }

    public async Task<IReadOnlyCollection<DocumentTypeDto>> ListDocumentTypesAsync(CancellationToken cancellationToken = default)
    {
        var types = await dbContext.DocumentTypes.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Name).ToListAsync(cancellationToken);
        return types.Select(MapDocumentType).ToList();
    }

    private async Task<EmployeeDocumentDto> SetVerificationStatusAsync(Guid id, string status, string? comments, CancellationToken cancellationToken)
    {
        EnsureVerifier();
        var document = await dbContext.EmployeeDocuments.Include(x => x.Employee).Include(x => x.DocumentType).FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("Document was not found.");
        document.VerificationStatus = status;
        dbContext.DocumentVerificationHistories.Add(new DocumentVerificationHistory
        {
            DocumentId = document.Id,
            Status = status,
            Comments = comments?.Trim(),
            VerifiedBy = TryGetCurrentUserId(),
            VerifiedAt = DateTime.UtcNow
        });
        AddAudit("EmployeeDocuments", status, document.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(id, cancellationToken);
    }

    private IQueryable<EmployeeDocument> LoadDocuments() => dbContext.EmployeeDocuments.AsNoTracking()
        .Include(x => x.Employee)
        .Include(x => x.DocumentType)
        .Include(x => x.StorageReference);

    private void ValidateFilePolicy(DocumentType documentType, string fileName, string contentType, long fileSize)
    {
        if (fileSize <= 0)
        {
            throw new BusinessRuleException("File is required.");
        }
        if (fileSize > documentType.MaxFileSize)
        {
            throw new BusinessRuleException("File size exceeds the allowed limit.");
        }

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var allowed = documentType.AllowedExtensions.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(x => x.StartsWith('.') ? x.ToLowerInvariant() : "." + x.ToLowerInvariant())
            .ToArray();
        if (!allowed.Contains(extension))
        {
            throw new BusinessRuleException("File extension is not allowed for this document type.");
        }
        if (string.IsNullOrWhiteSpace(contentType))
        {
            throw new BusinessRuleException("File content type is required.");
        }
    }

    private void EnsureCanAccessEmployee(Employee employee)
    {
        if (HasDocumentAdminAccess() || employee.UserId == TryGetCurrentUserId())
        {
            return;
        }
        throw new ForbiddenException("You are not allowed to access this employee document.");
    }

    private void EnsureCanAccessDocument(EmployeeDocument document)
    {
        if (!CanAccessDocument(document))
        {
            throw new ForbiddenException("You are not allowed to access this document.");
        }
    }

    private bool CanAccessDocument(EmployeeDocument document)
    {
        if (HasDocumentAdminAccess() || document.Employee?.UserId == TryGetCurrentUserId())
        {
            return true;
        }

        if (document.DocumentType?.AccessRules is null || document.DocumentType.AccessRules.Count == 0)
        {
            return false;
        }

        return document.DocumentType.AccessRules.Any(x => x.Role != null && currentUser.Roles.Contains(x.Role.Name) && (x.AccessLevel == "View" || x.AccessLevel == "Manage"));
    }

    private bool HasDocumentAdminAccess() => currentUser.Permissions.Contains("EmployeeDocument.Manage") || currentUser.Permissions.Contains("EmployeeDocument.View") || currentUser.Permissions.Contains("Employee.Edit");
    private void EnsureVerifier()
    {
        if (!currentUser.Permissions.Contains("EmployeeDocument.Verify") && !currentUser.Permissions.Contains("EmployeeDocument.Manage"))
        {
            throw new ForbiddenException("Document verification permission is required.");
        }
    }

    private Guid? TryGetCurrentUserId() => Guid.TryParse(currentUser.UserId, out var id) ? id : null;
    private void AddAudit(string module, string action, Guid documentId) => dbContext.AuditLogs.Add(new AuditLog { ActorUserId = currentUser.UserId, Module = module, Action = action, EntityType = "EmployeeDocument", EntityId = documentId.ToString(), Outcome = "Success" });

    private static EmployeeDocumentDto MapDocument(EmployeeDocument x) => new(x.Id, x.EmployeeId, x.DocumentTypeId, x.DocumentType?.Name, x.Title, x.FileName, x.IssueDate, x.ExpiryDate, x.VerificationStatus, x.ConfidentialFlag, x.IsArchived, x.StorageReference?.FileSize, x.StorageReference?.ContentType, x.StorageReference?.UploadedAt);
    private static DocumentTypeDto MapDocumentType(DocumentType x) => new(x.Id, x.Code, x.Name, x.AllowedExtensions, x.MaxFileSize, x.RequiresExpiry, x.RequiresVerification, x.ConfidentialityLevel, x.Status);
}
