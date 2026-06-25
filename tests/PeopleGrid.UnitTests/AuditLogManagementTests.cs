using FluentAssertions;
using PeopleGrid.Application.Features.AuditLogs.DTOs;
using PeopleGrid.Application.Features.AuditLogs.Validators;
using PeopleGrid.Application.Security;

namespace PeopleGrid.UnitTests;

public sealed class AuditLogManagementTests
{
    [Fact]
    public void AuditLogQueryValidator_ShouldRejectInvalidDateRange()
    {
        var validator = new AuditLogQueryValidator();
        var request = new AuditLogQuery(null, null, DateTime.UtcNow, DateTime.UtcNow.AddDays(-1), null, null, null, null, null, null, null);

        var result = validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(AuditLogQuery.DateTo));
    }

    [Fact]
    public void AuditLogExportQueryValidator_ShouldRejectUnsupportedFormat()
    {
        var validator = new AuditLogExportQueryValidator();
        var request = new AuditLogExportQuery(null, null, null, null, null, null, null, null, null, null, null, "pdf");

        var result = validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(AuditLogExportQuery.Format));
    }

    [Fact]
    public void PermissionConstants_ShouldIncludeAuditExport()
    {
        PermissionConstants.All.Should().Contain(PermissionConstants.AuditExport);
    }
}
