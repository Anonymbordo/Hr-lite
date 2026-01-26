using HrLite.Domain.Enums;

namespace HrLite.Application.DTOs.Leave;

public class LeaveRequestDto
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }

    public Guid LeaveTypeId { get; set; }
    public string LeaveTypeCode { get; set; } = string.Empty;
    public string LeaveTypeName { get; set; } = string.Empty;

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int Days { get; set; }

    public string Reason { get; set; } = string.Empty;
    public LeaveStatus Status { get; set; }

    public Guid? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? RejectReason { get; set; }
}
