using HrLite.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace HrLite.Application.DTOs;

public class UpdateEmployeeDto
{
    [Required]
    public string FirstName { get; set; } = string.Empty;
    [Required]
    public string LastName { get; set; } = string.Empty;
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    [Phone]
    public string? Phone { get; set; }
    public int Role { get; set; }
    public int Status { get; set; } = (int)EmployeeStatus.Active;
    public Guid? DepartmentId { get; set; }
    public Guid? ManagerId { get; set; }
    public decimal Salary { get; set; }
    public DateTime HireDate { get; set; }
}
