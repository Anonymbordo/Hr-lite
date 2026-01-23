using HrLite.Application.DTOs.Leave;
using HrLite.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HrLite.Application.Services;

public class LeaveTypesService : ILeaveTypesService
{
    private readonly IApplicationDbContext _context;

    public LeaveTypesService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<LeaveTypeDto>> GetAllAsync()
    {
        return await _context.LeaveTypes
            .OrderBy(lt => lt.Id)
            .Select(lt => new LeaveTypeDto
            {
                Id = lt.Id,
                Code = lt.Code,
                Name = lt.Name
            })
            .ToListAsync();
    }
}
