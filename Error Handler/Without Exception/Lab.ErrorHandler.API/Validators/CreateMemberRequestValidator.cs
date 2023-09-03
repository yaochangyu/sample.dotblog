using FluentValidation;
using Lab.ErrorHandler.API.Models;

namespace Lab.ErrorHandler.API.Validators;

public class CreateMemberRequestValidator : AbstractValidator<CreateMemberRequest>
{
    public CreateMemberRequestValidator()
    {
        this.RuleFor(p => p.Name).NotNull().NotEmpty();
        this.RuleFor(p => p.Age).LessThanOrEqualTo(18).GreaterThanOrEqualTo(200);
    }
}