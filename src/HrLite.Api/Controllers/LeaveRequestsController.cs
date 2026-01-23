using HrLite.Application.DTOs;
using HrLite.Application.DTOs.Leave;
using HrLite.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HrLite.Api.Controllers;

[ApiController]
[Route("api/leave-requests")]
[Authorize]
public class LeaveRequestsController : ControllerBase
{
    private readonly ILeaveRequestsService _service;

    public LeaveRequestsController(ILeaveRequestsService service)
    {
        _service = service;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResultDto<LeaveRequestDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] LeaveRequestQueryParameters query)
    {
        return Ok(await _service.GetAsync(query));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(LeaveRequestDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(int id)
    {
        return Ok(await _service.GetByIdAsync(id));
    }

    [HttpPost]
    [ProducesResponseType(typeof(LeaveRequestDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Create([FromBody] CreateLeaveRequestDto dto)
    {
        return Ok(await _service.CreateAsync(dto));
    }

    [HttpPost("{id:int}/approve")]
    [ProducesResponseType(typeof(LeaveRequestDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Approve(int id)
    {
        return Ok(await _service.ApproveAsync(id));
    }

    [HttpPost("{id:int}/reject")]
    [ProducesResponseType(typeof(LeaveRequestDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Reject(int id, [FromBody] RejectLeaveRequestDto dto)
    {
        return Ok(await _service.RejectAsync(id, dto));
    }

    [HttpPost("{id:int}/cancel")]
    [ProducesResponseType(typeof(LeaveRequestDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Cancel(int id)
    {
        return Ok(await _service.CancelAsync(id));
    }

    [HttpPost("ai/normalize-reason")]
    [ProducesResponseType(typeof(NormalizeLeaveReasonResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> NormalizeReason([FromBody] NormalizeLeaveReasonRequestDto dto)
    {
        return Ok(await _service.NormalizeReasonAsync(dto));
    }

    [HttpPost("{id:int}/ai/explain-decision")]
    [ProducesResponseType(typeof(ExplainDecisionResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExplainDecision(int id)
    {
        return Ok(await _service.ExplainDecisionAsync(id));
    }
}
