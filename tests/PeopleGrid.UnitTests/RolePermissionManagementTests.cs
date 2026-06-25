using FluentAssertions;
using PeopleGrid.Application.Features.Roles.DTOs;
using PeopleGrid.Application.Features.Roles.Validators;
using PeopleGrid.Application.Security;

namespace PeopleGrid.UnitTests;

public sealed class RolePermissionManagementTests
{
    [Fact]
    public void CreateRoleValidator_ShouldRejectInvalidRoleCode()
    {
        var validator = new CreateRoleRequestValidator();
        var request = new CreateRoleRequest("hr admin", "HR Admin", null, "Active", []);

        var result = validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(CreateRoleRequest.Code));
    }

    [Fact]
    public void AssignRolePermissionsValidator_ShouldRejectDuplicatePermissions()
    {
        var permissionId = Guid.NewGuid();
        var validator = new AssignRolePermissionsRequestValidator();
        var request = new AssignRolePermissionsRequest([permissionId, permissionId]);

        var result = validator.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void DefaultRoles_ShouldContainRequiredEnterpriseRoles()
    {
        RoleConstants.DefaultRoles.Should().Contain([
            RoleConstants.SuperAdmin,
            RoleConstants.HrAdmin,
            RoleConstants.Manager,
            RoleConstants.Employee,
            RoleConstants.Finance,
            RoleConstants.Auditor
        ]);
    }
}
