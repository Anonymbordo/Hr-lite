using HrLite.Domain.Entities;

namespace HrLite.Application.Interfaces;

public interface ILeaveTypeRepository
{
    Task<List<LeaveType>> GetAllAsync();
    Task<List<string>> GetAllCodesAsync();
    Task<LeaveType?> GetByCodeAsync(string code);
}
