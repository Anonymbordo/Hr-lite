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

    /// <summary>
    /// Çalışan listesi - Sayfalama, filtreleme (isActive, departmentId, search) ve sıralama ile
    /// </summary>
    /// <param name="query">Filtreleme ve sayfalama parametreleri</param>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResultDto<EmployeeDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] EmployeeQueryParameters query)
    {
        return Ok(await _service.GetAsync(query));
    }

    /// <summary>
    /// Çalışan detayı - ID ile (manager/department bilgileriyle)
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(EmployeeDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(int id)
    {
        return Ok(await _service.GetByIdAsync(id));
    }

    /// <summary>
    /// Yeni çalışan oluştur - Email tekilliği ve manager validasyonu ile
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateEmployeeDto dto)
    {
        var result = await _service.CreateAsync(dto);
        return Ok(result);
    }

    /// <summary>
    /// Çalışan güncelle - Self-manager kontrolü ve email tekilliği ile
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateEmployeeDto dto)
    {
        var result = await _service.UpdateAsync(id, dto);
        return Ok(result);
    }

    /// <summary>
    /// Çalışanı pasifleştir (IsActive=false)
    /// </summary>
    [HttpPut("{id}/deactivate")]
    public async Task<IActionResult> Deactivate(int id)
    {
        await _service.DeactivateAsync(id);
        return NoContent();
    }

    // AI Tetikleme Endpoint'i
    /// <summary>
    /// AI ile iş tanımı üret - Çalışanın departman ve rol bilgisinden JSON şemalı job description taslağı
    /// </summary>
    /// <remarks>
    /// PII göndermeden, sadece rol ve departman bilgisiyle AI üzerinden yapılandırılmış iş tanımı üretir.
    /// Dönen şema: titleSuggested, responsibilities[], requirements[], niceToHave[], jobDescription.
    /// Taslak Employee.JobDescriptionDraft alanına JSON olarak kaydedilir.
    /// </remarks>
    [HttpPost("{id}/ai/job-description")]
    [ProducesResponseType(typeof(JobDescriptionDraftDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> GenerateJobDescription(int id)
    {
        try
        {
            var result = await _service.GenerateAiJobDescriptionAsync(id);
            return Ok(new { employeeId = id, draft = result });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new { message = "AI servis hatası", detail = ex.Message });
        }
    }
}