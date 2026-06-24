using FluentAssertions;
using PeopleGrid.Application.Security;

namespace PeopleGrid.UnitTests;

public sealed class PermissionConstantsTests
{
    [Fact]
    public void DefaultPermissions_ShouldUseModuleActionFormat()
    {
        PermissionConstants.All.Should().OnlyContain(x => x.Contains('.'));
    }
}
