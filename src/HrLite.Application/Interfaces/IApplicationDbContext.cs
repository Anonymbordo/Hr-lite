using HrLite.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HrLite.Application.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Employee> Employees { get; }
    DbSet<Department> Departments { get; }
    DbSet<LeaveRequest> LeaveRequests { get; }
    DbSet<LeaveType> LeaveTypes { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
