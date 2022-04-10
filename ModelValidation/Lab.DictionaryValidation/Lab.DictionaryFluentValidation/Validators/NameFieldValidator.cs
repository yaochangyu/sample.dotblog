using FluentValidation;
using FluentValidation.Results;
using Lab.DictionaryFluentValidation.Fields;

namespace Lab.DictionaryFluentValidation.Validators;

public class NameFieldValidator : AbstractValidator<object>
{
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

        var propertyInfos = instance.GetType().GetProperties();
        foreach (var propertyInfo in propertyInfos)
        {
            var value = propertyInfo.GetValue(instance);
            if (value == null)
            {
                return isValid;
            }

            var propertyName = $"name.{propertyInfo.Name}";
            switch (propertyInfo.Name)
            {
                case NameFieldNames.FirstName:
                    break;
                case NameFieldNames.LastName:
                    break;
                case NameFieldNames.FullName:
                    this.RuleFor(p => value)
                        .NotEmpty()
                        .WithName(propertyName)
                        .OverridePropertyName(propertyName)
                        ;

                    break;
            }
        }

        return isValid;
    }
}