using HrLite.Application.DTOs;
using HrLite.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HrLite.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmployeesController : ControllerBase
{
    private readonly IEmployeeService _service;

    public EmployeesController(IEmployeeService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        return Ok(await _service.GetAllAsync());
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateEmployeeDto dto)
    {
        try
        {
            var result = await _service.CreateAsync(dto);
            return Ok(result);
        }
        catch (Exception ex)
        {
            // Email hatasını yakalayıp kullanıcıya düzgün mesaj dönüyoruz
            return BadRequest(new { message = ex.Message }); 
        }
    }

    // AI Tetikleme Endpoint'i
    [HttpPost("{id}/ai/job-description")]
    public async Task<IActionResult> GenerateJobDescription(int id)
    {
        try 
        {
            var result = await _service.GenerateAiJobDescriptionAsync(id);
            return Ok(new { EmployeeId = id, JobDescription = result });
        }
        catch(Exception ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}