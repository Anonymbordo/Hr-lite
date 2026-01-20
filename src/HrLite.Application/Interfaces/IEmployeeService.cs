using HrLite.Application.DTOs;

namespace HrLite.Application.Interfaces;

public interface IEmployeeService
{
    Task<List<EmployeeDto>> GetAllAsync();
    Task<EmployeeDto> CreateAsync(CreateEmployeeDto createDto);
    
    // AI Job Description isteği burada olacak
    Task<string> GenerateAiJobDescriptionAsync(int employeeId);
}