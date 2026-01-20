using System.Text.Json;
using HrLite.Application.DTOs;
using HrLite.Application.Interfaces;
using HrLite.Domain.Entities;
using HrLite.Domain.Enums;
using HrLite.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HrLite.Infrastructure.Services;

public class EmployeeService : IEmployeeService
{
    private readonly ApplicationDbContext _context;
    private readonly IAiService _aiService;

    public EmployeeService(ApplicationDbContext context, IAiService aiService)
    {
        _context = context;
        _aiService = aiService;
    }

    public async Task<PagedResultDto<EmployeeDto>> GetAsync(EmployeeQueryParameters queryParams)
    {
        var query = _context.Employees
            .Include(e => e.Department)
            .AsQueryable();

        if (queryParams.IsActive.HasValue)
        {
            query = query.Where(e => e.IsActive == queryParams.IsActive.Value);
        }

        if (queryParams.DepartmentId.HasValue)
        {
            query = query.Where(e => e.DepartmentId == queryParams.DepartmentId.Value);
        }

        if (!string.IsNullOrWhiteSpace(queryParams.Search))
        {
            var term = queryParams.Search.ToLower();
            query = query.Where(e => e.FirstName.ToLower().Contains(term) || e.LastName.ToLower().Contains(term) || e.Email.ToLower().Contains(term));
        }

        query = queryParams.Sort?.ToLower() switch
        {
            "firstname" => query.OrderBy(e => e.FirstName),
            "-firstname" => query.OrderByDescending(e => e.FirstName),
            "lastname" => query.OrderBy(e => e.LastName),
            "-lastname" => query.OrderByDescending(e => e.LastName),
            "email" => query.OrderBy(e => e.Email),
            "-email" => query.OrderByDescending(e => e.Email),
            "department" => query.OrderBy(e => e.Department!.Name),
            "-department" => query.OrderByDescending(e => e.Department!.Name),
            _ => query.OrderBy(e => e.Id)
        };

        var total = await query.CountAsync();
        var items = await query
            .Skip((queryParams.Page - 1) * queryParams.PageSize)
            .Take(queryParams.PageSize)
            .Select(e => new EmployeeDto
            {
                Id = e.Id,
                FirstName = e.FirstName,
                LastName = e.LastName,
                Email = e.Email,
                Role = e.Role.ToString(),
                DepartmentId = e.DepartmentId,
                DepartmentName = e.Department != null ? e.Department.Name : "Departman Yok",
                ManagerId = e.ManagerId,
                IsActive = e.IsActive,
                JobDescriptionDraft = e.JobDescriptionDraft
            })
            .ToListAsync();

        return new PagedResultDto<EmployeeDto>
        {
            Items = items,
            TotalCount = total,
            Page = queryParams.Page,
            PageSize = queryParams.PageSize
        };
    }

    public async Task<EmployeeDto> GetByIdAsync(int id)
    {
        var entity = await _context.Employees
            .Include(e => e.Department)
            .Include(e => e.Manager)
            .FirstOrDefaultAsync(e => e.Id == id) ?? throw new Exception("Çalışan bulunamadı.");

        return ToDto(entity);
    }

    public async Task<EmployeeDto> CreateAsync(CreateEmployeeDto dto)
    {
        if (!Enum.IsDefined(typeof(Role), dto.Role))
        {
            throw new Exception("Geçersiz rol değeri. Geçerli değerler: 1=Employee, 2=HR, 3=Admin.");
        }

        await ValidateEmailUnique(dto.Email, null);
        await ValidateManager(dto.ManagerId, null);

        var entity = new Employee
        {
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Email = dto.Email,
            Role = (Role)dto.Role,
            DepartmentId = dto.DepartmentId,
            ManagerId = dto.ManagerId,
            HireDate = dto.HireDate,
            PasswordHash = "defaultHash",
            IsActive = true
        };

        _context.Employees.Add(entity);
        await _context.SaveChangesAsync();

        // Tam detay için ilişkilerle birlikte tekrar yükle
        return await GetByIdAsync(entity.Id);
    }

    public async Task<EmployeeDto> UpdateAsync(int id, UpdateEmployeeDto dto)
    {
        var entity = await _context.Employees.FindAsync(id) ?? throw new Exception("Çalışan bulunamadı.");

        if (!Enum.IsDefined(typeof(Role), dto.Role))
        {
            throw new Exception("Geçersiz rol değeri. Geçerli değerler: 1=Employee, 2=HR, 3=Admin.");
        }

        await ValidateEmailUnique(dto.Email, id);
        await ValidateManager(dto.ManagerId, id);

        entity.FirstName = dto.FirstName;
        entity.LastName = dto.LastName;
        entity.Email = dto.Email;
        entity.Role = (Role)dto.Role;
        entity.DepartmentId = dto.DepartmentId;
        entity.ManagerId = dto.ManagerId;
        entity.IsActive = dto.IsActive;

        await _context.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    public async Task DeactivateAsync(int id)
    {
        var entity = await _context.Employees.FindAsync(id) ?? throw new Exception("Çalışan bulunamadı.");
        entity.IsActive = false;
        await _context.SaveChangesAsync();
    }

    public async Task<JobDescriptionDraftDto> GenerateAiJobDescriptionAsync(int employeeId)
    {
        var employee = await _context.Employees
            .Include(e => e.Department)
            .FirstOrDefaultAsync(e => e.Id == employeeId);

        if (employee == null) throw new Exception("Çalışan bulunamadı.");

        var roleName = employee.Role.ToString();
        var departmentName = employee.Department?.Name ?? "Genel";

        var aiResponse = await _aiService.GenerateJobDescriptionAsync(roleName, departmentName);

        employee.JobDescriptionDraft = JsonSerializer.Serialize(aiResponse);
        await _context.SaveChangesAsync();

        return aiResponse;
    }

    private static EmployeeDto ToDto(Employee e)
    {
        return new EmployeeDto
        {
            Id = e.Id,
            FirstName = e.FirstName,
            LastName = e.LastName,
            Email = e.Email,
            Role = e.Role.ToString(),
            DepartmentId = e.DepartmentId,
            DepartmentName = e.Department != null ? e.Department.Name : "Departman Yok",
            ManagerId = e.ManagerId,
            IsActive = e.IsActive,
            JobDescriptionDraft = e.JobDescriptionDraft
        };
    }

    private async Task ValidateEmailUnique(string email, int? currentId)
    {
        var exists = await _context.Employees.AnyAsync(e => e.Email == email && e.Id != currentId);
        if (exists)
        {
            throw new Exception($"'{email}' adresi zaten kullanımda!");
        }
    }

    private async Task ValidateManager(int? managerId, int? currentId)
    {
        if (!managerId.HasValue) return;
        if (currentId.HasValue && managerId.Value == currentId.Value)
        {
            throw new Exception("Çalışan kendi yöneticisi olamaz.");
        }

        var managerExists = await _context.Employees.AnyAsync(e => e.Id == managerId.Value);
        if (!managerExists)
        {
            throw new Exception("Manager bulunamadı.");
        }
    }
}