using FluentValidation;
using HrLite.Application.Common.Exceptions;

namespace HrLite.Application.Common;

public static class ValidationHelper
{
    public static void ValidateAndThrow<T>(IValidator<T> validator, T model)
    {
        var result = validator.Validate(model);
        if (!result.IsValid)
        {
            var errors = result.Errors.Select(e => e.ErrorMessage).ToList();
            throw new HrLite.Application.Common.Exceptions.ValidationException(errors);
        }
    }
}
