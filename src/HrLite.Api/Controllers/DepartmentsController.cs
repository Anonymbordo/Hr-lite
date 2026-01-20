using HrLite.Application.DTOs;
using HrLite.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HrLite.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DepartmentsController : ControllerBase
{
    private readonly IDepartmentService _service;

    // DbContext YOK! Sadece Interface var.
    public DepartmentsController(IDepartmentService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        return Ok(await _service.GetAllAsync());
    }

    [HttpPost]
    public async Task<IActionResult> Create(DepartmentDto dto)
    {
        var result = await _service.CreateAsync(dto);
        return Ok(result);
    }
}