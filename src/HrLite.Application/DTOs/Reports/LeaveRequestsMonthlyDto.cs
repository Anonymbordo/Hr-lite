namespace HrLite.Application.DTOs.Reports;

public class LeaveRequestsMonthlyDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public int TotalRequests { get; set; }
    public int ApprovedRequests { get; set; }
    public int PendingRequests { get; set; }
    public int RejectedRequests { get; set; }
}
