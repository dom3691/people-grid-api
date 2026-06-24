using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PeopleGrid.Shared.Responses;

namespace PeopleGrid.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class PermissionsController : ControllerBase
{
    [HttpGet]
    public ActionResult<ApiResponse<object>> Get()
    {
        return Ok(ApiResponse<object>.Ok(new { Module = "Permissions", Status = "Ready" }));
    }
}
