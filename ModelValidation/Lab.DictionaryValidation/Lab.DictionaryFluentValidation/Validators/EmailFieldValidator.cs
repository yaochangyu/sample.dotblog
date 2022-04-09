using FluentValidation;

namespace Lab.DictionaryFluentValidation.Validators;

public class EmailFieldValidator : AbstractValidator<string>
{
    public EmailFieldValidator()
    {
        this.RuleFor(p => p).EmailAddress();
    }
}