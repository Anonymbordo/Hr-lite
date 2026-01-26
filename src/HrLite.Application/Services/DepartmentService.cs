using HrLite.Application.Common.Exceptions;
using HrLite.Application.DTOs;
using HrLite.Application.Interfaces;
using HrLite.Domain.Entities;

namespace HrLite.Application.Services;

public class DepartmentService : IDepartmentService
{
    private readonly IDepartmentRepository _departments;
    private readonly IUnitOfWork _unitOfWork;

    public DepartmentService(IDepartmentRepository departments, IUnitOfWork unitOfWork)
    {
        _departments = departments;
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResultDto<DepartmentDto>> GetAsync(bool? isActive, int page, int pageSize, string? sort)
    {
        var result = await _departments.GetPagedAsync(isActive, page, pageSize, sort);
        var items = result.Items.Select(d => new DepartmentDto
        {
            Id = d.Id,
            Name = d.Name,
            Description = d.Description,
            IsActive = d.IsActive
        }).ToList();

        return new PagedResultDto<DepartmentDto>
        {
            Items = items,
            TotalCount = result.TotalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<DepartmentDto> GetByIdAsync(Guid id)
    {
        var entity = await _departments.GetByIdAsync(id);
        if (entity == null)
        {
            throw new NotFoundException("Department", id);
        }

        return new DepartmentDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            IsActive = entity.IsActive
        };
    }

    public async Task<DepartmentDto> CreateAsync(DepartmentDto dto)
    {
        if (dto == null)
        {
            throw new ValidationException("Request body is required.");
        }

        var entity = new Department
        {
            Name = dto.Name,
            Description = dto.Description,
            IsActive = true
        };

        _departments.Add(entity);
        await _unitOfWork.SaveChangesAsync();

        return new DepartmentDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            IsActive = entity.IsActive
        };
    }

    public async Task<DepartmentDto> UpdateAsync(Guid id, DepartmentDto dto)
    {
        if (dto == null)
        {
            throw new ValidationException("Request body is required.");
        }

        var entity = await _departments.GetByIdAsync(id);
        if (entity == null)
        {
            throw new NotFoundException("Department", id);
        }

        entity.Name = dto.Name;
        entity.Description = dto.Description;

        await _unitOfWork.SaveChangesAsync();

        return new DepartmentDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            IsActive = entity.IsActive
        };
    }

    public async Task DeactivateAsync(Guid id)
    {
        var entity = await _departments.GetByIdAsync(id);
        if (entity == null)
        {
            throw new NotFoundException("Department", id);
        }

        entity.IsActive = false;
        await _unitOfWork.SaveChangesAsync();
    }
}
