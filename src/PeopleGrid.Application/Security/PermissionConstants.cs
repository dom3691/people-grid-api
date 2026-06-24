namespace PeopleGrid.Application.Security;

public static class PermissionConstants
{
    public const string EmployeeView = "Employee.View";
    public const string EmployeeCreate = "Employee.Create";
    public const string EmployeeEdit = "Employee.Edit";
    public const string EmployeeDeactivate = "Employee.Deactivate";
    public const string LeaveApply = "Leave.Apply";
    public const string LeaveApprove = "Leave.Approve";
    public const string PayrollProcess = "Payroll.Process";
    public const string ReportExport = "Report.Export";
    public const string SettingsManage = "Settings.Manage";
    public const string AuditView = "Audit.View";

    public static readonly string[] All =
    [
        EmployeeView, EmployeeCreate, EmployeeEdit, EmployeeDeactivate,
        LeaveApply, LeaveApprove, PayrollProcess, ReportExport,
        SettingsManage, AuditView,
        "User.View", "User.Create", "User.Edit", "User.Deactivate",
        "Role.Manage", "Permission.Manage", "Department.Manage",
        "Approval.Manage", "Notification.View", "Tenant.Manage"
    ];
}
