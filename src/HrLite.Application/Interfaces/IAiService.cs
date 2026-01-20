using HrLite.Application.DTOs;

namespace HrLite.Application.Interfaces;

public interface IAiService
{
    // Görev ismi ve departmana göre JSON şemalı iş tanımı isteği
    Task<JobDescriptionDraftDto> GenerateJobDescriptionAsync(string roleName, string departmentName);
}