using HrLite.Application.DTOs;
using HrLite.Application.DTOs.Leave;

namespace HrLite.Application.Interfaces;

public interface ILeaveRequestsService
{
    Task<PagedResultDto<LeaveRequestDto>> GetAsync(LeaveRequestQueryParameters query);
    Task<LeaveRequestDto> GetByIdAsync(int id);
    Task<LeaveRequestDto> CreateAsync(CreateLeaveRequestDto dto);

    Task<LeaveRequestDto> ApproveAsync(int id);
    Task<LeaveRequestDto> RejectAsync(int id, RejectLeaveRequestDto dto);
    Task<LeaveRequestDto> CancelAsync(int id);

    Task<NormalizeLeaveReasonResponseDto> NormalizeReasonAsync(NormalizeLeaveReasonRequestDto dto);
    Task<ExplainDecisionResponseDto> ExplainDecisionAsync(int id);
}
