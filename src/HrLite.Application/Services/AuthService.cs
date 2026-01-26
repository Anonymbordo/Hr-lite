using HrLite.Application.Common;
using HrLite.Application.Common.Exceptions;
using HrLite.Application.DTOs.Auth;
using HrLite.Application.Interfaces;
using HrLite.Application.Validators;
using HrLite.Domain.Enums;

namespace HrLite.Application.Services;

public class AuthService : IAuthService
{
    private static readonly LoginRequestValidator LoginValidator = new();
    private readonly IEmployeeRepository _employees;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public AuthService(IEmployeeRepository employees, IJwtTokenGenerator jwtTokenGenerator)
    {
        _employees = employees;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        if (request == null)
        {
            throw new ValidationException("Request body is required.");
        }

        ValidationHelper.ValidateAndThrow(LoginValidator, request);

        var employee = await _employees.GetByEmailAsync(request.Email);

        if (employee == null || employee.Status != EmployeeStatus.Active)
        {
            throw new UnauthorizedException("Invalid email or password.");
        }

        // Simple password check (in production, use BCrypt or similar)
        if (!VerifyPassword(request.Password, employee.PasswordHash))
        {
            throw new UnauthorizedException("Invalid email or password.");
        }

        var token = _jwtTokenGenerator.GenerateToken(employee);

        return new LoginResponse
        {
            Token = token,
            EmployeeId = employee.Id,
            Email = employee.Email,
            Role = employee.Role.ToString(),
            ExpiresAt = DateTime.UtcNow.AddHours(8)
        };
    }

    private bool VerifyPassword(string password, string passwordHash)
    {
        // Simple comparison for demo (in production use BCrypt.Net.BCrypt.Verify)
        return password == passwordHash;
    }
}
