namespace HrLite.Application.DTOs;

public class EmployeeDto
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string Role { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public Guid? DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public Guid? ManagerId { get; set; }
    public decimal Salary { get; set; }
    public DateTime HireDate { get; set; }
    public string? JobDescriptionDraft { get; set; } // AI Çıktısı burada görünecek
}
