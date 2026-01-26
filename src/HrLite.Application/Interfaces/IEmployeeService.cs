using HrLite.Application.DTOs;

namespace HrLite.Application.Interfaces;

public interface IEmployeeService
{
    Task<PagedResultDto<EmployeeDto>> GetAsync(EmployeeQueryParameters query);
    Task<EmployeeDto> GetByIdAsync(Guid id);
    Task<EmployeeDto> CreateAsync(CreateEmployeeDto createDto);
    Task<EmployeeDto> UpdateAsync(Guid id, UpdateEmployeeDto updateDto);
    Task DeactivateAsync(Guid id);

    // AI Job Description JSON şeması ile üretim
    Task<JobDescriptionDraftDto> GenerateAiJobDescriptionAsync(Guid employeeId);
}
