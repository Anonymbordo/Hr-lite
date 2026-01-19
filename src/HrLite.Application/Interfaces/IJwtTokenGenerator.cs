using HrLite.Domain.Entities;

namespace HrLite.Application.Interfaces;

public interface IJwtTokenGenerator
{
    string GenerateToken(Employee employee);
}
