namespace HrLite.Application.DTOs;

public class EmployeeQueryParameters
{
    public string? Status { get; set; }
    public Guid? DepartmentId { get; set; }
    public string? Search { get; set; }
    public string? Sort { get; set; } = "firstName"; // firstName|lastName|email|department
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
