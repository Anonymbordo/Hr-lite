using FluentValidation;
using HrLite.Application.DTOs.Leave;

namespace HrLite.Application.Validators;

public class CreateLeaveRequestDtoValidator : AbstractValidator<CreateLeaveRequestDto>
{
    public CreateLeaveRequestDtoValidator()
    {
        RuleFor(x => x.LeaveTypeCode).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty();
        RuleFor(x => x.StartDate).NotEmpty();
        RuleFor(x => x.EndDate).NotEmpty();
        RuleFor(x => x)
            .Must(x => x.StartDate.Date <= x.EndDate.Date)
            .WithMessage("StartDate must be less than or equal to EndDate.");
    }
}
