using FluentValidation;

namespace Lab.DictionaryFluentValidation.Validators;

public class RequireFieldValidator : AbstractValidator<string>
{
    public RequireFieldValidator()
    {
        this.RuleFor(customer => customer).NotEmpty();
    }
}