using HrLite.Application.DTOs;
using HrLite.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HrLite.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EmployeesController : ControllerBase
{
    private readonly IEmployeeService _service;

    public EmployeesController(IEmployeeService service)
    {
        _service = service;
    }

    /// <summary>
    /// Çalışan listesi - Sayfalama, filtreleme (status, departmentId, search) ve sıralama ile
    /// </summary>
    /// <param name="query">Filtreleme ve sayfalama parametreleri</param>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResultDto<EmployeeDto>), StatusCodes.Status200OK)]
    [Authorize(Roles = "HR,Admin")]
    public async Task<IActionResult> GetAll([FromQuery] EmployeeQueryParameters query)
    {
        return Ok(await _service.GetAsync(query));
    }

    /// <summary>
    /// Çalışan detayı - ID ile (manager/department bilgileriyle)
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(EmployeeDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(Guid id)
    {
        return Ok(await _service.GetByIdAsync(id));
    }

    /// <summary>
    /// Yeni çalışan oluştur - Email tekilliği ve manager validasyonu ile
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "HR,Admin")]
    public async Task<IActionResult> Create([FromBody] CreateEmployeeDto dto)
    {
        var result = await _service.CreateAsync(dto);
        return Ok(result);
    }

    /// <summary>
    /// Çalışan güncelle - Self-manager kontrolü ve email tekilliği ile
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "HR,Admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateEmployeeDto dto)
    {
        var result = await _service.UpdateAsync(id, dto);
        return Ok(result);
    }

    /// <summary>
    /// Çalışanı pasifleştir (Status=Inactive)
    /// </summary>
    [HttpPut("{id:guid}/deactivate")]
    [Authorize(Roles = "HR,Admin")]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        await _service.DeactivateAsync(id);
        return Ok();
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
    [HttpPost("{id:guid}/ai/job-description")]
    [ProducesResponseType(typeof(JobDescriptionDraftDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [Authorize(Roles = "HR,Admin")]
    public async Task<IActionResult> GenerateJobDescription(Guid id)
    {
        var result = await _service.GenerateAiJobDescriptionAsync(id);
        return Ok(new { employeeId = id, draft = result });
    }
}
