using Microsoft.AspNetCore.Http;
using PeopleGrid.Application.Abstractions;

namespace PeopleGrid.Infrastructure.Identity;

public sealed class CurrentUserService(IHttpContextAccessor accessor) : ICurrentUserService
{
    public string? UserId => accessor.HttpContext?.User.Claims.FirstOrDefault(x => x.Type == "UserId")?.Value;
    public string? Email => accessor.HttpContext?.User.Claims.FirstOrDefault(x => x.Type == "Email")?.Value;
    public IReadOnlyCollection<string> Roles => accessor.HttpContext?.User.Claims.Where(x => x.Type == "Roles").Select(x => x.Value).ToArray() ?? [];
    public IReadOnlyCollection<string> Permissions => accessor.HttpContext?.User.Claims.Where(x => x.Type == "Permissions").Select(x => x.Value).ToArray() ?? [];
    public bool IsAuthenticated => accessor.HttpContext?.User.Identity?.IsAuthenticated == true;
}
