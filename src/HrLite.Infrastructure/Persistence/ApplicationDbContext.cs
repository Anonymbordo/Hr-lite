using HrLite.Application.Interfaces;
using HrLite.Domain.Common;
using HrLite.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace HrLite.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext
{
    private readonly ICurrentUserService? _currentUserService;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        ICurrentUserService currentUserService)
        : base(options)
    {
        _currentUserService = currentUserService;
    }

    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();
    public DbSet<LeaveType> LeaveTypes => Set<LeaveType>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // --- EMPLOYEE (ÇALIŞAN) AYARLARI ---
        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(200);
            
            // Email Eşsizliği
            entity.HasIndex(e => e.Email).IsUnique();
            
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Phone).HasMaxLength(30);
            entity.Property(e => e.Salary).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Status).IsRequired();
            
            // Employee.cs içinde bu alan varsa açık kalsın:
            entity.Property(e => e.PasswordHash).IsRequired();

           // Departman İlişkisi
            entity.HasOne(e => e.Department)
                .WithMany(d => d.Employees) 
                .HasForeignKey(e => e.DepartmentId)
                .OnDelete(DeleteBehavior.SetNull);

            // Yönetici İlişkisi
            entity.HasOne(e => e.Manager)
                .WithMany(e => e.DirectReports)
                .HasForeignKey(e => e.ManagerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // --- DEPARTMENT AYARLARI ---
        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasKey(d => d.Id);
            entity.Property(d => d.Name).IsRequired().HasMaxLength(200);
            entity.HasIndex(d => d.Name).IsUnique();
        });

        // --- LEAVE REQUEST (İZİN) AYARLARI ---
        modelBuilder.Entity<LeaveRequest>(entity =>
        {
            entity.HasKey(lr => lr.Id);
            entity.Property(lr => lr.Reason).IsRequired().HasMaxLength(500);
            entity.Property(lr => lr.Days).IsRequired();

            entity.HasOne(lr => lr.Employee)
                .WithMany(e => e.LeaveRequests)
                .HasForeignKey(lr => lr.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(lr => lr.LeaveType)
                .WithMany(lt => lt.LeaveRequests)
                .HasForeignKey(lr => lr.LeaveTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // --- LEAVE TYPE (İZİN TÜRÜ) AYARLARI ---
        modelBuilder.Entity<LeaveType>(entity =>
        {
            entity.HasKey(lt => lt.Id);
            entity.Property(lt => lt.Code).IsRequired().HasMaxLength(50);
            entity.Property(lt => lt.Name).IsRequired().HasMaxLength(200);
            entity.Property(lt => lt.DefaultAnnualQuotaDays).IsRequired();

            entity.HasIndex(lt => lt.Code).IsUnique();
        });
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.AddInterceptors(new AuditInterceptor(_currentUserService));
    }
}
