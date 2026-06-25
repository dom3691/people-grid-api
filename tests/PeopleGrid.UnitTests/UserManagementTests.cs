using FluentAssertions;
using PeopleGrid.Application.Features.Users.DTOs;
using PeopleGrid.Application.Features.Users.Validators;

namespace PeopleGrid.UnitTests;

public sealed class UserManagementTests
{
    [Fact]
    public void CreateUserValidator_ShouldRejectInvalidEmail()
    {
        var validator = new CreateUserRequestValidator();
        var request = new CreateUserRequest(
            "EMP-001",
            "not-an-email",
            "jane.doe",
            "Jane",
            "Doe",
            null,
            null,
            null,
            null,
            null,
            null,
            [],
            "Password@123");

        var result = validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(CreateUserRequest.Email));
    }

    [Fact]
    public void AssignUserRolesValidator_ShouldRejectDuplicateRoles()
    {
        var roleId = Guid.NewGuid();
        var validator = new AssignUserRolesRequestValidator();
        var request = new AssignUserRolesRequest([roleId, roleId]);

        var result = validator.Validate(request);

        result.IsValid.Should().BeFalse();
    }
}
