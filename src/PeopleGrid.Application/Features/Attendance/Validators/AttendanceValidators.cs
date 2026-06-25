using FluentValidation;
using PeopleGrid.Application.Features.Attendance.DTOs;

namespace PeopleGrid.Application.Features.Attendance.Validators;

public sealed class ClockEventRequestValidator : AbstractValidator<ClockEventRequest>
{
    public ClockEventRequestValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.EventTime).NotEmpty();
        RuleFor(x => x.Source).NotEmpty().MaximumLength(50);
    }
}

public sealed class AttendanceCorrectionRequestDtoValidator : AbstractValidator<AttendanceCorrectionRequestDto>
{
    public AttendanceCorrectionRequestDtoValidator()
    {
        RuleFor(x => x.AttendanceRecordId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(1000);
        RuleFor(x => x).Must(x => x.RequestedClockIn is not null || x.RequestedClockOut is not null).WithMessage("Corrected clock-in or clock-out is required.");
    }
}

public sealed class ImportAttendanceEventRequestValidator : AbstractValidator<ImportAttendanceEventRequest>
{
    public ImportAttendanceEventRequestValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.EventTime).NotEmpty();
        RuleFor(x => x.EventType).Must(x => x is "ClockIn" or "ClockOut").WithMessage("Event type is invalid.");
        RuleFor(x => x.Source).NotEmpty().MaximumLength(50);
    }
}
