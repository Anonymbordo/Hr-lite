namespace HrLite.Application.DTOs.Leave;

public class CreateLeaveRequestDto
{
    public string LeaveTypeCode { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Reason { get; set; } = string.Empty;
}
