using HrLite.Application.DTOs;
using HrLite.Application.Interfaces;
using HrLite.Domain.Entities;
using HrLite.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace HrLite.Infrastructure.Persistence.Repositories;

public class EmployeeRepository : IEmployeeRepository
{
    private readonly ApplicationDbContext _context;

    public EmployeeRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResultDto<Employee>> GetPagedAsync(
        EmployeeStatus? statusFilter,
        Guid? departmentId,
        string? search,
        string? sort,
        int page,
        int pageSize)
    {
        var query = _context.Employees
            .Include(e => e.Department)
            .AsNoTracking()
            .AsQueryable();

        if (statusFilter.HasValue)
        {
            query = query.Where(e => e.Status == statusFilter.Value);
        }

        if (departmentId.HasValue)
        {
            query = query.Where(e => e.DepartmentId == departmentId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.ToLower();
            query = query.Where(e =>
                e.FirstName.ToLower().Contains(term) ||
                e.LastName.ToLower().Contains(term) ||
                e.Email.ToLower().Contains(term));
        }

        query = sort?.ToLower() switch
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

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResultDto<Employee>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public Task<Employee?> GetByIdAsync(Guid id)
    {
        return _context.Employees.FirstOrDefaultAsync(e => e.Id == id);
    }

    public Task<Employee?> GetByIdWithDetailsAsync(Guid id)
    {
        return _context.Employees
            .Include(e => e.Department)
            .Include(e => e.Manager)
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public Task<Employee?> GetByEmailAsync(string email)
    {
        return _context.Employees.FirstOrDefaultAsync(e => e.Email == email);
    }

    public Task<bool> EmailExistsAsync(string email, Guid? excludeId)
    {
        return _context.Employees.AnyAsync(e => e.Email == email && e.Id != excludeId);
    }

    public Task<bool> ExistsAsync(Guid id)
    {
        return _context.Employees.AnyAsync(e => e.Id == id);
    }

    public Task<List<Employee>> GetByStatusWithDepartmentAsync(EmployeeStatus status)
    {
        return _context.Employees
            .Include(e => e.Department)
            .Where(e => e.Status == status)
            .AsNoTracking()
            .ToListAsync();
    }

    public void Add(Employee employee)
    {
        _context.Employees.Add(employee);
    }
}
