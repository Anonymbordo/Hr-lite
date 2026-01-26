using HrLite.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace HrLite.Application.DTOs;

public class CreateEmployeeDto
{
    [Required]
    public string FirstName { get; set; } = string.Empty;
    
    [Required]
    public string LastName { get; set; } = string.Empty;
    
    [Required]
    [EmailAddress] // E-posta formatı kontrolü
    public string Email { get; set; } = string.Empty;

    [Phone]
    public string? Phone { get; set; }

    public int Role { get; set; } // Enum değeri (0, 1, 2...)
    public int Status { get; set; } = (int)EmployeeStatus.Active;
    public Guid? DepartmentId { get; set; }
    public Guid? ManagerId { get; set; }
    public DateTime HireDate { get; set; }
    public decimal Salary { get; set; }
}
