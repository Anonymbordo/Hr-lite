using HrLite.Application.Common;
using HrLite.Domain.Entities;
using HrLite.Domain.Enums;
using HrLite.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HrLite.Infrastructure.Seed;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        // Seed should be idempotent. Some environments may have partial data.

        if (!await context.LeaveTypes.AnyAsync())
        {
            var leaveTypes = new List<LeaveType>
            {
                new LeaveType { Id = Guid.NewGuid(), Code = "ANNUAL", Name = "Annual Leave", DefaultAnnualQuotaDays = 14 },
                new LeaveType { Id = Guid.NewGuid(), Code = "SICK", Name = "Sick Leave", DefaultAnnualQuotaDays = 0 },
                new LeaveType { Id = Guid.NewGuid(), Code = "UNPAID", Name = "Unpaid Leave", DefaultAnnualQuotaDays = 0 }
            };

            await context.LeaveTypes.AddRangeAsync(leaveTypes);
            await context.SaveChangesAsync();
        }

        var leaveTypesByCode = await context.LeaveTypes
            .AsNoTracking()
            .ToDictionaryAsync(lt => lt.Code);

        var existingDepartments = await context.Departments.AsNoTracking().ToListAsync();
        // If core demo data exists, do not re-seed employees/departments/leave requests.
        if (existingDepartments.Any())
        {
            await BackfillJobDescriptionsAsync(context, existingDepartments);
            return;
        }

        var engineeringId = Guid.NewGuid();
        var hrId = Guid.NewGuid();
        var salesId = Guid.NewGuid();
        var financeId = Guid.NewGuid();

        // Seed Departments
        var departments = new List<Department>
        {
            new Department { Id = engineeringId, Name = "Engineering", Description = "Software Development", IsActive = true },
            new Department { Id = hrId, Name = "Human Resources", Description = "HR Management", IsActive = true },
            new Department { Id = salesId, Name = "Sales", Description = "Sales and Marketing", IsActive = true },
            new Department { Id = financeId, Name = "Finance", Description = "Financial Operations", IsActive = true }
        };

        await context.Departments.AddRangeAsync(departments);
        await context.SaveChangesAsync();

        var departmentNamesById = departments.ToDictionary(d => d.Id, d => d.Name);

        var adminId = Guid.NewGuid();
        var hrUser1Id = Guid.NewGuid();
        var hrUser2Id = Guid.NewGuid();
        var employee1Id = Guid.NewGuid();
        var employee2Id = Guid.NewGuid();
        var employee3Id = Guid.NewGuid();
        var employee4Id = Guid.NewGuid();
        var employee5Id = Guid.NewGuid();
        var employee6Id = Guid.NewGuid();
        var employee7Id = Guid.NewGuid();

        // Seed Employees (password is "password123" for all)
        var employees = new List<Employee>
        {
            new Employee
            {
                Id = adminId,
                FirstName = "Admin",
                LastName = "User",
                Email = "admin@hrlite.com",
                Phone = "+1-555-0001",
                PasswordHash = "password123",
                Role = Role.Admin,
                Status = EmployeeStatus.Active,
                DepartmentId = hrId,
                HireDate = DateTime.UtcNow.AddYears(-3),
                Salary = 90000m
            },
            new Employee
            {
                Id = hrUser1Id,
                FirstName = "Sarah",
                LastName = "Johnson",
                Email = "sarah.johnson@hrlite.com",
                Phone = "+1-555-0002",
                PasswordHash = "password123",
                Role = Role.HR,
                Status = EmployeeStatus.Active,
                DepartmentId = hrId,
                HireDate = DateTime.UtcNow.AddYears(-2),
                Salary = 80000m
            },
            new Employee
            {
                Id = hrUser2Id,
                FirstName = "Michael",
                LastName = "Brown",
                Email = "michael.brown@hrlite.com",
                Phone = "+1-555-0003",
                PasswordHash = "password123",
                Role = Role.HR,
                Status = EmployeeStatus.Active,
                DepartmentId = hrId,
                HireDate = DateTime.UtcNow.AddYears(-1),
                Salary = 78000m
            },
            new Employee
            {
                Id = employee1Id,
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@hrlite.com",
                Phone = "+1-555-0101",
                PasswordHash = "password123",
                Role = Role.Employee,
                Status = EmployeeStatus.Active,
                DepartmentId = engineeringId,
                HireDate = DateTime.UtcNow.AddYears(-1),
                Salary = 70000m
            },
            new Employee
            {
                Id = employee2Id,
                FirstName = "Jane",
                LastName = "Smith",
                Email = "jane.smith@hrlite.com",
                Phone = "+1-555-0102",
                PasswordHash = "password123",
                Role = Role.Employee,
                Status = EmployeeStatus.Active,
                DepartmentId = engineeringId,
                HireDate = DateTime.UtcNow.AddMonths(-6),
                Salary = 68000m
            },
            new Employee
            {
                Id = employee3Id,
                FirstName = "Alice",
                LastName = "Williams",
                Email = "alice.williams@hrlite.com",
                Phone = "+1-555-0103",
                PasswordHash = "password123",
                Role = Role.Employee,
                Status = EmployeeStatus.Active,
                DepartmentId = engineeringId,
                HireDate = DateTime.UtcNow.AddMonths(-8),
                Salary = 69000m
            },
            new Employee
            {
                Id = employee4Id,
                FirstName = "Bob",
                LastName = "Davis",
                Email = "bob.davis@hrlite.com",
                Phone = "+1-555-0201",
                PasswordHash = "password123",
                Role = Role.Employee,
                Status = EmployeeStatus.Active,
                DepartmentId = salesId,
                HireDate = DateTime.UtcNow.AddMonths(-4),
                Salary = 62000m
            },
            new Employee
            {
                Id = employee5Id,
                FirstName = "Carol",
                LastName = "Miller",
                Email = "carol.miller@hrlite.com",
                Phone = "+1-555-0202",
                PasswordHash = "password123",
                Role = Role.Employee,
                Status = EmployeeStatus.Active,
                DepartmentId = salesId,
                HireDate = DateTime.UtcNow.AddMonths(-10),
                Salary = 61000m
            },
            new Employee
            {
                Id = employee6Id,
                FirstName = "David",
                LastName = "Wilson",
                Email = "david.wilson@hrlite.com",
                Phone = "+1-555-0301",
                PasswordHash = "password123",
                Role = Role.Employee,
                Status = EmployeeStatus.Active,
                DepartmentId = financeId,
                HireDate = DateTime.UtcNow.AddMonths(-7),
                Salary = 73000m
            },
            new Employee
            {
                Id = employee7Id,
                FirstName = "Emma",
                LastName = "Moore",
                Email = "emma.moore@hrlite.com",
                Phone = "+1-555-0302",
                PasswordHash = "password123",
                Role = Role.Employee,
                Status = EmployeeStatus.Active,
                DepartmentId = financeId,
                HireDate = DateTime.UtcNow.AddMonths(-3),
                Salary = 71000m
            }
        };

        foreach (var employee in employees)
        {
            var departmentName = employee.DepartmentId.HasValue
                && departmentNamesById.TryGetValue(employee.DepartmentId.Value, out var name)
                ? name
                : "Genel";
            employee.JobDescriptionDraft = JobDescriptionTemplate.BuildJson(employee.Role.ToString(), departmentName);
        }

        await context.Employees.AddRangeAsync(employees);
        await context.SaveChangesAsync();

        var annualLeaveTypeId = leaveTypesByCode["ANNUAL"].Id;
        var sickLeaveTypeId = leaveTypesByCode["SICK"].Id;
        var unpaidLeaveTypeId = leaveTypesByCode["UNPAID"].Id;

        // Seed Leave Requests for 2026
        var leaveRequests = new List<LeaveRequest>
        {
            new LeaveRequest
            {
                EmployeeId = employee1Id,
                LeaveTypeId = annualLeaveTypeId,
                StartDate = new DateTime(2026, 1, 10),
                EndDate = new DateTime(2026, 1, 15),
                Days = CountDays(new DateTime(2026, 1, 10), new DateTime(2026, 1, 15)),
                Reason = "Personal vacation",
                Status = LeaveStatus.Approved,
                ApprovedBy = hrUser1Id,
                ApprovedAt = new DateTime(2026, 1, 5)
            },
            new LeaveRequest
            {
                EmployeeId = employee2Id,
                LeaveTypeId = sickLeaveTypeId,
                StartDate = new DateTime(2026, 2, 1),
                EndDate = new DateTime(2026, 2, 5),
                Days = CountDays(new DateTime(2026, 2, 1), new DateTime(2026, 2, 5)),
                Reason = "Medical appointment",
                Status = LeaveStatus.Pending
            },
            new LeaveRequest
            {
                EmployeeId = employee3Id,
                LeaveTypeId = annualLeaveTypeId,
                StartDate = new DateTime(2026, 2, 14),
                EndDate = new DateTime(2026, 2, 16),
                Days = CountDays(new DateTime(2026, 2, 14), new DateTime(2026, 2, 16)),
                Reason = "Family event",
                Status = LeaveStatus.Approved,
                ApprovedBy = hrUser1Id,
                ApprovedAt = new DateTime(2026, 2, 10)
            },
            new LeaveRequest
            {
                EmployeeId = employee4Id,
                LeaveTypeId = unpaidLeaveTypeId,
                StartDate = new DateTime(2026, 1, 20),
                EndDate = new DateTime(2026, 1, 22),
                Days = CountDays(new DateTime(2026, 1, 20), new DateTime(2026, 1, 22)),
                Reason = "Business trip",
                Status = LeaveStatus.Rejected,
                RejectReason = "Peak season, cannot approve"
            },
            new LeaveRequest
            {
                EmployeeId = employee5Id,
                LeaveTypeId = annualLeaveTypeId,
                StartDate = new DateTime(2026, 3, 5),
                EndDate = new DateTime(2026, 3, 12),
                Days = CountDays(new DateTime(2026, 3, 5), new DateTime(2026, 3, 12)),
                Reason = "Annual vacation",
                Status = LeaveStatus.Pending
            },
            new LeaveRequest
            {
                EmployeeId = employee6Id,
                LeaveTypeId = sickLeaveTypeId,
                StartDate = new DateTime(2026, 1, 8),
                EndDate = new DateTime(2026, 1, 10),
                Days = CountDays(new DateTime(2026, 1, 8), new DateTime(2026, 1, 10)),
                Reason = "Sick leave",
                Status = LeaveStatus.Approved,
                ApprovedBy = hrUser2Id,
                ApprovedAt = new DateTime(2026, 1, 7)
            }
        };

        await context.LeaveRequests.AddRangeAsync(leaveRequests);
        await context.SaveChangesAsync();
    }

    private static async Task BackfillJobDescriptionsAsync(
        ApplicationDbContext context,
        IReadOnlyCollection<Department> departments)
    {
        var departmentNamesById = departments.ToDictionary(d => d.Id, d => d.Name);
        var employees = await context.Employees
            .Where(e => e.JobDescriptionDraft == null || e.JobDescriptionDraft == string.Empty)
            .ToListAsync();

        if (employees.Count == 0)
        {
            return;
        }

        foreach (var employee in employees)
        {
            var departmentName = employee.DepartmentId.HasValue
                && departmentNamesById.TryGetValue(employee.DepartmentId.Value, out var name)
                ? name
                : "Genel";
            employee.JobDescriptionDraft = JobDescriptionTemplate.BuildJson(employee.Role.ToString(), departmentName);
        }

        await context.SaveChangesAsync();
    }

    private static int CountDays(DateTime startDate, DateTime endDate)
    {
        var days = (endDate.Date - startDate.Date).Days + 1;
        return days < 0 ? 0 : days;
    }
}
