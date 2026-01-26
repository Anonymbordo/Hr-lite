using FluentValidation;
using HrLite.Application.DTOs.Leave;

namespace HrLite.Application.Validators;

public class NormalizeLeaveReasonRequestDtoValidator : AbstractValidator<NormalizeLeaveReasonRequestDto>
{
    public NormalizeLeaveReasonRequestDtoValidator()
    {
        RuleFor(x => x.Text).NotEmpty();
    }
}
