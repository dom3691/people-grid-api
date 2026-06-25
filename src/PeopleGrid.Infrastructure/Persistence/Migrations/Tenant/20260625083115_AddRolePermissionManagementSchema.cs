using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PeopleGrid.Infrastructure.Persistence.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class AddRolePermissionManagementSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RolePermissions_Permissions_PermissionId",
                table: "RolePermissions");

            migrationBuilder.DropForeignKey(
                name: "FK_RolePermissions_Roles_RoleId",
                table: "RolePermissions");

            migrationBuilder.DropForeignKey(
                name: "FK_UserRoles_Roles_RoleId",
                table: "UserRoles");

            migrationBuilder.DropForeignKey(
                name: "FK_UserRoles_Users_UserId",
                table: "UserRoles");

            migrationBuilder.AlterColumn<string>(
                name: "AssignedBy",
                table: "UserRoles",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Roles",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Roles",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "Roles",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Roles",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Active");

            migrationBuilder.AddColumn<DateTime>(
                name: "AssignedAt",
                table: "RolePermissions",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<string>(
                name: "AssignedBy",
                table: "RolePermissions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Module",
                table: "Permissions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Permissions",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "Permissions",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "Action",
                table: "Permissions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_Status",
                table: "Roles",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_Module_Action",
                table: "Permissions",
                columns: new[] { "Module", "Action" });

            migrationBuilder.AddForeignKey(
                name: "FK_RolePermissions_Permissions_PermissionId",
                table: "RolePermissions",
                column: "PermissionId",
                principalTable: "Permissions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RolePermissions_Roles_RoleId",
                table: "RolePermissions",
                column: "RoleId",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserRoles_Roles_RoleId",
                table: "UserRoles",
                column: "RoleId",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserRoles_Users_UserId",
                table: "UserRoles",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql("""
                DECLARE @Now datetime2 = SYSUTCDATETIME();
                DECLARE @SystemUser nvarchar(100) = N'SYSTEM';

                IF NOT EXISTS (SELECT 1 FROM Roles WHERE Code = N'SUPERADMIN')
                    INSERT INTO Roles (Id, Name, Code, Description, IsSystemRole, Status, IsActive, CreatedAt, CreatedBy, IsDeleted)
                    VALUES ('11111111-1111-1111-1111-111111111111', N'Super Admin', N'SUPERADMIN', N'Super Admin system role', 1, N'Active', 1, @Now, @SystemUser, 0);

                IF NOT EXISTS (SELECT 1 FROM Roles WHERE Code = N'HRADMIN')
                    INSERT INTO Roles (Id, Name, Code, Description, IsSystemRole, Status, IsActive, CreatedAt, CreatedBy, IsDeleted)
                    VALUES ('22222222-2222-2222-2222-222222222222', N'HR Admin', N'HRADMIN', N'HR Admin system role', 1, N'Active', 1, @Now, @SystemUser, 0);

                IF NOT EXISTS (SELECT 1 FROM Roles WHERE Code = N'MANAGER')
                    INSERT INTO Roles (Id, Name, Code, Description, IsSystemRole, Status, IsActive, CreatedAt, CreatedBy, IsDeleted)
                    VALUES ('33333333-3333-3333-3333-333333333333', N'Manager', N'MANAGER', N'Manager system role', 1, N'Active', 1, @Now, @SystemUser, 0);

                IF NOT EXISTS (SELECT 1 FROM Roles WHERE Code = N'EMPLOYEE')
                    INSERT INTO Roles (Id, Name, Code, Description, IsSystemRole, Status, IsActive, CreatedAt, CreatedBy, IsDeleted)
                    VALUES ('44444444-4444-4444-4444-444444444444', N'Employee', N'EMPLOYEE', N'Employee system role', 1, N'Active', 1, @Now, @SystemUser, 0);

                IF NOT EXISTS (SELECT 1 FROM Roles WHERE Code = N'FINANCE')
                    INSERT INTO Roles (Id, Name, Code, Description, IsSystemRole, Status, IsActive, CreatedAt, CreatedBy, IsDeleted)
                    VALUES ('55555555-5555-5555-5555-555555555555', N'Finance', N'FINANCE', N'Finance system role', 1, N'Active', 1, @Now, @SystemUser, 0);

                IF NOT EXISTS (SELECT 1 FROM Roles WHERE Code = N'AUDITOR')
                    INSERT INTO Roles (Id, Name, Code, Description, IsSystemRole, Status, IsActive, CreatedAt, CreatedBy, IsDeleted)
                    VALUES ('66666666-6666-6666-6666-666666666666', N'Auditor', N'AUDITOR', N'Auditor system role', 1, N'Active', 1, @Now, @SystemUser, 0);

                DECLARE @Permissions TABLE (Id uniqueidentifier, Code nvarchar(150), Module nvarchar(100), Action nvarchar(100), Description nvarchar(500));

                INSERT INTO @Permissions (Id, Code, Module, Action, Description)
                VALUES
                    ('aaaaaaaa-0001-0000-0000-000000000001', N'Employee.View', N'Employee', N'View', N'Employee.View'),
                    ('aaaaaaaa-0002-0000-0000-000000000002', N'Employee.Create', N'Employee', N'Create', N'Employee.Create'),
                    ('aaaaaaaa-0003-0000-0000-000000000003', N'Employee.Edit', N'Employee', N'Edit', N'Employee.Edit'),
                    ('aaaaaaaa-0004-0000-0000-000000000004', N'Employee.Deactivate', N'Employee', N'Deactivate', N'Employee.Deactivate'),
                    ('aaaaaaaa-0005-0000-0000-000000000005', N'Leave.Apply', N'Leave', N'Apply', N'Leave.Apply'),
                    ('aaaaaaaa-0006-0000-0000-000000000006', N'Leave.Approve', N'Leave', N'Approve', N'Leave.Approve'),
                    ('aaaaaaaa-0007-0000-0000-000000000007', N'Payroll.Process', N'Payroll', N'Process', N'Payroll.Process'),
                    ('aaaaaaaa-0008-0000-0000-000000000008', N'Report.Export', N'Report', N'Export', N'Report.Export'),
                    ('aaaaaaaa-0009-0000-0000-000000000009', N'Settings.Manage', N'Settings', N'Manage', N'Settings.Manage'),
                    ('aaaaaaaa-0010-0000-0000-000000000010', N'Audit.View', N'Audit', N'View', N'Audit.View'),
                    ('aaaaaaaa-0011-0000-0000-000000000011', N'User.View', N'User', N'View', N'User.View'),
                    ('aaaaaaaa-0012-0000-0000-000000000012', N'User.Create', N'User', N'Create', N'User.Create'),
                    ('aaaaaaaa-0013-0000-0000-000000000013', N'User.Edit', N'User', N'Edit', N'User.Edit'),
                    ('aaaaaaaa-0014-0000-0000-000000000014', N'User.Deactivate', N'User', N'Deactivate', N'User.Deactivate'),
                    ('aaaaaaaa-0015-0000-0000-000000000015', N'Role.Manage', N'Role', N'Manage', N'Role.Manage'),
                    ('aaaaaaaa-0016-0000-0000-000000000016', N'Permission.Manage', N'Permission', N'Manage', N'Permission.Manage'),
                    ('aaaaaaaa-0017-0000-0000-000000000017', N'Department.Manage', N'Department', N'Manage', N'Department.Manage'),
                    ('aaaaaaaa-0018-0000-0000-000000000018', N'Approval.Manage', N'Approval', N'Manage', N'Approval.Manage'),
                    ('aaaaaaaa-0019-0000-0000-000000000019', N'Notification.View', N'Notification', N'View', N'Notification.View'),
                    ('aaaaaaaa-0020-0000-0000-000000000020', N'Tenant.Manage', N'Tenant', N'Manage', N'Tenant.Manage');

                INSERT INTO Permissions (Id, Code, Module, Action, Description, CreatedAt, CreatedBy)
                SELECT p.Id, p.Code, p.Module, p.Action, p.Description, @Now, @SystemUser
                FROM @Permissions p
                WHERE NOT EXISTS (SELECT 1 FROM Permissions existing WHERE existing.Code = p.Code);

                INSERT INTO RolePermissions (Id, RoleId, PermissionId, AssignedBy, AssignedAt, CreatedAt, CreatedBy)
                SELECT NEWID(), r.Id, p.Id, @SystemUser, @Now, @Now, @SystemUser
                FROM Roles r
                CROSS JOIN Permissions p
                WHERE r.Code = N'SUPERADMIN'
                  AND p.Code IN (SELECT Code FROM @Permissions)
                  AND NOT EXISTS (
                      SELECT 1
                      FROM RolePermissions rp
                      WHERE rp.RoleId = r.Id AND rp.PermissionId = p.Id
                  );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RolePermissions_Permissions_PermissionId",
                table: "RolePermissions");

            migrationBuilder.DropForeignKey(
                name: "FK_RolePermissions_Roles_RoleId",
                table: "RolePermissions");

            migrationBuilder.DropForeignKey(
                name: "FK_UserRoles_Roles_RoleId",
                table: "UserRoles");

            migrationBuilder.DropForeignKey(
                name: "FK_UserRoles_Users_UserId",
                table: "UserRoles");

            migrationBuilder.DropIndex(
                name: "IX_Roles_Status",
                table: "Roles");

            migrationBuilder.DropIndex(
                name: "IX_Permissions_Module_Action",
                table: "Permissions");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "AssignedAt",
                table: "RolePermissions");

            migrationBuilder.DropColumn(
                name: "AssignedBy",
                table: "RolePermissions");

            migrationBuilder.AlterColumn<string>(
                name: "AssignedBy",
                table: "UserRoles",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Roles",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(150)",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Roles",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "Roles",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Module",
                table: "Permissions",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Permissions",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "Permissions",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(150)",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "Action",
                table: "Permissions",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AddForeignKey(
                name: "FK_RolePermissions_Permissions_PermissionId",
                table: "RolePermissions",
                column: "PermissionId",
                principalTable: "Permissions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RolePermissions_Roles_RoleId",
                table: "RolePermissions",
                column: "RoleId",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserRoles_Roles_RoleId",
                table: "UserRoles",
                column: "RoleId",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserRoles_Users_UserId",
                table: "UserRoles",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
