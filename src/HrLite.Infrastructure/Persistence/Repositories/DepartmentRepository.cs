using HrLite.Application.DTOs;
using HrLite.Application.Interfaces;
using HrLite.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HrLite.Infrastructure.Persistence.Repositories;

public class DepartmentRepository : IDepartmentRepository
{
    private readonly ApplicationDbContext _context;

    public DepartmentRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResultDto<Department>> GetPagedAsync(bool? isActive, int page, int pageSize, string? sort)
    {
        var query = _context.Departments.AsNoTracking().AsQueryable();

        if (isActive.HasValue)
        {
            query = query.Where(d => d.IsActive == isActive.Value);
        }

        query = sort?.ToLower() switch
        {
            "name" => query.OrderBy(d => d.Name),
            "-name" => query.OrderByDescending(d => d.Name),
            _ => query.OrderBy(d => d.Id)
        };

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResultDto<Department>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public Task<Department?> GetByIdAsync(Guid id)
    {
        return _context.Departments.FirstOrDefaultAsync(d => d.Id == id);
    }

    public Task<bool> ExistsAsync(Guid id)
    {
        return _context.Departments.AnyAsync(d => d.Id == id);
    }

    public void Add(Department department)
    {
        _context.Departments.Add(department);
    }
}
