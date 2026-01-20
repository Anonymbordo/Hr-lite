using HrLite.Application.DTOs;
using HrLite.Application.Interfaces;
using HrLite.Domain.Entities;
using HrLite.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HrLite.Infrastructure.Services;

public class DepartmentService : IDepartmentService
{
    private readonly ApplicationDbContext _context;

    public DepartmentService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResultDto<DepartmentDto>> GetAsync(bool? isActive, int page, int pageSize, string? sort)
    {
        var query = _context.Departments.AsQueryable();

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

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(d => new DepartmentDto
            {
                Id = d.Id,
                Name = d.Name,
                Description = d.Description
            })
            .ToListAsync();

        return new PagedResultDto<DepartmentDto>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<DepartmentDto> GetByIdAsync(int id)
    {
        var entity = await _context.Departments.FindAsync(id) ?? throw new Exception("Department not found");

        return new DepartmentDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description
        };
    }

    public async Task<DepartmentDto> CreateAsync(DepartmentDto dto)
    {
        var entity = new Department
        {
            Name = dto.Name,
            Description = dto.Description,
            IsActive = true
        };

        _context.Departments.Add(entity);
        await _context.SaveChangesAsync();

        dto.Id = entity.Id;
        return dto;
    }

    public async Task<DepartmentDto> UpdateAsync(int id, DepartmentDto dto)
    {
        var entity = await _context.Departments.FindAsync(id) ?? throw new Exception("Department not found");

        entity.Name = dto.Name;
        entity.Description = dto.Description;

        await _context.SaveChangesAsync();

        return new DepartmentDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description
        };
    }

    public async Task DeactivateAsync(int id)
    {
        var entity = await _context.Departments.FindAsync(id) ?? throw new Exception("Department not found");
        entity.IsActive = false;
        await _context.SaveChangesAsync();
    }
}