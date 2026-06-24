using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PeopleGrid.Shared.Responses;

namespace PeopleGrid.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class TenantsController : ControllerBase
{
    [HttpGet]
    public ActionResult<ApiResponse<object>> Get()
    {
        return Ok(ApiResponse<object>.Ok(new { Module = "Tenants", Status = "Ready" }));
    }
}
