using FluentValidation;
using HrLite.Application.DTOs.Leave;

namespace HrLite.Application.Validators;

public class RejectLeaveRequestDtoValidator : AbstractValidator<RejectLeaveRequestDto>
{
    public RejectLeaveRequestDtoValidator()
    {
        RuleFor(x => x.RejectReason).NotEmpty();
    }
}
