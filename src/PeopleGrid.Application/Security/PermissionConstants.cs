namespace PeopleGrid.Application.Security;

public static class PermissionConstants
{
    public const string EmployeeView = "Employee.View";
    public const string EmployeeCreate = "Employee.Create";
    public const string EmployeeEdit = "Employee.Edit";
    public const string EmployeeDeactivate = "Employee.Deactivate";
    public const string EmployeeDocumentView = "EmployeeDocument.View";
    public const string EmployeeDocumentManage = "EmployeeDocument.Manage";
    public const string EmployeeDocumentVerify = "EmployeeDocument.Verify";
    public const string HRRequestView = "HRRequest.View";
    public const string HRRequestCreate = "HRRequest.Create";
    public const string HRRequestManage = "HRRequest.Manage";
    public const string ApprovalApprove = "Approval.Approve";
    public const string ApprovalManage = "Approval.Manage";
    public const string NotificationManage = "Notification.Manage";
    public const string LeaveView = "Leave.View";
    public const string LeaveManage = "Leave.Manage";
    public const string LeaveApply = "Leave.Apply";
    public const string LeaveApprove = "Leave.Approve";
    public const string PayrollProcess = "Payroll.Process";
    public const string ReportExport = "Report.Export";
    public const string SettingsManage = "Settings.Manage";
    public const string AuditView = "Audit.View";
    public const string AuditExport = "Audit.Export";

    public static readonly string[] All =
    [
        EmployeeView, EmployeeCreate, EmployeeEdit, EmployeeDeactivate,
        EmployeeDocumentView, EmployeeDocumentManage, EmployeeDocumentVerify,
        HRRequestView, HRRequestCreate, HRRequestManage, ApprovalApprove, ApprovalManage,
        NotificationManage, LeaveView, LeaveManage,
        LeaveApply, LeaveApprove, PayrollProcess, ReportExport,
        SettingsManage, AuditView, AuditExport,
        "User.View", "User.Create", "User.Edit", "User.Deactivate",
        "Role.Manage", "Permission.Manage", "Department.Manage",
        "Notification.View", "Tenant.Manage"
    ];
}
