using HrLite.Application.DTOs;
using HrLite.Domain.Entities;
using HrLite.Domain.Enums;

namespace HrLite.Application.Interfaces;

public interface ILeaveRequestRepository
{
    Task<PagedResultDto<LeaveRequest>> GetPagedAsync(
        LeaveStatus? statusFilter,
        Guid? employeeId,
        int page,
        int pageSize);

    Task<LeaveRequest?> GetByIdWithLeaveTypeAsync(Guid id);
    Task<List<LeaveRequest>> GetForEmployeeWithStatusesAsync(
        Guid employeeId,
        LeaveStatus[] statuses,
        Guid? excludeId = null);

    Task<List<LeaveRequest>> GetApprovedForEmployeeByLeaveTypeCodeAsync(
        Guid employeeId,
        string leaveTypeCode,
        Guid? excludeId = null);

    Task<List<LeaveRequest>> GetByYearAsync(int year);
    void Add(LeaveRequest entity);
}
