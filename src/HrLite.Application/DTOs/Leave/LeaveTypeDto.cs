namespace HrLite.Application.DTOs.Leave;

public class LeaveTypeDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int DefaultAnnualQuotaDays { get; set; }
}
