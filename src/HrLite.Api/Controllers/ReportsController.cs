using HrLite.Application.DTOs.Reports;
using HrLite.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HrLite.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "HR,Admin")]
public class ReportsController : ControllerBase
{
    private readonly IReportsService _reportsService;

    public ReportsController(IReportsService reportsService)
    {
        _reportsService = reportsService;
    }

    /// <summary>
    /// Get employee headcount grouped by department
    /// </summary>
    /// <returns>List of departments with employee counts</returns>
    /// <response code="200">Success</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden - Only HR and Admin can access</response>
    [HttpGet("headcount-by-department")]
    [ProducesResponseType(typeof(List<HeadcountByDepartmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetHeadcountByDepartment()
    {
        var result = await _reportsService.GetHeadcountByDepartmentAsync();
        return Ok(result);
    }

    /// <summary>
    /// Get monthly leave requests statistics for a specific year
    /// </summary>
    /// <param name="year">Year for the report (e.g., 2026)</param>
    /// <returns>Monthly leave request statistics</returns>
    /// <response code="200">Success</response>
    /// <response code="400">Invalid year parameter</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden - Only HR and Admin can access</response>
    [HttpGet("leave-requests-monthly")]
    [ProducesResponseType(typeof(List<LeaveRequestsMonthlyDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetLeaveRequestsMonthly([FromQuery] int year = 2026)
    {
        var result = await _reportsService.GetLeaveRequestsMonthlyAsync(year);
        return Ok(result);
    }

    /// <summary>
    /// Get AI-powered insights for HR data (aggregated)
    /// </summary>
    /// <param name="year">Year for analysis (e.g., 2026)</param>
    /// <returns>AI-generated summary, insights, and recommended actions</returns>
    /// <response code="200">Success</response>
    /// <response code="400">Invalid year parameter</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden - Only HR and Admin can access</response>
    /// <response code="409">AI service error or timeout</response>
    [HttpPost("ai/insights")]
    [ProducesResponseType(typeof(AiInsightsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> GetAiInsights([FromQuery] int year = 2026)
    {
        var result = await _reportsService.GetAiInsightsAsync(year);
        return Ok(result);
    }
}
