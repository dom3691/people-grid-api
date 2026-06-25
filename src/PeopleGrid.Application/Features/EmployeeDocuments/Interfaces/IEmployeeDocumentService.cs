using PeopleGrid.Application.Features.EmployeeDocuments.DTOs;

namespace PeopleGrid.Application.Features.EmployeeDocuments.Interfaces;

public interface IEmployeeDocumentService
{
    Task<EmployeeDocumentDto> UploadAsync(Guid employeeId, UploadEmployeeDocumentRequest request, Stream fileStream, string fileName, string contentType, long fileSize, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<EmployeeDocumentDto>> ListEmployeeDocumentsAsync(Guid employeeId, CancellationToken cancellationToken = default);
    Task<EmployeeDocumentDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<DocumentDownloadDto> DownloadAsync(Guid id, CancellationToken cancellationToken = default);
    Task ArchiveAsync(Guid id, CancellationToken cancellationToken = default);
    Task<EmployeeDocumentDto> VerifyAsync(Guid id, VerifyDocumentRequest request, CancellationToken cancellationToken = default);
    Task<EmployeeDocumentDto> RejectAsync(Guid id, RejectDocumentRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<EmployeeDocumentDto>> ListExpiringAsync(ExpiringDocumentsQuery query, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<DocumentTypeDto>> ListDocumentTypesAsync(CancellationToken cancellationToken = default);
}
