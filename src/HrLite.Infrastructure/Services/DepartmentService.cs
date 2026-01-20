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

    public async Task<List<DepartmentDto>> GetAllAsync()
    {
        // Entity -> DTO dönüşümü (Controller'a Entity gitmez!)
        return await _context.Departments
            .Select(d => new DepartmentDto 
            { 
                Id = d.Id, 
                Name = d.Name, 
                Description = d.Description 
            })
            .ToListAsync();
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
}