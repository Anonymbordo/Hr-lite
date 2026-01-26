using HrLite.Application.DTOs;
using HrLite.Application.Interfaces;
using HrLite.Domain.Entities;
using HrLite.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace HrLite.Infrastructure.Persistence.Repositories;

public class LeaveRequestRepository : ILeaveRequestRepository
{
    private readonly ApplicationDbContext _context;

    public LeaveRequestRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResultDto<LeaveRequest>> GetPagedAsync(
        LeaveStatus? statusFilter,
        Guid? employeeId,
        int page,
        int pageSize)
    {
        var query = _context.LeaveRequests
            .Include(lr => lr.LeaveType)
            .AsNoTracking()
            .AsQueryable();

        if (employeeId.HasValue)
        {
            query = query.Where(lr => lr.EmployeeId == employeeId.Value);
        }

        if (statusFilter.HasValue)
        {
            query = query.Where(lr => lr.Status == statusFilter.Value);
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(lr => lr.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResultDto<LeaveRequest>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public Task<LeaveRequest?> GetByIdWithLeaveTypeAsync(Guid id)
    {
        return _context.LeaveRequests
            .Include(lr => lr.LeaveType)
            .FirstOrDefaultAsync(lr => lr.Id == id);
    }

    public Task<List<LeaveRequest>> GetForEmployeeWithStatusesAsync(
        Guid employeeId,
        LeaveStatus[] statuses,
        Guid? excludeId = null)
    {
        var query = _context.LeaveRequests
            .AsNoTracking()
            .Where(lr => lr.EmployeeId == employeeId);

        if (statuses.Length > 0)
        {
            query = query.Where(lr => statuses.Contains(lr.Status));
        }

        if (excludeId.HasValue)
        {
            query = query.Where(lr => lr.Id != excludeId.Value);
        }

        return query.ToListAsync();
    }

    public Task<List<LeaveRequest>> GetApprovedForEmployeeByLeaveTypeCodeAsync(
        Guid employeeId,
        string leaveTypeCode,
        Guid? excludeId = null)
    {
        var normalized = leaveTypeCode.ToLower();
        var query = _context.LeaveRequests
            .AsNoTracking()
            .Where(lr => lr.EmployeeId == employeeId)
            .Where(lr => lr.Status == LeaveStatus.Approved)
            .Where(lr => lr.LeaveType.Code.ToLower() == normalized);

        if (excludeId.HasValue)
        {
            query = query.Where(lr => lr.Id != excludeId.Value);
        }

        return query.ToListAsync();
    }

    public Task<List<LeaveRequest>> GetByYearAsync(int year)
    {
        return _context.LeaveRequests
            .AsNoTracking()
            .Where(lr => lr.StartDate.Year == year)
            .ToListAsync();
    }

    public void Add(LeaveRequest entity)
    {
        _context.LeaveRequests.Add(entity);
    }
}
