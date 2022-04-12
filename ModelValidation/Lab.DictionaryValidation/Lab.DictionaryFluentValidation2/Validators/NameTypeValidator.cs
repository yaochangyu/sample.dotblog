using FluentValidation;
using FluentValidation.Results;
using Lab.DictionaryFluentValidation2.Fields;

namespace Lab.DictionaryFluentValidation2.Validators;

public class NameTypeValidator : AbstractValidator<object>
{
    private readonly string _propertyName;

    public NameTypeValidator(string propertyName)
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

        var propertyInfos = instance.GetType().GetProperties();
        foreach (var propertyInfo in propertyInfos)
        {
            var value = propertyInfo.GetValue(instance);
            if (value == null)
            {
                continue;
            }

            var propertyName = $"{this._propertyName}.{propertyInfo.Name}";
            switch (propertyInfo.Name)
            {
                case NameTypeNames.FirstName:
                    break;
                case NameTypeNames.LastName:
                    break;
                case NameTypeNames.FullName:
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