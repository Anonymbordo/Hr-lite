using HrLite.Application.DTOs.Leave;
using HrLite.Application.Interfaces;

namespace HrLite.Application.Services;

public class LeaveTypesService : ILeaveTypesService
{
    private readonly ILeaveTypeRepository _leaveTypes;

    public LeaveTypesService(ILeaveTypeRepository leaveTypes)
    {
        _leaveTypes = leaveTypes;
    }

    public async Task<List<LeaveTypeDto>> GetAllAsync()
    {
        var items = await _leaveTypes.GetAllAsync();
        return items.Select(lt => new LeaveTypeDto
        {
            Id = lt.Id,
            Code = lt.Code,
            Name = lt.Name,
            DefaultAnnualQuotaDays = lt.DefaultAnnualQuotaDays
        }).ToList();
    }
}
