using HrLite.Domain.Entities;
using HrLite.Domain.Enums;
using HrLite.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HrLite.Infrastructure.Seed;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        if (await context.Departments.AnyAsync())
        {
            return; // Database already seeded
        }

        // Seed Departments
        var departments = new List<Department>
        {
            new Department { Id = 1, Name = "Engineering", Description = "Software Development", CreatedAt = DateTime.UtcNow, CreatedBy = 0 },
            new Department { Id = 2, Name = "Human Resources", Description = "HR Management", CreatedAt = DateTime.UtcNow, CreatedBy = 0 },
            new Department { Id = 3, Name = "Sales", Description = "Sales and Marketing", CreatedAt = DateTime.UtcNow, CreatedBy = 0 },
            new Department { Id = 4, Name = "Finance", Description = "Financial Operations", CreatedAt = DateTime.UtcNow, CreatedBy = 0 }
        };

        await context.Departments.AddRangeAsync(departments);
        await context.SaveChangesAsync();

        // Seed Employees (password is "password123" for all)
        var employees = new List<Employee>
        {
            // Admin
            new Employee
            {
                Id = 1,
                FirstName = "Admin",
                LastName = "User",
                Email = "admin@hrlite.com",
                PasswordHash = "password123", // In production, use BCrypt
                Role = Role.Admin,
                DepartmentId = 2,
                HireDate = DateTime.UtcNow.AddYears(-3),
                CreatedAt = DateTime.UtcNow,
                CreatedBy = 0
            },
            // HR Users
            new Employee
            {
                Id = 2,
                FirstName = "Sarah",
                LastName = "Johnson",
                Email = "sarah.johnson@hrlite.com",
                PasswordHash = "password123",
                Role = Role.HR,
                DepartmentId = 2,
                HireDate = DateTime.UtcNow.AddYears(-2),
                CreatedAt = DateTime.UtcNow,
                CreatedBy = 0
            },
            new Employee
            {
                Id = 3,
                FirstName = "Michael",
                LastName = "Brown",
                Email = "michael.brown@hrlite.com",
                PasswordHash = "password123",
                Role = Role.HR,
                DepartmentId = 2,
                HireDate = DateTime.UtcNow.AddYears(-1),
                CreatedAt = DateTime.UtcNow,
                CreatedBy = 0
            },
            // Regular Employees - Engineering
            new Employee
            {
                Id = 4,
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@hrlite.com",
                PasswordHash = "password123",
                Role = Role.Employee,
                DepartmentId = 1,
                HireDate = DateTime.UtcNow.AddYears(-1),
                CreatedAt = DateTime.UtcNow,
                CreatedBy = 0
            },
            new Employee
            {
                Id = 5,
                FirstName = "Jane",
                LastName = "Smith",
                Email = "jane.smith@hrlite.com",
                PasswordHash = "password123",
                Role = Role.Employee,
                DepartmentId = 1,
                HireDate = DateTime.UtcNow.AddMonths(-6),
                CreatedAt = DateTime.UtcNow,
                CreatedBy = 0
            },
            new Employee
            {
                Id = 6,
                FirstName = "Alice",
                LastName = "Williams",
                Email = "alice.williams@hrlite.com",
                PasswordHash = "password123",
                Role = Role.Employee,
                DepartmentId = 1,
                HireDate = DateTime.UtcNow.AddMonths(-8),
                CreatedAt = DateTime.UtcNow,
                CreatedBy = 0
            },
            // Sales
            new Employee
            {
                Id = 7,
                FirstName = "Bob",
                LastName = "Davis",
                Email = "bob.davis@hrlite.com",
                PasswordHash = "password123",
                Role = Role.Employee,
                DepartmentId = 3,
                HireDate = DateTime.UtcNow.AddMonths(-4),
                CreatedAt = DateTime.UtcNow,
                CreatedBy = 0
            },
            new Employee
            {
                Id = 8,
                FirstName = "Carol",
                LastName = "Miller",
                Email = "carol.miller@hrlite.com",
                PasswordHash = "password123",
                Role = Role.Employee,
                DepartmentId = 3,
                HireDate = DateTime.UtcNow.AddMonths(-10),
                CreatedAt = DateTime.UtcNow,
                CreatedBy = 0
            },
            // Finance
            new Employee
            {
                Id = 9,
                FirstName = "David",
                LastName = "Wilson",
                Email = "david.wilson@hrlite.com",
                PasswordHash = "password123",
                Role = Role.Employee,
                DepartmentId = 4,
                HireDate = DateTime.UtcNow.AddMonths(-7),
                CreatedAt = DateTime.UtcNow,
                CreatedBy = 0
            },
            new Employee
            {
                Id = 10,
                FirstName = "Emma",
                LastName = "Moore",
                Email = "emma.moore@hrlite.com",
                PasswordHash = "password123",
                Role = Role.Employee,
                DepartmentId = 4,
                HireDate = DateTime.UtcNow.AddMonths(-3),
                CreatedAt = DateTime.UtcNow,
                CreatedBy = 0
            }
        };

        await context.Employees.AddRangeAsync(employees);
        await context.SaveChangesAsync();

        // Seed Leave Requests for 2026
        var leaveRequests = new List<LeaveRequest>
        {
            new LeaveRequest
            {
                EmployeeId = 4,
                StartDate = new DateTime(2026, 1, 10),
                EndDate = new DateTime(2026, 1, 15),
                Reason = "Personal vacation",
                Status = LeaveStatus.Approved,
                ApprovedBy = 2,
                ApprovedAt = new DateTime(2026, 1, 5),
                CreatedAt = new DateTime(2026, 1, 3),
                CreatedBy = 4
            },
            new LeaveRequest
            {
                EmployeeId = 5,
                StartDate = new DateTime(2026, 2, 1),
                EndDate = new DateTime(2026, 2, 5),
                Reason = "Medical appointment",
                Status = LeaveStatus.Pending,
                CreatedAt = new DateTime(2026, 1, 25),
                CreatedBy = 5
            },
            new LeaveRequest
            {
                EmployeeId = 6,
                StartDate = new DateTime(2026, 2, 14),
                EndDate = new DateTime(2026, 2, 16),
                Reason = "Family event",
                Status = LeaveStatus.Approved,
                ApprovedBy = 2,
                ApprovedAt = new DateTime(2026, 2, 10),
                CreatedAt = new DateTime(2026, 2, 8),
                CreatedBy = 6
            },
            new LeaveRequest
            {
                EmployeeId = 7,
                StartDate = new DateTime(2026, 1, 20),
                EndDate = new DateTime(2026, 1, 22),
                Reason = "Business trip",
                Status = LeaveStatus.Rejected,
                RejectionReason = "Peak season, cannot approve",
                CreatedAt = new DateTime(2026, 1, 15),
                CreatedBy = 7
            },
            new LeaveRequest
            {
                EmployeeId = 8,
                StartDate = new DateTime(2026, 3, 5),
                EndDate = new DateTime(2026, 3, 12),
                Reason = "Annual vacation",
                Status = LeaveStatus.Pending,
                CreatedAt = new DateTime(2026, 2, 20),
                CreatedBy = 8
            },
            new LeaveRequest
            {
                EmployeeId = 9,
                StartDate = new DateTime(2026, 1, 8),
                EndDate = new DateTime(2026, 1, 10),
                Reason = "Sick leave",
                Status = LeaveStatus.Approved,
                ApprovedBy = 3,
                ApprovedAt = new DateTime(2026, 1, 7),
                CreatedAt = new DateTime(2026, 1, 6),
                CreatedBy = 9
            }
        };

        await context.LeaveRequests.AddRangeAsync(leaveRequests);
        await context.SaveChangesAsync();
    }
}
