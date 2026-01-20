using HrLite.Application.DTOs;
using HrLite.Application.Interfaces;
using HrLite.Domain.Entities;
using HrLite.Domain.Enums;
using HrLite.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HrLite.Infrastructure.Services;

public class EmployeeService : IEmployeeService
{
    private readonly ApplicationDbContext _context;
    private readonly IAiService _aiService;

    public EmployeeService(ApplicationDbContext context, IAiService aiService)
    {
        _context = context;
        _aiService = aiService;
    }

    public async Task<List<EmployeeDto>> GetAllAsync()
    {
        return await _context.Employees
            .Include(e => e.Department) // İlişkiyi getir
            .Select(e => new EmployeeDto
            {
                Id = e.Id,
                FirstName = e.FirstName,
                LastName = e.LastName,
                Email = e.Email,
                Role = e.Role.ToString(),
                DepartmentName = e.Department != null ? e.Department.Name : "Departman Yok",
                JobDescriptionDraft = e.JobDescriptionDraft
            })
            .ToListAsync();
    }

    public async Task<EmployeeDto> CreateAsync(CreateEmployeeDto dto)
    {
        // 1. KURAL: Email Unique Kontrolü (Uygulama Seviyesinde)
        // Veritabanında Index var ama kullanıcıya düzgün hata dönmek için burada da bakıyoruz.
        if (await _context.Employees.AnyAsync(e => e.Email == dto.Email))
        {
            throw new Exception($"'{dto.Email}' adresi zaten kullanımda!");
        }

        var entity = new Employee
        {
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Email = dto.Email,
            Role = (Role)dto.Role,
            DepartmentId = dto.DepartmentId,
            HireDate = dto.HireDate,
            PasswordHash = "defaultHash", // Şimdilik varsayılan
            IsActive = true
        };

        _context.Employees.Add(entity);
        await _context.SaveChangesAsync();

        return new EmployeeDto { Id = entity.Id, Email = entity.Email };
    }

    public async Task<string> GenerateAiJobDescriptionAsync(int employeeId)
    {
        var employee = await _context.Employees
            .Include(e => e.Department)
            .FirstOrDefaultAsync(e => e.Id == employeeId);

        if (employee == null) throw new Exception("Çalışan bulunamadı.");

        // 2. KURAL: PII (Kişisel Veri) Koruması
        // AI'a Ahmet'in adını, emailini göndermiyoruz! Sadece rolünü ve departmanını gönderiyoruz.
        var roleName = employee.Role.ToString();
        var departmentName = employee.Department?.Name ?? "Genel";

        // AI Servisini çağırıyoruz
        var aiResponse = await _aiService.GenerateJobDescriptionAsync(roleName, departmentName);

        // 3. KURAL: AI Çıktısını Veritabanına Kaydetme
        employee.JobDescriptionDraft = aiResponse;
        await _context.SaveChangesAsync();

        return aiResponse;
    }
}