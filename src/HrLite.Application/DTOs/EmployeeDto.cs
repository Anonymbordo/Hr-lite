namespace HrLite.Application.DTOs;

public class EmployeeDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public int? DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public int? ManagerId { get; set; }
    public bool IsActive { get; set; }
    public string? JobDescriptionDraft { get; set; } // AI Çıktısı burada görünecek
}