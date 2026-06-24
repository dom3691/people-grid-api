namespace PeopleGrid.Application.Security;

public static class RoleConstants
{
    public const string SuperAdmin = "Super Admin";
    public const string HrAdmin = "HR Admin";
    public const string Manager = "Manager";
    public const string Employee = "Employee";
    public const string Finance = "Finance";
    public const string Auditor = "Auditor";

    public static readonly string[] DefaultRoles = [SuperAdmin, HrAdmin, Manager, Employee, Finance, Auditor];
}
