using HrLite.Domain.Common;

namespace HrLite.Domain.Entities;

public class Department : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation Properties
    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}
