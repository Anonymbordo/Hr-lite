using HrLite.Domain.Enums;

namespace HrLite.Application.DTOs.Leave;

public class LeaveRequestDto
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }

    public int LeaveTypeId { get; set; }
    public string LeaveTypeCode { get; set; } = string.Empty;
    public string LeaveTypeName { get; set; } = string.Empty;

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalDays { get; set; }

    public string Reason { get; set; } = string.Empty;
    public LeaveStatus Status { get; set; }

    public int? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? RejectionReason { get; set; }
}
