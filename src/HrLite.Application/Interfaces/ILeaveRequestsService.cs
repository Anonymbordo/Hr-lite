using HrLite.Application.DTOs;
using HrLite.Application.DTOs.Leave;

namespace HrLite.Application.Interfaces;

public interface ILeaveRequestsService
{
    Task<PagedResultDto<LeaveRequestDto>> GetAsync(LeaveRequestQueryParameters query);
    Task<LeaveRequestDto> GetByIdAsync(Guid id);
    Task<LeaveRequestDto> CreateAsync(CreateLeaveRequestDto dto);

    Task<LeaveRequestDto> ApproveAsync(Guid id);
    Task<LeaveRequestDto> RejectAsync(Guid id, RejectLeaveRequestDto dto);
    Task<LeaveRequestDto> CancelAsync(Guid id);

    Task<NormalizeLeaveReasonResponseDto> NormalizeReasonAsync(NormalizeLeaveReasonRequestDto dto);
    Task<ExplainDecisionResponseDto> ExplainDecisionAsync(Guid id);
}
