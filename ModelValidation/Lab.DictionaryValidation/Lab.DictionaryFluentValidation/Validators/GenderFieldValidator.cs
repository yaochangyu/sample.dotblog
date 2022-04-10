using FluentValidation;
using FluentValidation.Results;
using Lab.DictionaryFluentValidation.Fields;

namespace Lab.DictionaryFluentValidation.Validators;

public class GenderFieldValidator : AbstractValidator<object>
{
    private const string ErrorCode = nameof(GenderFieldValidator);

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

        var srcValues = GenderFieldValues.GetFieldValues();
        var destValue = instance.ToString();
        if (srcValues.ContainsKey(destValue) == false)
        {
            var validationFailure = new ValidationFailure("gender",
                                                $"'{destValue}' is invalid value.")
            {
                ErrorCode = ErrorCode
            };
            context.AddFailure(validationFailure);
        }

        return isValid;
    }
}