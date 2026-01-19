using HrLite.Application.DTOs.Auth;

namespace HrLite.Application.Interfaces;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request);
}
