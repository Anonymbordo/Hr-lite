using HrLite.Application.DTOs;

namespace HrLite.Application.Interfaces;

public interface IEmployeeService
{
    Task<PagedResultDto<EmployeeDto>> GetAsync(EmployeeQueryParameters query);
    Task<EmployeeDto> GetByIdAsync(int id);
    Task<EmployeeDto> CreateAsync(CreateEmployeeDto createDto);
    Task<EmployeeDto> UpdateAsync(int id, UpdateEmployeeDto updateDto);
    Task DeactivateAsync(int id);

    // AI Job Description JSON şeması ile üretim
    Task<JobDescriptionDraftDto> GenerateAiJobDescriptionAsync(int employeeId);
}