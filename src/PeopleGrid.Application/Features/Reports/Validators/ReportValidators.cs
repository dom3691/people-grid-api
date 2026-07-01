using FluentValidation;
using PeopleGrid.Application.Features.Reports.DTOs;

namespace PeopleGrid.Application.Features.Reports.Validators;

public sealed class ReportQueryValidator : AbstractValidator<ReportQuery>
{
    public ReportQueryValidator()
    {
        RuleFor(x => x).Must(x => x.FromDate is null || x.ToDate is null || x.FromDate <= x.ToDate)
            .WithMessage("Date range start must be before or equal to end date.");
        RuleFor(x => x.PageNumber).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 500);
    }
}

public sealed class ReportExportRequestValidator : AbstractValidator<ReportExportRequest>
{
    public ReportExportRequestValidator()
    {
        RuleFor(x => x.ReportCode).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Format).Must(x => x is "PDF" or "Excel").WithMessage("Export format must be PDF or Excel.");
        RuleFor(x => x.Filters).NotNull().SetValidator(new ReportQueryValidator());
    }
}

