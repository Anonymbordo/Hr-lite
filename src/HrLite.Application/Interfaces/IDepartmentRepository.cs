using HrLite.Application.DTOs;
using HrLite.Domain.Entities;

namespace HrLite.Application.Interfaces;

public interface IDepartmentRepository
{
    Task<PagedResultDto<Department>> GetPagedAsync(bool? isActive, int page, int pageSize, string? sort);
    Task<Department?> GetByIdAsync(Guid id);
    Task<bool> ExistsAsync(Guid id);
    void Add(Department department);
}
