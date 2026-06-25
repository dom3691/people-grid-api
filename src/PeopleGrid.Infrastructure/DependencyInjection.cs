using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PeopleGrid.Application.Abstractions;
using PeopleGrid.Application.Features.AuditLogs.Interfaces;
using PeopleGrid.Application.Features.EmployeeDocuments.Interfaces;
using PeopleGrid.Application.Features.Employees.Interfaces;
using PeopleGrid.Application.Features.Organization.Interfaces;
using PeopleGrid.Application.Features.Roles.Interfaces;
using PeopleGrid.Application.Features.Settings.Interfaces;
using PeopleGrid.Application.Features.Users.Interfaces;
using PeopleGrid.Infrastructure.Email;
using PeopleGrid.Infrastructure.Files;
using PeopleGrid.Infrastructure.Identity;
using PeopleGrid.Infrastructure.Persistence;
using PeopleGrid.Infrastructure.Security;
using PeopleGrid.Infrastructure.Seeders;
using PeopleGrid.Infrastructure.Services;
using PeopleGrid.Infrastructure.Tenancy;

namespace PeopleGrid.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection("Jwt"));
        services.Configure<AuthOptions>(configuration.GetSection("Auth"));
        services.AddHttpContextAccessor();

        services.AddDbContext<PlatformDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("PlatformDb")));

        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            var provider = sp.GetRequiredService<ITenantConnectionProvider>();
            options.UseSqlServer(provider.GetTenantConnectionString());
        });

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IApplicationDbContextFactory, ApplicationDbContextFactory>();

        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<ICurrentTenantService, CurrentTenantService>();
        services.AddScoped<ITenantResolver, TenantResolver>();
        services.AddScoped<ITenantConnectionProvider, TenantConnectionProvider>();
        services.AddScoped<ITenantProvisioningService, TenantProvisioningService>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IFileStorageService, LocalFileStorageService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IOrganizationService, OrganizationService>();
        services.AddScoped<ISettingsService, SettingsService>();
        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<IEmployeeService, EmployeeService>();
        services.AddScoped<IEmployeeDocumentService, EmployeeDocumentService>();
        services.AddScoped<RolePermissionSeeder>();

        return services;
    }
}
