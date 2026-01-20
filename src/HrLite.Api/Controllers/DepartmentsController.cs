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

    /// <summary>
    /// Departman listesi - Sayfalama, filtreleme ve sıralama ile
    /// </summary>
    /// <param name="isActive">Aktif/pasif filtresi (null=hepsi)</param>
    /// <param name="page">Sayfa numarası (1-based)</param>
    /// <param name="pageSize">Sayfa başına kayıt</param>
    /// <param name="sort">Sıralama (name, -name)</param>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResultDto<DepartmentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] bool? isActive, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? sort = "name")
    {
        return Ok(await _service.GetAsync(isActive, page, pageSize, sort));
    }

    /// <summary>
    /// Departman detayı - ID ile
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(DepartmentDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(int id)
    {
        return Ok(await _service.GetByIdAsync(id));
    }

    /// <summary>
    /// Yeni departman oluştur
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] DepartmentDto dto)
    {
        var result = await _service.CreateAsync(dto);
        return Ok(result);
    }

    /// <summary>
    /// Departman güncelle
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] DepartmentDto dto)
    {
        var result = await _service.UpdateAsync(id, dto);
        return Ok(result);
    }

    /// <summary>
    /// Departmanı pasifleştir (IsActive=false)
    /// </summary>
    [HttpPut("{id}/deactivate")]
    public async Task<IActionResult> Deactivate(int id)
    {
        await _service.DeactivateAsync(id);
        return NoContent();
    }
}