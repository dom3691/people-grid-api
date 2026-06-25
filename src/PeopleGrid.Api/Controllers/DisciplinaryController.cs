using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PeopleGrid.Api.Security;
using PeopleGrid.Application.Features.Disciplinary.DTOs;
using PeopleGrid.Application.Features.Disciplinary.Interfaces;
using PeopleGrid.Shared.Responses;

namespace PeopleGrid.Api.Controllers;

[ApiController]
[Route("api/disciplinary")]
[Authorize]
public sealed class DisciplinaryController(IDisciplinaryService disciplinaryService) : ControllerBase
{
    [HttpPost("cases")][HasPermission("Disciplinary.Manage")] public async Task<ActionResult<ApiResponse<DisciplinaryCaseDto>>> Create(CreateDisciplinaryCaseRequest request, CancellationToken ct) => Ok(ApiResponse<DisciplinaryCaseDto>.Ok(await disciplinaryService.CreateCaseAsync(request, ct), "Disciplinary case created"));
    [HttpGet("cases")][HasPermission("Disciplinary.View")] public async Task<ActionResult<ApiResponse<PaginatedResponse<DisciplinaryCaseDto>>>> List([FromQuery] DisciplinaryCaseQuery query, CancellationToken ct) => Ok(ApiResponse<PaginatedResponse<DisciplinaryCaseDto>>.Ok(await disciplinaryService.ListCasesAsync(query, ct)));
    [HttpGet("cases/{id:guid}")][HasPermission("Disciplinary.View")] public async Task<ActionResult<ApiResponse<DisciplinaryCaseDto>>> Detail(Guid id, CancellationToken ct) => Ok(ApiResponse<DisciplinaryCaseDto>.Ok(await disciplinaryService.GetCaseAsync(id, ct)));
    [HttpPost("cases/{id:guid}/respond")][HasPermission("Disciplinary.Respond")] public async Task<ActionResult<ApiResponse<DisciplinaryCaseDto>>> Respond(Guid id, DisciplinaryResponseRequest request, CancellationToken ct) => Ok(ApiResponse<DisciplinaryCaseDto>.Ok(await disciplinaryService.RespondAsync(id, request, ct), "Response submitted"));
    [HttpPost("cases/{id:guid}/review")][HasPermission("Disciplinary.Manage")] public async Task<ActionResult<ApiResponse<DisciplinaryCaseDto>>> Review(Guid id, DisciplinaryReviewRequest request, CancellationToken ct) => Ok(ApiResponse<DisciplinaryCaseDto>.Ok(await disciplinaryService.ReviewAsync(id, request, ct), "Case reviewed"));
    [HttpPost("cases/{id:guid}/warning-letter")][HasPermission("Disciplinary.Manage")] public async Task<ActionResult<ApiResponse<DisciplinaryCaseDto>>> Warning(Guid id, WarningLetterRequest request, CancellationToken ct) => Ok(ApiResponse<DisciplinaryCaseDto>.Ok(await disciplinaryService.IssueWarningAsync(id, request, ct), "Warning letter issued"));
    [HttpPost("cases/{id:guid}/suspension")][HasPermission("Disciplinary.Manage")] public async Task<ActionResult<ApiResponse<DisciplinaryCaseDto>>> Suspension(Guid id, SuspensionRequest request, CancellationToken ct) => Ok(ApiResponse<DisciplinaryCaseDto>.Ok(await disciplinaryService.RecordSuspensionAsync(id, request, ct), "Suspension recorded"));
    [HttpPost("cases/{id:guid}/escalate")][HasPermission("Disciplinary.Manage")] public async Task<ActionResult<ApiResponse<DisciplinaryCaseDto>>> Escalate(Guid id, EscalateDisciplinaryRequest request, CancellationToken ct) => Ok(ApiResponse<DisciplinaryCaseDto>.Ok(await disciplinaryService.EscalateAsync(id, request, ct), "Case escalated"));
    [HttpPost("cases/{id:guid}/close")][HasPermission("Disciplinary.Manage")] public async Task<ActionResult<ApiResponse<DisciplinaryCaseDto>>> Close(Guid id, CloseDisciplinaryCaseRequest request, CancellationToken ct) => Ok(ApiResponse<DisciplinaryCaseDto>.Ok(await disciplinaryService.CloseAsync(id, request, ct), "Case closed"));
    [HttpGet("employees/{employeeId:guid}/history")][HasPermission("Disciplinary.View")] public async Task<ActionResult<ApiResponse<IReadOnlyCollection<DisciplinaryCaseDto>>>> History(Guid employeeId, CancellationToken ct) => Ok(ApiResponse<IReadOnlyCollection<DisciplinaryCaseDto>>.Ok(await disciplinaryService.EmployeeHistoryAsync(employeeId, ct)));
}
