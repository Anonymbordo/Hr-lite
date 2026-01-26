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

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(LeaveRequestDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(Guid id)
    {
        return Ok(await _service.GetByIdAsync(id));
    }

    [HttpPost]
    [ProducesResponseType(typeof(LeaveRequestDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Create([FromBody] CreateLeaveRequestDto dto)
    {
        return Ok(await _service.CreateAsync(dto));
    }

    [HttpPost("{id:guid}/approve")]
    [ProducesResponseType(typeof(LeaveRequestDto), StatusCodes.Status200OK)]
    [Authorize(Roles = "HR,Admin")]
    public async Task<IActionResult> Approve(Guid id)
    {
        return Ok(await _service.ApproveAsync(id));
    }

    [HttpPost("{id:guid}/reject")]
    [ProducesResponseType(typeof(LeaveRequestDto), StatusCodes.Status200OK)]
    [Authorize(Roles = "HR,Admin")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] RejectLeaveRequestDto dto)
    {
        return Ok(await _service.RejectAsync(id, dto));
    }

    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(typeof(LeaveRequestDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Cancel(Guid id)
    {
        return Ok(await _service.CancelAsync(id));
    }

    [HttpPost("ai/normalize-reason")]
    [ProducesResponseType(typeof(NormalizeLeaveReasonResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> NormalizeReason([FromBody] NormalizeLeaveReasonRequestDto dto)
    {
        return Ok(await _service.NormalizeReasonAsync(dto));
    }

    [HttpPost("{id:guid}/ai/explain-decision")]
    [ProducesResponseType(typeof(ExplainDecisionResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExplainDecision(Guid id)
    {
        return Ok(await _service.ExplainDecisionAsync(id));
    }
}
