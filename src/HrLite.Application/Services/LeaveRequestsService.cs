using HrLite.Application.Common;
using HrLite.Application.Common.Exceptions;
using HrLite.Application.DTOs;
using HrLite.Application.DTOs.Leave;
using HrLite.Application.Interfaces;
using HrLite.Application.Validators;
using HrLite.Domain.Enums;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace HrLite.Application.Services;

public class LeaveRequestsService : ILeaveRequestsService
{
    private const string AnnualLeaveTypeCodeDefault = "ANNUAL";
    private static readonly CreateLeaveRequestDtoValidator CreateValidator = new();
    private static readonly RejectLeaveRequestDtoValidator RejectValidator = new();
    private static readonly NormalizeLeaveReasonRequestDtoValidator NormalizeValidator = new();

    private readonly ILeaveRequestRepository _leaveRequests;
    private readonly ILeaveTypeRepository _leaveTypes;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly ILlmClient _llmClient;
    private readonly IConfiguration _configuration;

    public LeaveRequestsService(
        ILeaveRequestRepository leaveRequests,
        ILeaveTypeRepository leaveTypes,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        ILlmClient llmClient,
        IConfiguration configuration)
    {
        _leaveRequests = leaveRequests;
        _leaveTypes = leaveTypes;
        _unitOfWork = unitOfWork;
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

        var result = await _leaveRequests.GetPagedAsync(
            statusFilter,
            isEmployee ? currentEmployeeId : null,
            page,
            pageSize);

        var items = result.Items.Select(ToDto).ToList();

        return new PagedResultDto<LeaveRequestDto>
        {
            Items = items,
            TotalCount = result.TotalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<LeaveRequestDto> GetByIdAsync(Guid id)
    {
        EnsureAuthenticated();

        var entity = await _leaveRequests.GetByIdWithLeaveTypeAsync(id);

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

        ValidationHelper.ValidateAndThrow(CreateValidator, dto);

        var start = dto.StartDate.Date;
        var end = dto.EndDate.Date;

        var leaveType = await _leaveTypes.GetByCodeAsync(dto.LeaveTypeCode);

        if (leaveType == null)
        {
            throw new ValidationException($"Unknown leave type code: {dto.LeaveTypeCode}");
        }

        var employeeId = _currentUser.UserId;

        await EnsureNoOverlapAsync(employeeId, start, end);
        await EnsureAnnualQuotaAsync(employeeId, leaveType, start, end);

        var entity = new HrLite.Domain.Entities.LeaveRequest
        {
            EmployeeId = employeeId,
            LeaveTypeId = leaveType.Id,
            StartDate = start,
            EndDate = end,
            Days = CountInclusiveDays(start, end),
            Reason = dto.Reason.Trim(),
            Status = LeaveStatus.Pending
        };

        _leaveRequests.Add(entity);
        await _unitOfWork.SaveChangesAsync();

        var created = await _leaveRequests.GetByIdWithLeaveTypeAsync(entity.Id);
        if (created == null)
        {
            throw new NotFoundException($"Leave request not found. Id: {entity.Id}");
        }

        return ToDto(created);
    }

    public async Task<LeaveRequestDto> ApproveAsync(Guid id)
    {
        EnsureAuthenticated();
        EnsureHrOrAdmin();

        var entity = await _leaveRequests.GetByIdWithLeaveTypeAsync(id);

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
        await EnsureAnnualQuotaAsync(entity.EmployeeId, entity.LeaveType, entity.StartDate.Date, entity.EndDate.Date, excludeLeaveRequestId: entity.Id);

        entity.Status = LeaveStatus.Approved;
        entity.ApprovedBy = _currentUser.UserId;
        entity.ApprovedAt = DateTime.UtcNow;
        entity.RejectReason = null;

        await _unitOfWork.SaveChangesAsync();

        return ToDto(entity);
    }

    public async Task<LeaveRequestDto> RejectAsync(Guid id, RejectLeaveRequestDto dto)
    {
        EnsureAuthenticated();
        EnsureHrOrAdmin();

        if (dto == null)
        {
            throw new ValidationException("Request body is required.");
        }
        ValidationHelper.ValidateAndThrow(RejectValidator, dto);

        var entity = await _leaveRequests.GetByIdWithLeaveTypeAsync(id);

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
        entity.RejectReason = dto.RejectReason.Trim();

        await _unitOfWork.SaveChangesAsync();

        return ToDto(entity);
    }

    public async Task<LeaveRequestDto> CancelAsync(Guid id)
    {
        EnsureAuthenticated();

        var entity = await _leaveRequests.GetByIdWithLeaveTypeAsync(id);

        if (entity == null)
        {
            throw new NotFoundException($"Leave request not found. Id: {id}");
        }

        // Employee can only cancel their own request.
        if (IsEmployee() && entity.EmployeeId != _currentUser.UserId)
        {
            throw new ForbiddenException();
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

        await _unitOfWork.SaveChangesAsync();

        return ToDto(entity);
    }

    public async Task<NormalizeLeaveReasonResponseDto> NormalizeReasonAsync(NormalizeLeaveReasonRequestDto dto)
    {
        EnsureAuthenticated();

        if (dto == null)
        {
            throw new ValidationException("Request body is required.");
        }
        ValidationHelper.ValidateAndThrow(NormalizeValidator, dto);

        var allowedCodes = await _leaveTypes.GetAllCodesAsync();

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

    public async Task<ExplainDecisionResponseDto> ExplainDecisionAsync(Guid id)
    {
        EnsureAuthenticated();

        var entity = await _leaveRequests.GetByIdWithLeaveTypeAsync(id);

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
            totalDays = entity.Days,
            status = entity.Status.ToString(),
            rejectReason = entity.RejectReason
        };

        var factsJson = JsonSerializer.Serialize(facts, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var fallbackExplanation = BuildDecisionExplanation(entity);
        string json;
        try
        {
            json = await _llmClient.ExplainLeaveDecisionAsync(factsJson);
        }
        catch (TaskCanceledException)
        {
            return new ExplainDecisionResponseDto { Explanation = fallbackExplanation };
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<ExplainDecisionResponseDto>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (parsed == null ||
                string.IsNullOrWhiteSpace(parsed.Explanation) ||
                LooksLikeAiFailure(parsed.Explanation))
            {
                return new ExplainDecisionResponseDto { Explanation = fallbackExplanation };
            }

            return parsed;
        }
        catch (JsonException)
        {
            return new ExplainDecisionResponseDto { Explanation = fallbackExplanation };
        }
    }

    private static bool LooksLikeAiFailure(string? explanation)
    {
        if (string.IsNullOrWhiteSpace(explanation))
        {
            return true;
        }

        return explanation.Contains("AI aciklamasi", StringComparison.OrdinalIgnoreCase)
            || explanation.Contains("uretilemedi", StringComparison.OrdinalIgnoreCase)
            || explanation.Contains("servisi", StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildDecisionExplanation(HrLite.Domain.Entities.LeaveRequest entity)
    {
        var dateRange = $"{entity.StartDate:yyyy-MM-dd} - {entity.EndDate:yyyy-MM-dd}";
        var baseLine =
            $"Izin talebi {entity.LeaveType.Name} ({entity.LeaveType.Code}) turundedir ve {dateRange} tarihlerini kapsar. " +
            $"Toplam {entity.Days} gun icin kaydedilmistir.";

        var statusLine = entity.Status switch
        {
            LeaveStatus.Approved => BuildApprovedLine(entity.ApprovedAt),
            LeaveStatus.Rejected => BuildRejectedLine(entity.RejectReason),
            LeaveStatus.Pending => "Durum: Incelemede. Onay veya ret islemi henuz tamamlanmamistir.",
            _ => $"Durum: {entity.Status}."
        };

        return $"{baseLine} {statusLine}";
    }

    private static string BuildApprovedLine(DateTime? approvedAt)
    {
        if (approvedAt.HasValue)
        {
            return $"Durum: Onaylandi. Islem tarihi: {approvedAt:yyyy-MM-dd HH:mm}.";
        }

        return "Durum: Onaylandi. Onay tarihi kaydedilmemistir.";
    }

    private static string BuildRejectedLine(string? rejectReason)
    {
        if (!string.IsNullOrWhiteSpace(rejectReason))
        {
            return $"Durum: Reddedildi. Red nedeni: {rejectReason.Trim()}";
        }

        return "Durum: Reddedildi. Red nedeni kaydedilmemistir.";
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
            Days = lr.Days,
            Reason = lr.Reason,
            Status = lr.Status,
            ApprovedBy = lr.ApprovedBy,
            ApprovedAt = lr.ApprovedAt,
            RejectReason = lr.RejectReason
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
            throw new UnauthorizedException();
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
            throw new ForbiddenException();
        }
    }

    private void EnsureCanAccessLeave(Guid employeeId)
    {
        if (IsEmployee() && employeeId != _currentUser.UserId)
        {
            throw new ForbiddenException();
        }
    }

    private async Task EnsureNoOverlapAsync(Guid employeeId, DateTime newStart, DateTime newEnd, Guid? excludeLeaveRequestId = null)
    {
        var candidates = await _leaveRequests.GetForEmployeeWithStatusesAsync(
            employeeId,
            new[] { LeaveStatus.Pending, LeaveStatus.Approved },
            excludeLeaveRequestId);

        var overlaps = candidates.Any(lr =>
            newStart <= lr.EndDate.Date &&
            newEnd >= lr.StartDate.Date);

        if (overlaps)
        {
            throw new BusinessException("Leave request dates overlap with an existing pending/approved request.", "LEAVE_OVERLAP");
        }
    }

    private async Task EnsureAnnualQuotaAsync(
        Guid employeeId,
        HrLite.Domain.Entities.LeaveType leaveType,
        DateTime newStart,
        DateTime newEnd,
        Guid? excludeLeaveRequestId = null)
    {
        var annualCode = _configuration.GetValue<string>("LeavePolicy:AnnualLeaveTypeCode", AnnualLeaveTypeCodeDefault)
            ?? AnnualLeaveTypeCodeDefault;

        if (!string.Equals(leaveType.Code, annualCode, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var annualQuotaDays = leaveType.DefaultAnnualQuotaDays;
        if (annualQuotaDays <= 0)
        {
            annualQuotaDays = _configuration.GetValue<int>("LeavePolicy:AnnualQuotaDays", 14);
        }
        if (annualQuotaDays <= 0)
        {
            annualQuotaDays = 14;
        }

        var approvedAnnualLeaves = await _leaveRequests.GetApprovedForEmployeeByLeaveTypeCodeAsync(
            employeeId,
            annualCode,
            excludeLeaveRequestId);

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
