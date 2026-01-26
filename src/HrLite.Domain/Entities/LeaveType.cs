using HrLite.Domain.Common;

namespace HrLite.Domain.Entities;

public class LeaveType : BaseEntity
{
    public string Code { get; set; } = string.Empty; // e.g. "Annual", "Sick"
    public string Name { get; set; } = string.Empty;
    public int DefaultAnnualQuotaDays { get; set; }

    // Navigation
    public ICollection<LeaveRequest> LeaveRequests { get; set; } = new List<LeaveRequest>();
}
