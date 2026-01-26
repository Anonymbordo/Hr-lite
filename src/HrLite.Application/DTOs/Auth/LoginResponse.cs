namespace HrLite.Application.DTOs.Auth;

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public Guid EmployeeId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}
