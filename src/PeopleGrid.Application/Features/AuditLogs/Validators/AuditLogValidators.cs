using FluentValidation;
using PeopleGrid.Application.Features.AuditLogs.DTOs;

namespace PeopleGrid.Application.Features.AuditLogs.Validators;

public sealed class AuditLogQueryValidator : AbstractValidator<AuditLogQuery>
{
    public AuditLogQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200);
        RuleFor(x => x.DateTo)
            .GreaterThanOrEqualTo(x => x.DateFrom)
            .When(x => x.DateFrom.HasValue && x.DateTo.HasValue)
            .WithMessage("DateTo must be greater than or equal to DateFrom.");
    }
}

public sealed class AuditLogExportQueryValidator : AbstractValidator<AuditLogExportQuery>
{
    private static readonly string[] SupportedFormats = ["csv", "excel", "xlsx"];

    public AuditLogExportQueryValidator()
    {
        RuleFor(x => x.Format)
            .NotEmpty()
            .Must(x => SupportedFormats.Contains(x.Trim().ToLowerInvariant()))
            .WithMessage("Export format must be csv, excel, or xlsx.");

        RuleFor(x => x.DateTo)
            .GreaterThanOrEqualTo(x => x.DateFrom)
            .When(x => x.DateFrom.HasValue && x.DateTo.HasValue)
            .WithMessage("DateTo must be greater than or equal to DateFrom.");
    }
}
