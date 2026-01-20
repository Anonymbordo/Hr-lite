using HrLite.Application.DTOs;

namespace HrLite.Application.Interfaces;

public interface IDepartmentService
{
    Task<PagedResultDto<DepartmentDto>> GetAsync(bool? isActive, int page, int pageSize, string? sort);
    Task<DepartmentDto> GetByIdAsync(int id);
    Task<DepartmentDto> CreateAsync(DepartmentDto departmentDto);
    Task<DepartmentDto> UpdateAsync(int id, DepartmentDto departmentDto);
    Task DeactivateAsync(int id);
}