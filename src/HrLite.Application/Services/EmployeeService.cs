using System.Text.Json;
using HrLite.Application.Common;
using HrLite.Application.Common.Exceptions;
using HrLite.Application.DTOs;
using HrLite.Application.Interfaces;
using HrLite.Application.Validators;
using HrLite.Domain.Entities;
using HrLite.Domain.Enums;

namespace HrLite.Application.Services;

public class EmployeeService : IEmployeeService
{
    private static readonly CreateEmployeeDtoValidator CreateValidator = new();
    private static readonly UpdateEmployeeDtoValidator UpdateValidator = new();

    private readonly IEmployeeRepository _employees;
    private readonly IDepartmentRepository _departments;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILlmClient _llmClient;
    private readonly ICurrentUserService _currentUser;

    public EmployeeService(
        IEmployeeRepository employees,
        IDepartmentRepository departments,
        IUnitOfWork unitOfWork,
        ILlmClient llmClient,
        ICurrentUserService currentUser)
    {
        _employees = employees;
        _departments = departments;
        _unitOfWork = unitOfWork;
        _llmClient = llmClient;
        _currentUser = currentUser;
    }

    public async Task<PagedResultDto<EmployeeDto>> GetAsync(EmployeeQueryParameters queryParams)
    {
        EnsureAuthenticated();
        EnsureHrOrAdmin();

        var page = queryParams.Page < 1 ? 1 : queryParams.Page;
        var pageSize = queryParams.PageSize < 1 ? 20 : queryParams.PageSize;

        EmployeeStatus? statusFilter = null;
        if (!string.IsNullOrWhiteSpace(queryParams.Status))
        {
            if (!Enum.TryParse<EmployeeStatus>(queryParams.Status, ignoreCase: true, out var parsed))
            {
                throw new ValidationException($"Invalid status: {queryParams.Status}");
            }

            statusFilter = parsed;
        }

        var result = await _employees.GetPagedAsync(
            statusFilter,
            queryParams.DepartmentId,
            queryParams.Search,
            queryParams.Sort,
            page,
            pageSize);

        var items = result.Items.Select(ToDto).ToList();

        return new PagedResultDto<EmployeeDto>
        {
            Items = items,
            TotalCount = result.TotalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<EmployeeDto> GetByIdAsync(Guid id)
    {
        EnsureAuthenticated();
        EnsureCanAccessEmployee(id);

        var entity = await _employees.GetByIdWithDetailsAsync(id);

        if (entity == null)
        {
            throw new NotFoundException("Employee", id);
        }

        return ToDto(entity);
    }

    public async Task<EmployeeDto> CreateAsync(CreateEmployeeDto dto)
    {
        EnsureAuthenticated();
        EnsureHrOrAdmin();

        if (dto == null)
        {
            throw new ValidationException("Request body is required.");
        }

        ValidationHelper.ValidateAndThrow(CreateValidator, dto);
        ValidateRole(dto.Role);
        var status = ValidateStatus(dto.Status);

        await ValidateEmailUnique(dto.Email, null);
        await ValidateDepartment(dto.DepartmentId);
        await ValidateManager(dto.ManagerId, null);

        var departmentName = "Genel";
        if (dto.DepartmentId.HasValue)
        {
            var department = await _departments.GetByIdAsync(dto.DepartmentId.Value);
            if (department != null)
            {
                departmentName = department.Name;
            }
        }

        var entity = new Employee
        {
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Email = dto.Email,
            Phone = dto.Phone,
            Role = (Role)dto.Role,
            Status = status,
            DepartmentId = dto.DepartmentId,
            ManagerId = dto.ManagerId,
            HireDate = dto.HireDate,
            Salary = dto.Salary,
            PasswordHash = "defaultHash",
            JobDescriptionDraft = JobDescriptionTemplate.BuildJson(((Role)dto.Role).ToString(), departmentName)
        };

        _employees.Add(entity);
        await _unitOfWork.SaveChangesAsync();

        return await GetByIdAsync(entity.Id);
    }

    public async Task<EmployeeDto> UpdateAsync(Guid id, UpdateEmployeeDto dto)
    {
        EnsureAuthenticated();
        EnsureHrOrAdmin();

        if (dto == null)
        {
            throw new ValidationException("Request body is required.");
        }

        var entity = await _employees.GetByIdAsync(id);
        if (entity == null)
        {
            throw new NotFoundException("Employee", id);
        }

        ValidationHelper.ValidateAndThrow(UpdateValidator, dto);
        ValidateRole(dto.Role);
        var status = ValidateStatus(dto.Status);

        await ValidateEmailUnique(dto.Email, id);
        await ValidateDepartment(dto.DepartmentId);
        await ValidateManager(dto.ManagerId, id);

        entity.FirstName = dto.FirstName;
        entity.LastName = dto.LastName;
        entity.Email = dto.Email;
        entity.Phone = dto.Phone;
        entity.Role = (Role)dto.Role;
        entity.Status = status;
        entity.DepartmentId = dto.DepartmentId;
        entity.ManagerId = dto.ManagerId;
        entity.HireDate = dto.HireDate;
        entity.Salary = dto.Salary;

        if (string.IsNullOrWhiteSpace(entity.JobDescriptionDraft))
        {
            var departmentName = "Genel";
            if (dto.DepartmentId.HasValue)
            {
                var department = await _departments.GetByIdAsync(dto.DepartmentId.Value);
                if (department != null)
                {
                    departmentName = department.Name;
                }
            }

            entity.JobDescriptionDraft = JobDescriptionTemplate.BuildJson(entity.Role.ToString(), departmentName);
        }

        await _unitOfWork.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    public async Task DeactivateAsync(Guid id)
    {
        EnsureAuthenticated();
        EnsureHrOrAdmin();

        var entity = await _employees.GetByIdAsync(id);
        if (entity == null)
        {
            throw new NotFoundException("Employee", id);
        }

        entity.Status = EmployeeStatus.Inactive;
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<JobDescriptionDraftDto> GenerateAiJobDescriptionAsync(Guid employeeId)
    {
        EnsureAuthenticated();
        EnsureHrOrAdmin();

        var employee = await _employees.GetByIdWithDetailsAsync(employeeId);

        if (employee == null)
        {
            throw new NotFoundException("Employee", employeeId);
        }

        var roleName = employee.Role.ToString();
        var departmentName = employee.Department?.Name ?? "Genel";

        var json = await _llmClient.GenerateJobDescriptionAsync(roleName, departmentName);

        JobDescriptionDraftDto? draft;
        try
        {
            draft = JsonSerializer.Deserialize<JobDescriptionDraftDto>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (JsonException ex)
        {
            throw new BusinessException($"Failed to parse AI response: {ex.Message}", "AI_PARSE_ERROR");
        }

        if (draft == null)
        {
            throw new BusinessException("Failed to parse AI response.", "AI_PARSE_ERROR");
        }

        employee.JobDescriptionDraft = json;
        await _unitOfWork.SaveChangesAsync();

        return draft;
    }

    private static EmployeeDto ToDto(Employee e)
    {
        var jobDescriptionDraft = string.IsNullOrWhiteSpace(e.JobDescriptionDraft)
            ? JobDescriptionTemplate.BuildJson(e.Role.ToString(), e.Department?.Name ?? "Genel")
            : e.JobDescriptionDraft;

        return new EmployeeDto
        {
            Id = e.Id,
            FirstName = e.FirstName,
            LastName = e.LastName,
            Email = e.Email,
            Phone = e.Phone,
            Role = e.Role.ToString(),
            Status = e.Status.ToString(),
            DepartmentId = e.DepartmentId,
            DepartmentName = e.Department != null ? e.Department.Name : "Departman Yok",
            ManagerId = e.ManagerId,
            Salary = e.Salary,
            HireDate = e.HireDate,
            JobDescriptionDraft = jobDescriptionDraft
        };
    }

    private static void ValidateRole(int role)
    {
        if (!Enum.IsDefined(typeof(Role), role))
        {
            throw new ValidationException("Invalid role value.");
        }
    }

    private static EmployeeStatus ValidateStatus(int status)
    {
        if (!Enum.IsDefined(typeof(EmployeeStatus), status))
        {
            throw new ValidationException("Invalid status value.");
        }

        return (EmployeeStatus)status;
    }

    private async Task ValidateEmailUnique(string email, Guid? currentId)
    {
        if (await _employees.EmailExistsAsync(email, currentId))
        {
            throw new BusinessException($"'{email}' adresi zaten kullanimda.");
        }
    }

    private async Task ValidateDepartment(Guid? departmentId)
    {
        if (!departmentId.HasValue)
        {
            return;
        }

        if (!await _departments.ExistsAsync(departmentId.Value))
        {
            throw new ValidationException("Department not found.");
        }
    }

    private async Task ValidateManager(Guid? managerId, Guid? currentId)
    {
        if (!managerId.HasValue)
        {
            return;
        }

        if (currentId.HasValue && managerId.Value == currentId.Value)
        {
            throw new ValidationException("Employee cannot be their own manager.");
        }

        if (!await _employees.ExistsAsync(managerId.Value))
        {
            throw new ValidationException("Manager not found.");
        }
    }

    private void EnsureAuthenticated()
    {
        if (!_currentUser.IsAuthenticated)
        {
            throw new UnauthorizedException();
        }
    }

    private bool IsEmployee()
        => string.Equals(_currentUser.Role, Role.Employee.ToString(), StringComparison.OrdinalIgnoreCase);

    private bool IsHrOrAdmin()
        => string.Equals(_currentUser.Role, Role.HR.ToString(), StringComparison.OrdinalIgnoreCase)
           || string.Equals(_currentUser.Role, Role.Admin.ToString(), StringComparison.OrdinalIgnoreCase);

    private void EnsureHrOrAdmin()
    {
        if (!IsHrOrAdmin())
        {
            throw new ForbiddenException();
        }
    }

    private void EnsureCanAccessEmployee(Guid employeeId)
    {
        if (IsEmployee() && employeeId != _currentUser.UserId)
        {
            throw new ForbiddenException();
        }
    }
}
