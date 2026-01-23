using HrLite.Application.Common.Exceptions;
using HrLite.Application.DTOs;
using HrLite.Application.DTOs.Leave;
using HrLite.Application.Interfaces;
using HrLite.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace HrLite.Application.Services;

public class LeaveRequestsService : ILeaveRequestsService
{
    private const string AnnualLeaveTypeCodeDefault = "Annual";

    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly ILlmClient _llmClient;
    private readonly IConfiguration _configuration;

    public LeaveRequestsService(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        ILlmClient llmClient,
        IConfiguration configuration)
    {
        _context = context;
        _currentUser = currentUser;
        _llmClient = llmClient;
        _configuration = configuration;
    }

    public async Task<PagedResultDto<LeaveRequestDto>> GetAsync(LeaveRequestQueryParameters query)
    {
        EnsureAuthenticated();

        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize < 1 ? 20 : query.PageSize;

        LeaveStatus? statusFilter = null;
        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            if (!Enum.TryParse<LeaveStatus>(query.Status, ignoreCase: true, out var parsed))
            {
                throw new ValidationException($"Invalid status: {query.Status}");
            }

            statusFilter = parsed;
        }

        var isEmployee = IsEmployee();
        var currentEmployeeId = _currentUser.UserId;

        var q = _context.LeaveRequests
            .AsNoTracking()
            .Include(lr => lr.LeaveType)
            .Where(lr => !isEmployee || lr.EmployeeId == currentEmployeeId);

        if (statusFilter.HasValue)
        {
            q = q.Where(lr => lr.Status == statusFilter.Value);
        }

        var totalCount = await q.CountAsync();

