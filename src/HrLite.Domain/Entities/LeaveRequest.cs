using HrLite.Domain.Common;
using HrLite.Domain.Enums;

namespace HrLite.Domain.Entities;

public class LeaveRequest : BaseEntity
{
    public Guid EmployeeId { get; set; }
    public Guid LeaveTypeId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int Days { get; set; }
    public string Reason { get; set; } = string.Empty;
    public LeaveStatus Status { get; set; } = LeaveStatus.Pending;
    public Guid? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? RejectReason { get; set; }

    // Navigation Properties
    public Employee Employee { get; set; } = null!;
    public LeaveType LeaveType { get; set; } = null!;
}
