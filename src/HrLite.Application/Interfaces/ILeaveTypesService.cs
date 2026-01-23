using HrLite.Application.DTOs.Leave;

namespace HrLite.Application.Interfaces;

public interface ILeaveTypesService
{
    Task<List<LeaveTypeDto>> GetAllAsync();
}