        var items = await q
            .OrderByDescending(lr => lr.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(lr => ToDto(lr))
            .ToListAsync();

        return new PagedResultDto<LeaveRequestDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<LeaveRequestDto> GetByIdAsync(int id)
    {
        EnsureAuthenticated();

        var entity = await _context.LeaveRequests
            .AsNoTracking()
            .Include(lr => lr.LeaveType)
            .FirstOrDefaultAsync(lr => lr.Id == id);

        if (entity == null)
        {
            throw new NotFoundException($"Leave request not found. Id: {id}");
        }

        EnsureCanAccessLeave(entity.EmployeeId);

        return ToDto(entity);
    }

    public async Task<LeaveRequestDto> CreateAsync(CreateLeaveRequestDto dto)
    {
        EnsureAuthenticated();

        if (dto == null)
        {
            throw new ValidationException("Request body is required.");
        }

        if (string.IsNullOrWhiteSpace(dto.LeaveTypeCode))
        {
            throw new ValidationException("LeaveTypeCode is required.");
        }

        if (string.IsNullOrWhiteSpace(dto.Reason))
        {
            throw new ValidationException("Reason is required.");
        }

        var start = dto.StartDate.Date;
        var end = dto.EndDate.Date;
        if (start > end)
        {
            throw new ValidationException("StartDate must be less than or equal to EndDate.");
        }

        var leaveType = await _context.LeaveTypes
            .FirstOrDefaultAsync(lt => lt.Code.ToLower() == dto.LeaveTypeCode.Trim().ToLower());

        if (leaveType == null)
        {
            throw new ValidationException($"Unknown leave type code: {dto.LeaveTypeCode}");
        }

        var employeeId = _currentUser.UserId;

        await EnsureNoOverlapAsync(employeeId, start, end);
        await EnsureAnnualQuotaAsync(employeeId, leaveType.Code, start, end);

        var entity = new HrLite.Domain.Entities.LeaveRequest
        {
            EmployeeId = employeeId,
            LeaveTypeId = leaveType.Id,
            StartDate = start,
            EndDate = end,
            Reason = dto.Reason.Trim(),
            Status = LeaveStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = employeeId
        };

        _context.LeaveRequests.Add(entity);
        await _context.SaveChangesAsync();

        var created = await _context.LeaveRequests
            .AsNoTracking()
            .Include(lr => lr.LeaveType)
            .FirstAsync(lr => lr.Id == entity.Id);

        return ToDto(created);
    }

    public async Task<LeaveRequestDto> ApproveAsync(int id)
    {
        EnsureAuthenticated();
        EnsureHrOrAdmin();

        var entity = await _context.LeaveRequests
            .Include(lr => lr.LeaveType)
            .FirstOrDefaultAsync(lr => lr.Id == id);

        if (entity == null)
        {
            throw new NotFoundException($"Leave request not found. Id: {id}");
        }

        if (entity.Status == LeaveStatus.Approved)
        {
            throw new BusinessException("Leave request already approved.", "LEAVE_ALREADY_APPROVED");
        }

        if (entity.Status != LeaveStatus.Pending)
        {
            throw new BusinessException($"Leave request cannot be approved from status: {entity.Status}", "INVALID_STATUS_TRANSITION");
        }

        // Safety check: even if it passed at create time, ensure no overlap and quota at approval.
        await EnsureNoOverlapAsync(entity.EmployeeId, entity.StartDate.Date, entity.EndDate.Date, excludeLeaveRequestId: entity.Id);
        await EnsureAnnualQuotaAsync(entity.EmployeeId, entity.LeaveType.Code, entity.StartDate.Date, entity.EndDate.Date, excludeLeaveRequestId: entity.Id);

        entity.Status = LeaveStatus.Approved;
        entity.ApprovedBy = _currentUser.UserId;
        entity.ApprovedAt = DateTime.UtcNow;
        entity.RejectionReason = null;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = _currentUser.UserId;

        await _context.SaveChangesAsync();

        return ToDto(entity);
    }

    public async Task<LeaveRequestDto> RejectAsync(int id, RejectLeaveRequestDto dto)
    {
        EnsureAuthenticated();
        EnsureHrOrAdmin();

        if (dto == null || string.IsNullOrWhiteSpace(dto.RejectionReason))
        {
            throw new ValidationException("RejectionReason is required.");
        }

        var entity = await _context.LeaveRequests
            .Include(lr => lr.LeaveType)
            .FirstOrDefaultAsync(lr => lr.Id == id);

        if (entity == null)
        {
            throw new NotFoundException($"Leave request not found. Id: {id}");
        }

        if (entity.Status == LeaveStatus.Rejected)
        {
            throw new BusinessException("Leave request already rejected.", "LEAVE_ALREADY_REJECTED");
        }

        if (entity.Status != LeaveStatus.Pending)
        {
            throw new BusinessException($"Leave request cannot be rejected from status: {entity.Status}", "INVALID_STATUS_TRANSITION");
        }

        entity.Status = LeaveStatus.Rejected;
        entity.RejectionReason = dto.RejectionReason.Trim();
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = _currentUser.UserId;

        await _context.SaveChangesAsync();

        return ToDto(entity);
    }

    public async Task<LeaveRequestDto> CancelAsync(int id)
    {
        EnsureAuthenticated();

        var entity = await _context.LeaveRequests
            .Include(lr => lr.LeaveType)
            .FirstOrDefaultAsync(lr => lr.Id == id);

        if (entity == null)
        {
            throw new NotFoundException($"Leave request not found. Id: {id}");
        }

        // Employee can only cancel their own request.
        if (IsEmployee() && entity.EmployeeId != _currentUser.UserId)
        {
            throw new UnauthorizedAccessException();
        }

        if (entity.Status == LeaveStatus.Cancelled)
        {
            throw new BusinessException("Leave request already cancelled.", "LEAVE_ALREADY_CANCELLED");
        }

        if (entity.Status != LeaveStatus.Pending)
        {
            throw new BusinessException($"Leave request cannot be cancelled from status: {entity.Status}", "INVALID_STATUS_TRANSITION");
        }

        entity.Status = LeaveStatus.Cancelled;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = _currentUser.UserId;

        await _context.SaveChangesAsync();

        return ToDto(entity);
    }

    public async Task<NormalizeLeaveReasonResponseDto> NormalizeReasonAsync(NormalizeLeaveReasonRequestDto dto)
    {
        EnsureAuthenticated();

        if (dto == null || string.IsNullOrWhiteSpace(dto.Text))
        {
            throw new ValidationException("Text is required.");
        }

        var allowedCodes = await _context.LeaveTypes
            .AsNoTracking()
            .OrderBy(lt => lt.Id)
            .Select(lt => lt.Code)
            .ToListAsync();

        var json = await _llmClient.NormalizeLeaveReasonAsync(dto.Text, allowedCodes);

        try
        {
            var parsed = JsonSerializer.Deserialize<NormalizeLeaveReasonResponseDto>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (parsed == null)
            {
                throw new BusinessException("Failed to parse AI normalize response.", "AI_PARSE_ERROR");
            }

            // Clamp suggested leave type to allowed list.
            if (string.IsNullOrWhiteSpace(parsed.SuggestedLeaveTypeCode) ||
                !allowedCodes.Any(c => string.Equals(c, parsed.SuggestedLeaveTypeCode, StringComparison.OrdinalIgnoreCase)))
            {
                parsed.SuggestedLeaveTypeCode = allowedCodes.FirstOrDefault() ?? string.Empty;
            }

            return parsed;
        }
        catch (JsonException ex)
        {
            throw new BusinessException($"Failed to parse AI response: {ex.Message}", "AI_PARSE_ERROR");
        }
    }

    public async Task<ExplainDecisionResponseDto> ExplainDecisionAsync(int id)
    {
        EnsureAuthenticated();

        var entity = await _context.LeaveRequests
            .AsNoTracking()
            .Include(lr => lr.LeaveType)
            .FirstOrDefaultAsync(lr => lr.Id == id);

        if (entity == null)
        {
            throw new NotFoundException($"Leave request not found. Id: {id}");
        }

        EnsureCanAccessLeave(entity.EmployeeId);

        var facts = new
        {
            leaveRequestId = entity.Id,
            employeeId = entity.EmployeeId,
            leaveTypeCode = entity.LeaveType.Code,
            leaveTypeName = entity.LeaveType.Name,
            startDate = entity.StartDate.Date,
            endDate = entity.EndDate.Date,
            totalDays = CountInclusiveDays(entity.StartDate.Date, entity.EndDate.Date),
            status = entity.Status.ToString(),
            rejectionReason = entity.RejectionReason
        };

        var factsJson = JsonSerializer.Serialize(facts, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var json = await _llmClient.ExplainLeaveDecisionAsync(factsJson);

        try
        {
            var parsed = JsonSerializer.Deserialize<ExplainDecisionResponseDto>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (parsed == null)
            {
                throw new BusinessException("Failed to parse AI explanation response.", "AI_PARSE_ERROR");
            }

            return parsed;
        }
        catch (JsonException ex)
        {
            throw new BusinessException($"Failed to parse AI response: {ex.Message}", "AI_PARSE_ERROR");
        }
    }

    private static LeaveRequestDto ToDto(HrLite.Domain.Entities.LeaveRequest lr)
    {
        return new LeaveRequestDto
        {
            Id = lr.Id,
            EmployeeId = lr.EmployeeId,
            LeaveTypeId = lr.LeaveTypeId,
            LeaveTypeCode = lr.LeaveType.Code,
            LeaveTypeName = lr.LeaveType.Name,
            StartDate = lr.StartDate,
            EndDate = lr.EndDate,
            TotalDays = CountInclusiveDays(lr.StartDate.Date, lr.EndDate.Date),
            Reason = lr.Reason,
            Status = lr.Status,
            ApprovedBy = lr.ApprovedBy,
            ApprovedAt = lr.ApprovedAt,
            RejectionReason = lr.RejectionReason
        };
    }

    private static int CountInclusiveDays(DateTime startDate, DateTime endDate)
    {
        var days = (endDate.Date - startDate.Date).Days + 1;
        return days < 0 ? 0 : days;
    }

    private void EnsureAuthenticated()
    {
        if (!_currentUser.IsAuthenticated)
        {
            throw new UnauthorizedAccessException();
        }
    }

    private bool IsEmployee()
        => string.Equals(_currentUser.Role, Role.Employee.ToString(), StringComparison.OrdinalIgnoreCase);

    private bool IsHrOrAdmin()
        => string.Equals(_currentUser.Role, Role.HR.ToString(), StringComparison.OrdinalIgnoreCase)
           || string.Equals(_currentUser.Role, Role.Admin.ToString(), StringComparison.OrdinalIgnoreCase);

    private void EnsureHrOrAdmin()
    {
        if (!IsHrOrAdmin())
        {
            throw new UnauthorizedAccessException();
        }
    }

    private void EnsureCanAccessLeave(int employeeId)
    {
        if (IsEmployee() && employeeId != _currentUser.UserId)
        {
            throw new UnauthorizedAccessException();
        }
    }

    private async Task EnsureNoOverlapAsync(int employeeId, DateTime newStart, DateTime newEnd, int? excludeLeaveRequestId = null)
    {
        var q = _context.LeaveRequests
            .AsNoTracking()
            .Where(lr => lr.EmployeeId == employeeId)
            .Where(lr => lr.Status == LeaveStatus.Pending || lr.Status == LeaveStatus.Approved);

        if (excludeLeaveRequestId.HasValue)
        {
            q = q.Where(lr => lr.Id != excludeLeaveRequestId.Value);
        }

        var overlaps = await q.AnyAsync(lr =>
            newStart <= lr.EndDate.Date &&
            newEnd >= lr.StartDate.Date);

        if (overlaps)
        {
            throw new BusinessException("Leave request dates overlap with an existing pending/approved request.", "LEAVE_OVERLAP");
        }
    }

    private async Task EnsureAnnualQuotaAsync(
        int employeeId,
        string leaveTypeCode,
        DateTime newStart,
        DateTime newEnd,
        int? excludeLeaveRequestId = null)
    {
        var annualCode = _configuration.GetValue<string>("LeavePolicy:AnnualLeaveTypeCode", AnnualLeaveTypeCodeDefault)
            ?? AnnualLeaveTypeCodeDefault;

        if (!string.Equals(leaveTypeCode, annualCode, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var annualQuotaDays = _configuration.GetValue<int>("LeavePolicy:AnnualQuotaDays", 14);
        if (annualQuotaDays <= 0)
        {
            annualQuotaDays = 14;
        }

        var q = _context.LeaveRequests
            .AsNoTracking()
            .Include(lr => lr.LeaveType)
            .Where(lr => lr.EmployeeId == employeeId)
            .Where(lr => lr.Status == LeaveStatus.Approved)
            .Where(lr => lr.LeaveType.Code.ToLower() == annualCode.ToLower());

        if (excludeLeaveRequestId.HasValue)
        {
            q = q.Where(lr => lr.Id != excludeLeaveRequestId.Value);
        }

        var approvedAnnualLeaves = await q.ToListAsync();

        for (var year = newStart.Year; year <= newEnd.Year; year++)
        {
            var used = approvedAnnualLeaves.Sum(lr => CountDaysWithinYear(lr.StartDate.Date, lr.EndDate.Date, year));
            var requested = CountDaysWithinYear(newStart, newEnd, year);

            if (used + requested > annualQuotaDays)
            {
                throw new BusinessException(
                    $"Annual leave quota exceeded for year {year}. Used: {used}, Requested: {requested}, Quota: {annualQuotaDays}.",
                    "ANNUAL_QUOTA_EXCEEDED");
            }
        }
    }

    private static int CountDaysWithinYear(DateTime startDate, DateTime endDate, int year)
    {
        var yearStart = new DateTime(year, 1, 1);
        var yearEnd = new DateTime(year, 12, 31);

        var from = startDate.Date < yearStart ? yearStart : startDate.Date;
        var to = endDate.Date > yearEnd ? yearEnd : endDate.Date;

        if (from > to)
        {
            return 0;
        }

        return CountInclusiveDays(from, to);
    }
}
