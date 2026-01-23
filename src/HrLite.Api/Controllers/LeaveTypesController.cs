using HrLite.Application.DTOs.Leave;
using HrLite.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HrLite.Api.Controllers;

[ApiController]
[Route("api/leave-types")]
[Authorize]
public class LeaveTypesController : ControllerBase
{
    private readonly ILeaveTypesService _service;

    public LeaveTypesController(ILeaveTypesService service)
    {
        _service = service;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<LeaveTypeDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        return Ok(await _service.GetAllAsync());
    }
}
