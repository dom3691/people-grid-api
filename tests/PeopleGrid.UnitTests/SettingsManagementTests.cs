using FluentAssertions;
using PeopleGrid.Application.Features.Settings.DTOs;
using PeopleGrid.Application.Features.Settings.Validators;

namespace PeopleGrid.UnitTests;

public sealed class SettingsManagementTests
{
    [Fact]
    public void CompanyProfileValidator_ShouldRejectInvalidEmail()
    {
        var validator = new CompanyProfileRequestValidator();
        var request = new CompanyProfileRequest("PeopleGrid", null, null, null, "not-email", null);

        var result = validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(CompanyProfileRequest.ContactEmail));
    }

    [Fact]
    public void ApprovalLevelValidator_ShouldRequirePositiveSequence()
    {
        var validator = new ApprovalLevelRequestValidator();
        var request = new ApprovalLevelRequest("Line Manager", 0, null, "Active");

        var result = validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(ApprovalLevelRequest.SequenceOrder));
    }

    [Fact]
    public void LeaveTypeValidator_ShouldRejectNegativeDefaultDays()
    {
        var validator = new LeaveTypeRequestValidator();
        var request = new LeaveTypeRequest("ANNUAL", "Annual Leave", -1, true, "Active");

        var result = validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(LeaveTypeRequest.DefaultDays));
    }
}
