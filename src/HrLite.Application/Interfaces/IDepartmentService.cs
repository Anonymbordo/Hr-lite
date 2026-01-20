using HrLite.Application.DTOs;

namespace HrLite.Application.Interfaces;

public interface IDepartmentService
{
    Task<List<DepartmentDto>> GetAllAsync();
    Task<DepartmentDto> CreateAsync(DepartmentDto departmentDto);
}