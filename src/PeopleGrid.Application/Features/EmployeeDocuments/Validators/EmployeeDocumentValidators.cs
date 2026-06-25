using FluentValidation;
using PeopleGrid.Application.Features.EmployeeDocuments.DTOs;

namespace PeopleGrid.Application.Features.EmployeeDocuments.Validators;

public sealed class UploadEmployeeDocumentRequestValidator : AbstractValidator<UploadEmployeeDocumentRequest>
{
    public UploadEmployeeDocumentRequestValidator()
    {
        RuleFor(x => x.DocumentTypeId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x).Must(x => x.IssueDate is null || x.ExpiryDate is null || x.IssueDate <= DateOnly.FromDateTime(x.ExpiryDate.Value))
            .WithMessage("Expiry date cannot be earlier than issue date.");
    }
}

public sealed class VerifyDocumentRequestValidator : AbstractValidator<VerifyDocumentRequest>
{
    public VerifyDocumentRequestValidator()
    {
        RuleFor(x => x.Comments).MaximumLength(1000);
    }
}

public sealed class RejectDocumentRequestValidator : AbstractValidator<RejectDocumentRequest>
{
    public RejectDocumentRequestValidator()
    {
        RuleFor(x => x.Comments).NotEmpty().MaximumLength(1000);
    }
}
