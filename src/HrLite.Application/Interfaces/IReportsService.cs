using HrLite.Application.DTOs.Reports;

namespace HrLite.Application.Interfaces;

public interface IReportsService
{
    Task<List<HeadcountByDepartmentDto>> GetHeadcountByDepartmentAsync();
    Task<List<LeaveRequestsMonthlyDto>> GetLeaveRequestsMonthlyAsync(int year);
    Task<AiInsightsResponse> GetAiInsightsAsync(int year);
}
