using HrLite.Application.DTOs;
using HrLite.Domain.Entities;
using HrLite.Domain.Enums;

namespace HrLite.Application.Interfaces;

public interface IEmployeeRepository
{
    Task<PagedResultDto<Employee>> GetPagedAsync(
        EmployeeStatus? statusFilter,
        Guid? departmentId,
        string? search,
        string? sort,
        int page,
        int pageSize);

    Task<Employee?> GetByIdAsync(Guid id);
    Task<Employee?> GetByIdWithDetailsAsync(Guid id);
    Task<Employee?> GetByEmailAsync(string email);
    Task<bool> EmailExistsAsync(string email, Guid? excludeId);
    Task<bool> ExistsAsync(Guid id);
    Task<List<Employee>> GetByStatusWithDepartmentAsync(EmployeeStatus status);
    void Add(Employee employee);
}
