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
    public int Role { get; set; }
    public int? DepartmentId { get; set; }
    public int? ManagerId { get; set; }
    public bool IsActive { get; set; } = true;
}
