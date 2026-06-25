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
    public const string AttendanceView = "Attendance.View";
    public const string AttendanceClock = "Attendance.Clock";
    public const string AttendanceManage = "Attendance.Manage";
    public const string OnboardingView = "Onboarding.View";
    public const string OnboardingManage = "Onboarding.Manage";
    public const string DisciplinaryView = "Disciplinary.View";
    public const string DisciplinaryManage = "Disciplinary.Manage";
    public const string DisciplinaryRespond = "Disciplinary.Respond";
    public const string ExitView = "Exit.View";
    public const string ExitManage = "Exit.Manage";
    public const string PayrollView = "Payroll.View";
    public const string PayrollManage = "Payroll.Manage";
    public const string LeaveApply = "Leave.Apply";
    public const string LeaveApprove = "Leave.Approve";
    public const string PayrollProcess = "Payroll.Process";
    public const string PerformanceView = "Performance.View";
    public const string PerformanceManage = "Performance.Manage";
    public const string RecruitmentView = "Recruitment.View";
    public const string RecruitmentManage = "Recruitment.Manage";
    public const string TrainingView = "Training.View";
    public const string TrainingManage = "Training.Manage";
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
        AttendanceView, AttendanceClock, AttendanceManage,
        OnboardingView, OnboardingManage,
        DisciplinaryView, DisciplinaryManage, DisciplinaryRespond,
        ExitView, ExitManage,
        LeaveApply, LeaveApprove, PayrollView, PayrollManage, PayrollProcess,
        PerformanceView, PerformanceManage, RecruitmentView, RecruitmentManage, TrainingView, TrainingManage,
        ReportExport,
        SettingsManage, AuditView, AuditExport,
        "User.View", "User.Create", "User.Edit", "User.Deactivate",
        "Role.Manage", "Permission.Manage", "Department.Manage",
        "Notification.View", "Tenant.Manage"
    ];
}
