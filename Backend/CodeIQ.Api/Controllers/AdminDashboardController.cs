using CodeIQ.Application.Abstractions;
using CodeIQ.Application.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CodeIQ.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/dashboard")]
public sealed class AdminDashboardController : ControllerBase
{
    private readonly IAdminDashboardService _dashboard;

    public AdminDashboardController(IAdminDashboardService dashboard)
    {
        _dashboard = dashboard;
    }

    [HttpGet("overview")]
    public async Task<ActionResult<AdminDashboardOverviewDto>> Overview(CancellationToken cancellationToken)
    {
        return Ok(await _dashboard.GetOverviewAsync(cancellationToken));
    }
}
