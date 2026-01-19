using HrLite.Application.Common.Exceptions;
using HrLite.Application.DTOs.Auth;
using HrLite.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HrLite.Application.Services;

public class AuthService : IAuthService
{
    private readonly IApplicationDbContext _context;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public AuthService(IApplicationDbContext context, IJwtTokenGenerator jwtTokenGenerator)
    {
        _context = context;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            throw new ValidationException("Email and password are required.");
        }

        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.Email == request.Email && e.IsActive);

        if (employee == null)
        {
            throw new BusinessException("Invalid email or password.", "INVALID_CREDENTIALS");
        }

        // Simple password check (in production, use BCrypt or similar)
        if (!VerifyPassword(request.Password, employee.PasswordHash))
        {
            throw new BusinessException("Invalid email or password.", "INVALID_CREDENTIALS");
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
