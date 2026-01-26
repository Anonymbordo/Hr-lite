using FluentValidation;
using HrLite.Application.DTOs;
using HrLite.Domain.Enums;

namespace HrLite.Application.Validators;

public class CreateEmployeeDtoValidator : AbstractValidator<CreateEmployeeDto>
{
    public CreateEmployeeDtoValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty();
        RuleFor(x => x.LastName).NotEmpty();
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Role)
            .Must(role => Enum.IsDefined(typeof(Role), role))
            .WithMessage("Role is invalid.");
        RuleFor(x => x.Status)
            .Must(status => Enum.IsDefined(typeof(EmployeeStatus), status))
            .WithMessage("Status is invalid.");
        RuleFor(x => x.HireDate).NotEmpty();
        RuleFor(x => x.Salary).GreaterThanOrEqualTo(0);
    }
}
