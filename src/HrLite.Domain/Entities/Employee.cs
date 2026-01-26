using HrLite.Domain.Common;
using HrLite.Domain.Enums;

namespace HrLite.Domain.Entities;

public class Employee : BaseEntity
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public Role Role { get; set; }
    public EmployeeStatus Status { get; set; } = EmployeeStatus.Active;
    public Guid? DepartmentId { get; set; }
    public DateTime HireDate { get; set; }
    public decimal Salary { get; set; }
    public string? JobDescriptionDraft { get; set; }

    // Self-Referencing Properties
    public Guid? ManagerId { get; set; }
    public Employee? Manager { get; set; }
    public ICollection<Employee> DirectReports { get; set; } = new List<Employee>();

    // --- MEVCUT İLİŞKİLER ---
    public Department? Department { get; set; }
    public ICollection<LeaveRequest> LeaveRequests { get; set; } = new List<LeaveRequest>();
}
