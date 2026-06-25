using FluentAssertions;
using PeopleGrid.Application.Features.Organization.DTOs;
using PeopleGrid.Application.Features.Organization.Validators;

namespace PeopleGrid.UnitTests;

public sealed class OrganizationManagementTests
{
    [Fact]
    public void CreateDepartmentValidator_ShouldRejectInvalidCode()
    {
        var validator = new CreateDepartmentRequestValidator();
        var request = new CreateDepartmentRequest("hr admin", "Human Resources", null, "Active");

        var result = validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(CreateDepartmentRequest.Code));
    }

    [Fact]
    public void CreateUnitValidator_ShouldRequireDepartment()
    {
        var validator = new CreateUnitRequestValidator();
        var request = new CreateUnitRequest(Guid.Empty, "PAYROLL", "Payroll", "Active");

        var result = validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(CreateUnitRequest.DepartmentId));
    }

    [Fact]
    public void AssignManagerValidator_ShouldRejectSelfManagerAssignment()
    {
        var userId = Guid.NewGuid();
        var validator = new AssignManagerRequestValidator();
        var request = new AssignManagerRequest(userId, userId, DateTime.UtcNow);

        var result = validator.Validate(request);

        result.IsValid.Should().BeFalse();
    }
}
