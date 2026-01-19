using HrLite.Application.Common.Exceptions;
using HrLite.Application.DTOs.Reports;
using HrLite.Application.Interfaces;
using HrLite.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace HrLite.Application.Services;

public class ReportsService : IReportsService
{
    private readonly IApplicationDbContext _context;
    private readonly ILlmClient _llmClient;

    public ReportsService(IApplicationDbContext context, ILlmClient llmClient)
    {
        _context = context;
        _llmClient = llmClient;
    }

    public async Task<List<HeadcountByDepartmentDto>> GetHeadcountByDepartmentAsync()
    {
        var headcounts = await _context.Employees
            .Where(e => e.IsActive)
            .GroupBy(e => e.Department!.Name)
            .Select(g => new HeadcountByDepartmentDto
            {
                DepartmentName = g.Key,
                EmployeeCount = g.Count()
            })
            .OrderByDescending(h => h.EmployeeCount)
            .ToListAsync();

        return headcounts;
    }

    public async Task<List<LeaveRequestsMonthlyDto>> GetLeaveRequestsMonthlyAsync(int year)
    {
        if (year < 2000 || year > 2100)
        {
            throw new ValidationException("Invalid year. Must be between 2000 and 2100.");
        }

        var leaveRequests = await _context.LeaveRequests
            .Where(lr => lr.StartDate.Year == year)
            .ToListAsync();

        var monthlyData = leaveRequests
            .GroupBy(lr => lr.StartDate.Month)
            .Select(g => new LeaveRequestsMonthlyDto
            {
                Year = year,
                Month = g.Key,
                TotalRequests = g.Count(),
                ApprovedRequests = g.Count(lr => lr.Status == LeaveStatus.Approved),
                PendingRequests = g.Count(lr => lr.Status == LeaveStatus.Pending),
                RejectedRequests = g.Count(lr => lr.Status == LeaveStatus.Rejected)
            })
            .OrderBy(m => m.Month)
            .ToList();

        return monthlyData;
    }

    public async Task<AiInsightsResponse> GetAiInsightsAsync(int year)
    {
        if (year < 2000 || year > 2100)
        {
            throw new ValidationException("Invalid year. Must be between 2000 and 2100.");
        }

        // Get aggregated data (NO raw employee data)
        var headcounts = await GetHeadcountByDepartmentAsync();
        var monthlyLeaves = await GetLeaveRequestsMonthlyAsync(year);

        var aggregatedData = new
        {
            year,
            headcountByDepartment = headcounts,
            leaveRequestsMonthly = monthlyLeaves,
            totalEmployees = headcounts.Sum(h => h.EmployeeCount),
            totalLeaveRequests = monthlyLeaves.Sum(m => m.TotalRequests)
        };

        var jsonData = JsonSerializer.Serialize(aggregatedData, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        try
        {
            var llmResponse = await _llmClient.GenerateInsightsAsync(jsonData);
            var insights = JsonSerializer.Deserialize<AiInsightsResponse>(llmResponse, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (insights == null)
            {
                throw new BusinessException("Failed to parse AI insights response.", "AI_PARSE_ERROR");
            }

            return insights;
        }
        catch (JsonException ex)
        {
            throw new BusinessException($"Failed to parse AI response: {ex.Message}", "AI_PARSE_ERROR");
        }
        catch (TaskCanceledException)
        {
            throw new BusinessException("AI request timed out.", "AI_TIMEOUT");
        }
    }
}
