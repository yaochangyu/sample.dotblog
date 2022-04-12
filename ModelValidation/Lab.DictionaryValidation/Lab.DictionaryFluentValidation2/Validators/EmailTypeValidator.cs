using FluentValidation;
using FluentValidation.Results;

namespace Lab.DictionaryFluentValidation2.Validators;

public class EmailTypeValidator : AbstractValidator<object>
{
    private readonly string _propertyName;

    public EmailTypeValidator(string propertyName)
    {
        this._propertyName = propertyName;
    }

    /// <summary>
    ///     return true 繼續往下驗證
    ///     https://docs.fluentvalidation.net/en/latest/advanced.html?highlight=PreValidate#prevalidate
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
            .EmailAddress()
            .WithName(this._propertyName)
            .WithErrorCode(nameof(EmailTypeValidator))
            ;
        return isValid;
    }
}