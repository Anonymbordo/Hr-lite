using HrLite.Domain.Common;
using HrLite.Domain.Enums;

namespace HrLite.Domain.Entities;

public class Employee : BaseEntity
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public Role Role { get; set; }
    public int? DepartmentId { get; set; }
    public DateTime HireDate { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation Properties
    public Department? Department { get; set; }
    public ICollection<LeaveRequest> LeaveRequests { get; set; } = new List<LeaveRequest>();
}
