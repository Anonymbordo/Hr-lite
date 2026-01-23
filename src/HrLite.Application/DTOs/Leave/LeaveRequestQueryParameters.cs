namespace HrLite.Application.DTOs.Leave;

public class LeaveRequestQueryParameters
{
    public string? Status { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
