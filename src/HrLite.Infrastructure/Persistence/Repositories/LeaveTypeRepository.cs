using HrLite.Application.Interfaces;
using HrLite.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HrLite.Infrastructure.Persistence.Repositories;

public class LeaveTypeRepository : ILeaveTypeRepository
{
    private readonly ApplicationDbContext _context;

    public LeaveTypeRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<List<LeaveType>> GetAllAsync()
    {
        return _context.LeaveTypes
            .AsNoTracking()
            .OrderBy(lt => lt.Id)
            .ToListAsync();
    }

    public Task<List<string>> GetAllCodesAsync()
    {
        return _context.LeaveTypes
            .AsNoTracking()
            .OrderBy(lt => lt.Id)
            .Select(lt => lt.Code)
            .ToListAsync();
    }

    public Task<LeaveType?> GetByCodeAsync(string code)
    {
        var normalized = code.Trim().ToLower();
        return _context.LeaveTypes
            .FirstOrDefaultAsync(lt => lt.Code.ToLower() == normalized);
    }
}
