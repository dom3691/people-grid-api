using Microsoft.AspNetCore.Mvc;
using PeopleGrid.Application.Auth.DTOs;
using PeopleGrid.Infrastructure.Services;
using PeopleGrid.Shared.Responses;

namespace PeopleGrid.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var response = await authService.LoginAsync(request, cancellationToken);
        return Ok(ApiResponse<LoginResponse>.Ok(response, "Login successful"));
    }
}
