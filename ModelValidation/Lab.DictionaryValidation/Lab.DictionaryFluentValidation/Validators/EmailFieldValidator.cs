using FluentValidation;
using FluentValidation.Results;
using Lab.DictionaryFluentValidation.Fields;

namespace Lab.DictionaryFluentValidation.Validators;

public class EmailFieldValidator : AbstractValidator<object>
{
    /// <summary>
    /// return true 繼續往下驗證
    /// https://docs.fluentvalidation.net/en/latest/advanced.html?highlight=PreValidate#prevalidate
    /// </summary>
    /// <param name="context"></param>
    /// <param name="result"></param>
    /// <returns></returns>
    protected override bool PreValidate(ValidationContext<object> context, ValidationResult result)
    {
        var isValid = true;
        var instance = context.InstanceToValidate;
        if (instance == null)
        {
            return isValid;
        }

        this.RuleFor(p => p.ToString())
            .NotEmpty()
            .WithName(ProfileFieldNames.ContactEmail)
            .EmailAddress();
            ;
        return isValid;
    }
}