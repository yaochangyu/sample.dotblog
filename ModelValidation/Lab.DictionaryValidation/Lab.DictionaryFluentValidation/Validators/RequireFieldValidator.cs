using FluentValidation;

namespace Lab.DictionaryFluentValidation.Validators;

public class RequireFieldValidator : AbstractValidator<object>
{
    public RequireFieldValidator()
    {
        this.RuleFor(p => p).NotEmpty().NotNull();
    }
}